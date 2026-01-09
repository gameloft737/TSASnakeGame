
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
    [SerializeField] private GameObject forceFieldVisualPrefab; // Particle system prefab for the force field visual
    [SerializeField] private Color forceFieldColor = new Color(0.3f, 0.7f, 1f, 0.4f); // Light blue
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private float visualYOffset = -0.5f; // How far below the snake the visual sits
    [SerializeField] private bool useParticleSystem = true; // Whether to create a particle system if no prefab is assigned
    
    [Header("Audio")]
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioSource audioSource;
    
    // Custom stat names for upgrade data
    private const string STAT_RADIUS = "radius";
    
    private Transform playerTransform;
    private GameObject forceFieldVisual;
    private ParticleSystem forceFieldParticleSystem;
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
        
        // Re-check for player transform in case it wasn't found in Awake
        if (playerTransform == null)
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("ForceFieldAbility: Could not find PlayerMovement! Ability will not work.");
                return;
            }
        }
        
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
    /// Creates the visual representation of the force field - either as a particle system or a flat disc
    /// </summary>
    private void CreateForceFieldVisual()
    {
        if (forceFieldVisualPrefab != null)
        {
            // Use the assigned prefab (particle system or other visual)
            forceFieldVisual = Instantiate(forceFieldVisualPrefab, playerTransform.position, Quaternion.identity);
            forceFieldVisual.transform.SetParent(transform);
            
            // Get particle system reference if it exists
            forceFieldParticleSystem = forceFieldVisual.GetComponent<ParticleSystem>();
            if (forceFieldParticleSystem == null)
            {
                forceFieldParticleSystem = forceFieldVisual.GetComponentInChildren<ParticleSystem>();
            }
            
            // Start the particle system if found
            if (forceFieldParticleSystem != null)
            {
                forceFieldParticleSystem.Play();
            }
        }
        else if (useParticleSystem)
        {
            // Create a default particle system for the force field
            CreateDefaultParticleSystem();
        }
        else
        {
            // Create a flat cylinder visual as fallback
            CreateCylinderVisual();
        }
        
        UpdateVisualScale();
    }
    
    /// <summary>
    /// Creates a default particle system for the force field visual
    /// </summary>
    private void CreateDefaultParticleSystem()
    {
        forceFieldVisual = new GameObject("ForceFieldParticleVisual");
        forceFieldVisual.transform.SetParent(transform);
        forceFieldVisual.transform.position = playerTransform.position;
        
        forceFieldParticleSystem = forceFieldVisual.AddComponent<ParticleSystem>();
        
        // Configure main module - continuous looping effect
        var main = forceFieldParticleSystem.main;
        main.duration = 1f;
        main.loop = true;
        main.startLifetime = 1.5f;
        main.startSpeed = 0.5f;
        main.startSize = 0.2f;
        main.startColor = forceFieldColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 200;
        
        // Configure emission - continuous particles
        var emission = forceFieldParticleSystem.emission;
        emission.rateOverTime = 50f;
        
        // Configure shape - circle/ring around the player
        var shape = forceFieldParticleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = GetRadius();
        shape.radiusThickness = 0.1f; // Emit from edge of circle
        shape.arc = 360f;
        shape.rotation = new Vector3(-90f, 0f, 0f); // Horizontal circle
        
        // Configure velocity over lifetime - swirl effect
        var velocityOverLifetime = forceFieldParticleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.orbitalY = 2f; // Orbit around Y axis
        
        // Configure size over lifetime - slight pulse
        var sizeOverLifetime = forceFieldParticleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f, 0.5f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Configure color over lifetime - fade in and out
        var colorOverLifetime = forceFieldParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(forceFieldColor, 0f), 
                new GradientColorKey(forceFieldColor, 0.5f),
                new GradientColorKey(forceFieldColor, 1f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0f, 0f), 
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(1f, 0.8f),
                new GradientAlphaKey(0f, 1f) 
            }
        );
        colorOverLifetime.color = gradient;
        
        // Configure renderer
        var renderer = forceFieldVisual.GetComponent<ParticleSystemRenderer>();
        Material particleMat = new Material(Shader.Find("Particles/Standard Unlit"));
        particleMat.color = forceFieldColor;
        renderer.material = particleMat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        forceFieldParticleSystem.Play();
    }
    
    /// <summary>
    /// Creates a cylinder visual as fallback
    /// </summary>
    private void CreateCylinderVisual()
    {
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
    /// </summary>
    private void UpdateVisualScale()
    {
        if (forceFieldVisual == null) return;
        
        float radius = GetRadius();
        
        // If using particle system, update the shape radius
        if (forceFieldParticleSystem != null)
        {
            var shape = forceFieldParticleSystem.shape;
            shape.radius = radius;
            
            // Scale the visual object to match radius
            forceFieldVisual.transform.localScale = Vector3.one * (radius / baseRadius);
        }
        else
        {
            // Cylinder primitive: X and Z control diameter, Y controls height
            // We want a flat disc, so make Y very small and X/Z large
            forceFieldVisual.transform.localScale = new Vector3(radius * 2f, 0.2f, radius * 2f);
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
        int enemiesHit = 0;
        
        enemiesToRemove.Clear();
        
        foreach (AppleEnemy enemy in enemiesInField)
        {
            if (enemy == null)
            {
                enemiesToRemove.Add(enemy);
                continue;
            }
            
            enemy.TakeDamage(currentDamage);
            enemiesHit++;
            
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
        
        if (enemiesHit > 0)
        {
            Debug.Log($"ForceFieldAbility: Dealt {currentDamage:F1} damage to {enemiesHit} enemies!");
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