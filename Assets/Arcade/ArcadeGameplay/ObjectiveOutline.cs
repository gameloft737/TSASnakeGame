
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Adds a glowing wireframe outline around the objective's collider.
/// Attach this to any ObjectiveTrigger GameObject.
/// </summary>
[RequireComponent(typeof(ObjectiveTrigger))]
public class ObjectiveOutline : MonoBehaviour
{
    [Header("Wireframe Settings")]
    [Tooltip("Color of the wireframe outline")]
    public Color outlineColor = new Color(1f, 0.8f, 0f, 1f); // Golden yellow
    
    [Tooltip("Width of the wireframe lines")]
    [Range(0.01f, 1f)]
    public float lineWidth = 0.1f;
    
    [Tooltip("Padding around the collider bounds")]
    public float padding = 0.1f;
    
    [Header("Corner Settings")]
    [Tooltip("Add corner pieces for clean intersections")]
    public bool addCornerPieces = true;
    
    [Tooltip("Size multiplier for corner pieces (1.0 = same as line width)")]
    [Range(0.8f, 2f)]
    public float cornerSizeMultiplier = 1.0f;

    [Header("Animation")]
    [Tooltip("Should the outline pulse?")]
    public bool pulseEffect = true;
    
    [Tooltip("Speed of the pulse animation")]
    [Range(0.1f, 5f)]
    public float pulseSpeed = 1.5f;
    
    [Tooltip("Minimum scale during pulse (multiplier of line width)")]
    [Range(0.5f, 1f)]
    public float pulseMinScale = 0.7f;
    
    [Tooltip("Maximum scale during pulse (multiplier of line width)")]
    [Range(1f, 2f)]
    public float pulseMaxScale = 1.3f;
    
    [Tooltip("Pulse the color brightness as well")]
    public bool pulseColorBrightness = true;
    
    [Tooltip("Minimum brightness multiplier")]
    [Range(0.3f, 1f)]
    public float pulseMinBrightness = 0.5f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    // Private variables
    private ObjectiveTrigger objectiveTrigger;
    private bool isOutlineActive = false;
    private Coroutine animationCoroutine;
    
    // Wireframe objects
    private GameObject wireframeContainer;
    private List<LineRenderer> lineRenderers = new List<LineRenderer>();
    private List<GameObject> cornerSpheres = new List<GameObject>();
    private Material wireframeMaterial;
    
    // Cached collider bounds
    private Bounds cachedBounds;
    private Collider targetCollider;
    
    // Track if we should wait for cutscene
    private bool waitingForCutscene = true;

    private void Awake()
    {
        objectiveTrigger = GetComponent<ObjectiveTrigger>();
        
        // Find the collider on this object or children
        targetCollider = GetComponent<Collider>();
        if (targetCollider == null)
            targetCollider = GetComponentInChildren<Collider>();
        
        if (targetCollider == null && showDebugInfo)
            Debug.LogWarning($"ObjectiveOutline [{gameObject.name}]: No collider found!");
        
        if (showDebugInfo)
            Debug.Log($"ObjectiveOutline [{gameObject.name}]: Found collider: {(targetCollider != null ? targetCollider.GetType().Name : "NONE")}");
    }

    private void Start()
    {
        isOutlineActive = false;
        waitingForCutscene = true;
        
        // Don't check outline immediately - wait for cutscene to end
        // The ObjectiveManager will call UpdateAllOutlines() when cutscene ends
    }

    private void OnDisable() { DisableOutline(); }
    private void OnDestroy() { CleanupWireframe(); }

    private void CleanupWireframe()
    {
        if (wireframeContainer != null)
        {
            Destroy(wireframeContainer);
            wireframeContainer = null;
        }
        if (wireframeMaterial != null)
        {
            Destroy(wireframeMaterial);
            wireframeMaterial = null;
        }
        lineRenderers.Clear();
        cornerSpheres.Clear();
    }

    /// <summary>
    /// Check if this objective is the current one and update outline accordingly.
    /// Only shows outline after cutscene has ended.
    /// </summary>
    public void CheckAndUpdateOutline()
    {
        if (ObjectiveManager.Instance == null) return;
        
        // Check if the game has started (cutscene ended)
        // We use reflection or a public property to check hasStarted
        bool gameHasStarted = HasGameStarted();
        
        if (!gameHasStarted)
        {
            if (showDebugInfo)
                Debug.Log($"ObjectiveOutline [{gameObject.name}]: Waiting for cutscene to end...");
            return;
        }
        
        waitingForCutscene = false;
        
        bool shouldBeActive = ObjectiveManager.Instance.CurrentObjectiveIndex == objectiveTrigger.myObjectiveIndex;
        
        if (showDebugInfo)
            Debug.Log($"ObjectiveOutline [{gameObject.name}]: shouldBeActive={shouldBeActive}, isActive={isOutlineActive}");
        
        if (shouldBeActive && !isOutlineActive) EnableOutline();
        else if (!shouldBeActive && isOutlineActive) DisableOutline();
    }
    
