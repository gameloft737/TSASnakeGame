using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class AbilityDrop : MonoBehaviour
{
    [Header("Drop Settings")]
    [SerializeField] private float groundedCheckTime = 0.3f; // Faster grounding detection
    [SerializeField] private float stillThreshold = 0.15f; // Lower threshold for quicker detection
    [SerializeField] private float collectionRadius = 2.5f; // Slightly larger radius for reliable collection
    
    [Header("UI References")]
    [SerializeField] private Transform uiContainer;
    
    [Header("Visual Feedback")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;
    
    private Rigidbody rb;
    private SphereCollider sphereCollider;
    private bool isGrounded = false;
    private bool isCollected = false;
    private bool isDying = false;
    private float stillTimer = 0f;
    private Camera worldSpaceCamera;
    private Vector3 originalScale;
    private Transform playerTransform;
    private Animator animator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;
        
        // Get or add sphere collider for reliable collection
        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            sphereCollider = gameObject.AddComponent<SphereCollider>();
        }
        // Ensure collider is set as trigger for collection
        sphereCollider.isTrigger = true;
        sphereCollider.radius = collectionRadius;
        
        // Find and disable any animator to prevent animations
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        if (animator != null)
        {
            animator.enabled = false;
        }
        
        // Find the camera for world space UI
        AbilityCollector collector = FindFirstObjectByType<AbilityCollector>();
        if (collector != null)
        {
            AbilityManager abilityManager = collector.GetComponent<AbilityManager>();
            if (abilityManager != null)
            {
                worldSpaceCamera = abilityManager.GetWorldSpaceCamera();
            }
            
            // Cache player transform for continuous collection check
            playerTransform = collector.transform;
        }
        
        // Set up UI container
        if (uiContainer != null)
        {
            // Set camera for world space canvas
            Canvas canvas = uiContainer.GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && worldSpaceCamera != null)
            {
                canvas.worldCamera = worldSpaceCamera;
            }
            
            uiContainer.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isCollected || isDying) return;
        
        // Check if the drop has stopped moving (grounded)
        if (!isGrounded && rb != null)
        {
            float velocity = rb.linearVelocity.magnitude;
            
            if (velocity < stillThreshold)
            {
                stillTimer += Time.deltaTime;
                
                if (stillTimer >= groundedCheckTime)
                {
                    OnGrounded();
                }
            }
            else
            {
                stillTimer = 0f;
            }
        }
        
        // Continuous collection check - works even before fully grounded for better responsiveness
        if (!isCollected && !isDying && playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= collectionRadius && isGrounded)
            {
                // Try to collect via the AbilityCollector
                AbilityCollector collector = playerTransform.GetComponent<AbilityCollector>();
                if (collector != null)
                {
                    collector.TryCollectDrop(this);
                }
            }
        }
        
        // Pulse effect when grounded to make it more visible
        if (isGrounded && !isDying)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = originalScale * pulse;
        }
        
        // Drops now stay forever - no lifetime countdown
    }

    private void OnGrounded()
    {
        if (isGrounded) return; // Prevent multiple calls
        isGrounded = true;
        
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        
        // Show simple UI indicator if available (optional)
        if (uiContainer != null)
        {
            uiContainer.gameObject.SetActive(true);
        }
        
        Debug.Log($"[AbilityDrop] Grounded at {transform.position}");
    }

    public bool IsGrounded() => isGrounded;
    public bool IsCollected() => isCollected;
    
    // Allow collection even if not fully grounded - improves responsiveness
    public bool CanBeCollected() => !isCollected && !isDying;
    
    public void Collect()
    {
        if (isCollected) return;
        isCollected = true;
        isDying = true; // Prevent death from triggering
        
        // Reset scale immediately
        transform.localScale = originalScale;
        
        // Cancel any pending destruction
        CancelInvoke();
        StopAllCoroutines();
        
        // Hide UI immediately
        if (uiContainer != null)
        {
            uiContainer.gameObject.SetActive(false);
        }
        
        // Destroy immediately - no animation delay
        Destroy(gameObject);
        
        Debug.Log("[AbilityDrop] Collected!");
    }
    
    // Handle trigger-based collection as backup
    private void OnTriggerEnter(Collider other)
    {
        if (isCollected || isDying) return;
        if (!isGrounded) return;
        
        AbilityCollector collector = other.GetComponent<AbilityCollector>();
        if (collector != null)
        {
            collector.TryCollectDrop(this);
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (isCollected || isDying) return;
        if (!isGrounded) return;
        
        AbilityCollector collector = other.GetComponent<AbilityCollector>();
        if (collector != null)
        {
            collector.TryCollectDrop(this);
        }
    }
    
    private void OnDestroy()
    {
        // Clean up any remaining references
        CancelInvoke();
        StopAllCoroutines();
    }
}