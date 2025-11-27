using UnityEngine;

public class TurretAbility : BaseAbility
{
    [Header("Turret Settings")]
    [SerializeField] private float shootInterval = 1.5f;
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private GameObject turretBody;
    [SerializeField] private Transform originPoint;
    
    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float projectileDamage = 25f;
    [SerializeField] private float projectileLifetime = 5f;
    
    [Header("Spawn Settings")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 1f, 0);
    
    private float shootTimer = 0f;
    private Transform playerTransform;
    private AppleEnemy cachedNearestApple;
    private float targetSearchTimer = 0f;

    private void Start()
    {
        playerTransform = transform.parent != null ? transform.parent : transform;
        
        // Create default projectile if none assigned
        if (projectilePrefab == null)
        {
            projectilePrefab = CreateDefaultProjectile();
        }
    }

    private void Update()
    {
        // Periodically search for nearest apple (performance optimization)
        targetSearchTimer += Time.deltaTime;
        if (targetSearchTimer >= (shootInterval/4))
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
        
        if (shootTimer >= shootInterval)
        {
            TryShoot();
            shootTimer = 0f;
        }
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
        
        projectileScript.Initialize(direction, projectileSpeed, projectileDamage, projectileLifetime);
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