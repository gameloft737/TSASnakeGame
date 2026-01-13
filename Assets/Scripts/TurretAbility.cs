using UnityEngine;

/// <summary>
/// Active ability that spawns a turret that automatically shoots at enemies.
/// Uses AbilityUpgradeData for level-based stats if assigned.
/// </summary>
public class TurretAbility : BaseAbility
{
    [Header("Turret Settings")]
    [SerializeField] private float baseShootInterval = 1.5f;
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private GameObject turretBody;
    [SerializeField] private Transform originPoint;
    
    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float baseProjectileDamage = 25f;
    [SerializeField] private float projectileLifetime = 5f;
    
    [Header("Spawn Settings")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 1f, 0);
    
    [Header("Level Progression (Fallback if no UpgradeData)")]
    [SerializeField] private float damageIncreasePerLevel = 10f;
    [SerializeField] private float intervalDecreasePerLevel = 0.2f;
    
    [Header("Multi-Shot Settings (Fallback if no UpgradeData)")]
    [SerializeField] private int baseProjectileCount = 1;
    [SerializeField] private float spreadAngle = 15f; // Angle between projectiles when shooting multiple
    
    private float shootTimer = 0f;
    private Transform playerTransform;
    private AppleEnemy cachedNearestApple;
    private float targetSearchTimer = 0f;
    
    // Dynamic values that scale with level
    private float currentShootInterval;
    private float currentProjectileDamage;
    private int currentProjectileCount;

    protected override void Awake()
    {
        base.Awake(); // Initialize duration system
        
        // Set initial values based on level
        UpdateStatsForLevel();
    }

    private void Start()
    {
        playerTransform = transform.parent != null ? transform.parent : transform;
        
        // Create default projectile if none assigned
        if (projectilePrefab == null)
        {
            projectilePrefab = CreateDefaultProjectile();
        }
        
        Debug.Log($"TurretAbility: Started at level {currentLevel} - Damage: {currentProjectileDamage}, Fire Rate: {currentShootInterval:F2}s");
    }

    protected override void Update()
    {
        base.Update(); // Check for freeze state
        
        if (!isActive || isFrozen) return; // Don't update when frozen
        
        // Periodically search for nearest apple (performance optimization)
        targetSearchTimer += Time.deltaTime;
        if (targetSearchTimer >= (currentShootInterval / 4))
        {
            cachedNearestApple = FindNearestApple();
            targetSearchTimer = 0f;
        }
        
        // Face the cached nearest apple
        if (cachedNearestApple != null && turretBody != null)
        {
            Vector3 turretPosition = originPoint != null ? originPoint.position : playerTransform.position + spawnOffset;
            Vector3 targetPosition = GetAppleCenterPosition(cachedNearestApple);
            Vector3 direction = (targetPosition - turretPosition).normalized;
            
            if (direction.sqrMagnitude > 0.01f)
            {
                turretBody.transform.rotation = Quaternion.LookRotation(direction);
            }
        }
        
        shootTimer += Time.deltaTime;
        
        if (shootTimer >= currentShootInterval)
        {
            TryShoot();
            shootTimer = 0f;
        }
    }

    protected override void ActivateAbility()
    {
        base.ActivateAbility();
        
        if (turretBody != null)
        {
            turretBody.SetActive(true);
        }
        
        Debug.Log($"TurretAbility: Activated at level {currentLevel}");
    }
    
    protected override void DeactivateAbility()
    {
        base.DeactivateAbility();
        
        if (turretBody != null)
        {
            turretBody.SetActive(false);
        }
        
        Debug.Log("TurretAbility: Deactivated");
        // No longer destroy - abilities are permanent
    }
    
    protected override void OnLevelUp()
    {
        base.OnLevelUp();
        
        UpdateStatsForLevel();
        
        Debug.Log($"TurretAbility: Level {currentLevel} - Damage: {currentProjectileDamage}, Fire Rate: {currentShootInterval:F2}s, Projectiles: {currentProjectileCount}");
    }
    
    /// <summary>
    /// Applies custom stats from the upgrade data
    /// </summary>
    protected override void ApplyCustomStats(AbilityLevelStats stats)
    {
        // Custom stats can include "shootInterval" and "damage"
        // We'll apply them in UpdateStatsForLevel() which is called after this
    }
    
