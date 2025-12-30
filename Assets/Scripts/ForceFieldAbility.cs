using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Active ability that creates a force field around the snake's head.
/// Enemies that enter the force field take damage over time (like Garlic in Vampire Survivors).
/// Uses AbilityUpgradeData for level-based stats.
/// </summary>
public class ForceFieldAbility : BaseAbility
{
    [Header("Force Field Settings (Fallback if no UpgradeData)")]
    [SerializeField] private float baseRadius = 3f;
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float baseDamageInterval = 0.5f; // How often to deal damage
    
    [Header("Visual Settings")]
    [SerializeField] private GameObject forceFieldVisualPrefab;
    [SerializeField] private Color forceFieldColor = new Color(0.3f, 0.7f, 1f, 0.4f); // Light blue
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private float visualYOffset = -0.5f; // How far below the snake the visual sits
    [SerializeField] private float visualThickness = 0.2f; // Thickness of the circular visual
    
    [Header("Audio")]
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioSource audioSource;
    
    // Custom stat names for upgrade data
    private const string STAT_RADIUS = "radius";
    
    private Transform playerTransform;
    private GameObject forceFieldVisual;
    private float damageTimer = 0f;
    private HashSet<AppleEnemy> enemiesInField = new HashSet<AppleEnemy>();
    private List<AppleEnemy> enemiesToRemove = new List<AppleEnemy>();
    
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
    
    protected override void ActivateAbility()
    {
        base.ActivateAbility();
        CreateForceFieldVisual();
        Debug.Log($"ForceFieldAbility: Activated at level {currentLevel} with radius {GetRadius()}");
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (!isActive || isFrozen) return;
        
        // Update visual position
        UpdateVisualPosition();
        
        // Find enemies in range
        FindEnemiesInField();
        
        // Deal damage at intervals
        damageTimer += Time.deltaTime;
        float currentInterval = GetDamageInterval();
        
        if (damageTimer >= currentInterval)
        {
            DealDamageToEnemiesInField();
            damageTimer = 0f;
        }
    }
    
    /// <summary>
    /// Creates the visual representation of the force field as a flat circular disc underneath the snake
    /// </summary>
    private void CreateForceFieldVisual()
    {
        if (forceFieldVisualPrefab != null)
        {
            forceFieldVisual = Instantiate(forceFieldVisualPrefab, playerTransform.position, Quaternion.identity);
            forceFieldVisual.transform.SetParent(transform);
        }
        else
        {
            // Create a flat cylinder visual to represent the force field as a disc underneath the snake
            forceFieldVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            forceFieldVisual.name = "ForceFieldVisual";
            forceFieldVisual.transform.SetParent(transform);
            
            // Remove collider - we handle collision detection manually
            Collider col = forceFieldVisual.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            // Set up material with transparency
            Renderer renderer = forceFieldVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Try to find a transparent shader
                Shader transparentShader = Shader.Find("Sprites/Default");
                if (transparentShader == null)
                {
                    transparentShader = Shader.Find("Universal Render Pipeline/Lit");
                }
                
                Material mat = new Material(transparentShader);
                mat.color = forceFieldColor;
                
                // Enable transparency if using URP shader
                if (mat.HasProperty("_Surface"))
                {
                    mat.SetFloat("_Surface", 1); // 1 = Transparent
                    mat.SetFloat("_Blend", 0); // Alpha blend
                }
                
                renderer.material = mat;
            }
        }
        
