using UnityEngine;

public class InstantDeathTrigger : MonoBehaviour
{
    [Header("Particle Settings")]
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private Vector3 particleOffset = Vector3.zero;
    
    [Header("References")]
    [SerializeField] private SnakeHealth snakeHealth;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private Transform raycastOrigin;
    
    [Header("Raycast Settings")]
    [SerializeField] private LayerMask snakeLayer;
    [SerializeField] private float rayDistance = 10f;
    
    [Header("Immunity Settings")]
    [SerializeField] private float postDeathImmunityDuration = 2f;
    [SerializeField] private float postRespawnImmunityDuration = 1f;
    
    private bool hasImmunity = false;
    private float immunityTimer = 0f;
    private float currentImmunityDuration = 0f;

    private void Start()
    {
        if (snakeHealth == null)
        {
            snakeHealth = FindFirstObjectByType<SnakeHealth>();
        }
        
        if (waveManager == null)
        {
            waveManager = FindFirstObjectByType<WaveManager>();
        }
        
        if (raycastOrigin == null)
        {
            raycastOrigin = transform;
        }
        
        // Subscribe to death event to grant immunity
        if (snakeHealth != null)
        {
            snakeHealth.onDeath.AddListener(OnSnakeDeath);
        }
        
        // Subscribe to wave start to grant immunity on respawn
        if (waveManager != null)
        {
            waveManager.OnWaveStarted.AddListener(OnWaveStarted);
        }
        
        // Grant initial immunity at start
        GrantImmunity(postRespawnImmunityDuration);
    }
    
    private void OnDestroy()
    {
        if (snakeHealth != null)
        {
            snakeHealth.onDeath.RemoveListener(OnSnakeDeath);
        }
        
        if (waveManager != null)
        {
            waveManager.OnWaveStarted.RemoveListener(OnWaveStarted);
        }
    }
    
    private void OnSnakeDeath()
    {
        // Grant immunity when snake dies
        GrantImmunity(postDeathImmunityDuration);
        Debug.Log($"[InstantDeathTrigger] Death immunity granted for {postDeathImmunityDuration}s");
    }
    
    private void OnWaveStarted(int waveIndex)
    {
        // Grant immunity when wave starts (after respawn)
        GrantImmunity(postRespawnImmunityDuration);
        Debug.Log($"[InstantDeathTrigger] Respawn immunity granted for {postRespawnImmunityDuration}s");
    }

    private void FixedUpdate()
    {
        // Update immunity timer
        if (hasImmunity)
        {
            immunityTimer += Time.fixedDeltaTime;
            
            if (immunityTimer >= currentImmunityDuration)
            {
                // Check if still hitting snake
                if (!IsHittingSnake())
                {
                    hasImmunity = false;
                    immunityTimer = 0f;
                    Debug.Log("[InstantDeathTrigger] Immunity expired");
                }
                else
                {
                    // Still hitting snake, keep immunity until they move away
                    Debug.Log("[InstantDeathTrigger] Immunity duration reached but still hitting snake");
                }
            }
            
            return; // Skip damage check while immune
        }
        
        // Additional safety: don't check during death/respawn
        if (snakeHealth != null && !snakeHealth.IsAlive())
        {
            return;
        }
        
        if (waveManager != null && !waveManager.IsWaveActive())
        {
            return;
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
        Debug.Log("[InstantDeathTrigger] Triggering instant death!");
        
        if (particlePrefab != null)
        {
            Vector3 spawnPosition = hitPoint + particleOffset;
            Instantiate(particlePrefab, spawnPosition, Quaternion.identity);
        }
        
        if (snakeHealth != null)
        {
            snakeHealth.TakeDamage(snakeHealth.GetMaxHealth());
        }
        
        // Immunity will be granted by OnSnakeDeath() callback
    }
    
    private void GrantImmunity(float duration)
    {
        hasImmunity = true;
        immunityTimer = 0f;
        currentImmunityDuration = duration;
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
}