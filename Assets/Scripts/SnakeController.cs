using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveForce = 50f;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float rotationSpeed = 180f;
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

    private Rigidbody rb;
    private bool isGrounded;
    private bool moveForward;
    private float rotationInput;
    private Vector3 surfaceNormal = Vector3.up;

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

        if (moveForward)
        {
            // Move along the surface
            Vector3 moveDir = Vector3.ProjectOnPlane(orientation.forward, surfaceNormal).normalized;
            
            // Check current speed along surface
            Vector3 planarVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, surfaceNormal);
            
            if (planarVelocity.magnitude < maxSpeed)
            {
                rb.AddForce(moveDir * moveForce, ForceMode.Acceleration);
            }
        }
    }

    private void ApplyGroundFriction()
    {
        if (!isGrounded) return;

        // Slow down movement along the surface
        Vector3 planarVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, surfaceNormal);
        rb.AddForce(-planarVelocity * groundDrag, ForceMode.Acceleration);
    }

    private void HandleRotation()
    {
        if (orientation == null) return;

        float rotation = rotationInput * rotationSpeed * Time.deltaTime;
        orientation.Rotate(Vector3.up, rotation, Space.Self);
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

    // Input callbacks
    public void OnForward(InputAction.CallbackContext context) => moveForward = context.ReadValueAsButton();
    public void OnMove(InputAction.CallbackContext context) => rotationInput = context.ReadValue<Vector2>().x;

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