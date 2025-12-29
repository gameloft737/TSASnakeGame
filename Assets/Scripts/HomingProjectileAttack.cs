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
        
        // Play sound
        if (fireSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(fireSound);
        }
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
    
    /// <summary>
    /// Finds the closest enemies within targeting range
    /// </summary>
    private List<AppleEnemy> FindClosestEnemies(int maxCount)
    {
        List<AppleEnemy> result = new List<AppleEnemy>();
        float currentRange = GetTargetingRange();
        float rangeSqr = currentRange * currentRange;
        
        AppleEnemy[] allEnemies = FindObjectsByType<AppleEnemy>(FindObjectsSortMode.None);
        
        // Create a list of enemies with distances
        List<(AppleEnemy enemy, float distSqr)> enemiesWithDist = new List<(AppleEnemy, float)>();
        
        foreach (AppleEnemy enemy in allEnemies)
        {
            if (enemy == null || enemy.IsFrozen()) continue;
            
            float distSqr = (enemy.transform.position - firePoint.position).sqrMagnitude;
            if (distSqr <= rangeSqr)
            {
                enemiesWithDist.Add((enemy, distSqr));
            }
        }
        
        // Sort by distance
        enemiesWithDist.Sort((a, b) => a.distSqr.CompareTo(b.distSqr));
        
        // Take the closest ones
        for (int i = 0; i < Mathf.Min(maxCount, enemiesWithDist.Count); i++)
        {
            result.Add(enemiesWithDist[i].enemy);
        }
        
        return result;
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
        Debug.Log($"Homing Projectile upgraded! Count: {GetProjectileCount()}, Speed: {GetProjectileSpeed():F1}, Homing: {GetHomingStrength():F1}");
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
        isInitialized = true;
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
        
        // Check if target is still valid
        if (currentTarget == null || currentTarget.IsFrozen())
        {
            if (retargetOnMiss)
            {
                FindNewTarget();
            }
        }
        
        // Move towards target or forward
        Vector3 moveDirection = transform.forward;
        
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
        
        // Move
        transform.position += moveDirection * speed * Time.deltaTime;
    }
    
    private void FindNewTarget()
    {
        float closestDistSqr = targetingRange * targetingRange;
        AppleEnemy closest = null;
        
        AppleEnemy[] allEnemies = FindObjectsByType<AppleEnemy>(FindObjectsSortMode.None);
        
        foreach (AppleEnemy enemy in allEnemies)
        {
            if (enemy == null || enemy.IsFrozen()) continue;
            
            float distSqr = (enemy.transform.position - transform.position).sqrMagnitude;
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
            Destroy(gameObject);
        }
    }
}