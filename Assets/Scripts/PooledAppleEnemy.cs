using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Extension component that makes AppleEnemy work with object pooling.
/// Add this component alongside AppleEnemy on your enemy prefabs.
/// 
/// This demonstrates how to properly implement IPooledObject for complex objects.
/// </summary>
[RequireComponent(typeof(AppleEnemy))]
public class PooledAppleEnemy : MonoBehaviour, IPooledObject
{
    private AppleEnemy appleEnemy;
    private NavMeshAgent agent;
    private Collider[] colliders;
    private Renderer[] renderers;
    
    // Store initial state for reset
    private Vector3 initialScale;
    private bool wasInitialized = false;
    
    private void Awake()
    {
        CacheComponents();
    }
    
    private void CacheComponents()
    {
        if (wasInitialized) return;
        
        appleEnemy = GetComponent<AppleEnemy>();
        agent = GetComponent<NavMeshAgent>();
        colliders = GetComponentsInChildren<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
        initialScale = transform.localScale;
        wasInitialized = true;
    }
    
    /// <summary>
    /// Called when this object is spawned from the pool.
    /// Resets all state to make it ready for use.
    /// </summary>
    public void OnSpawnFromPool()
    {
        CacheComponents();
        
        // Re-enable all components
        enabled = true;
        
        // Re-enable colliders
        foreach (var col in colliders)
        {
            if (col != null) col.enabled = true;
        }
        
        // Re-enable renderers
        foreach (var rend in renderers)
        {
            if (rend != null) rend.enabled = true;
        }
        
        // Reset transform
        transform.localScale = initialScale;
        
        // Re-enable NavMeshAgent
        if (agent != null)
        {
            agent.enabled = true;
            // Warp to current position to properly place on NavMesh
            agent.Warp(transform.position);
        }
        
        // CRITICAL: Call AppleEnemy's OnSpawnFromPool to reset its state
        // This is essential because ObjectPool.Spawn only calls GetComponent<IPooledObject>()
        // which returns the first IPooledObject it finds (this component), not AppleEnemy
        if (appleEnemy != null)
        {
            appleEnemy.OnSpawnFromPool();
        }
        
        // The AppleEnemy.Initialize() will be called by EnemySpawner
        // which sets up the snake references and health
    }
    
    /// <summary>
    /// Called when this object is returned to the pool.
    /// Cleans up state and disables components.
    /// </summary>
    public void OnReturnToPool()
    {
        // CRITICAL: Call AppleEnemy's OnReturnToPool first to clean up its state
        if (appleEnemy != null)
        {
            appleEnemy.OnReturnToPool();
        }
        
        // Stop all coroutines on this object
        StopAllCoroutines();
        
        // Disable NavMeshAgent first (before disabling colliders)
        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            agent.enabled = false;
        }
        
        // Disable colliders
        foreach (var col in colliders)
        {
            if (col != null) col.enabled = false;
        }
        
        // Optionally disable renderers (saves some GPU work)
        foreach (var rend in renderers)
        {
            if (rend != null) rend.enabled = false;
        }
        
        // Reset parent (pool will handle this, but just in case)
        // transform.SetParent(null);
    }
}