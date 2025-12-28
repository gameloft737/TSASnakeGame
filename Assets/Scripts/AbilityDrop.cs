using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AbilityDrop : MonoBehaviour
{
    [Header("Drop Settings")]
    [SerializeField] private float lifetimeAfterGround = 10f;
    [SerializeField] private float groundedCheckTime = 1f;
    [SerializeField] private float stillThreshold = 0.1f;
    
    [Header("UI References")]
    [SerializeField] private Transform uiContainer;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private string closeTrigger = "Close";
    
    private Rigidbody rb;
    private bool isGrounded = false;
    private bool isCollected = false;
    private bool isDying = false;
    private float lifetimeTimer = 0f;
    private float stillTimer = 0f;
    private Camera worldSpaceCamera;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
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
        
        // Trigger open animation
        if (animator != null)
        {
            animator.SetTrigger(openTrigger);
        }
        
        // Show simple UI indicator if available (optional)
        if (uiContainer != null)
        {
            uiContainer.gameObject.SetActive(true);
            
            // You could add a simple sprite or text here to indicate
            // this is an ability drop that can be collected
        }
    }
    
    private void StartDeathAnimation()
    {
        if (isDying || isCollected) return;
        isDying = true;
        
        // Trigger close animation
        if (animator != null)
        {
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
        if (isCollected || isDying) return;
        isCollected = true;
        isDying = true; // Prevent death animation from triggering
        
        // Cancel any pending destruction
        CancelInvoke();
        StopAllCoroutines();
        
        // Trigger close animation on collection
        if (animator != null)
        {
            animator.SetTrigger(closeTrigger);
        }
        
        // Hide UI immediately
        if (uiContainer != null)
        {
            uiContainer.gameObject.SetActive(false);
        }
        
        // Destroy after brief delay for animation
        Destroy(gameObject, 0.3f);
    }
    
    private void OnDestroy()
    {
        // Clean up any remaining references
        CancelInvoke();
        StopAllCoroutines();
    }
}