    /// <summary>
    /// Check if the game has started (cutscene has ended)
    /// </summary>
    private bool HasGameStarted()
    {
        // Check if ObjectiveManager has started objectives
        // We can check if the objective UI is visible or if currentIndex > -1
        if (ObjectiveManager.Instance == null) return false;
        
        // The ObjectiveManager shows UI and starts objectives after cutscene ends
        // We can check if the objectiveUIContainer is active
        var uiContainer = ObjectiveManager.Instance.objectiveUIContainer;
        if (uiContainer != null)
        {
            return uiContainer.activeInHierarchy;
        }
        
        // Fallback: check if objectiveTextUI is active
        var textUI = ObjectiveManager.Instance.objectiveTextUI;
        if (textUI != null)
        {
            return textUI.gameObject.activeInHierarchy;
        }
        
        // If we can't determine, assume started
        return true;
    }

    public void EnableOutline()
    {
        if (isOutlineActive) return;
        if (targetCollider == null)
        {
            Debug.LogError($"ObjectiveOutline [{gameObject.name}]: Cannot enable - no collider found!");
            return;
        }
        
        // Double-check that cutscene has ended
        if (!HasGameStarted())
        {
            if (showDebugInfo)
                Debug.Log($"ObjectiveOutline [{gameObject.name}]: Cutscene not ended yet, not enabling outline");
            return;
        }
        
        isOutlineActive = true;
        
        CreateWireframe();
        
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateOutline());
        
