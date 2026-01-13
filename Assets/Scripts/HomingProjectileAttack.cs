
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attack that shoots a homing projectile that automatically targets the closest apple enemy.
/// Uses AttackUpgradeData for level-based stats.
/// </summary>
public class HomingProjectileAttack : Attack
{
    [Header("Projectile Settings (Base - overridden by upgrade data if assigned)")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float homingStrength = 10f; // How strongly the projectile homes in
    [SerializeField] private float projectileLifetime = 5f;
    [SerializeField] private int projectileCount = 1; // Number of projectiles per shot
    [SerializeField] private float spreadAngle = 15f; // Angle spread for multiple projectiles
    
    [Header("Targeting")]
    [SerializeField] private float targetingRange = 30f; // Range to find targets
    [SerializeField] private bool retargetOnMiss = true; // Find new target if current dies
    
    [Header("Explosion Settings (Evolution)")]
    [SerializeField] private bool explosionEnabled = false; // Enabled when evolved
    [SerializeField] private float explosionRadius = 3f; // Radius of explosion
    [SerializeField] private float explosionDamageMultiplier = 0.5f; // Explosion damage as % of projectile damage
    [SerializeField] private GameObject explosionEffectPrefab; // Optional explosion VFX
    [SerializeField] private AudioClip explosionSound; // Optional explosion sound
    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private TrailRenderer projectileTrailPrefab;
    [SerializeField] private Color projectileColor = new Color(0.2f, 1f, 0.4f, 1f); // Green
    
    [Header("Animation")]
    [SerializeField] private string animationTriggerName = "Spit";
    [SerializeField] private Animator animator;
    
    [Header("Audio")]
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private AudioSource audioSource;
    
    // Custom stat names for upgrade data
    private const string STAT_PROJECTILE_SPEED = "projectileSpeed";
    private const string STAT_HOMING_STRENGTH = "homingStrength";
    private const string STAT_PROJECTILE_COUNT = "projectileCount";
    private const string STAT_SPREAD_ANGLE = "spreadAngle";
    private const string STAT_TARGETING_RANGE = "targetingRange";
    private const string STAT_EXPLOSION_ENABLED = "explosionEnabled";
    private const string STAT_EXPLOSION_RADIUS = "explosionRadius";
    private const string STAT_EXPLOSION_DAMAGE_MULTIPLIER = "explosionDamageMultiplier";
    
    private void Awake()
    {
        attackType = AttackType.Burst;
        
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.5f;
            }
        }
        
