using UnityEngine;
using System.Collections;

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
    
    [Header("Reset Settings")]
    [SerializeField] private bool hasTriggered = false;
    [SerializeField] private bool canStartCounting = false;
    [SerializeField] private float resetDelay = 2f;
    
    private bool isHittingSnake = false;
    private float noHitTimer = 0f;

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
        if (hasTriggered)
        {
            CheckForReset();
            return;
        }
        
        if (raycastOrigin != null)
        {
            Ray ray = new Ray(raycastOrigin.position, raycastOrigin.forward);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, rayDistance, snakeLayer))
            {
                TriggerDeath(hit.point);
            }
        }
    }
    
    private void CheckForReset()
    {
        if (!canStartCounting) return;
        
        bool currentlyHitting = false;
        
        if (raycastOrigin != null)
        {
            Ray ray = new Ray(raycastOrigin.position, raycastOrigin.forward);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, rayDistance, snakeLayer))
            {
                currentlyHitting = true;
            }
        }
        
        if (currentlyHitting)
        {
            noHitTimer = 0f;
            isHittingSnake = true;
        }
        else
        {
            if (isHittingSnake)
            {
                noHitTimer += Time.fixedDeltaTime;
                
                if (noHitTimer >= resetDelay)
                {
                    hasTriggered = false;
                    isHittingSnake = false;
                    noHitTimer = 0f;
                    canStartCounting = false;
                }
            }
        }
    }
    
    private void TriggerDeath(Vector3 hitPoint)
    {
        hasTriggered = true;
        isHittingSnake = true;
        noHitTimer = 0f;
        
        if (particlePrefab != null)
        {
            Vector3 spawnPosition = hitPoint + particleOffset;
            Instantiate(particlePrefab, spawnPosition, Quaternion.identity);
        }
        
        if (snakeHealth != null)
        {
            snakeHealth.TakeDamage(snakeHealth.GetMaxHealth());
        }
    }
    
    private void OnDrawGizmos()
    {
        if (raycastOrigin == null) return;
        
        Gizmos.color = hasTriggered ? Color.red : Color.yellow;
        Vector3 start = raycastOrigin.position;
        Vector3 end = start + raycastOrigin.forward * rayDistance;
        
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(start, 0.1f);
        Gizmos.DrawWireSphere(end, 0.2f);
    }
    
    public void OnNewRound()
    {
        canStartCounting = true;
    }
}