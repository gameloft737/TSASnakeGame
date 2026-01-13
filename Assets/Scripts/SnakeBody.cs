using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using System;

public class SnakeBody : MonoBehaviour
{
    [SerializeField] private Transform head;
    [SerializeField] private GameObject bodyPartPrefab;
    [SerializeField] private int bodyLength = 5;
    [SerializeField] private float segmentSpacing = 0.5f;
    
    [Header("Constraint Settings")]
    [SerializeField] private int constraintIterations = 2; // Reduced from 3 - still stable but faster
    [SerializeField] private bool useAdaptiveConstraints = true; // Only run extra iterations when needed
    private float lastHeadSpeed = 0f;
    private Rigidbody cachedHeadRigidbody; // Cached to avoid GetComponent every frame
    
    [Header("Growth Settings")]
    [Tooltip("Base number of apples needed to add a segment")]
    [SerializeField] private int baseApplesPerSegment = 5;
    [Tooltip("Multiplier applied to the threshold after each segment is added (e.g., 1.2 means 20% more apples needed each time)")]
    [SerializeField] private float growthMultiplier = 1.2f;
    [Tooltip("Maximum apples required for a segment (caps the multiplier effect)")]
    [SerializeField] private int maxApplesPerSegment = 50;
    
    // Growth tracking
    private int totalApplesEaten = 0;
    private int applesForCurrentSegment = 0;
    private int currentApplesThreshold;
    private int segmentsAddedFromApples = 0;
    
    // Material system
    [SerializeField] private Renderer headRenderer;
    private GameObject currentAttachment;
    private Material currentBodyMaterial; // Track current body material for new segments
    
    public List<BodyPart> bodyParts = new List<BodyPart>();
    
    public static event Action OnBodyPartsInitialized;
    
    private void OnEnable()
    {
        AppleEnemy.OnAppleDied += OnAppleEaten;
    }
    
    private void OnDisable()
    {
        AppleEnemy.OnAppleDied -= OnAppleEaten;
    }
    
    void Start()
    {
        // Initialize the growth threshold
        currentApplesThreshold = baseApplesPerSegment;
        
        // Set static references for AppleEnemy optimization
        SnakeHealth health = GetComponent<SnakeHealth>();
        AppleEnemy.SetSnakeReferences(this, health);
        
        // Cache head rigidbody for performance
        if (head != null)
        {
            cachedHeadRigidbody = head.GetComponent<Rigidbody>();
        }
        
        // Get head renderer if not assigned
        if (headRenderer == null)
        {
            headRenderer = head.GetComponent<Renderer>();
            if (headRenderer == null)
            {
                headRenderer = head.GetComponentInChildren<Renderer>();
            }
        }

        // Create body parts and set up the chain
        for (int i = 0; i < bodyLength; i++)
        {
            Vector3 startPos = head.position - head.forward * segmentSpacing * (i + 1);
            GameObject part = Instantiate(bodyPartPrefab, startPos, head.rotation);
            BodyPart bodyPartComponent = part.GetComponent<BodyPart>();
            bodyParts.Add(bodyPartComponent);
            
            // Set up the leader chain - each segment follows the one in front
            Transform leader = (i == 0) ? head : bodyParts[i - 1].transform;
            bodyPartComponent.Initialize(leader, segmentSpacing);
            
            // Apply tail scaling (smaller segments at the tail end)
            // Calculate how far from the tail this segment is
            int distanceFromTail = bodyLength - 1 - i;
            if (distanceFromTail < 3)
            {
                // Scales from tail: smallest at tail (index 0 from tail), getting bigger toward head
                float[] tailScales = { 0.5f, 0.7f, 0.85f };
                Vector3 scale = part.transform.localScale;
                scale.x = tailScales[distanceFromTail];
                part.transform.localScale = scale;
            }
            
            // Capture the final scale after all modifications
            bodyPartComponent.CaptureBaseScale();
        }
        
        OnBodyPartsInitialized?.Invoke();
    }
    
