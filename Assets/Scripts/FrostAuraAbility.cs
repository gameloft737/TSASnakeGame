using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

/// <summary>
/// Creates a frost aura around body parts that slows and damages nearby enemies.
/// Enemies within range are slowed and take cold damage over time.
/// </summary>
public class FrostAuraAbility : BaseAbility
{
    [Header("Frost Aura Settings")]
    [SerializeField] private float baseRadius = 2.5f;
    [SerializeField] private float baseDamage = 5f;
    [SerializeField] private float baseDamageInterval = 0.5f;
    [SerializeField] private float baseSlowPercent = 0.4f; // 40% slow
    
    [Header("Visual Settings")]
    [SerializeField] private GameObject frostAuraVisualPrefab;
    [SerializeField] private Color frostColor = new Color(0.5f, 0.8f, 1f, 0.4f); // Ice blue
    [SerializeField] private bool showDebugGizmos = true;
    
    [Header("Audio")]
    [SerializeField] private AudioClip frostSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("References")]
    [SerializeField] private SnakeBody snakeBody;
    
    // Custom stat names for upgrade data
    private const string STAT_RADIUS = "radius";
    private const string STAT_SLOW_PERCENT = "slowPercent";
    
    private List<GameObject> auraVisuals = new List<GameObject>();
    private float damageTimer = 0f;
    private bool hasInitialized = false;
    private Dictionary<AppleEnemy, FrostSlowData> frostedEnemies = new Dictionary<AppleEnemy, FrostSlowData>();
    private List<AppleEnemy> enemiesToRemove = new List<AppleEnemy>();

    private class FrostSlowData
    {
        public float originalSpeed;
        public bool isSlowed;
        public NavMeshAgent agent;
    }

    private void OnEnable()
    {
        SnakeBody.OnBodyPartsInitialized += InitializeAura;
    }

    private void OnDisable()
    {
        SnakeBody.OnBodyPartsInitialized -= InitializeAura;
        
        // Restore all slowed enemies
        RestoreAllEnemySpeeds();
    }

    protected override void Awake()
    {
        base.Awake();
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }
        }
    }

    private void Start()
    {
        if (snakeBody == null)
        {
            snakeBody = GetComponentInParent<SnakeBody>();
        }
        
        if (snakeBody != null && snakeBody.bodyParts != null && snakeBody.bodyParts.Count > 0)
        {
            InitializeAura();
        }
    }

    protected override void Update()
    {
        base.Update();
        
        if (!isActive || isFrozen) return;
        
        // Update damage timer
        damageTimer += Time.deltaTime;
        float currentInterval = GetDamageInterval();
        
        if (damageTimer >= currentInterval)
        {
            ApplyFrostEffects();
            damageTimer = 0f;
        }
        
        // Update visual effects
        UpdateVisualEffects();
        
        // Clean up enemies that left the aura
        CleanupFrostedEnemies();
    }

    private void InitializeAura()
    {
        if (hasInitialized) return;
        
        if (snakeBody == null)
        {
            snakeBody = GetComponentInParent<SnakeBody>();
            if (snakeBody == null)
            {
                Debug.LogWarning("FrostAuraAbility: SnakeBody not found!");
                return;
            }
        }

        CreateAuraVisuals();
        hasInitialized = true;
        Debug.Log($"FrostAuraAbility: Initialized at level {currentLevel} with radius {GetAuraRadius()}");
    }

    private void CreateAuraVisuals()
    {
        ClearAuraVisuals();
        
        if (snakeBody == null || snakeBody.bodyParts == null) return;
        
        List<BodyPart> bodyParts = snakeBody.bodyParts;
        int totalParts = bodyParts.Count;
        
        // Create visual on every few body parts based on level
        int spacing = Mathf.Max(2, 5 - currentLevel);
        
        for (int i = 0; i < totalParts; i += spacing)
        {
            BodyPart part = bodyParts[i];
            
            GameObject auraVisual;
            if (frostAuraVisualPrefab != null)
            {
                auraVisual = Instantiate(frostAuraVisualPrefab, part.transform.position, Quaternion.identity);
            }
            else
            {
                auraVisual = CreateDefaultAuraVisual();
            }
            
            auraVisual.transform.SetParent(part.transform);
            auraVisual.transform.localPosition = Vector3.zero;
            
            auraVisuals.Add(auraVisual);
        }
    }

    private GameObject CreateDefaultAuraVisual()
    {
        GameObject aura = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        aura.name = "FrostAuraVisual";
        
        // Remove collider
        Collider col = aura.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        // Set up transparent material
        Renderer renderer = aura.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = frostColor;
            renderer.material = mat;
        }
        
        // Scale based on radius
        float radius = GetAuraRadius();
        aura.transform.localScale = Vector3.one * radius * 2f;
        
        return aura;
    }

    private void UpdateVisualEffects()
    {
        float radius = GetAuraRadius();
        // Subtle pulsing effect with frost shimmer
        float pulseScale = 1f + Mathf.Sin(Time.time * 3f) * 0.05f;
        
        foreach (GameObject visual in auraVisuals)
        {
            if (visual != null)
            {
                visual.transform.localScale = Vector3.one * radius * 2f * pulseScale;
                
                // Rotate slowly for visual effect
                visual.transform.Rotate(0, Time.deltaTime * 20f, 0);
            }
        }
    }

    private void ApplyFrostEffects()
    {
        if (snakeBody == null || snakeBody.bodyParts == null) return;
        
        float radius = GetAuraRadius();
        float radiusSqr = radius * radius;
        float damage = GetFrostDamage();
        float slowPercent = GetSlowPercent();
        
        HashSet<AppleEnemy> enemiesInRange = new HashSet<AppleEnemy>();
        AppleEnemy[] allEnemies = FindObjectsByType<AppleEnemy>(FindObjectsSortMode.None);
        
        foreach (BodyPart part in snakeBody.bodyParts)
        {
            if (part == null) continue;
            
            Vector3 partPos = part.transform.position;
            
            foreach (AppleEnemy enemy in allEnemies)
            {
                if (enemy == null || enemy.IsFrozen()) continue;
                
                float distanceSqr = (enemy.transform.position - partPos).sqrMagnitude;
                
                if (distanceSqr <= radiusSqr)
                {
                    enemiesInRange.Add(enemy);
                    
                    // Deal damage
                    enemy.TakeDamage(damage);
                    
                    // Apply slow if not already slowed
                    ApplySlowToEnemy(enemy, slowPercent);
                }
            }
        }
        
        // Mark enemies that left the aura for speed restoration
        enemiesToRemove.Clear();
        foreach (var kvp in frostedEnemies)
        {
            if (!enemiesInRange.Contains(kvp.Key))
            {
                enemiesToRemove.Add(kvp.Key);
            }
        }
        
        // Restore speed for enemies that left
        foreach (AppleEnemy enemy in enemiesToRemove)
        {
            RestoreEnemySpeed(enemy);
        }
        
        // Play frost sound if we affected any enemies
        if (enemiesInRange.Count > 0 && frostSound != null && audioSource != null)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.PlayOneShot(frostSound, 0.3f);
            }
        }
    }

    private void ApplySlowToEnemy(AppleEnemy enemy, float slowPercent)
    {
        if (frostedEnemies.ContainsKey(enemy)) return; // Already slowed
        
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        if (agent == null) return;
        
        FrostSlowData data = new FrostSlowData
        {
            originalSpeed = agent.speed,
            isSlowed = true,
            agent = agent
        };
        
        // Apply slow
        agent.speed = data.originalSpeed * (1f - slowPercent);
        frostedEnemies[enemy] = data;
        
        Debug.Log($"FrostAuraAbility: Slowed {enemy.name} from {data.originalSpeed} to {agent.speed}");
    }

    private void RestoreEnemySpeed(AppleEnemy enemy)
    {
        if (!frostedEnemies.ContainsKey(enemy)) return;
        
        FrostSlowData data = frostedEnemies[enemy];
        
        if (data.agent != null && data.isSlowed)
        {
            data.agent.speed = data.originalSpeed;
            Debug.Log($"FrostAuraAbility: Restored {enemy.name} speed to {data.originalSpeed}");
        }
        
        frostedEnemies.Remove(enemy);
    }

    private void RestoreAllEnemySpeeds()
    {
        foreach (var kvp in frostedEnemies)
        {
            if (kvp.Key != null && kvp.Value.agent != null && kvp.Value.isSlowed)
            {
                kvp.Value.agent.speed = kvp.Value.originalSpeed;
            }
        }
        frostedEnemies.Clear();
    }

    private void CleanupFrostedEnemies()
    {
        enemiesToRemove.Clear();
        
        foreach (var kvp in frostedEnemies)
        {
            if (kvp.Key == null)
            {
                enemiesToRemove.Add(kvp.Key);
            }
        }
        
        foreach (AppleEnemy enemy in enemiesToRemove)
        {
            frostedEnemies.Remove(enemy);
        }
    }

    private float GetAuraRadius()
    {
        float radius;
        if (upgradeData != null)
        {
            radius = GetCustomStat(STAT_RADIUS, baseRadius);
        }
        else
        {
            radius = baseRadius + (currentLevel - 1) * 0.5f;
        }
        
        float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetRangeMultiplier() : 1f;
        return radius * multiplier;
    }

    private float GetFrostDamage()
    {
        float damage;
        if (upgradeData != null)
        {
            damage = GetDamage();
        }
        else
        {
            damage = baseDamage + (currentLevel - 1) * 2f;
        }
        
        float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetDamageMultiplier() : 1f;
        return damage * multiplier;
    }

    private float GetDamageInterval()
    {
        if (upgradeData != null)
        {
            float cooldown = GetCooldown();
            return cooldown > 0 ? cooldown : baseDamageInterval;
        }
        return Mathf.Max(0.2f, baseDamageInterval - (currentLevel - 1) * 0.05f);
    }

    private float GetSlowPercent()
    {
        if (upgradeData != null)
        {
            return GetCustomStat(STAT_SLOW_PERCENT, baseSlowPercent);
        }
        return Mathf.Min(0.8f, baseSlowPercent + (currentLevel - 1) * 0.1f); // Cap at 80% slow
    }

    protected override void OnLevelUp()
    {
        base.OnLevelUp();
        
        // Recreate visuals with new settings
        CreateAuraVisuals();
        
        Debug.Log($"FrostAuraAbility: Level {currentLevel} - Radius: {GetAuraRadius():F1}, Damage: {GetFrostDamage():F0}, Slow: {GetSlowPercent() * 100:F0}%");
    }

    private void ClearAuraVisuals()
    {
        foreach (GameObject visual in auraVisuals)
        {
            if (visual != null)
            {
                Destroy(visual);
            }
        }
        auraVisuals.Clear();
    }

    protected override void DeactivateAbility()
    {
        base.DeactivateAbility();
        ClearAuraVisuals();
        RestoreAllEnemySpeeds();
    }

    private void OnDestroy()
    {
        ClearAuraVisuals();
        RestoreAllEnemySpeeds();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        if (snakeBody != null && snakeBody.bodyParts != null)
        {
            float radius = Application.isPlaying ? GetAuraRadius() : baseRadius;
            Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);
            
            foreach (BodyPart part in snakeBody.bodyParts)
            {
                if (part != null)
                {
                    Gizmos.DrawWireSphere(part.transform.position, radius);
                }
            }
        }
    }
}