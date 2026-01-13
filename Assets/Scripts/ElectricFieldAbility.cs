using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Creates an electric field around body parts that periodically shocks nearby enemies.
/// The field pulses at intervals, dealing damage to all enemies within range of any body part.
/// </summary>
public class ElectricFieldAbility : BaseAbility
{
    [Header("Electric Field Settings")]
    [SerializeField] private float baseRadius = 2f;
    [SerializeField] private float baseDamage = 8f;
    [SerializeField] private float baseShockInterval = 1f;
    
    [Header("Visual Settings")]
    [SerializeField] private GameObject electricFieldVisualPrefab;
    [SerializeField] private Color electricColor = new Color(0.3f, 0.7f, 1f, 0.5f); // Electric blue
    [SerializeField] private bool showDebugGizmos = true;
    
    [Header("Audio")]
    [SerializeField] private AudioClip shockSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("References")]
    [SerializeField] private SnakeBody snakeBody;
    
    // Custom stat names for upgrade data
    private const string STAT_RADIUS = "radius";
    
    private List<GameObject> fieldVisuals = new List<GameObject>();
    private float shockTimer = 0f;
    private bool hasInitialized = false;
    private HashSet<AppleEnemy> shockedEnemies = new HashSet<AppleEnemy>();

    private void OnEnable()
    {
        SnakeBody.OnBodyPartsInitialized += InitializeFields;
    }

    private void OnDisable()
    {
        SnakeBody.OnBodyPartsInitialized -= InitializeFields;
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
            InitializeFields();
        }
    }

    protected override void Update()
    {
        base.Update();
        
        if (!isActive || isFrozen) return;
        
        // Update shock timer
        shockTimer += Time.deltaTime;
        float currentInterval = GetShockInterval();
        
        if (shockTimer >= currentInterval)
        {
            ShockNearbyEnemies();
            shockTimer = 0f;
        }
        
        // Update visual positions (they follow body parts automatically via parenting)
        UpdateVisualEffects();
    }

    private void InitializeFields()
    {
        if (hasInitialized) return;
        
        if (snakeBody == null)
        {
            snakeBody = GetComponentInParent<SnakeBody>();
            if (snakeBody == null)
            {
                Debug.LogWarning("ElectricFieldAbility: SnakeBody not found!");
                return;
            }
        }

        CreateFieldVisuals();
        hasInitialized = true;
        Debug.Log($"ElectricFieldAbility: Initialized at level {currentLevel} with radius {GetFieldRadius()}");
    }

    private void CreateFieldVisuals()
    {
        ClearFieldVisuals();
        
        if (snakeBody == null || snakeBody.bodyParts == null) return;
        
        List<BodyPart> bodyParts = snakeBody.bodyParts;
        int totalParts = bodyParts.Count;
        
        // Create visual on every 3rd body part based on level
        int spacing = Mathf.Max(2, 5 - currentLevel);
        
        for (int i = 0; i < totalParts; i += spacing)
        {
            BodyPart part = bodyParts[i];
            
            GameObject fieldVisual;
            if (electricFieldVisualPrefab != null)
            {
                fieldVisual = Instantiate(electricFieldVisualPrefab, part.transform.position, Quaternion.identity);
            }
            else
            {
                fieldVisual = CreateDefaultFieldVisual();
            }
            
            fieldVisual.transform.SetParent(part.transform);
            fieldVisual.transform.localPosition = Vector3.zero;
            
            fieldVisuals.Add(fieldVisual);
        }
    }

    private GameObject CreateDefaultFieldVisual()
    {
        GameObject field = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        field.name = "ElectricFieldVisual";
        
        // Remove collider
        Collider col = field.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        // Set up transparent material
        Renderer renderer = field.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = electricColor;
            renderer.material = mat;
        }
        
        // Scale based on radius
        float radius = GetFieldRadius();
        field.transform.localScale = Vector3.one * radius * 2f;
        
        return field;
    }

    private void UpdateVisualEffects()
    {
        float radius = GetFieldRadius();
        float pulseScale = 1f + Mathf.Sin(Time.time * 5f) * 0.1f; // Subtle pulsing effect
        
        foreach (GameObject visual in fieldVisuals)
        {
            if (visual != null)
            {
                visual.transform.localScale = Vector3.one * radius * 2f * pulseScale;
            }
        }
    }

    private void ShockNearbyEnemies()
    {
        if (snakeBody == null || snakeBody.bodyParts == null) return;
        
        float radius = GetFieldRadius();
        float radiusSqr = radius * radius;
        float damage = GetShockDamage();
        
        shockedEnemies.Clear();
        
        // Use AppleEnemy's static list instead of FindObjectsByType for better performance
        var allEnemies = AppleEnemy.GetAllActiveEnemies();
        
        foreach (BodyPart part in snakeBody.bodyParts)
        {
            if (part == null) continue;
            
            Vector3 partPos = part.transform.position;
            
            foreach (AppleEnemy enemy in allEnemies)
            {
                if (enemy == null || enemy.IsFrozen()) continue;
                if (shockedEnemies.Contains(enemy)) continue; // Don't shock same enemy twice per pulse
                
                float distanceSqr = (enemy.transform.position - partPos).sqrMagnitude;
                
                if (distanceSqr <= radiusSqr)
                {
                    enemy.TakeDamage(damage);
                    shockedEnemies.Add(enemy);
                }
            }
        }
        
        // Play shock sound if we hit any enemies
        if (shockedEnemies.Count > 0 && shockSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shockSound, 0.5f);
        }
        
        if (shockedEnemies.Count > 0)
        {
            Debug.Log($"ElectricFieldAbility: Shocked {shockedEnemies.Count} enemies for {damage} damage!");
        }
    }

    private float GetFieldRadius()
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

    private float GetShockDamage()
    {
        float damage;
        if (upgradeData != null)
        {
            damage = GetDamage();
        }
        else
        {
            damage = baseDamage + (currentLevel - 1) * 3f;
        }
        
        float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetDamageMultiplier() : 1f;
        return damage * multiplier;
    }

    private float GetShockInterval()
    {
        if (upgradeData != null)
        {
            float cooldown = GetCooldown();
            return cooldown > 0 ? cooldown : baseShockInterval;
        }
        return Mathf.Max(0.3f, baseShockInterval - (currentLevel - 1) * 0.15f);
    }

    protected override void OnLevelUp()
    {
        base.OnLevelUp();
        
        // Recreate visuals with new settings
        CreateFieldVisuals();
        
        Debug.Log($"ElectricFieldAbility: Level {currentLevel} - Radius: {GetFieldRadius():F1}, Damage: {GetShockDamage():F0}, Interval: {GetShockInterval():F2}s");
    }

    private void ClearFieldVisuals()
    {
        foreach (GameObject visual in fieldVisuals)
        {
            if (visual != null)
            {
                Destroy(visual);
            }
        }
        fieldVisuals.Clear();
    }

    protected override void DeactivateAbility()
    {
        base.DeactivateAbility();
        ClearFieldVisuals();
    }

    private void OnDestroy()
    {
        ClearFieldVisuals();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        if (snakeBody != null && snakeBody.bodyParts != null)
        {
            float radius = Application.isPlaying ? GetFieldRadius() : baseRadius;
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.3f);
            
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