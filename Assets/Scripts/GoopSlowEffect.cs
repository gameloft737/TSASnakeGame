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
        Debug.Log("========== GOOP SLOW EFFECT AWAKE CALLED ==========");
        Debug.Log("GoopSlowEffect: Starting on " + (transform.parent != null ? transform.parent.name : "NO PARENT"));
        
        // Get the NavMeshAgent from parent (AppleEnemy)
        agent = GetComponentInParent<NavMeshAgent>();
        
        if (agent == null)
        {
            Debug.LogWarning("GoopSlowEffect: No NavMeshAgent found on parent!");
            Destroy(gameObject);
            return;
        }
        
        Debug.Log("GoopSlowEffect: Found NavMeshAgent with speed: " + agent.speed);
        
        // Store original speed
        originalSpeed = agent.speed;
        
        // Apply slow effect
        ApplySlow();
        
        
        effectTimer = effectDuration;
        lastRefreshTime = Time.time;
    }
    
    private void Start()
    {
        Debug.Log("========== GOOP SLOW EFFECT START CALLED ==========");
    }
    
    private void Update()
    {
        if (!isActive) return;
        
        // Count down the timer
        effectTimer -= Time.deltaTime;
        
        if (effectTimer <= 0f)
        {
            Debug.Log("GoopSlowEffect: Effect timer expired, removing slow");
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
            Debug.Log($"GoopSlowEffect: Applied slow. Original speed: {originalSpeed}, New speed: {newSpeed}");
        }
    }
    
    private void RemoveSlow()
    {
        if (agent != null && isActive)
        {
            agent.speed = originalSpeed;
            isActive = false;
            Debug.Log("GoopSlowEffect: Restored original speed: " + originalSpeed);
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
            Debug.Log("GoopSlowEffect: Effect refreshed");
            
            // Reapply slow in case it was removed
            if (!isActive)
            {
                ApplySlow();
            }
        }
    }
    
    private void OnDestroy()
    {
        Debug.Log("GoopSlowEffect: OnDestroy called");
        
        // Make sure to restore speed when destroyed
        if (agent != null && isActive)
        {
            agent.speed = originalSpeed;
        }
        
    }
}