    private void UpdateStatsForLevel()
    {
        // Use upgrade data if available
        if (upgradeData != null)
        {
            // Get damage from upgrade data (core stat)
            currentProjectileDamage = GetDamage();
            if (currentProjectileDamage <= 0) currentProjectileDamage = baseProjectileDamage;
            
            // Get cooldown (shoot interval) from upgrade data (core stat)
            currentShootInterval = GetCooldown();
            if (currentShootInterval <= 0)
            {
                // Fall back to calculated interval
                currentShootInterval = baseShootInterval - (intervalDecreasePerLevel * (currentLevel - 1));
            }
            
            // Get projectile count from custom stats (how many darts to shoot at once)
            int customProjectileCount = Mathf.RoundToInt(GetCustomStat("projectileCount", 0f));
            if (customProjectileCount > 0)
            {
                currentProjectileCount = customProjectileCount;
            }
            else
            {
                currentProjectileCount = baseProjectileCount;
            }
        }
        else
        {
            // Fallback: Increase damage with each level
            currentProjectileDamage = baseProjectileDamage + (damageIncreasePerLevel * (currentLevel - 1));
            
            // Decrease interval (faster firing) with each level
            currentShootInterval = baseShootInterval - (intervalDecreasePerLevel * (currentLevel - 1));
            
            // Use base projectile count
            currentProjectileCount = baseProjectileCount;
        }
        
        // Clamp to reasonable minimum (don't go below 0.3s interval)
        currentShootInterval = Mathf.Max(currentShootInterval, 0.3f);
        
        // Ensure at least 1 projectile
        currentProjectileCount = Mathf.Max(currentProjectileCount, 1);
    }

    private void TryShoot()
    {
        // Use cached apple, but verify it's still valid
        if (cachedNearestApple != null)
        {
            ShootAtTarget(cachedNearestApple.transform);
        }
    }
    
    private Vector3 GetAppleCenterPosition(AppleEnemy apple)
    {
        // Get NavMeshAgent height for accurate center position
        UnityEngine.AI.NavMeshAgent agent = apple.GetComponent<UnityEngine.AI.NavMeshAgent>();
        float height = agent != null ? agent.height * 0.5f : 0.5f;
        
        return apple.transform.position + Vector3.up * height;
    }

    private AppleEnemy FindNearestApple()
    {
        // Use AppleEnemy's static list instead of FindObjectsByType for better performance
        var apples = AppleEnemy.GetAllActiveEnemies();
        
        if (apples.Count == 0) return null;
        
        AppleEnemy nearest = null;
        // Apply range multiplier from PlayerStats to detection range
        float effectiveRange = GetEffectiveDetectionRange();
        float nearestDistance = effectiveRange;
        Vector3 playerPos = playerTransform.position;
        
        foreach (var apple in apples)
        {
            if (apple != null)
            {
                float distance = Vector3.Distance(playerPos, apple.transform.position);
                
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = apple;
                }
            }
        }
        
        return nearest;
    }
    
    /// <summary>
    /// Gets the effective detection range with range multiplier applied
    /// </summary>
    private float GetEffectiveDetectionRange()
    {
        float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetRangeMultiplier() : 1f;
        return detectionRange * multiplier;
    }

    private void ShootAtTarget(Transform target)
    {
        // Play turret shooting sound
        SoundManager.Play("Turret", gameObject);
        
        Vector3 spawnPosition = originPoint != null ? originPoint.position : playerTransform.position + spawnOffset;
        
        // Aim at the target's center position using NavMeshAgent height
        AppleEnemy apple = target.GetComponentInParent<AppleEnemy>();
        Vector3 targetPosition = apple != null ? GetAppleCenterPosition(apple) : target.position + Vector3.up * 0.5f;
        
        Vector3 baseDirection = (targetPosition - spawnPosition).normalized;
        
        // Shoot multiple projectiles if projectile count > 1
        if (currentProjectileCount == 1)
        {
            // Single projectile - shoot straight at target
            SpawnProjectile(spawnPosition, baseDirection);
        }
        else
        {
            // Multiple projectiles - spread them out
            float totalSpread = spreadAngle * (currentProjectileCount - 1);
            float startAngle = -totalSpread / 2f;
            
            for (int i = 0; i < currentProjectileCount; i++)
            {
                float angle = startAngle + (spreadAngle * i);
                Vector3 spreadDirection = Quaternion.Euler(0, angle, 0) * baseDirection;
                SpawnProjectile(spawnPosition, spreadDirection);
            }
        }
    }
    
    private void SpawnProjectile(Vector3 spawnPosition, Vector3 direction)
    {
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.LookRotation(direction));
        
        TurretProjectile projectileScript = projectile.GetComponent<TurretProjectile>();
        if (projectileScript == null)
        {
            projectileScript = projectile.AddComponent<TurretProjectile>();
        }
        
        // Use current level-scaled damage
        projectileScript.Initialize(direction, projectileSpeed, currentProjectileDamage, projectileLifetime);
    }

    private GameObject CreateDefaultProjectile()
    {
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "TurretProjectile";
        projectile.transform.localScale = Vector3.one * 0.3f;
        
        // Set color
        Renderer renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.yellow;
        }
        
        // Remove collider (projectile script will handle its own)
        Collider collider = projectile.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
        
        projectile.SetActive(false);
        return projectile;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 position = transform.parent != null ? transform.parent.position : transform.position;
        
        // Show effective range with multiplier in play mode, base range in editor
        float displayRange = Application.isPlaying ? GetEffectiveDetectionRange() : detectionRange;
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(position, displayRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position + spawnOffset, 0.2f);
    }
}