        Debug.Log($"ObjectiveOutline: ENABLED wireframe for {gameObject.name}");
    }

    public void DisableOutline()
    {
        if (!isOutlineActive) return;
        isOutlineActive = false;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        CleanupWireframe();
        
        Debug.Log($"ObjectiveOutline: DISABLED wireframe for {gameObject.name}");
    }

    private void CreateWireframe()
    {
        if (wireframeContainer != null) return;
        
        // Get collider bounds
        cachedBounds = targetCollider.bounds;
        
        // Add padding
        Vector3 size = cachedBounds.size + Vector3.one * padding * 2f;
        Vector3 center = cachedBounds.center;
        
        // Create container
        wireframeContainer = new GameObject("WireframeOutline");
        wireframeContainer.transform.SetParent(null); // World space
        wireframeContainer.transform.position = Vector3.zero;
        wireframeContainer.transform.rotation = Quaternion.identity;
        
        // Create material
        wireframeMaterial = new Material(Shader.Find("Sprites/Default"));
        wireframeMaterial.color = outlineColor;
        
        // Calculate the 8 corners of the bounding box
        Vector3 halfSize = size * 0.5f;
        Vector3[] corners = new Vector3[8];
        corners[0] = center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
        corners[1] = center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
        corners[2] = center + new Vector3(halfSize.x, -halfSize.y, halfSize.z);
        corners[3] = center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
        corners[4] = center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
        corners[5] = center + new Vector3(halfSize.x, halfSize.y, -halfSize.z);
        corners[6] = center + new Vector3(halfSize.x, halfSize.y, halfSize.z);
        corners[7] = center + new Vector3(-halfSize.x, halfSize.y, halfSize.z);
        
        // Create the 12 edges of the box
        // Bottom face
        CreateLine(corners[0], corners[1]);
        CreateLine(corners[1], corners[2]);
        CreateLine(corners[2], corners[3]);
        CreateLine(corners[3], corners[0]);
        
        // Top face
        CreateLine(corners[4], corners[5]);
        CreateLine(corners[5], corners[6]);
        CreateLine(corners[6], corners[7]);
        CreateLine(corners[7], corners[4]);
        
        // Vertical edges
        CreateLine(corners[0], corners[4]);
        CreateLine(corners[1], corners[5]);
        CreateLine(corners[2], corners[6]);
        CreateLine(corners[3], corners[7]);
        
        // Add corner pieces for clean intersections
        if (addCornerPieces)
        {
            foreach (Vector3 corner in corners)
            {
                CreateCornerSphere(corner);
            }
        }
        
        if (showDebugInfo)
            Debug.Log($"ObjectiveOutline [{gameObject.name}]: Created wireframe with {lineRenderers.Count} lines and {cornerSpheres.Count} corners");
    }

    private void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("Line");
        lineObj.transform.SetParent(wireframeContainer.transform);
        
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = wireframeMaterial;
        lr.startColor = outlineColor;
        lr.endColor = outlineColor;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.allowOcclusionWhenDynamic = false;
        lr.numCapVertices = 4; // Rounded caps
        lr.numCornerVertices = 4; // Rounded corners
        
        lineRenderers.Add(lr);
    }
    
    private void CreateCornerSphere(Vector3 position)
    {
        // Use a cube instead of sphere - blends better with the lines
        GameObject corner = GameObject.CreatePrimitive(PrimitiveType.Cube);
        corner.name = "Corner";
        corner.transform.SetParent(wireframeContainer.transform);
        corner.transform.position = position;
        
        // Size matches line width for seamless connection
        float cornerSize = lineWidth * cornerSizeMultiplier;
        corner.transform.localScale = Vector3.one * cornerSize;
        
        // Rotate 45 degrees for diamond look (optional, can be removed)
        // corner.transform.rotation = Quaternion.Euler(45, 45, 0);
        
        // Remove collider
        Collider col = corner.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        // Apply material
        MeshRenderer renderer = corner.GetComponent<MeshRenderer>();
        renderer.material = wireframeMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        
        cornerSpheres.Add(corner);
    }

    private IEnumerator AnimateOutline()
    {
        float time = 0f;
        
        while (isOutlineActive)
        {
            time += Time.deltaTime;
            
            // Update bounds in case object moves
            if (targetCollider != null)
            {
                Bounds newBounds = targetCollider.bounds;
                if (newBounds.center != cachedBounds.center || newBounds.size != cachedBounds.size)
                {
                    // Bounds changed, recreate wireframe
                    CleanupWireframe();
                    CreateWireframe();
                }
            }
            
            // Pulse effect - scale line width and brightness
            if (pulseEffect)
            {
                // Calculate pulse value (0 to 1, oscillating)
                float pulseT = (Mathf.Sin(time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
                
                // Scale the line width
                float currentWidth = Mathf.Lerp(lineWidth * pulseMinScale, lineWidth * pulseMaxScale, pulseT);
                float currentSphereScale = currentWidth * cornerSizeMultiplier;
                
                // Calculate pulsed color (brightness)
                Color pulsedColor = outlineColor;
                if (pulseColorBrightness)
                {
                    float brightness = Mathf.Lerp(pulseMinBrightness, 1f, pulseT);
                    pulsedColor = new Color(
                        outlineColor.r * brightness,
                        outlineColor.g * brightness,
                        outlineColor.b * brightness,
                        outlineColor.a
                    );
                }
                
                // Update material
                if (wireframeMaterial != null)
                {
                    wireframeMaterial.color = pulsedColor;
                }
                
                // Update all line renderers
                foreach (LineRenderer lr in lineRenderers)
                {
                    if (lr != null)
                    {
                        lr.startWidth = currentWidth;
                        lr.endWidth = currentWidth;
                        lr.startColor = pulsedColor;
                        lr.endColor = pulsedColor;
                    }
                }
                
                // Update corner spheres
                foreach (GameObject sphere in cornerSpheres)
                {
                    if (sphere != null)
                    {
                        sphere.transform.localScale = Vector3.one * currentSphereScale;
                    }
                }
            }
            
            yield return null;
        }
    }

    public void OnObjectiveCompleted() { DisableOutline(); }

#if UNITY_EDITOR
    // Draw gizmo in editor for visualization
    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) col = GetComponentInChildren<Collider>();
        
        if (col != null)
        {
            Gizmos.color = outlineColor;
            Bounds bounds = col.bounds;
            Vector3 size = bounds.size + Vector3.one * padding * 2f;
            Gizmos.DrawWireCube(bounds.center, size);
            
            // Draw corner pieces in gizmo
            if (addCornerPieces)
            {
                Vector3 halfSize = size * 0.5f;
                Vector3 center = bounds.center;
                float sphereSize = lineWidth * cornerSizeMultiplier * 0.5f;
                
                Gizmos.DrawSphere(center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z), sphereSize);
                Gizmos.DrawSphere(center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z), sphereSize);
                Gizmos.DrawSphere(center + new Vector3(halfSize.x, -halfSize.y, halfSize.z), sphereSize);
                Gizmos.DrawSphere(center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z), sphereSize);
                Gizmos.DrawSphere(center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z), sphereSize);
                Gizmos.DrawSphere(center + new Vector3(halfSize.x, halfSize.y, -halfSize.z), sphereSize);
                Gizmos.DrawSphere(center + new Vector3(halfSize.x, halfSize.y, halfSize.z), sphereSize);
                Gizmos.DrawSphere(center + new Vector3(-halfSize.x, halfSize.y, halfSize.z), sphereSize);
            }
        }
    }
#endif
}