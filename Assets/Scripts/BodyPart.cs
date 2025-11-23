using UnityEngine;

public class BodyPart : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float positionStrength = 50f;
    [SerializeField] private float dampingStrength = 10f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float stoppedDamping = 5f;
    
    // Material changing support
    [SerializeField] private Renderer bodyRenderer;
    private MaterialPropertyBlock propBlock;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        
        rb.linearDamping = 1f;
        rb.angularDamping = 0.5f;
        
        // Get renderer if not assigned
        if (bodyRenderer == null)
        {
            bodyRenderer = GetComponent<Renderer>();
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<Renderer>();
            }
        }
        
        propBlock = new MaterialPropertyBlock();
    }

    public void FollowTarget(Vector3 targetPos, Quaternion targetRot, bool headIsMoving)
    {
        Vector3 positionError = targetPos - transform.position;
        
        if (!headIsMoving)
        {
            rb.AddForce(-rb.linearVelocity * stoppedDamping, ForceMode.Acceleration);
            
            if (positionError.magnitude > 0.05f)
            {
                Vector3 weakForce = positionError * (positionStrength * 0.2f);
                rb.AddForce(weakForce, ForceMode.Acceleration);
            }
        }
        else
        {
            Vector3 targetVelocity = positionError * positionStrength;
            Vector3 velocityError = targetVelocity - rb.linearVelocity;
            
            Vector3 force = (positionError * positionStrength) + (velocityError * dampingStrength);
            rb.AddForce(force, ForceMode.Acceleration);
        }
        
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
        
        Quaternion newRot = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(newRot);
    }
    
    // Change material of this body part
    public void SetMaterial(Material newMaterial)
    {
        if (bodyRenderer != null && newMaterial != null)
        {
            bodyRenderer.material = newMaterial;
        }
    }
    
    // More efficient version using MaterialPropertyBlock (doesn't create material instances)
    public void SetColor(Color color)
    {
        if (bodyRenderer != null)
        {
            propBlock.SetColor("_Color", color);
            bodyRenderer.SetPropertyBlock(propBlock);
        }
    }
}