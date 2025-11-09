using UnityEngine;

public class BodyPart : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float positionStrength = 50f; // Proportional term
    [SerializeField] private float dampingStrength = 10f;  // Derivative term (anti-bounce)
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float stoppedDamping = 5f;    // Extra damping when head stopped

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        
        // Light drag helps stability
        rb.linearDamping = 1f;
        rb.angularDamping = 0.5f;
    }

    public void FollowTarget(Vector3 targetPos, Quaternion targetRot, bool headIsMoving)
    {
        Vector3 positionError = targetPos - transform.position;
        
        // When head stops, heavily dampen the body parts
        if (!headIsMoving)
        {
            // Apply strong damping force to quickly stop movement
            rb.AddForce(-rb.linearVelocity * stoppedDamping, ForceMode.Acceleration);
            
            // Only apply weak correction force if error is significant
            if (positionError.magnitude > 0.05f)
            {
                Vector3 weakForce = positionError * (positionStrength * 0.2f);
                rb.AddForce(weakForce, ForceMode.Acceleration);
            }
        }
        else
        {
            // Normal PD controller when head is moving
            Vector3 targetVelocity = positionError * positionStrength;
            Vector3 velocityError = targetVelocity - rb.linearVelocity;
            
            Vector3 force = (positionError * positionStrength) + (velocityError * dampingStrength);
            rb.AddForce(force, ForceMode.Acceleration);
        }
        
        // Cap speed to prevent extreme forces
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
        
        // Smooth rotation towards target
        Quaternion newRot = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(newRot);
    }
}