        // Create default fire point if not assigned
        if (firePoint == null)
        {
            firePoint = transform;
        }
    }
    
    /// <summary>
    /// Apply custom stats from upgrade data
    /// </summary>
    protected override void ApplyCustomStats(AttackLevelStats stats)
    {
        projectileSpeed = GetCustomStat(STAT_PROJECTILE_SPEED, projectileSpeed);
        homingStrength = GetCustomStat(STAT_HOMING_STRENGTH, homingStrength);
        projectileCount = Mathf.RoundToInt(GetCustomStat(STAT_PROJECTILE_COUNT, projectileCount));
        spreadAngle = GetCustomStat(STAT_SPREAD_ANGLE, spreadAngle);
        targetingRange = GetCustomStat(STAT_TARGETING_RANGE, targetingRange);
        
        // Explosion settings (for evolution)
        float explosionEnabledValue = GetCustomStat(STAT_EXPLOSION_ENABLED, explosionEnabled ? 1f : 0f);
        explosionEnabled = explosionEnabledValue > 0.5f;
        explosionRadius = GetCustomStat(STAT_EXPLOSION_RADIUS, explosionRadius);
        explosionDamageMultiplier = GetCustomStat(STAT_EXPLOSION_DAMAGE_MULTIPLIER, explosionDamageMultiplier);
    }
    
    protected override void OnActivate()
    {
        FireProjectiles();
        
        // Play animation
        if (animator != null && !string.IsNullOrEmpty(animationTriggerName))
        {
            animator.SetTrigger(animationTriggerName);
        }
        
        // Play muzzle flash
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        
        // Play sound using SoundManager
        SoundManager.Play("MagicOrb", gameObject);
    }
    
    protected override void OnHoldUpdate()
    {
        // Burst attack - nothing to do on hold
    }
    
    protected override void OnDeactivate()
    {
        // Burst attack - nothing to do on deactivate
    }
    
    /// <summary>
    /// Fires homing projectiles at the closest enemies
    /// </summary>
    private void FireProjectiles()
    {
        int count = GetProjectileCount();
        float currentSpread = GetSpreadAngle();
        
        // Find targets for each projectile
        List<AppleEnemy> targets = FindClosestEnemies(count);
        
        for (int i = 0; i < count; i++)
        {
            // Calculate spread angle for this projectile
            float angleOffset = 0f;
            if (count > 1)
            {
                angleOffset = Mathf.Lerp(-currentSpread / 2f, currentSpread / 2f, (float)i / (count - 1));
            }
            
            // Get target for this projectile (cycle through available targets)
            AppleEnemy target = targets.Count > 0 ? targets[i % targets.Count] : null;
            
            SpawnProjectile(angleOffset, target);
        }
    }
    
    /// <summary>
    /// Spawns a single homing projectile
    /// </summary>
    private void SpawnProjectile(float angleOffset, AppleEnemy initialTarget)
    {
        Vector3 spawnPos = firePoint.position;
        Quaternion spawnRot = firePoint.rotation * Quaternion.Euler(0f, angleOffset, 0f);
        
        GameObject projectileObj;
        
        if (projectilePrefab != null)
        {
            projectileObj = Instantiate(projectilePrefab, spawnPos, spawnRot);
        }
        else
        {
            // Create a simple projectile if no prefab assigned
            projectileObj = CreateDefaultProjectile(spawnPos, spawnRot);
        }
        
        // Add or get the homing component
        HomingProjectile homing = projectileObj.GetComponent<HomingProjectile>();
        if (homing == null)
        {
            homing = projectileObj.AddComponent<HomingProjectile>();
        }
        
        // Initialize the projectile
        float currentDamage = GetDamage();
        float currentSpeed = GetProjectileSpeed();
        float currentHoming = GetHomingStrength();
        float currentRange = GetTargetingRange();
        
        homing.Initialize(
            currentDamage,
            currentSpeed,
            currentHoming,
            projectileLifetime,
            initialTarget,
            currentRange,
            retargetOnMiss
        );
        
        // Set explosion parameters if enabled
        if (IsExplosionEnabled())
        {
            homing.SetExplosionParams(
                true,
                GetExplosionRadius(),
                currentDamage * GetExplosionDamageMultiplier(),
                explosionEffectPrefab,
                explosionSound
            );
        }
    }
    
    /// <summary>
    /// Creates a default projectile if no prefab is assigned
    /// </summary>
    private GameObject CreateDefaultProjectile(Vector3 position, Quaternion rotation)
    {
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "HomingProjectile";
        projectile.transform.position = position;
        projectile.transform.rotation = rotation;
        projectile.transform.localScale = Vector3.one * 0.3f;
        
        // Set up renderer
        Renderer renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = projectileColor;
            renderer.material = mat;
        }
        
        // Set up collider as trigger
        Collider col = projectile.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        
        // Add rigidbody
        Rigidbody rb = projectile.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        
        // Add trail if prefab exists
        if (projectileTrailPrefab != null)
        {
            TrailRenderer trail = Instantiate(projectileTrailPrefab, projectile.transform);
            trail.transform.localPosition = Vector3.zero;
        }
        
        return projectile;
    }
    
    // Cached list to avoid allocations during targeting
    private static List<(AppleEnemy enemy, float distSqr)> s_enemiesWithDist = new List<(AppleEnemy, float)>(64);
    private static List<AppleEnemy> s_targetResults = new List<AppleEnemy>(8);
    
    /// <summary>
    /// Finds the closest enemies within targeting range.
    /// Uses AppleEnemy's static list instead of FindObjectsByType for better performance.
    /// </summary>
    private List<AppleEnemy> FindClosestEnemies(int maxCount)
    {
        s_targetResults.Clear();
        s_enemiesWithDist.Clear();
        
        float currentRange = GetTargetingRange();
        float rangeSqr = currentRange * currentRange;
        Vector3 firePos = firePoint.position;
        
        // Use AppleEnemy's static list instead of FindObjectsByType
        // This is much faster as it avoids the expensive scene query
        var allEnemies = AppleEnemy.GetAllActiveEnemies();
        int enemyCount = allEnemies.Count;
        
        for (int i = 0; i < enemyCount; i++)
        {
            AppleEnemy enemy = allEnemies[i];
            if (enemy == null || enemy.IsFrozen() || enemy.IsAlly()) continue;
            
            float distSqr = (enemy.transform.position - firePos).sqrMagnitude;
            if (distSqr <= rangeSqr)
            {
                s_enemiesWithDist.Add((enemy, distSqr));
            }
        }
        
        // Sort by distance
        s_enemiesWithDist.Sort((a, b) => a.distSqr.CompareTo(b.distSqr));
        
        // Take the closest ones
        int resultCount = Mathf.Min(maxCount, s_enemiesWithDist.Count);
        for (int i = 0; i < resultCount; i++)
        {
            s_targetResults.Add(s_enemiesWithDist[i].enemy);
        }
        
        return s_targetResults;
    }
    
    // Stat getters with upgrade data support
    private float GetProjectileSpeed()
    {
        if (upgradeData != null)
        {
            return GetCustomStat(STAT_PROJECTILE_SPEED, projectileSpeed);
        }
        return projectileSpeed;
    }
    
    private float GetHomingStrength()
    {
        if (upgradeData != null)
        {
            return GetCustomStat(STAT_HOMING_STRENGTH, homingStrength);
        }
        return homingStrength;
    }
    
    private int GetProjectileCount()
    {
        if (upgradeData != null)
        {
            return Mathf.RoundToInt(GetCustomStat(STAT_PROJECTILE_COUNT, projectileCount));
        }
        return projectileCount;
    }
    
    private float GetSpreadAngle()
    {
        if (upgradeData != null)
        {
            return GetCustomStat(STAT_SPREAD_ANGLE, spreadAngle);
        }
        return spreadAngle;
    }
    
    private float GetTargetingRange()
    {
        if (upgradeData != null)
        {
            float baseRange = GetCustomStat(STAT_TARGETING_RANGE, targetingRange);
            float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetRangeMultiplier() : 1f;
            return baseRange * multiplier;
        }
        
        float fallbackMultiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetRangeMultiplier() : 1f;
        return targetingRange * fallbackMultiplier;
    }
    
    protected override void OnUpgrade()
    {
        Debug.Log($"Homing Projectile upgraded! Count: {GetProjectileCount()}, Speed: {GetProjectileSpeed():F1}, Homing: {GetHomingStrength():F1}, Explosion: {IsExplosionEnabled()}");
    }
    
    // Explosion stat getters
    private bool IsExplosionEnabled()
    {
        if (upgradeData != null)
        {
            float value = GetCustomStat(STAT_EXPLOSION_ENABLED, explosionEnabled ? 1f : 0f);
            return value > 0.5f;
        }
        return explosionEnabled;
    }
    
    private float GetExplosionRadius()
    {
        if (upgradeData != null)
        {
            float baseRadius = GetCustomStat(STAT_EXPLOSION_RADIUS, explosionRadius);
            float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetRangeMultiplier() : 1f;
            return baseRadius * multiplier;
        }
        return explosionRadius;
    }
    
    private float GetExplosionDamageMultiplier()
    {
        if (upgradeData != null)
        {
            return GetCustomStat(STAT_EXPLOSION_DAMAGE_MULTIPLIER, explosionDamageMultiplier);
        }
        return explosionDamageMultiplier;
    }
    
    private void OnDrawGizmosSelected()
    {
        Transform point = firePoint != null ? firePoint : transform;
        
        // Draw targeting range
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.3f);
        Gizmos.DrawWireSphere(point.position, GetTargetingRange());
        
        // Draw fire direction
        Gizmos.color = Color.red;
        Gizmos.DrawRay(point.position, point.forward * 3f);
    }
}

