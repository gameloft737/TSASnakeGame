using UnityEngine;
using System.Collections;

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
    
    // Swallow animation
    private Vector3 originalScale;
    private Coroutine swallowCoroutine;

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
        originalScale = transform.localScale;
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
    
    /// <summary>
    /// Animates this body part to bulge out temporarily (swallow effect)
    /// </summary>
    public void AnimateBulge(float bulgeScale = 1.3f, float duration = 0.2f)
    {
        if (swallowCoroutine != null)
        {
            StopCoroutine(swallowCoroutine);
        }
        swallowCoroutine = StartCoroutine(BulgeCoroutine(bulgeScale, duration));
    }
    
    private IEnumerator BulgeCoroutine(float bulgeScale, float duration)
    {
        float elapsed = 0f;
        float halfDuration = duration / 2f;
        
        // Scale up
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * bulgeScale, t);
            yield return null;
        }
        
        // Scale back down
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            transform.localScale = Vector3.Lerp(originalScale * bulgeScale, originalScale, t);
            yield return null;
        }
        
        // Ensure we end at exactly the original scale
        transform.localScale = originalScale;
        swallowCoroutine = null;
    }
}