    /// <summary>
    /// Called when an apple enemy dies. Tracks apples eaten and adds segments accordingly.
    /// </summary>
    private void OnAppleEaten(AppleEnemy apple)
    {
        totalApplesEaten++;
        applesForCurrentSegment++;
        
        // Check if we've eaten enough apples to add a segment
        if (applesForCurrentSegment >= currentApplesThreshold)
        {
            // Add a segment
            IncreaseSize(1);
            segmentsAddedFromApples++;
            
            #if UNITY_EDITOR
            Debug.Log($"[SnakeBody] Added segment from eating apples! Total segments from apples: {segmentsAddedFromApples}, Total apples eaten: {totalApplesEaten}");
            #endif
            
            // Reset counter for next segment
            applesForCurrentSegment = 0;
            
            // Calculate new threshold with multiplier (capped at max)
            int newThreshold = Mathf.RoundToInt(currentApplesThreshold * growthMultiplier);
            currentApplesThreshold = Mathf.Min(newThreshold, maxApplesPerSegment);
            
            #if UNITY_EDITOR
            Debug.Log($"[SnakeBody] Next segment requires {currentApplesThreshold} apples");
            #endif
        }
    }
    
    /// <summary>
    /// Gets the current progress towards the next segment (0 to 1)
    /// </summary>
    public float GetGrowthProgress()
    {
        return (float)applesForCurrentSegment / currentApplesThreshold;
    }
    
    /// <summary>
    /// Gets the number of apples needed for the next segment
    /// </summary>
    public int GetApplesNeededForNextSegment()
    {
        return currentApplesThreshold - applesForCurrentSegment;
    }
    
    /// <summary>
    /// Gets the total number of apples eaten
    /// </summary>
    public int GetTotalApplesEaten()
    {
        return totalApplesEaten;
    }
    
    /// <summary>
    /// Gets the number of segments added from eating apples
    /// </summary>
    public int GetSegmentsFromApples()
    {
        return segmentsAddedFromApples;
    }

