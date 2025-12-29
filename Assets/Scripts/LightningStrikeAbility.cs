using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Active ability that strikes nearby enemies randomly with lightning.
/// Upgrades increase the number of strikes and how often it strikes.
/// Uses AbilityUpgradeData for level-based stats.
/// </summary>
public class LightningStrikeAbility : BaseAbility
{
    [Header("Lightning Settings (Fallback if no UpgradeData)")]
    [SerializeField] private float strikeInterval = 2f; // Time between strike volleys
    [SerializeField] private int strikeCount = 1; // Number of enemies to strike per volley
    [SerializeField] private float strikeDamage = 25f; // Damage per strike
    [SerializeField] private float strikeRange = 15f; // Range to find enemies
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject lightningEffectPrefab; // Optional lightning VFX prefab
    [SerializeField] private float effectDuration = 0.3f; // How long the lightning effect lasts
    [SerializeField] private Color lightningColor = new Color(0.5f, 0.8f, 1f, 1f); // Light blue
    
    [Header("Audio")]
    [SerializeField] private AudioClip strikeSound;
    [SerializeField] private AudioSource audioSource;
    
    // Custom stat names for upgrade data
    private const string STAT_STRIKE_COUNT = "strikeCount";
    private const string STAT_STRIKE_RANGE = "strikeRange";
    
    private float strikeTimer = 0f;
    private Transform playerTransform;
    private List<AppleEnemy> nearbyEnemies = new List<AppleEnemy>();
    