/// <summary>
/// Component that handles homing projectile behavior
/// </summary>
public class HomingProjectile : MonoBehaviour
{
    private float damage;
    private float speed;
    private float homingStrength;
    private float lifetime;
    private float targetingRange;
    private bool retargetOnMiss;
    
    private AppleEnemy currentTarget;
    private float spawnTime;
    private bool isInitialized = false;
    
    // Forward flight phase settings
    private float forwardFlightDuration = 0.3f; // Time to fly forward before homing
    private float forwardFlightDistance = 3f; // Distance to fly forward before homing
    private Vector3 initialForwardDirection;
    private Vector3 spawnPosition;
    private bool isInForwardPhase = true;
    
    // Explosion settings
    private bool explosionEnabled = false;
    private float explosionRadius = 3f;
    private float explosionDamage = 0f;
    private GameObject explosionEffectPrefab;
    private AudioClip explosionSound;
    
    public void Initialize(float damage, float speed, float homingStrength, float lifetime,
                          AppleEnemy initialTarget, float targetingRange, bool retargetOnMiss)
    {
        this.damage = damage;
        this.speed = speed;
        this.homingStrength = homingStrength;
        this.lifetime = lifetime;
        this.currentTarget = initialTarget;
        this.targetingRange = targetingRange;
        this.retargetOnMiss = retargetOnMiss;
        
        spawnTime = Time.time;
        spawnPosition = transform.position;
        initialForwardDirection = transform.forward;
        isInForwardPhase = true;
        isInitialized = true;
    }
    
