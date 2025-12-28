using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AbilityDrop : MonoBehaviour
{
    [Header("Drop Settings")]
    [SerializeField] private float lifetimeAfterGround = 10f;
    [SerializeField] private float groundedCheckTime = 0.5f; // Reduced for faster grounding
    [SerializeField] private float stillThreshold = 0.2f; // Slightly higher threshold
    [SerializeField] private float collectionRadius = 2f; // Radius for continuous collection check
    
    [Header("UI References")]
    [SerializeField] private Transform uiContainer;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openBool = "IsOpen"; // Changed to bool for more reliable animation
    [SerializeField] private string openTrigger = "Open"; // Keep trigger as fallback
    [SerializeField] private string closeTrigger = "Close";
    [SerializeField] private bool useBoolAnimation = true; // Toggle between bool and trigger
    
    [Header("Visual Feedback")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;
    
    private Rigidbody rb;
    private bool isGrounded = false;
    private bool isCollected = false;
    private bool isDying = false;
    private float lifetimeTimer = 0f;
    private float stillTimer = 0f;
    private Camera worldSpaceCamera;
    private Vector3 originalScale;
    private Transform playerTransform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;
        
        // Find animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
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
        
        // Continuous collection check when grounded - fixes player walking through drops
        if (isGrounded && !isCollected && !isDying && playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= collectionRadius)
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
        
        // Count down lifetime after grounded
        if (isGrounded && !isDying)
        {
            lifetimeTimer += Time.deltaTime;
            
            // Start death animation 1 second before actual destruction
            if (lifetimeTimer >= lifetimeAfterGround - 1f)
            {
                StartDeathAnimation();
            }
        }
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
        
        // Trigger open animation - use bool for more reliable state
        if (animator != null)
        {
            if (useBoolAnimation)
            {
                animator.SetBool(openBool, true);
            }
            else
            {
                animator.SetTrigger(openTrigger);
            }
        }
        
        // Show simple UI indicator if available (optional)
        if (uiContainer != null)
        {
            uiContainer.gameObject.SetActive(true);
        }
        
        Debug.Log($"[AbilityDrop] Grounded at {transform.position}");
    }
    
    private void StartDeathAnimation()
    {
        if (isDying || isCollected) return;
        isDying = true;
        
        // Reset scale
        transform.localScale = originalScale;
        
        // Trigger close animation
        if (animator != null)
        {
            if (useBoolAnimation)
            {
                animator.SetBool(openBool, false);
            }
            animator.SetTrigger(closeTrigger);
        }
        
        // Hide UI
        if (uiContainer != null)
        {
            uiContainer.gameObject.SetActive(false);
        }
        
        // Destroy after animation time
        Destroy(gameObject, 1f);
    }

    public bool IsGrounded() => isGrounded;
    public bool IsCollected() => isCollected;
    
    public void Collect()
    {
        if (isCollected) return; // Only check isCollected, not isDying
        isCollected = true;
        isDying = true; // Prevent death animation from triggering
        
        // Reset scale immediately
        transform.localScale = originalScale;
        
        // Cancel any pending destruction
        CancelInvoke();
        StopAllCoroutines();
        
        // Trigger close animation on collection
        if (animator != null)
        {
            if (useBoolAnimation)
            {
                animator.SetBool(openBool, false);
            }
            animator.SetTrigger(closeTrigger);
        }
        
        // Hide UI immediately
        if (uiContainer != null)
        {
            uiContainer.gameObject.SetActive(false);
        }
        
        // Destroy immediately or after brief delay
        Destroy(gameObject, 0.1f); // Reduced delay for faster response
        
        Debug.Log("[AbilityDrop] Collected!");
    }
    
    private void OnDestroy()
    {
        // Clean up any remaining references
        CancelInvoke();
        StopAllCoroutines();
    }
}