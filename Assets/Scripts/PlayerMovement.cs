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
    [SerializeField] private float mouseSensitivity = 2f; // Mouse sensitivity (not multiplied by deltaTime!)
    [SerializeField] private float mouseSmoothing = 0.15f; // Lower = smoother, higher = more responsive
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
    [SerializeField] private bool useMouseInAimMode = true; // Toggle which camera mode uses mouse
    
    [Header("Classic Mode Settings")]
    [SerializeField] private float gridCellSize = 1f;
    [SerializeField] private float classicMoveInterval = 0.15f; // Time between grid moves

    private Rigidbody rb;
    [SerializeField]private bool isGrounded;
    private bool moveForward;
    private float rotationInput;
    private Vector2 lookInput; // Mouse/look input
    private Vector2 smoothedLookInput; // Smoothed mouse input
    private Vector3 surfaceNormal = Vector3.up;
    private bool isFrozen = false; // Whether the player is frozen (for ability selection)
    private Vector3 frozenVelocity; // Store velocity when frozen
    
    // Classic mode state
    private bool isClassicMode = false;
    private Vector2Int classicDirection = Vector2Int.up; // Current movement direction
    private Vector2Int nextClassicDirection = Vector2Int.up; // Queued direction
    private float classicMoveTimer = 0f;
    private Vector2Int lastInputDirection = Vector2Int.zero;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = false;
        
        // Set static player reference for XP drops (optimization)
        XPDrop.SetPlayerReference(transform);
    }
    
    private void OnDestroy()
    {
        // Clear cached references when player is destroyed
        XPDrop.ClearCachedReferences();
    }

    void Update()
    {
        if (isFrozen) return; // Skip all updates when frozen
        
        CheckGround();
        AlignToSurface();
        SmoothMouseInput();
        
        if (isClassicMode)
        {
            HandleClassicModeRotation();
        }
        else
        {
            HandleRotation();
        }
        
        UpdateWeaponRotation();
    }

    void FixedUpdate()
    {
        if (isFrozen) return; // Skip physics when frozen
        
        ApplyGravity();
        HandleMovement();
        ApplyGroundFriction();
    }
    
    #region Classic Mode
    
    /// <summary>
    /// Enables or disables classic mode (smooth movement but restricted to 4 cardinal directions)
    /// </summary>
    public void SetClassicMode(bool enabled, float cellSize = 1f, float moveInterval = 0.15f)
    {
        isClassicMode = enabled;
        
        if (enabled)
        {
            // Snap orientation to nearest cardinal direction
            SnapOrientationToCardinal();
            Debug.Log("[PlayerMovement] Classic mode enabled - movement restricted to 4 directions");
        }
        else
        {
            Debug.Log("[PlayerMovement] Classic mode disabled");
        }
    }
    
    public bool IsClassicMode() => isClassicMode;
    
    /// <summary>
    /// Snaps the orientation to the nearest cardinal direction (N, E, S, W)
    /// </summary>
    private void SnapOrientationToCardinal()
    {
        if (orientation == null) return;
        
        // Get current forward direction
        Vector3 forward = orientation.forward;
        forward.y = 0;
        forward.Normalize();
        
        // Find nearest cardinal direction
        Vector3[] cardinals = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
        Vector3 nearest = cardinals[0];
        float maxDot = -1f;
        
        foreach (var dir in cardinals)
        {
            float dot = Vector3.Dot(forward, dir);
            if (dot > maxDot)
            {
                maxDot = dot;
                nearest = dir;
            }
        }
        
        // Snap to that direction
        orientation.rotation = Quaternion.LookRotation(nearest, Vector3.up);
    }
    
    /// <summary>
    /// In classic mode, handles absolute direction input like classic Snake
    /// W/Up = North, S/Down = South, A/Left = West, D/Right = East
    /// Holding a direction key also boosts speed (like holding W in normal mode)
    /// </summary>
    private void HandleClassicModeRotation()
    {
        if (orientation == null) return;
        
        Vector3 newDirection = Vector3.zero;
        bool isHoldingDirection = false;
        
        if (Keyboard.current != null)
        {
            // Check for direction input - absolute directions like classic Snake
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                newDirection = Vector3.forward; // North
                isHoldingDirection = true;
            }
            else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            {
                newDirection = Vector3.back; // South
                isHoldingDirection = true;
            }
            else if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                newDirection = Vector3.left; // West
                isHoldingDirection = true;
            }
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                newDirection = Vector3.right; // East
                isHoldingDirection = true;
            }
        }
        
        // In classic mode, holding any direction key gives speed boost (like holding W in normal mode)
        moveForward = isHoldingDirection;
        
        // If a direction was pressed, check if it's valid (not reversing)
        if (newDirection != Vector3.zero)
        {
            Vector3 currentDir = orientation.forward;
            currentDir.y = 0;
            currentDir.Normalize();
            
            // Don't allow reversing direction (can't go back on yourself)
            float dot = Vector3.Dot(currentDir, newDirection);
            if (dot > -0.9f) // Allow if not directly opposite
            {
                // Set the new direction
                orientation.rotation = Quaternion.LookRotation(newDirection, Vector3.up);
            }
        }
    }
    
    /// <summary>
    /// Gets the current cardinal direction the snake is facing
    /// </summary>
    public Vector2Int GetClassicDirection()
    {
        if (orientation == null) return Vector2Int.up;
        
        Vector3 forward = orientation.forward;
        forward.y = 0;
        forward.Normalize();
        
        // Determine which cardinal direction we're closest to
        if (Mathf.Abs(forward.z) > Mathf.Abs(forward.x))
        {
            return forward.z > 0 ? Vector2Int.up : Vector2Int.down;
        }
        else
        {
            return forward.x > 0 ? Vector2Int.right : Vector2Int.left;
        }
    }
    
    #endregion

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

    private void SmoothMouseInput()
    {
        // Smooth the mouse input using lerp
        smoothedLookInput = Vector2.Lerp(smoothedLookInput, lookInput, 1f - mouseSmoothing);
    }

    private void HandleRotation()
    {
        if (orientation == null) return;

        float rotation = 0f;
        
        // Determine if we should use mouse based on settings (aim camera removed)
        bool shouldUseMouse = !useMouseInAimMode;
        
        if (shouldUseMouse)
        {
            // Use BOTH keyboard AND mouse input
            float mouseRotation = smoothedLookInput.x * mouseSensitivity;
            float keyboardRotation = rotationInput * rotationSpeed * Time.deltaTime;
            rotation = mouseRotation + keyboardRotation;
        }
        else
        {
            // Use ONLY keyboard input
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
    public void OnForward(InputAction.CallbackContext context)
    {
        if (isFrozen) return; // Block all input when frozen
        moveForward = context.ReadValueAsButton();
    }
    
    public void OnMove(InputAction.CallbackContext context)
    {
        if (isFrozen) return; // Block all input when frozen
        rotationInput = context.ReadValue<Vector2>().x;
    }
    
    public void OnLook(InputAction.CallbackContext context)
    {
        if (isFrozen) return; // Block all input when frozen
        lookInput = context.ReadValue<Vector2>();
    }

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

    /// <summary>
    /// Freezes or unfreezes the player movement
    /// </summary>
    public void SetFrozen(bool frozen)
    {
        if (frozen && !isFrozen)
        {
            // Store current velocity and freeze
            frozenVelocity = rb.linearVelocity;
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
            
            // Clear all input to prevent any movement/rotation
            lookInput = Vector2.zero;
            smoothedLookInput = Vector2.zero;
            rotationInput = 0f;
            moveForward = false;
        }
        else if (!frozen && isFrozen)
        {
            // Restore velocity and unfreeze
            rb.isKinematic = false;
            rb.linearVelocity = frozenVelocity;
        }
        
        isFrozen = frozen;
    }

    /// <summary>
    /// Returns whether the player is currently frozen
    /// </summary>
    public bool IsFrozen() => isFrozen;
}