    /// <summary>
    /// Set the forward flight parameters
    /// </summary>
    public void SetForwardFlightParams(float duration, float distance)
    {
        forwardFlightDuration = duration;
        forwardFlightDistance = distance;
    }
    
    /// <summary>
    /// Set explosion parameters for evolved projectiles
    /// </summary>
    public void SetExplosionParams(bool enabled, float radius, float damage, GameObject effectPrefab, AudioClip sound)
    {
        explosionEnabled = enabled;
        explosionRadius = radius;
        explosionDamage = damage;
        explosionEffectPrefab = effectPrefab;
        explosionSound = sound;
    }
    
    private void Update()
    {
        if (!isInitialized) return;
        
        // Check lifetime
        if (Time.time - spawnTime >= lifetime)
        {
            Destroy(gameObject);
            return;
        }
        
        // Check if we should transition from forward phase to homing phase
        if (isInForwardPhase)
        {
            float timeSinceSpawn = Time.time - spawnTime;
            float distanceTraveled = Vector3.Distance(transform.position, spawnPosition);
            
            // Transition to homing phase after time or distance threshold
            if (timeSinceSpawn >= forwardFlightDuration || distanceTraveled >= forwardFlightDistance)
            {
                isInForwardPhase = false;
            }
        }
        
        // Check if target is still valid (only matters in homing phase)
        if (!isInForwardPhase && (currentTarget == null || currentTarget.IsFrozen()))
        {
            if (retargetOnMiss)
            {
                FindNewTarget();
            }
        }
        
        // Determine movement direction based on phase
        Vector3 moveDirection;
        
        if (isInForwardPhase)
        {
            // Forward phase: fly straight in the initial direction
            moveDirection = initialForwardDirection;
        }
        else
        {
            // Homing phase: turn towards target
            moveDirection = transform.forward;
            
            if (currentTarget != null)
            {
                Vector3 toTarget = (currentTarget.transform.position - transform.position).normalized;
                moveDirection = Vector3.Lerp(moveDirection, toTarget, homingStrength * Time.deltaTime);
                moveDirection.Normalize();
                
                // Rotate to face movement direction
                if (moveDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(moveDirection);
                }
            }
        }
        
        // Move
        transform.position += moveDirection * speed * Time.deltaTime;
    }
    
    /// <summary>
    /// Finds a new target using AppleEnemy's static list for better performance.
    /// </summary>
    private void FindNewTarget()
    {
        float closestDistSqr = targetingRange * targetingRange;
        AppleEnemy closest = null;
        Vector3 myPos = transform.position;
        
        // Use AppleEnemy's static list instead of FindObjectsByType
        var allEnemies = AppleEnemy.GetAllActiveEnemies();
        int count = allEnemies.Count;
        
        for (int i = 0; i < count; i++)
        {
            AppleEnemy enemy = allEnemies[i];
            if (enemy == null || enemy.IsFrozen() || enemy.IsAlly()) continue;
            
            float distSqr = (enemy.transform.position - myPos).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = enemy;
            }
        }
        