    public void OnAdd(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            IncreaseSize(1);
            #if UNITY_EDITOR
            Debug.Log($"Added segment! Total body length: {bodyLength}");
            #endif
        }
    }

    void Update()
    {
        // Update body parts in order from head to tail
        // Each segment follows its leader
        for (int i = 0; i < bodyParts.Count; i++)
        {
            bodyParts[i].FollowLeader();
        }
    }

    void LateUpdate()
    {
        int partCount = bodyParts.Count;
        if (partCount == 0) return;
        
        // Adaptive constraint iterations based on movement speed
        int iterations = constraintIterations;
        if (useAdaptiveConstraints && cachedHeadRigidbody != null)
        {
            // Use cached rigidbody reference
            lastHeadSpeed = cachedHeadRigidbody.linearVelocity.magnitude;
            
            // Use fewer iterations when moving slowly, more when moving fast
            if (lastHeadSpeed < 2f)
            {
                iterations = 1; // Slow movement - 1 iteration is enough
            }
            else if (lastHeadSpeed > 8f)
            {
                iterations = 3; // Fast movement - need more stability
            }
        }
        
        // Run constraint iterations
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            // Forward pass: from head to tail
            for (int i = 0; i < partCount; i++)
            {
                bodyParts[i].EnforceConstraint();
            }
            
            // Only do backward pass on first iteration for stability
            // Subsequent iterations only need forward pass
            if (iteration == 0)
            {
                for (int i = partCount - 1; i >= 0; i--)
                {
                    bodyParts[i].EnforceConstraint();
                }
            }
        }
    }
    
    public void ApplyForceToBody(Vector3 direction, float force)
    {
        // This method is kept for compatibility but the new system doesn't use physics forces
        // The body will naturally follow the head's movement
    }
    
    public void IncreaseSize(int amount = 1)
    {
        for (int i = 0; i < amount; i++)
        {
            // Get the last segment's position and rotation for spawning
            Transform lastSegment = bodyParts.Count > 0 ? bodyParts[bodyParts.Count - 1].transform : head;
            Vector3 spawnPos = lastSegment.position - lastSegment.forward * segmentSpacing;
            Quaternion spawnRot = lastSegment.rotation;
            
            GameObject newPart = Instantiate(bodyPartPrefab, spawnPos, spawnRot);
            BodyPart bodyPartComponent = newPart.GetComponent<BodyPart>();
            
            // Set up the leader - new segment follows the previous last segment
            bodyPartComponent.Initialize(lastSegment, segmentSpacing);
            
            // Apply current body material to new segment if one is set
            if (currentBodyMaterial != null)
            {
                bodyPartComponent.SetMaterial(currentBodyMaterial);
            }
            
            bodyParts.Add(bodyPartComponent);
            bodyLength++;
        }
        
        // Reapply tail taper to maintain the tapered look
        ApplyTailTaper();
    }
    
    /// <summary>
    /// Applies tapered scaling to the last 3 segments of the snake tail.
    /// Called after adding new segments to maintain the tapered appearance.
    /// </summary>
    private void ApplyTailTaper()
    {
        if (bodyParts.Count == 0) return;
        
        // Tail scales: smallest at the very end, getting bigger toward the head
        float[] tailScales = { 0.5f, 0.7f, 0.85f };
        
        // Apply taper to the last 3 segments (or fewer if snake is shorter)
        int segmentsToTaper = Mathf.Min(3, bodyParts.Count);
        
        for (int i = 0; i < segmentsToTaper; i++)
        {
            // Index from the end of the list
            int partIndex = bodyParts.Count - 1 - i;
            BodyPart part = bodyParts[partIndex];
            
            if (part != null)
            {
                Vector3 scale = part.transform.localScale;
                // Reset to default scale first (in case it was previously tapered differently)
                scale.x = 1f;
                // Apply the taper scale based on distance from tail
                scale.x = tailScales[i];
                part.transform.localScale = scale;
                
                // Update the base scale so animations work correctly
                part.CaptureBaseScale();
            }
        }
        
        // Reset any segments that were previously tapered but are no longer in the tail zone
        // This handles the case where the 4th-from-end segment was previously the 3rd-from-end
        if (bodyParts.Count > 3)
        {
            int fourthFromEnd = bodyParts.Count - 4;
            BodyPart part = bodyParts[fourthFromEnd];
            if (part != null)
            {
                Vector3 scale = part.transform.localScale;
                scale.x = 1f; // Reset to full size
                part.transform.localScale = scale;
                part.CaptureBaseScale();
            }
        }
    }
    
    public int GetBodyLength()
    {
        return bodyLength;
    }

    /// <summary>
    /// Applies visual variation to the snake.
    /// If headMaterial or bodyMaterial is null, the current material is preserved.
    /// This allows evolution attacks to change materials while regular attacks only change attachments.
    /// </summary>
    public void ApplyAttackVariation(Material headMaterial, Material bodyMaterial, GameObject attachmentObject)
    {
        #if UNITY_EDITOR
        Debug.Log($"SnakeBody.ApplyAttackVariation called - HeadMaterial: {(headMaterial != null ? headMaterial.name : "null")}, BodyMaterial: {(bodyMaterial != null ? bodyMaterial.name : "null")}, Attachment: {(attachmentObject != null ? attachmentObject.name : "null")}");
        #endif
        
        // Only apply head material if provided (evolution attacks)
        if (headMaterial != null)
        {
            if (headRenderer != null)
            {
                headRenderer.material = headMaterial;
                #if UNITY_EDITOR
                Debug.Log($"Applied head material: {headMaterial.name} to {headRenderer.gameObject.name}");
                #endif
            }
            else
            {
                // Try to find head renderer if not assigned
                if (head != null)
                {
                    headRenderer = head.GetComponent<Renderer>();
                    if (headRenderer == null)
                    {
                        headRenderer = head.GetComponentInChildren<Renderer>();
                    }
                    
                    if (headRenderer != null)
                    {
                        headRenderer.material = headMaterial;
                        #if UNITY_EDITOR
                        Debug.Log($"Found and applied head material: {headMaterial.name} to {headRenderer.gameObject.name}");
                        #endif
                    }
                    else
                    {
                        #if UNITY_EDITOR
                        Debug.LogWarning("SnakeBody: Cannot apply head material - no Renderer found on head!");
                        #endif
                    }
                }
                else
                {
                    #if UNITY_EDITOR
                    Debug.LogWarning("SnakeBody: Cannot apply head material - head transform is null!");
                    #endif
                }
            }
        }
        
        // Only apply body material if provided (evolution attacks)
        if (bodyMaterial != null)
        {
            // Store the current body material for new segments
            currentBodyMaterial = bodyMaterial;
            
            int appliedCount = 0;
            int partCount = bodyParts.Count;
            for (int i = 0; i < partCount; i++)
            {
                BodyPart part = bodyParts[i];
                if (part != null)
                {
                    part.SetMaterial(bodyMaterial);
                    appliedCount++;
                }
            }
            #if UNITY_EDITOR
            Debug.Log($"Applied body material: {bodyMaterial.name} to {appliedCount} body parts");
            #endif
        }
        
        // Always handle attachment changes
        if (currentAttachment != null)
        {
            currentAttachment.SetActive(false);
            currentAttachment = null;
        }
        
        if (attachmentObject != null)
        {
            currentAttachment = attachmentObject;
            currentAttachment.SetActive(true);
            #if UNITY_EDITOR
            Debug.Log($"Enabled attachment: {attachmentObject.name}");
            #endif
        }
    }
    
    /// <summary>
    /// Resets the snake materials to their default state.
    /// Call this when switching away from an evolved attack.
    /// </summary>
    public void ResetMaterials(Material defaultHeadMaterial, Material defaultBodyMaterial)
    {
        if (headRenderer != null && defaultHeadMaterial != null)
        {
            headRenderer.material = defaultHeadMaterial;
        }
        
        if (defaultBodyMaterial != null)
        {
            // Update the tracked body material
            currentBodyMaterial = defaultBodyMaterial;
            
            foreach (BodyPart part in bodyParts)
            {
                part.SetMaterial(defaultBodyMaterial);
            }
        }
    }
    
    public void ClearAttachment()
    {
        if (currentAttachment != null)
        {
            currentAttachment.SetActive(false);
            currentAttachment = null;
        }
    }
    
    public void TriggerSwallowAnimation(float bulgeScale = 1.3f, float bulgeSpeed = 0.08f)
    {
        StartCoroutine(SwallowAnimationCoroutine(bulgeScale, bulgeSpeed));
    }

    private IEnumerator SwallowAnimationCoroutine(float bulgeScale, float bulgeSpeed)
    {
        for (int i = 0; i < bodyParts.Count; i++)
        {
            bodyParts[i].AnimateBulge(bulgeScale, 0.2f);
            yield return new WaitForSeconds(bulgeSpeed);
        }
    }

    private void OnDrawGizmos()
    {
        if (bodyParts == null || bodyParts.Count == 0) return;

        // Draw the chain from head to tail
        Gizmos.color = Color.green;
        Gizmos.DrawLine(head.position, bodyParts[0].transform.position);
        
        for (int i = 0; i < bodyParts.Count - 1; i++)
        {
            Gizmos.DrawLine(bodyParts[i].transform.position, bodyParts[i + 1].transform.position);
        }
        
        // Draw spheres at each segment to show spacing
        Gizmos.color = Color.yellow;
        foreach (var part in bodyParts)
        {
            Gizmos.DrawWireSphere(part.transform.position, 0.1f);
        }
    }
}