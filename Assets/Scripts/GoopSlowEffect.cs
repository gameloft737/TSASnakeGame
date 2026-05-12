using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Slows down an AppleEnemy when attached as a child
/// </summary>
public class GoopSlowEffect : MonoBehaviour
{
    [Header("Slow Settings")]
    public float speedMultiplier = 0.5f; // 50% speed
    [SerializeField] private float effectDuration = 3f; // Lasts 3 seconds after leaving goop
    [SerializeField] private float contactRefreshRate = 0.2f; // How often to refresh while in contact
    
    [Header("Visual Feedback")]
    
    private NavMeshAgent agent;
    private float originalSpeed;
    private float effectTimer;
    private bool isActive = false;
    private float lastRefreshTime;

    private void Awake()
    {
        // Get the NavMeshAgent from parent (AppleEnemy)
        agent = GetComponentInParent<NavMeshAgent>();
        
        if (agent == null)
        {
            #if UNITY_EDITOR
            Debug.LogWarning("GoopSlowEffect: No NavMeshAgent found on parent!");
            #endif
            Destroy(gameObject);
            return;
        }
        
        // Store original speed
        originalSpeed = agent.speed;
        
        // Apply slow effect
        ApplySlow();
        
        effectTimer = effectDuration;
        lastRefreshTime = Time.time;
    }
    
    private void Update()
    {
        if (!isActive) return;
        
        // Count down the timer
        effectTimer -= Time.deltaTime;
        
        if (effectTimer <= 0f)
        {
            RemoveSlow();
        }
    }
    
    private void ApplySlow()
    {
        if (agent != null && !isActive)
        {
            float newSpeed = originalSpeed * speedMultiplier;
            agent.speed = newSpeed;
            isActive = true;
        }
    }
    
    private void RemoveSlow()
    {
        if (agent != null && isActive)
        {
            agent.speed = originalSpeed;
            isActive = false;
        }
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Refresh the effect duration (called when particle hits again)
    /// </summary>
    public void RefreshEffect()
    {
        // Only refresh if enough time has passed to prevent spam
        if (Time.time - lastRefreshTime >= contactRefreshRate)
        {
            effectTimer = effectDuration;
            lastRefreshTime = Time.time;
            
            // Reapply slow in case it was removed
            if (!isActive)
            {
                ApplySlow();
            }
        }
    }
    
    private void OnDestroy()
    {
        // Make sure to restore speed when destroyed
        if (agent != null && isActive)
        {
            agent.speed = originalSpeed;
        }
    }
}
