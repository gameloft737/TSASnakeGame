using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicContactAttack : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float mouthOpenDelay = 0.2f;
    [SerializeField] private float mouthCloseDelay = 0.3f;

    [Header("Detection")]
    [SerializeField] private float nearbyRange = 3f;
    [SerializeField] private LayerMask enemyLayer;
    
    [Header("Backup Detection")]
    [Tooltip("Range for backup distance-based attack when physics callbacks fail")]
    [SerializeField] private float backupAttackRange = 1.5f;
    [Tooltip("How often to check for enemies using backup system (seconds)")]
    [SerializeField] private float backupCheckInterval = 0.1f;

    [Header("Damage")]
    [SerializeField] private float damage = 100f; // Damage dealt per bite
    [SerializeField] private bool instantKill = false; // If true, bypasses health and kills instantly

    [Header("Animation")]
    [SerializeField] private AttackManager attackManager;
    [SerializeField] private SnakeBody snakeBody;
    [SerializeField] private string mouthOpenBool = "mouthOpen";

    private bool mouthOpen;
    private bool waitingToOpen;
    private bool waitingToClose;
    private float lastEnemySeenTime;
    private AppleEnemy trackedEnemy; // Track the enemy that triggered opening
    
    // Backup attack system
    private float lastBackupCheckTime;
    private float backupAttackRangeSqr;
    private HashSet<AppleEnemy> recentlyDamagedEnemies = new HashSet<AppleEnemy>();
    private float damageCooldown = 0.15f; // Prevent hitting same enemy too fast
    private Dictionary<AppleEnemy, float> enemyDamageTimestamps = new Dictionary<AppleEnemy, float>();
    
    private void Start()
    {
        backupAttackRangeSqr = backupAttackRange * backupAttackRange;
    }

    private void OnTriggerEnter(Collider other)
    {
        AppleEnemy enemy = other.GetComponentInParent<AppleEnemy>();
        if (enemy == null)
        {
            return;
        }
        
        if (enemy.isMetal)
        {
            Debug.Log($"[BasicContactAttack] OnTriggerEnter: {enemy.name} is metal, ignoring");
            return;
        }

        Debug.Log($"[BasicContactAttack] OnTriggerEnter: {enemy.name}, mouthOpen={mouthOpen}, waitingToOpen={waitingToOpen}");

        if (!mouthOpen && !waitingToOpen)
        {
            trackedEnemy = enemy; // Store reference to this enemy
            waitingToOpen = true;
            StartCoroutine(OpenAfterDelay());
        }
    }

    private IEnumerator OpenAfterDelay()
    {
        yield return new WaitForSeconds(mouthOpenDelay);

        waitingToOpen = false;
        mouthOpen = true;
        lastEnemySeenTime = Time.time;

        if (attackManager != null)
            attackManager.SetBool(mouthOpenBool, true);

        // Damage or kill the tracked enemy even if it left the trigger
        if (trackedEnemy != null)
        {
            snakeBody.TriggerSwallowAnimation();
            if (instantKill)
            {
                trackedEnemy.Die();
                SoundManager.Play("Bite", gameObject);
            }
            else
            {
                trackedEnemy.TakeDamage(damage);
                SoundManager.Play("Bite", gameObject);
            }
            trackedEnemy = null; // Clear reference after attacking
        }
    }

    private void OnTriggerStay(Collider other)
    {
        AppleEnemy enemy = other.GetComponentInParent<AppleEnemy>();
        if (enemy == null || enemy.isMetal) return;

        if (mouthOpen)
        {
            Debug.Log($"[BasicContactAttack] OnTriggerStay: Attacking {enemy.name}, instantKill={instantKill}");
            snakeBody.TriggerSwallowAnimation();
            if (instantKill)
            {
                enemy.Die();
                SoundManager.Play("Bite", gameObject);
            }
            else
            {
                enemy.TakeDamage(damage);
                SoundManager.Play("Bite", gameObject);
            }
        }
    }

    private void Update()
    {
        float currentTime = Time.time;
        
        // Backup attack system - runs independently of physics callbacks
        // This ensures enemies get damaged even when OnTriggerStay fails with many colliders
        if (currentTime - lastBackupCheckTime >= backupCheckInterval)
        {
            lastBackupCheckTime = currentTime;
            PerformBackupAttackCheck(currentTime);
        }
        
        // Clean up old damage timestamps to prevent memory growth
        if (Time.frameCount % 60 == 0)
        {
            CleanupDamageTimestamps(currentTime);
        }
        
        if (!mouthOpen || waitingToClose) return;

        bool enemyNearby = false;

        Collider[] hits = Physics.OverlapSphere(transform.position, nearbyRange, enemyLayer);

        foreach (var h in hits)
        {
            AppleEnemy e = h.GetComponentInParent<AppleEnemy>();
            if (e != null && !e.isMetal)
            {
                enemyNearby = true;
                lastEnemySeenTime = currentTime;
                break;
            }
        }

        // Check if enough time has passed since last enemy was seen
        if (!enemyNearby && currentTime >= lastEnemySeenTime + mouthCloseDelay)
        {
            waitingToClose = true;
            StartCoroutine(CloseMouth());
        }
    }
    
    /// <summary>
    /// Backup attack system that uses distance checks instead of physics callbacks.
    /// This ensures enemies get damaged even when Unity's OnTriggerStay fails
    /// due to too many colliders in the scene.
    /// </summary>
    private void PerformBackupAttackCheck(float currentTime)
    {
        // Only attack when mouth is open
        if (!mouthOpen) return;
        
        Vector3 myPos = transform.position;
        var allEnemies = AppleEnemy.GetAllActiveEnemies();
        int enemyCount = allEnemies.Count;
        
        for (int i = 0; i < enemyCount; i++)
        {
            AppleEnemy enemy = allEnemies[i];
            if (enemy == null || enemy.isMetal || enemy.IsAlly()) continue;
            
            // Check distance
            float distSqr = (enemy.transform.position - myPos).sqrMagnitude;
            if (distSqr > backupAttackRangeSqr) continue;
            
            // Check if we recently damaged this enemy (cooldown)
            if (enemyDamageTimestamps.TryGetValue(enemy, out float lastDamageTime))
            {
                if (currentTime - lastDamageTime < damageCooldown) continue;
            }
            
            // Attack the enemy
            snakeBody.TriggerSwallowAnimation();
            if (instantKill)
            {
                enemy.Die();
            }
            else
            {
                enemy.TakeDamage(damage);
            }
            SoundManager.Play("Bite", gameObject);
            
            // Record damage timestamp
            enemyDamageTimestamps[enemy] = currentTime;
            lastEnemySeenTime = currentTime;
        }
    }
    
    /// <summary>
    /// Cleans up old entries from the damage timestamps dictionary
    /// </summary>
    private void CleanupDamageTimestamps(float currentTime)
    {
        List<AppleEnemy> toRemove = null;
        
        foreach (var kvp in enemyDamageTimestamps)
        {
            // Remove entries older than 2 seconds or for null enemies
            if (kvp.Key == null || currentTime - kvp.Value > 2f)
            {
                if (toRemove == null) toRemove = new List<AppleEnemy>();
                toRemove.Add(kvp.Key);
            }
        }
        
        if (toRemove != null)
        {
            foreach (var enemy in toRemove)
            {
                enemyDamageTimestamps.Remove(enemy);
            }
        }
    }

    private IEnumerator CloseMouth()
    {
        yield return null;

        mouthOpen = false;
        waitingToClose = false;

        if (attackManager != null)
            attackManager.SetBool(mouthOpenBool, false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, nearbyRange);
    }
}