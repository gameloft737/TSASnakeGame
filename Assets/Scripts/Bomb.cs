using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Bomb that explodes when touched by an apple, killing all apples in radius
/// Has an arming delay to prevent immediate explosions after placement
/// </summary>
public class Bomb : MonoBehaviour
{
    [Header("Explosion Settings")]
    [SerializeField] private float explosionRadius = 10f;
    [SerializeField] private float minDeathDelay = 0.1f;
    [SerializeField] private float maxDeathDelay = 1.0f;
    [SerializeField] private GameObject bombObj;
    
    [Header("Particle Effects")]
    [SerializeField] private GameObject leftParticlesPrefab;
    [SerializeField] private GameObject rightParticlesPrefab;
    [SerializeField] private Transform leftSpawnPoint;
    [SerializeField] private Transform rightSpawnPoint;
    
    [Header("Detection")]
    [SerializeField] private LayerMask appleLayer;
    [SerializeField] private float detectionCheckInterval = 0.1f; // How often to check for nearby apples
    [SerializeField] private float triggerRadius = 2f; // Radius to detect apples for triggering explosion
    
    public float damage;
    private bool hasExploded = false;
    private bool isArmed = false; // Bomb can't explode until armed
    private Collider bombCollider;

    private void Start()
    {
        bombCollider = GetComponent<Collider>();
        
        // Ensure we have a collider for trigger detection
        if (bombCollider == null)
        {
            // Try to find collider in children
            bombCollider = GetComponentInChildren<Collider>();
        }
        
        // Start proximity detection as a backup to OnTriggerEnter
        StartCoroutine(ProximityDetectionRoutine());
    }

    /// <summary>
    /// Arms the bomb after a delay. Bomb can't explode until armed.
    /// </summary>
    public void ArmBomb(float delay)
    {
        StartCoroutine(ArmAfterDelay(delay));
    }

    private IEnumerator ArmAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isArmed = true;
        Debug.Log($"Bomb armed at {transform.position}");
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if bomb is armed before allowing explosion
        if (!isArmed) return;
        
        // Check if an apple touched the bomb
        if (!hasExploded && other.GetComponentInParent<AppleEnemy>() != null)
        {
            Debug.Log("Bomb triggered by apple via OnTriggerEnter!");
            Explode();
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Check if bomb is armed before allowing explosion
        if (!isArmed) return;
        
        // Check if an apple touched the bomb (backup for non-trigger colliders)
        if (!hasExploded && collision.gameObject.GetComponentInParent<AppleEnemy>() != null)
        {
            Debug.Log("Bomb triggered by apple via OnCollisionEnter!");
            Explode();
        }
    }
    
    /// <summary>
    /// Backup detection method using proximity checks
    /// This ensures bombs explode even if trigger/collision detection fails
    /// </summary>
    private IEnumerator ProximityDetectionRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(detectionCheckInterval);
        
        while (!hasExploded)
        {
            // Only check proximity if bomb is armed
            if (isArmed)
            {
                // Check for apples within trigger radius
                Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, triggerRadius, appleLayer);
                
                if (nearbyColliders.Length > 0)
                {
                    foreach (Collider col in nearbyColliders)
                    {
                        AppleEnemy apple = col.GetComponentInParent<AppleEnemy>();
                        if (apple != null)
                        {
                            Debug.Log($"Bomb triggered by apple via proximity detection! Apple: {apple.gameObject.name}");
                            Explode();
                            yield break;
                        }
                    }
                }
            }
            
            yield return wait;
        }
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        
        // Hide the bomb visual immediately, but don't destroy the GameObject yet
        // We need the GameObject alive to run coroutines
        if (bombObj != null)
        {
            bombObj.SetActive(false);
        }
        
        // Instantiate particle effects at specified points
        if (leftParticlesPrefab != null && leftSpawnPoint != null)
        {
            Instantiate(leftParticlesPrefab, leftSpawnPoint.position, leftSpawnPoint.rotation);
        }

        if (rightParticlesPrefab != null && rightSpawnPoint != null)
        {
            Instantiate(rightParticlesPrefab, rightSpawnPoint.position, rightSpawnPoint.rotation);
        }

        // Find all apples in explosion radius
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, appleLayer);
        List<AppleEnemy> applesInRange = new List<AppleEnemy>();

        foreach (Collider col in colliders)
        {
            AppleEnemy apple = col.GetComponentInParent<AppleEnemy>();
            if (apple != null && !applesInRange.Contains(apple))
            {
                applesInRange.Add(apple);
            }
        }

        // Kill apples with scaled delays based on distance
        foreach (AppleEnemy apple in applesInRange)
        {
            float distance = Vector3.Distance(transform.position, apple.transform.position);
            float normalizedDistance = Mathf.Clamp01(distance / explosionRadius);
            
            // Closest apples die with minDelay, furthest with maxDelay
            float delay = Mathf.Lerp(minDeathDelay, maxDeathDelay, normalizedDistance);
            
            StartCoroutine(KillAppleAfterDelay(apple, delay));
        }

        Debug.Log($"Bomb exploded! Killing {applesInRange.Count} apples in radius");

        // Destroy the bomb AFTER the longest delay + a small buffer
        Destroy(gameObject, maxDeathDelay + 0.1f);
    }
    
    private IEnumerator KillAppleAfterDelay(AppleEnemy apple, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (apple != null)
        {
            apple.TakeDamage(damage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw explosion radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
        
        // Draw trigger radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
        
        // Draw armed/unarmed state
        if (Application.isPlaying)
        {
            Gizmos.color = isArmed ? Color.green : Color.grey;
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
    }
}