    protected override void Awake()
    {
        base.Awake();
        
        // Find player transform
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        // Get or add audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound
            }
        }
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (!isActive || isFrozen) return;
        
        strikeTimer += Time.deltaTime;
        
        float currentInterval = GetStrikeInterval();
        
        if (strikeTimer >= currentInterval)
        {
            PerformLightningStrikes();
            strikeTimer = 0f;
        }
    }
    
    /// <summary>
    /// Applies custom stats from the upgrade data
    /// </summary>
    protected override void ApplyCustomStats(AbilityLevelStats stats)
    {
        // Get strike-specific stats from upgrade data
        strikeCount = Mathf.RoundToInt(GetCustomStat(STAT_STRIKE_COUNT, strikeCount));
        strikeRange = GetCustomStat(STAT_STRIKE_RANGE, strikeRange);
        
        // Use damage from upgrade data
        strikeDamage = GetDamage();
        
        // Use cooldown as strike interval
        strikeInterval = GetCooldown();
    }
    
    /// <summary>
    /// Gets the current strike interval based on level
    /// </summary>
    private float GetStrikeInterval()
    {
        if (upgradeData != null)
        {
            return GetCooldown();
        }
        return strikeInterval;
    }
    
    /// <summary>
    /// Gets the current strike count based on level
    /// </summary>
    private int GetStrikeCount()
    {
        if (upgradeData != null)
        {
            return Mathf.RoundToInt(GetCustomStat(STAT_STRIKE_COUNT, strikeCount));
        }
        return strikeCount;
    }
    
    /// <summary>
    /// Gets the current strike damage based on level
    /// </summary>
    private float GetStrikeDamage()
    {
        if (upgradeData != null)
        {
            float baseDamage = GetDamage();
            // Apply damage multiplier from PlayerStats
            float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetDamageMultiplier() : 1f;
            return baseDamage * multiplier;
        }
        
        float fallbackMultiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetDamageMultiplier() : 1f;
        return strikeDamage * fallbackMultiplier;
    }
    
    /// <summary>
    /// Gets the current strike range based on level
    /// </summary>
    private float GetStrikeRange()
    {
        if (upgradeData != null)
        {
            float baseRange = GetCustomStat(STAT_STRIKE_RANGE, strikeRange);
            // Apply range multiplier from PlayerStats
            float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetRangeMultiplier() : 1f;
            return baseRange * multiplier;
        }
        
        float fallbackMultiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetRangeMultiplier() : 1f;
        return strikeRange * fallbackMultiplier;
    }
    
    /// <summary>
    /// Performs lightning strikes on random nearby enemies
    /// </summary>
    private void PerformLightningStrikes()
    {
        if (playerTransform == null) return;
        
        // Find all nearby enemies
        FindNearbyEnemies();
        
        if (nearbyEnemies.Count == 0) return;
        
        int currentStrikeCount = GetStrikeCount();
        float currentDamage = GetStrikeDamage();
        
        // Shuffle the list to randomize targets
        ShuffleList(nearbyEnemies);
        
        // Strike up to strikeCount enemies
        int strikesToPerform = Mathf.Min(currentStrikeCount, nearbyEnemies.Count);
        
        for (int i = 0; i < strikesToPerform; i++)
        {
            AppleEnemy target = nearbyEnemies[i];
            if (target != null)
            {
                StrikeEnemy(target, currentDamage);
            }
        }
        
        // Play strike sound
        if (strikeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(strikeSound);
        }
    }
    
    /// <summary>
    /// Finds all enemies within strike range
    /// </summary>
    private void FindNearbyEnemies()
    {
        nearbyEnemies.Clear();
        
        float currentRange = GetStrikeRange();
        float rangeSqr = currentRange * currentRange;
        
        // Find all AppleEnemy objects
        AppleEnemy[] allEnemies = FindObjectsByType<AppleEnemy>(FindObjectsSortMode.None);
        
        foreach (AppleEnemy enemy in allEnemies)
        {
            if (enemy == null || enemy.IsFrozen()) continue;
            
            float distanceSqr = (enemy.transform.position - playerTransform.position).sqrMagnitude;
            if (distanceSqr <= rangeSqr)
            {
                nearbyEnemies.Add(enemy);
            }
        }
    }
    
    /// <summary>
    /// Strikes a single enemy with lightning
    /// </summary>
    private void StrikeEnemy(AppleEnemy enemy, float damage)
    {
        // Deal damage
        enemy.TakeDamage(damage);
        
        // Spawn visual effect
        if (lightningEffectPrefab != null)
        {
            GameObject effect = Instantiate(lightningEffectPrefab, enemy.transform.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
        else
        {
            // Create a simple visual effect if no prefab is assigned
            CreateSimpleLightningEffect(enemy.transform.position);
        }
        
        Debug.Log($"Lightning struck {enemy.name} for {damage} damage!");
    }
    
    /// <summary>
    /// Creates a simple lightning effect using a line renderer
    /// </summary>
    private void CreateSimpleLightningEffect(Vector3 targetPosition)
    {
        // Create a temporary game object for the effect
        GameObject effectObj = new GameObject("LightningEffect");
        effectObj.transform.position = playerTransform.position + Vector3.up * 10f; // Start from above
        
        LineRenderer line = effectObj.AddComponent<LineRenderer>();
        line.positionCount = 5;
        line.startWidth = 0.2f;
        line.endWidth = 0.1f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = lightningColor;
        line.endColor = Color.white;
        
        // Create jagged lightning path
        Vector3 start = playerTransform.position + Vector3.up * 10f;
        Vector3 end = targetPosition;
        
        line.SetPosition(0, start);
        
        for (int i = 1; i < 4; i++)
        {
            float t = i / 4f;
            Vector3 point = Vector3.Lerp(start, end, t);
            point += new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-0.5f, 0.5f),
                Random.Range(-1f, 1f)
            );
            line.SetPosition(i, point);
        }
        
        line.SetPosition(4, end);
        
        // Destroy after effect duration
        Destroy(effectObj, effectDuration);
    }
    
    /// <summary>
    /// Shuffles a list using Fisher-Yates algorithm
    /// </summary>
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
    
    protected override void OnLevelUp()
    {
        base.OnLevelUp();
        Debug.Log($"Lightning Strike upgraded! Strikes: {GetStrikeCount()}, Interval: {GetStrikeInterval():F1}s, Damage: {GetStrikeDamage():F0}");
    }
    
    protected override void OnEvolutionUnlocked()
    {
        base.OnEvolutionUnlocked();
        Debug.Log($"Lightning Strike evolved to level {currentLevel}!");
    }
    
    /// <summary>
    /// Debug visualization
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null)
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        if (playerTransform != null)
        {
            Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);
            Gizmos.DrawWireSphere(playerTransform.position, GetStrikeRange());
        }
    }
}