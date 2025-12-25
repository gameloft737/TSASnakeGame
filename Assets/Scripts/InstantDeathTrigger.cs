using UnityEngine;

public class InstantDeathTrigger : MonoBehaviour
{
    [Header("Particle Settings")]
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private Vector3 particleOffset = Vector3.zero;
    
    [Header("References")]
    [SerializeField] private SnakeHealth snakeHealth;
    [SerializeField] private Transform raycastOrigin;
    
    [Header("Raycast Settings")]
    [SerializeField] private LayerMask snakeLayer;
    [SerializeField] private float rayDistance = 10f;
    
    [Header("Immunity Settings")]
    [SerializeField] private float immunityDuration = 2f;
    
    private bool hasImmunity = false;
    private float immunityTimer = 0f;

    private void Start()
    {
        if (snakeHealth == null)
        {
            snakeHealth = FindFirstObjectByType<SnakeHealth>();
        }
        
        if (raycastOrigin == null)
        {
            raycastOrigin = transform;
        }
    }

    private void FixedUpdate()
    {
        // Update immunity timer
        if (hasImmunity)
        {
            immunityTimer += Time.fixedDeltaTime;
            
            if (immunityTimer >= immunityDuration)
            {
                // Check if still hitting snake
                if (!IsHittingSnake())
                {
                    hasImmunity = false;
                    immunityTimer = 0f;
                }
            }
            
            return; // Skip damage check while immune
        }
        
        // Check for collision and trigger death
        if (raycastOrigin != null)
        {
            Ray ray = new Ray(raycastOrigin.position, raycastOrigin.forward);
            
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, snakeLayer))
            {
                TriggerDeath(hit.point);
            }
        }
    }
    
    private bool IsHittingSnake()
    {
        if (raycastOrigin == null) return false;
        
        Ray ray = new Ray(raycastOrigin.position, raycastOrigin.forward);
        return Physics.Raycast(ray, rayDistance, snakeLayer);
    }
    
    private void TriggerDeath(Vector3 hitPoint)
    {
        if (particlePrefab != null)
        {
            Vector3 spawnPosition = hitPoint + particleOffset;
            Instantiate(particlePrefab, spawnPosition, Quaternion.identity);
        }
        
        if (snakeHealth != null)
        {
            snakeHealth.TakeDamage(snakeHealth.GetMaxHealth());
        }
        
        // Start immunity after death
        hasImmunity = true;
        immunityTimer = 0f;
    }
    
    private void OnDrawGizmos()
    {
        if (raycastOrigin == null) return;
        
        Gizmos.color = hasImmunity ? Color.green : Color.yellow;
        Vector3 start = raycastOrigin.position;
        Vector3 end = start + raycastOrigin.forward * rayDistance;
        
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(start, 0.1f);
        Gizmos.DrawWireSphere(end, 0.2f);
    }
    
    public void OnNewRound()
    {
        // Grant immunity at the start of a new round
        hasImmunity = true;
        immunityTimer = 0f;
    }
}