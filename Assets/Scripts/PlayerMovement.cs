using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveForce = 50f;
    public float maxSpeed = 5f;
    public float defaultSpeed = 2f; // Speed when not boosting
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float aimRotationSpeed = 30f; // Mouse sensitivity for aim mode
    [SerializeField] private float aimInputDelay = 0.5f; // Delay before look input is active
    [SerializeField] private float surfaceAlignSpeed = 10f;
    [SerializeField] private float gravityForce = 20f;
    
    [Header("Ground Friction")]
    [SerializeField] private float groundDrag = 10f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Orientation")]
    [SerializeField] private Transform orientation;
    
    [Header("Weapon")]
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private float weaponRotationSpeed = 10f;
    
    [Header("Camera Reference")]
    [SerializeField] private CameraManager cameraManager;

    private Rigidbody rb;
    private bool isGrounded;
    private bool moveForward;
    private float rotationInput;
    private Vector2 lookInput; // Mouse/look input
    private Vector3 surfaceNormal = Vector3.up;
    private float aimStartTime; // Track when aiming started
    private bool wasAiming; // Track previous aim state

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = false;
    }

    void Update()
    {
        CheckGround();
        AlignToSurface();
        HandleRotation();
        UpdateWeaponRotation();
    }

    void FixedUpdate()
    {
        ApplyGravity();
        HandleMovement();
        ApplyGroundFriction();
    }

    private void ApplyGravity()
    {
        if (isGrounded)
        {
            // Pull toward surface to stay attached
            rb.AddForce(-surfaceNormal * gravityForce, ForceMode.Acceleration);
        }
        else
        {
            // Standard gravity when airborne
            rb.AddForce(Vector3.down * gravityForce, ForceMode.Acceleration);
        }
    }

    private void HandleMovement()
    {
        if (!isGrounded || orientation == null) return;

        // Always move forward, use different speed based on input
        float targetSpeed = moveForward ? maxSpeed : defaultSpeed;
        
        // Move along the surface
        Vector3 moveDir = Vector3.ProjectOnPlane(orientation.forward, surfaceNormal).normalized;
        
        // Check current speed along surface
        Vector3 planarVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, surfaceNormal);
        float currentSpeed = planarVelocity.magnitude;
        
        // Apply stronger force when below target to overcome friction smoothly
        if (currentSpeed < targetSpeed)
        {
            // Scale force based on how far we are from target speed
            float speedDeficit = targetSpeed - currentSpeed;
            float forceMultiplier = Mathf.Clamp01(speedDeficit / targetSpeed) * 2f + 0.5f;
            rb.AddForce(moveDir * moveForce * forceMultiplier, ForceMode.Acceleration);
        }
    }

    private void ApplyGroundFriction()
    {
        if (!isGrounded) return;

        // Slow down movement along the surface
        Vector3 planarVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, surfaceNormal);
        float currentSpeed = planarVelocity.magnitude;
        
        // Always move forward, use different speed based on input
        float targetSpeed = moveForward ? maxSpeed : defaultSpeed;
        
        // Only apply friction if we're going faster than target speed
        if (currentSpeed > targetSpeed)
        {
            rb.AddForce(-planarVelocity * groundDrag, ForceMode.Acceleration);
        }
        else
        {
            // Apply minimal friction at slow speeds to reduce jitter
            rb.AddForce(-planarVelocity * (groundDrag * 0.3f), ForceMode.Acceleration);
        }
    }

    private void HandleRotation()
    {
        if (orientation == null) return;

        float rotation = 0f;
        
        // Check if we're aiming
        bool isAiming = cameraManager != null && cameraManager.IsAiming();
        
        // Detect when we start aiming
        if (isAiming && !wasAiming)
        {
            aimStartTime = Time.time;
        }
        wasAiming = isAiming;
        
        if (isAiming)
        {
            // Check if enough time has passed since starting to aim
            float timeSinceAimStart = Time.time - aimStartTime;
            if (timeSinceAimStart >= aimInputDelay)
            {
                // Use mouse/look input for rotation when aiming (after delay)
                rotation = lookInput.x * aimRotationSpeed * Time.deltaTime;
            }
        }
        else
        {
            // Use keyboard input for rotation when not aiming
            rotation = rotationInput * rotationSpeed * Time.deltaTime;
        }
        
        orientation.Rotate(Vector3.up, rotation, Space.Self);
    }

    private void UpdateWeaponRotation()
    {
        if (orientation == null || weaponHolder == null) return;

        // Get orientation's Y rotation only
        float yRotation = orientation.eulerAngles.y;
        
        // Create target rotation with only Y axis from orientation
        Quaternion targetRotation = Quaternion.Euler(0f, yRotation, 0f);
        
        // Smoothly rotate weapon holder
        weaponHolder.rotation = Quaternion.Slerp(weaponHolder.rotation, targetRotation, weaponRotationSpeed * Time.deltaTime);
    }

    private void AlignToSurface()
    {
        if (orientation == null) return;

        Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal)
                                * Quaternion.LookRotation(Vector3.ProjectOnPlane(orientation.forward, Vector3.up).normalized, Vector3.up);

        orientation.rotation = Quaternion.Slerp(orientation.rotation, targetRotation, surfaceAlignSpeed * Time.deltaTime);
    }

    private void CheckGround()
    {
        RaycastHit hit;

        if (Physics.Raycast(groundCheck.position, -transform.up, out hit, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            isGrounded = true;
            surfaceNormal = hit.normal;
        }
        else
        {
            isGrounded = false;
            surfaceNormal = Vector3.up;
        }
    }
    public void ApplyLunge(float force)
    {
        if (orientation == null) return;
        
        Vector3 lungeDirection = orientation.forward;
        
        // If grounded, project lunge along surface
        if (isGrounded)
        {
            lungeDirection = Vector3.ProjectOnPlane(orientation.forward, surfaceNormal).normalized;
        }
        
        rb.AddForce(lungeDirection * force, ForceMode.Impulse);
    }

    // Input callbacks
    public void OnForward(InputAction.CallbackContext context) => moveForward = context.ReadValueAsButton();
    public void OnMove(InputAction.CallbackContext context) => rotationInput = context.ReadValue<Vector2>().x;
    public void OnLook(InputAction.CallbackContext context) => lookInput = context.ReadValue<Vector2>();

    private void OnDrawGizmos()
    {
        if (groundCheck == null) return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, 0.1f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(groundCheck.position, groundCheck.position - transform.up * groundCheckDistance);
        Gizmos.DrawWireSphere(groundCheck.position - transform.up * groundCheckDistance, 0.05f);

        if (isGrounded)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(groundCheck.position, surfaceNormal);
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, surfaceNormal * 2f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }

    public bool IsGrounded() => isGrounded;
}