using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LungeAttack : Attack
{
    [Header("Lunge Settings")]
    [SerializeField] private float lungeForce = 30f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float hitRadius = 1.5f;
    [SerializeField] private float hitRange = 2.5f;
    
    [Header("Animation")]
    [SerializeField] private string attackTrigger = "Lunge";
    
    [Header("Explosion Effect (uses custom stats)")]
    [Tooltip("Particle effect for explosion (left side)")]
    [SerializeField] private GameObject explosionParticlesPrefab;
    [Tooltip("Secondary particle effect for explosion (optional)")]
    [SerializeField] private GameObject explosionParticlesPrefab2;
    
    [Header("Explosion Defaults (overridden by custom stats)")]
    [SerializeField] private float defaultExplosionRadius = 10f;
    [SerializeField] private float defaultExplosionDamage = 25f;
    [SerializeField] private float minDeathDelay = 0.1f;
    [SerializeField] private float maxDeathDelay = 1.0f;
    
    [SerializeField] private Transform orientation;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private SnakeBody snakeBody;
    [SerializeField] private AttackManager attackManager;
    
    private HashSet<AppleEnemy> hitEnemies = new HashSet<AppleEnemy>();

    // Custom stat names for explosion
    private const string STAT_EXPLOSION_ENABLED = "explosionEnabled";
    private const string STAT_EXPLOSION_RADIUS = "explosionRadius";
    private const string STAT_EXPLOSION_DAMAGE = "explosionDamage";

    private void Awake()
    {
        attackType = AttackType.Burst;
    }

    protected override void OnActivate()
    {
        ExecuteLunge();
    }

    private void ExecuteLunge()
    {
        if (orientation == null || playerMovement == null) return;

        if (attackManager != null)
        {
            attackManager.TriggerAnimation(attackTrigger);
        }

        playerMovement.ApplyLunge(lungeForce);
        
        if (snakeBody != null)
        {
            snakeBody.ApplyForceToBody(orientation.forward, lungeForce);
        }
        
        hitEnemies.Clear();
        
        Collider[] hitColliders = Physics.OverlapSphere(
            orientation.position + orientation.forward * (hitRange * 0.5f),
            hitRadius,
            enemyLayer
        );
        
        foreach (Collider col in hitColliders)
        {
            AppleEnemy apple = col.GetComponentInParent<AppleEnemy>();
            
            if (apple != null && !hitEnemies.Contains(apple))
            {
                hitEnemies.Add(apple);
                apple.TakeDamage(damage);
                Debug.Log($"Lunge hit {apple.name} for {damage} damage!");
            }
        }
        
        if (hitEnemies.Count > 0)
        {
            Debug.Log($"Lunge attack hit {hitEnemies.Count} enemies!");
        }
        
        // Check if explosion is enabled via custom stat
        if (IsExplosionEnabled())
        {
            TriggerExplosion();
        }
    }
    
    /// <summary>
    /// Checks if explosion is enabled for the current level
    /// </summary>
    private bool IsExplosionEnabled()
    {
        if (upgradeData == null) return false;
        
        // explosionEnabled > 0 means explosion is active
        float explosionEnabled = upgradeData.GetCustomStat(currentLevel, STAT_EXPLOSION_ENABLED, 0f);
        return explosionEnabled > 0f;
    }
    
    /// <summary>
    /// Gets the explosion radius from custom stats or default
    /// </summary>
    private float GetExplosionRadius()
    {
        if (upgradeData == null) return defaultExplosionRadius;
        return upgradeData.GetCustomStat(currentLevel, STAT_EXPLOSION_RADIUS, defaultExplosionRadius);
    }
    
    /// <summary>
    /// Gets the explosion damage from custom stats or default
    /// </summary>
    private float GetExplosionDamage()
    {
        if (upgradeData == null) return defaultExplosionDamage;
        return upgradeData.GetCustomStat(currentLevel, STAT_EXPLOSION_DAMAGE, defaultExplosionDamage);
    }
    
    /// <summary>
    /// Triggers an explosion at the lunge impact point, similar to bomb explosion
    /// </summary>
    private void TriggerExplosion()
    {
        Vector3 explosionCenter = orientation.position + orientation.forward * (hitRange * 0.5f);
        float explosionRadius = GetExplosionRadius();
        float explosionDamage = GetExplosionDamage();
        
        // Spawn particle effects at explosion center
        if (explosionParticlesPrefab != null)
        {
            Instantiate(explosionParticlesPrefab, explosionCenter, Quaternion.identity);
        }
        
        if (explosionParticlesPrefab2 != null)
        {
            Instantiate(explosionParticlesPrefab2, explosionCenter, Quaternion.identity);
        }
        
        // Find all apples in explosion radius
        Collider[] colliders = Physics.OverlapSphere(explosionCenter, explosionRadius, enemyLayer);
        List<AppleEnemy> applesInRange = new List<AppleEnemy>();
        
        foreach (Collider col in colliders)
        {
            AppleEnemy apple = col.GetComponentInParent<AppleEnemy>();
            if (apple != null && !applesInRange.Contains(apple))
            {
                // Don't double-damage enemies already hit by the lunge itself
                if (!hitEnemies.Contains(apple))
                {
                    applesInRange.Add(apple);
                }
            }
        }
        
        // Damage apples with scaled delays based on distance (like bomb)
        foreach (AppleEnemy apple in applesInRange)
        {
            float distance = Vector3.Distance(explosionCenter, apple.transform.position);
            float normalizedDistance = Mathf.Clamp01(distance / explosionRadius);
            
            // Closest apples take damage with minDelay, furthest with maxDelay
            float delay = Mathf.Lerp(minDeathDelay, maxDeathDelay, normalizedDistance);
            
            StartCoroutine(DamageAppleAfterDelay(apple, explosionDamage, delay));
        }
        
        Debug.Log($"Lunge explosion! Damaging {applesInRange.Count} additional enemies in radius {explosionRadius}");
    }
    
    private IEnumerator DamageAppleAfterDelay(AppleEnemy apple, float explosionDamage, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (apple != null)
        {
            apple.TakeDamage(explosionDamage);
            Debug.Log($"Lunge explosion hit {apple.name} for {explosionDamage} damage!");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (orientation == null) return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(orientation.position + orientation.forward * (hitRange * 0.5f), hitRadius);
        Gizmos.DrawLine(orientation.position, orientation.position + orientation.forward * hitRange);
        
        // Draw explosion radius if enabled
        if (IsExplosionEnabled())
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(orientation.position + orientation.forward * (hitRange * 0.5f), GetExplosionRadius());
        }
    }
}
