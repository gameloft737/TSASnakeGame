using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Bomb that explodes when touched by an apple, killing all apples in radius
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
    public float damage;
    private bool hasExploded = false;

    private void OnTriggerEnter(Collider other)
    {
        // Check if an apple touched the bomb
        if (!hasExploded && other.GetComponentInParent<AppleEnemy>() != null)
        {
            Explode();
        }
    }

    private void Explode()
{
    if (hasExploded) return;
    hasExploded = true;
    Destroy(bombObj);
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
        if (apple != null)
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
            Debug.Log("ffffffffffffffffffffffffffffffff");
            apple.TakeDamage(damage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}