        UpdateVisualScale();
    }
    
    /// <summary>
    /// Updates the visual position to follow the player, positioned underneath
    /// </summary>
    private void UpdateVisualPosition()
    {
        if (forceFieldVisual != null && playerTransform != null)
        {
            // Position the visual underneath the snake
            Vector3 position = playerTransform.position;
            position.y += visualYOffset;
            forceFieldVisual.transform.position = position;
            
            // Keep the visual flat (horizontal)
            forceFieldVisual.transform.rotation = Quaternion.identity;
        }
    }
    
    /// <summary>
    /// Updates the visual scale based on current radius
    /// Creates a flat disc shape (wide but thin cylinder)
    /// </summary>
    private void UpdateVisualScale()
    {
        if (forceFieldVisual != null)
        {
            float radius = GetRadius();
            // Cylinder primitive: X and Z control diameter, Y controls height
            // We want a flat disc, so make Y very small and X/Z large
            forceFieldVisual.transform.localScale = new Vector3(radius * 2f, visualThickness, radius * 2f);
        }
    }
    
    /// <summary>
    /// Finds all enemies within the force field radius
    /// </summary>
    private void FindEnemiesInField()
    {
        if (playerTransform == null) return;
        
        float currentRadius = GetRadius();
        float radiusSqr = currentRadius * currentRadius;
        
        // Clear enemies that are no longer in range or destroyed
        enemiesToRemove.Clear();
        foreach (AppleEnemy enemy in enemiesInField)
        {
            if (enemy == null)
            {
                enemiesToRemove.Add(enemy);
                continue;
            }
            
            float distanceSqr = (enemy.transform.position - playerTransform.position).sqrMagnitude;
            if (distanceSqr > radiusSqr)
            {
                enemiesToRemove.Add(enemy);
            }
        }
        
        foreach (AppleEnemy enemy in enemiesToRemove)
        {
            enemiesInField.Remove(enemy);
        }
        
        // Find new enemies in range
        AppleEnemy[] allEnemies = FindObjectsByType<AppleEnemy>(FindObjectsSortMode.None);
        
        foreach (AppleEnemy enemy in allEnemies)
        {
            if (enemy == null || enemy.IsFrozen()) continue;
            
            float distanceSqr = (enemy.transform.position - playerTransform.position).sqrMagnitude;
            if (distanceSqr <= radiusSqr)
            {
                enemiesInField.Add(enemy);
            }
        }
    }
    
    /// <summary>
    /// Deals damage to all enemies currently in the force field
    /// </summary>
    private void DealDamageToEnemiesInField()
    {
        if (enemiesInField.Count == 0) return;
        
        float currentDamage = GetFieldDamage();
        bool playedSound = false;
        
        enemiesToRemove.Clear();
        
        foreach (AppleEnemy enemy in enemiesInField)
        {
            if (enemy == null)
            {
                enemiesToRemove.Add(enemy);
                continue;
            }
            
            enemy.TakeDamage(currentDamage);
            
            if (!playedSound && damageSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(damageSound);
                playedSound = true;
            }
        }
        
        // Clean up null references
        foreach (AppleEnemy enemy in enemiesToRemove)
        {
            enemiesInField.Remove(enemy);
        }
    }
    
    /// <summary>
    /// Gets the current radius based on level
    /// </summary>
    private float GetRadius()
    {
        float radius;
        if (upgradeData != null)
        {
            radius = GetCustomStat(STAT_RADIUS, baseRadius);
        }
        else
        {
            radius = baseRadius + (currentLevel - 1) * 1f; // +1 radius per level
        }
        
        // Apply range multiplier from PlayerStats
        float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetRangeMultiplier() : 1f;
        return radius * multiplier;
    }
    
    /// <summary>
    /// Gets the current damage based on level
    /// </summary>
    private float GetFieldDamage()
    {
        float damage;
        if (upgradeData != null)
        {
            damage = GetDamage();
        }
        else
        {
            damage = baseDamage + (currentLevel - 1) * 5f; // +5 damage per level
        }
        
        // Apply damage multiplier from PlayerStats
        float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetDamageMultiplier() : 1f;
        return damage * multiplier;
    }
    
    /// <summary>
    /// Gets the current damage interval based on level
    /// </summary>
    private float GetDamageInterval()
    {
        if (upgradeData != null)
        {
            float cooldown = GetCooldown();
            return cooldown > 0 ? cooldown : baseDamageInterval;
        }
        return Mathf.Max(0.2f, baseDamageInterval - (currentLevel - 1) * 0.05f); // Faster at higher levels
    }
    
    protected override void OnLevelUp()
    {
        base.OnLevelUp();
        UpdateVisualScale();
        Debug.Log($"ForceFieldAbility: Level {currentLevel} - Radius: {GetRadius():F1}, Damage: {GetFieldDamage():F0}, Interval: {GetDamageInterval():F2}s");
    }
    
    protected override void DeactivateAbility()
    {
        base.DeactivateAbility();
        
        if (forceFieldVisual != null)
        {
            Destroy(forceFieldVisual);
        }
        
        enemiesInField.Clear();
    }
    
    /// <summary>
    /// Debug visualization - shows the force field as a flat disc
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        Transform drawTransform = playerTransform;
        if (drawTransform == null)
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
            {
                drawTransform = player.transform;
            }
        }
        
        if (drawTransform != null)
        {
            float radius = Application.isPlaying ? GetRadius() : baseRadius;
            Vector3 center = drawTransform.position + Vector3.up * visualYOffset;
            
            // Draw a flat disc gizmo
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.3f);
            
            // Draw circle outline
            int segments = 32;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);
            for (int i = 1; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
            
            // Draw cross lines for visibility
            Gizmos.DrawLine(center + new Vector3(-radius, 0, 0), center + new Vector3(radius, 0, 0));
            Gizmos.DrawLine(center + new Vector3(0, 0, -radius), center + new Vector3(0, 0, radius));
        }
    }
}