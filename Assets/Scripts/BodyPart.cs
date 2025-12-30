using UnityEngine;
using System.Collections;

public class BodyPart : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 15f;
    
    // Material changing support
    [SerializeField] private Renderer bodyRenderer;
    private MaterialPropertyBlock propBlock;
    
    // Swallow animation
    private Vector3 baseScale;
    private Coroutine swallowCoroutine;
    
    // Reference to the segment in front (closer to head)
    private Transform leader;
    private float targetDistance;
    
    // Smoothing for natural movement
    private Vector3 velocity;
    [SerializeField] private float smoothTime = 0.05f;

    private void Awake()
    {
        // Remove any Rigidbody that might exist from the old physics-based system
        // The new constraint-based system doesn't use physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb);
        }
        
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
    
    /// <summary>
    /// Initialize this body part with its leader (the segment in front of it) and the target distance
    /// </summary>
    public void Initialize(Transform leaderTransform, float distance)
    {
        leader = leaderTransform;
        targetDistance = distance;
    }
    
    /// <summary>
    /// Update the leader reference (used when segments are added/removed)
    /// </summary>
    public void SetLeader(Transform newLeader)
    {
        leader = newLeader;
    }
    
    /// <summary>
    /// Get the current leader
    /// </summary>
    public Transform GetLeader()
    {
        return leader;
    }

    /// <summary>
    /// Follow the leader while maintaining exact distance constraint
    /// </summary>
    public void FollowLeader()
    {
        if (leader == null) return;
        
        Vector3 leaderPos = leader.position;
        Vector3 currentPos = transform.position;
        
        // Calculate direction from this segment to the leader
        Vector3 direction = leaderPos - currentPos;
        float currentDistance = direction.magnitude;
        
        if (currentDistance > 0.001f)
        {
            direction /= currentDistance; // Normalize
            
            // Calculate the target position at exactly targetDistance behind the leader
            Vector3 targetPos = leaderPos - direction * targetDistance;
            
            // Smoothly move to target position for natural movement
            transform.position = Vector3.SmoothDamp(currentPos, targetPos, ref velocity, smoothTime);
            
            // After smoothing, enforce the distance constraint strictly
            EnforceDistanceConstraint();
            
            // Rotate to face the leader
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Strictly enforce the distance constraint - this prevents overlapping and stretching
    /// </summary>
    private void EnforceDistanceConstraint()
    {
        if (leader == null) return;
        
        Vector3 leaderPos = leader.position;
        Vector3 currentPos = transform.position;
        
        Vector3 direction = leaderPos - currentPos;
        float currentDistance = direction.magnitude;
        
        if (currentDistance > 0.001f)
        {
            direction /= currentDistance;
            
            // Place this segment at exactly targetDistance from the leader
            transform.position = leaderPos - direction * targetDistance;
        }
    }
    
    /// <summary>
    /// Called during FixedUpdate for physics-based constraint enforcement
    /// </summary>
    public void EnforceConstraint()
    {
        EnforceDistanceConstraint();
    }
    
    public void SetMaterial(Material newMaterial)
    {
        if (bodyRenderer != null && newMaterial != null)
        {
            bodyRenderer.material = newMaterial;
        }
    }
    
    public void SetColor(Color color)
    {
        if (bodyRenderer != null)
        {
            propBlock.SetColor("_Color", color);
            bodyRenderer.SetPropertyBlock(propBlock);
        }
    }
    
    /// <summary>
    /// Call this immediately after instantiation and scale changes
    /// </summary>
    public void CaptureBaseScale()
    {
        baseScale = transform.localScale;
    }
    
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
        Vector3 targetScale = baseScale * bulgeScale;
        
        // Scale up
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            transform.localScale = Vector3.Lerp(baseScale, targetScale, t);
            yield return null;
        }
        
        // Scale back down
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            transform.localScale = Vector3.Lerp(targetScale, baseScale, t);
            yield return null;
        }
        
        transform.localScale = baseScale;
        swallowCoroutine = null;
    }
}