        currentTarget = closest;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        AppleEnemy enemy = other.GetComponentInParent<AppleEnemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Debug.Log($"Homing projectile hit {enemy.name} for {damage} damage!");
            
            // Create explosion if enabled
            if (explosionEnabled)
            {
                CreateExplosion();
            }
            
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Creates an explosion that damages nearby enemies.
    /// Uses AppleEnemy's static list for better performance.
    /// </summary>
    private void CreateExplosion()
    {
        Vector3 explosionPos = transform.position;
        
        // Play bomb explosion sound
        SoundManager.Play("Bomb", gameObject);
        
        // Spawn explosion effect if available
        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab, explosionPos, Quaternion.identity);
            Destroy(effect, 3f); // Clean up after 3 seconds
        }
        else
        {
            // Create a simple default explosion effect
            CreateDefaultExplosionEffect(explosionPos);
        }
        
        // Find and damage all enemies in explosion radius
        // Use AppleEnemy's static list instead of FindObjectsByType
        float radiusSqr = explosionRadius * explosionRadius;
        var allEnemies = AppleEnemy.GetAllActiveEnemies();
        int count = allEnemies.Count;
        
        for (int i = 0; i < count; i++)
        {
            AppleEnemy nearbyEnemy = allEnemies[i];
            if (nearbyEnemy == null || nearbyEnemy.IsFrozen() || nearbyEnemy.IsAlly()) continue;
            
            float distSqr = (nearbyEnemy.transform.position - explosionPos).sqrMagnitude;
            if (distSqr <= radiusSqr)
            {
                // Calculate damage falloff based on distance (closer = more damage)
                float distance = Mathf.Sqrt(distSqr);
                float falloff = 1f - (distance / explosionRadius);
                float finalDamage = explosionDamage * Mathf.Max(0.3f, falloff); // Minimum 30% damage
                
                nearbyEnemy.TakeDamage(finalDamage);
                #if UNITY_EDITOR
                Debug.Log($"Explosion hit {nearbyEnemy.name} for {finalDamage:F1} damage!");
                #endif
            }
        }
    }
    
    /// <summary>
    /// Creates a simple default explosion visual effect
    /// </summary>
    private void CreateDefaultExplosionEffect(Vector3 position)
    {
        // Create a simple expanding sphere effect
        GameObject explosionObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        explosionObj.name = "ExplosionEffect";
        explosionObj.transform.position = position;
        explosionObj.transform.localScale = Vector3.one * 0.5f;
        
        // Remove collider
        Collider col = explosionObj.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        // Set up material with purple/arcane color
        Renderer renderer = explosionObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0.6f, 0.2f, 1f, 0.8f); // Purple/arcane color
            renderer.material = mat;
        }
        
        // Add explosion animation component
        ExplosionEffect effect = explosionObj.AddComponent<ExplosionEffect>();
        effect.Initialize(explosionRadius, 0.3f);
    }
}

/// <summary>
/// Simple component to animate explosion effect
/// </summary>
public class ExplosionEffect : MonoBehaviour
{
    private float targetRadius;
    private float duration;
    private float startTime;
    private Vector3 startScale;
    private Renderer rend;
    private Color startColor;
    
    public void Initialize(float radius, float duration)
    {
        this.targetRadius = radius;
        this.duration = duration;
        this.startTime = Time.time;
        this.startScale = transform.localScale;
        this.rend = GetComponent<Renderer>();
        if (rend != null)
        {
            startColor = rend.material.color;
        }
    }
    
    private void Update()
    {
        float elapsed = Time.time - startTime;
        float t = elapsed / duration;
        
        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }
        
        // Expand the sphere
        float currentRadius = Mathf.Lerp(0.5f, targetRadius * 2f, t);
        transform.localScale = Vector3.one * currentRadius;
        
        // Fade out
        if (rend != null)
        {
            Color c = startColor;
            c.a = Mathf.Lerp(0.8f, 0f, t);
            rend.material.color = c;
        }
    }
}