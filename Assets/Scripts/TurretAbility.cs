using UnityEngine;

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
    
    [Header("Level Progression")]
    [SerializeField] private float damageIncreasePerLevel = 10f;
    [SerializeField] private float intervalDecreasePerLevel = 0.2f;
    
    private float shootTimer = 0f;
    private Transform playerTransform;
    private AppleEnemy cachedNearestApple;
    private float targetSearchTimer = 0f;
    
    // Dynamic values that scale with level
    private float currentShootInterval;
    private float currentProjectileDamage;

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
        
        Debug.Log($"TurretAbility: Level {currentLevel} - Damage: {currentProjectileDamage}, Fire Rate: {currentShootInterval:F2}s");
    }
    
    private void UpdateStatsForLevel()
    {
        // Increase damage with each level
        currentProjectileDamage = baseProjectileDamage + (damageIncreasePerLevel * (currentLevel - 1));
        
        // Decrease interval (faster firing) with each level
        currentShootInterval = baseShootInterval - (intervalDecreasePerLevel * (currentLevel - 1));
        
        // Clamp to reasonable minimum (don't go below 0.5s interval)
        currentShootInterval = Mathf.Max(currentShootInterval, 0.5f);
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
        AppleEnemy[] apples = FindObjectsByType<AppleEnemy>(FindObjectsSortMode.None);
        
        if (apples.Length == 0) return null;
        
        AppleEnemy nearest = null;
        float nearestDistance = detectionRange;
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

    private void ShootAtTarget(Transform target)
    {
        Vector3 spawnPosition = originPoint != null ? originPoint.position : playerTransform.position + spawnOffset;
        
        // Aim at the target's center position using NavMeshAgent height
        AppleEnemy apple = target.GetComponentInParent<AppleEnemy>();
        Vector3 targetPosition = apple != null ? GetAppleCenterPosition(apple) : target.position + Vector3.up * 0.5f;
        
        Vector3 direction = (targetPosition - spawnPosition).normalized;
        
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
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(position, detectionRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position + spawnOffset, 0.2f);
    }
}