using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Passive ability that reflects damage back to enemies when they bite the snake's body.
/// When an enemy deals damage to the snake, they receive a percentage of that damage back.
/// </summary>
public class ThornsAbility : BaseAbility
{
    [Header("Thorns Settings")]
    [SerializeField] private float baseReflectPercent = 0.3f; // 30% damage reflection
    [SerializeField] private float baseReflectRadius = 2f; // Radius to find the attacking enemy
    [SerializeField] private float baseBonusDamage = 5f; // Flat bonus damage on top of reflection
    
    [Header("Visual Settings")]
    [SerializeField] private GameObject thornsVisualPrefab;
    [SerializeField] private Color thornsColor = new Color(0.6f, 0.2f, 0.8f, 0.7f); // Purple
    [SerializeField] private GameObject reflectEffectPrefab;
    
    [Header("Audio")]
    [SerializeField] private AudioClip reflectSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("References")]
    [SerializeField] private SnakeBody snakeBody;
    [SerializeField] private SnakeHealth snakeHealth;
    
    // Custom stat names for upgrade data
    private const string STAT_REFLECT_PERCENT = "reflectPercent";
    private const string STAT_BONUS_DAMAGE = "bonusDamage";
    
    private List<GameObject> thornsVisuals = new List<GameObject>();
    private bool hasInitialized = false;
    private float lastDamageTime = 0f;
    private float damageCooldown = 0.1f; // Prevent multiple reflections from same damage instance
    private float previousHealth = 0f;

    private void OnEnable()
    {
        SnakeBody.OnBodyPartsInitialized += InitializeThorns;
    }

    private void OnDisable()
    {
        SnakeBody.OnBodyPartsInitialized -= InitializeThorns;
        
        // Unsubscribe from health events
        if (snakeHealth != null)
        {
            snakeHealth.onHealthChanged.RemoveListener(OnHealthChanged);
        }
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
        
        if (snakeHealth == null)
        {
            snakeHealth = GetComponentInParent<SnakeHealth>();
            if (snakeHealth == null)
            {
                snakeHealth = FindFirstObjectByType<SnakeHealth>();
            }
        }
        
        // Subscribe to damage events
        if (snakeHealth != null)
        {
            snakeHealth.onHealthChanged.AddListener(OnHealthChanged);
            previousHealth = snakeHealth.GetCurrentHealth();
        }
        
        if (snakeBody != null && snakeBody.bodyParts != null && snakeBody.bodyParts.Count > 0)
        {
            InitializeThorns();
        }
    }

    private void InitializeThorns()
    {
        if (hasInitialized) return;
        
        if (snakeBody == null)
        {
            snakeBody = GetComponentInParent<SnakeBody>();
            if (snakeBody == null)
            {
                Debug.LogWarning("ThornsAbility: SnakeBody not found!");
                return;
            }
        }

        CreateThornsVisuals();
        hasInitialized = true;
        Debug.Log($"ThornsAbility: Initialized at level {currentLevel} with {GetReflectPercent() * 100:F0}% reflection");
    }

    private void CreateThornsVisuals()
    {
        ClearThornsVisuals();
        
        if (snakeBody == null || snakeBody.bodyParts == null) return;
        
        List<BodyPart> bodyParts = snakeBody.bodyParts;
        int totalParts = bodyParts.Count;
        
        // Create visual on every few body parts based on level
        int spacing = Mathf.Max(2, 6 - currentLevel);
        
        for (int i = 0; i < totalParts; i += spacing)
        {
            BodyPart part = bodyParts[i];
            
            GameObject thornsVisual;
            if (thornsVisualPrefab != null)
            {
                thornsVisual = Instantiate(thornsVisualPrefab, part.transform.position, Quaternion.identity);
            }
            else
            {
                thornsVisual = CreateDefaultThornsVisual();
            }
            
            thornsVisual.transform.SetParent(part.transform);
            thornsVisual.transform.localPosition = Vector3.zero;
            
            thornsVisuals.Add(thornsVisual);
        }
    }

    private GameObject CreateDefaultThornsVisual()
    {
        GameObject thorns = new GameObject("ThornsVisual");
        
        // Create small thorn spikes pointing outward
        int thornCount = 6;
        for (int i = 0; i < thornCount; i++)
        {
            GameObject thorn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            thorn.name = $"Thorn_{i}";
            thorn.transform.SetParent(thorns.transform);
            
            // Scale to look like a small thorn
            thorn.transform.localScale = new Vector3(0.05f, 0.15f, 0.05f);
            
            // Position around the body part
            float angle = i * (360f / thornCount) * Mathf.Deg2Rad;
            thorn.transform.localPosition = new Vector3(
                Mathf.Cos(angle) * 0.35f,
                0,
                Mathf.Sin(angle) * 0.35f
            );
            
            // Rotate to point outward
            thorn.transform.LookAt(thorns.transform.position + thorn.transform.localPosition * 2);
            thorn.transform.Rotate(90, 0, 0);
            
            // Set up material
            Renderer renderer = thorn.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = thornsColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", thornsColor * 0.3f);
                renderer.material = mat;
            }
            
            // Remove default collider
            Collider col = thorn.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
        }
        
        return thorns;
    }

    /// <summary>
    /// Called when the snake's health changes (takes damage)
    /// </summary>
    private void OnHealthChanged(float currentHealth, float maxHealth)
    {
        // Check if health decreased (damage was taken)
        if (currentHealth < previousHealth)
        {
            // Check if we're not on cooldown
            if (Time.time - lastDamageTime >= damageCooldown)
            {
                // Calculate actual damage taken
                float damageTaken = previousHealth - currentHealth;
                
                // Find nearby enemies and reflect damage
                ReflectDamageToNearbyEnemies(damageTaken);
                lastDamageTime = Time.time;
            }
        }
        
        previousHealth = currentHealth;
    }

    /// <summary>
    /// Reflects damage to all enemies currently in contact with body parts
    /// </summary>
    private void ReflectDamageToNearbyEnemies(float damageTaken)
    {
        if (snakeBody == null || snakeBody.bodyParts == null) return;
        
        float radius = GetReflectRadius();
        float radiusSqr = radius * radius;
        float reflectPercent = GetReflectPercent();
        float bonusDamage = GetBonusDamage();
        
        HashSet<AppleEnemy> damagedEnemies = new HashSet<AppleEnemy>();
        // Use AppleEnemy's static list instead of FindObjectsByType for better performance
        var allEnemies = AppleEnemy.GetAllActiveEnemies();
        
        foreach (BodyPart part in snakeBody.bodyParts)
        {
            if (part == null) continue;
            
            Vector3 partPos = part.transform.position;
            
            foreach (AppleEnemy enemy in allEnemies)
            {
                if (enemy == null || enemy.IsFrozen()) continue;
                if (damagedEnemies.Contains(enemy)) continue;
                
                float distanceSqr = (enemy.transform.position - partPos).sqrMagnitude;
                
                if (distanceSqr <= radiusSqr)
                {
                    // Calculate reflected damage based on actual damage taken
                    float reflectedDamage = (damageTaken * reflectPercent) + bonusDamage;
                    
                    enemy.TakeDamage(reflectedDamage);
                    damagedEnemies.Add(enemy);
                    
                    // Spawn reflect effect
                    if (reflectEffectPrefab != null)
                    {
                        Instantiate(reflectEffectPrefab, enemy.transform.position, Quaternion.identity);
                    }
                }
            }
        }
        
        // Play sound if we reflected damage
        if (damagedEnemies.Count > 0)
        {
            if (reflectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(reflectSound, 0.5f);
            }
            
            Debug.Log($"ThornsAbility: Reflected {(damageTaken * reflectPercent) + bonusDamage:F1} damage to {damagedEnemies.Count} enemies!");
        }
    }

    private float GetReflectPercent()
    {
        if (upgradeData != null)
        {
            return GetCustomStat(STAT_REFLECT_PERCENT, baseReflectPercent);
        }
        return baseReflectPercent + (currentLevel - 1) * 0.15f; // +15% per level
    }

    private float GetReflectRadius()
    {
        return baseReflectRadius + (currentLevel - 1) * 0.3f;
    }

    private float GetBonusDamage()
    {
        float damage;
        if (upgradeData != null)
        {
            damage = GetCustomStat(STAT_BONUS_DAMAGE, baseBonusDamage);
        }
        else
        {
            damage = baseBonusDamage + (currentLevel - 1) * 3f;
        }
        
        float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetDamageMultiplier() : 1f;
        return damage * multiplier;
    }

    protected override void OnLevelUp()
    {
        base.OnLevelUp();
        
        // Recreate visuals with new settings
        CreateThornsVisuals();
        
        Debug.Log($"ThornsAbility: Level {currentLevel} - Reflect: {GetReflectPercent() * 100:F0}%, Bonus Damage: {GetBonusDamage():F0}");
    }

    private void ClearThornsVisuals()
    {
        foreach (GameObject visual in thornsVisuals)
        {
            if (visual != null)
            {
                Destroy(visual);
            }
        }
        thornsVisuals.Clear();
    }

    protected override void DeactivateAbility()
    {
        base.DeactivateAbility();
        ClearThornsVisuals();
        
        if (snakeHealth != null)
        {
            snakeHealth.onHealthChanged.RemoveListener(OnHealthChanged);
        }
    }

    private void OnDestroy()
    {
        ClearThornsVisuals();
        
        if (snakeHealth != null)
        {
            snakeHealth.onHealthChanged.RemoveListener(OnHealthChanged);
        }
    }
}