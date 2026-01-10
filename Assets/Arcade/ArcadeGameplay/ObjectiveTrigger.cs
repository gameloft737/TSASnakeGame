using UnityEngine;
using System.Collections;

public class ObjectiveTrigger : MonoBehaviour
{
    [Header("Optional Objects to Unhide")]
    public GameObject[] objectsToUnhide; // Objects that will be made visible on completion

    [Header("Optional Visual Changes")]
    public Light[] lightsToDisable;              // Lights to turn off
    public Light[] lightsToEnable;          // Lights to turn ON when completed
    public float enabledLightIntensity = 1f; // Optional intensity control
    public Renderer[] emissionRenderers;         // Renderers with emission you want to change
    public Color emissionOffColor = Color.black; // Emission color when off

    [Header("Cutscene")]
    public CutsceneController cutsceneToPlay; // Assign in Inspector (optional)

    [Header("Emission Settings")]
    [Range(0f, 5f)]
    public float emissionOffStrength = 1f; // Multiplier applied to emissionOffColor

    [Header("Subtitles")]
    [TextArea(1, 3)]
    public string completionSubtitle;
    public float subtitleDuration = 3f;

    [Header("Objective Settings")]
    public int myObjectiveIndex;           // Index in ObjectiveManager
    public string objectiveName = "Objective"; // Name shown in top-left UI

    [Header("Interaction Settings")]
    public string interactionMessage = "Press E to interact";
    
    [Header("Range-Based Interaction")]
    [Tooltip("Maximum distance from player to interact")]
    public float interactionRange = 5f;
    
    [Tooltip("Maximum angle from center of screen to detect (in degrees)")]
    [Range(1f, 45f)]
    public float lookAtAngle = 15f;
    
    [Tooltip("Use raycast to check if player is looking at this object")]
    public bool useRaycast = true;
    
    [Tooltip("Layers that can block the raycast")]
    public LayerMask blockingLayers = ~0; // Default: everything
    
    [Tooltip("The transform to check distance/look-at from (if null, uses this object's transform)")]
    public Transform interactionPoint;

    [Header("Optional Animation Settings")]
    public Animator animator;
    public string animationTriggerName;
    public Animation legacyAnimation;
    public AnimationClip animationClip;

    [Header("Sound Settings")]
    public AudioSource audioSource;        // AudioSource on this object
    public AudioClip soundBefore;          // Plays while objective is active
    public AudioClip soundAfter;           // Plays when objective completes
    public bool loopBeforeSound = true;    // Should the "before" sound loop?
    public bool loopAfterSound = false;    // Should the "after" sound loop?

    [Header("Additional One-Shot Completion Sound")]
    public AudioClip completionOneShotSound;   // Plays once on completion (different from the 'after' sound)
    public float completionOneShotVolume = 1f; // Optional volume control

    [Header("Scene Transition")]
    public string sceneToLoad;         // Leave empty to disable scene loading
    public float sceneLoadDelay = 0f;  // Delay before loading the scene

    [Header("Debug")]
    public bool showDebugInfo = false;
    
    [Tooltip("If true, this objective is always active (bypasses ObjectiveManager check) - for testing only")]
    public bool debugAlwaysActive = false;

    private bool canInteract = false;
    private bool completed = false;
    private ObjectiveOutline objectiveOutline;
    private Transform playerTransform;
    private Camera playerCamera;

    private void Awake()
    {
        // Cache the ObjectiveOutline component if present
        objectiveOutline = GetComponent<ObjectiveOutline>();
        
        // Use this transform if no interaction point specified
        if (interactionPoint == null)
            interactionPoint = transform;
    }

    private void Start()
    {
        // Find the player and camera
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            Debug.Log($"ObjectiveTrigger [{gameObject.name}]: Found player '{player.name}'");
        }
        else
        {
            Debug.LogWarning($"ObjectiveTrigger [{gameObject.name}]: No GameObject with 'Player' tag found!");
        }
        
        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            Debug.Log($"ObjectiveTrigger [{gameObject.name}]: Found main camera '{playerCamera.name}'");
        }
        else
        {
            Debug.LogWarning($"ObjectiveTrigger [{gameObject.name}]: Camera.main is null!");
        }
        
        // Check InteractionPromptUI
        if (InteractionPromptUI.Instance == null)
        {
            Debug.LogWarning($"ObjectiveTrigger [{gameObject.name}]: InteractionPromptUI.Instance is null! Make sure InteractionPromptUI is in the scene.");
        }
        else
        {
            Debug.Log($"ObjectiveTrigger [{gameObject.name}]: InteractionPromptUI found");
        }
        
        // Check ObjectiveManager
        if (ObjectiveManager.Instance == null)
        {
            Debug.LogWarning($"ObjectiveTrigger [{gameObject.name}]: ObjectiveManager.Instance is null! Make sure ObjectiveManager is in the scene.");
        }
        else
        {
            Debug.Log($"ObjectiveTrigger [{gameObject.name}]: ObjectiveManager found. Current objective index: {ObjectiveManager.Instance.CurrentObjectiveIndex}");
        }
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        if (sceneLoadDelay > 0f)
            yield return new WaitForSeconds(sceneLoadDelay);

        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
    }

    // Called by ObjectiveManager to initialize/reset objective
    public void AssignObjective(int index)
    {
        myObjectiveIndex = index;
        completed = false;

        // Handle sound for new active objective
        if (audioSource != null)
        {
            if (soundBefore != null)
            {
                audioSource.clip = soundBefore;
                audioSource.loop = loopBeforeSound;
                audioSource.Play();
            }
        }
    }

    // Checks if this objective is currently active
    private bool IsActiveObjective()
    {
        // Debug mode: always active
        if (debugAlwaysActive && !completed)
        {
            return true;
        }
        
        return ObjectiveManager.Instance != null &&
               ObjectiveManager.Instance.CurrentObjectiveIndex == myObjectiveIndex &&
               !completed;
    }

    private void Update()
    {
        // Debug: Log every few seconds to confirm Update is running
        if (showDebugInfo && Time.frameCount % 300 == 0)
        {
            Debug.Log($"ObjectiveTrigger [{gameObject.name}]: Update running. IsActiveObjective: {IsActiveObjective()}, myIndex: {myObjectiveIndex}, currentIndex: {ObjectiveManager.Instance?.CurrentObjectiveIndex ?? -1}");
        }
        
        if (!IsActiveObjective())
        {
            if (canInteract)
            {
                canInteract = false;
                InteractionPromptUI.Instance?.HideMessage();
            }
            return;
        }
        
        // Check if player is in range and looking at the objective
        bool wasCanInteract = canInteract;
        canInteract = CheckPlayerCanInteract();
        
        // Show/hide interaction prompt
        if (canInteract && !wasCanInteract)
        {
            Debug.Log($"ObjectiveTrigger [{gameObject.name}]: Showing interaction prompt");
            if (InteractionPromptUI.Instance != null)
            {
                InteractionPromptUI.Instance.ShowMessage(interactionMessage);
            }
            else
            {
                Debug.LogError($"ObjectiveTrigger [{gameObject.name}]: Cannot show prompt - InteractionPromptUI.Instance is null!");
            }
        }
        else if (!canInteract && wasCanInteract)
        {
            Debug.Log($"ObjectiveTrigger [{gameObject.name}]: Hiding interaction prompt");
            InteractionPromptUI.Instance?.HideMessage();
        }
        
        // Check for interaction input
        if (canInteract && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log($"ObjectiveTrigger [{gameObject.name}]: E pressed, completing objective");
            CompleteObjective();
        }
    }
    
    /// <summary>
    /// Check if the player is within range and looking at this objective
    /// </summary>
    private bool CheckPlayerCanInteract()
    {
        if (playerTransform == null || playerCamera == null)
        {
            // Try to find them again
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
            
            if (playerTransform == null || playerCamera == null)
                return false;
        }
        
        Vector3 targetPosition = interactionPoint.position;
        Vector3 playerPosition = playerTransform.position;
        
        // Check distance
        float distance = Vector3.Distance(playerPosition, targetPosition);
        if (distance > interactionRange)
        {
            if (showDebugInfo)
                Debug.Log($"ObjectiveTrigger [{gameObject.name}]: Out of range ({distance:F1} > {interactionRange})");
            return false;
        }
        
        // Check if player is looking at the objective
        Vector3 directionToTarget = (targetPosition - playerCamera.transform.position).normalized;
        Vector3 cameraForward = playerCamera.transform.forward;
        
        float angle = Vector3.Angle(cameraForward, directionToTarget);
        if (angle > lookAtAngle)
        {
            if (showDebugInfo)
                Debug.Log($"ObjectiveTrigger [{gameObject.name}]: Not looking at ({angle:F1}° > {lookAtAngle}°)");
            return false;
        }
        
        // Optional: Raycast to check if there's a clear line of sight
        if (useRaycast)
        {
            RaycastHit hit;
            Vector3 rayOrigin = playerCamera.transform.position;
            Vector3 rayDirection = directionToTarget;
            
            if (Physics.Raycast(rayOrigin, rayDirection, out hit, interactionRange, blockingLayers))
            {
                // Check if we hit this object or a child of this object
                if (!IsPartOfThisObject(hit.transform))
                {
                    if (showDebugInfo)
                        Debug.Log($"ObjectiveTrigger [{gameObject.name}]: Blocked by {hit.transform.name}");
                    return false;
                }
            }
        }
        
        if (showDebugInfo)
            Debug.Log($"ObjectiveTrigger [{gameObject.name}]: Can interact! Distance: {distance:F1}, Angle: {angle:F1}°");
        
        return true;
    }
    
    /// <summary>
    /// Check if a transform is this object or a child of this object
    /// </summary>
    private bool IsPartOfThisObject(Transform other)
    {
        Transform current = other;
        while (current != null)
        {
            if (current == transform)
                return true;
            current = current.parent;
        }
        return false;
    }

    private void CompleteObjective()
    {
        // Show subtitle on completion
        if (!string.IsNullOrEmpty(completionSubtitle))
        {
            SubtitleUI.Instance?.ShowSubtitle(completionSubtitle, subtitleDuration);
        }

        completed = true;
        canInteract = false;
        InteractionPromptUI.Instance?.HideMessage();
        
        // Disable the outline on this objective
        if (objectiveOutline != null)
        {
            objectiveOutline.OnObjectiveCompleted();
        }

        // Play Animator trigger if assigned
        if (animator != null && !string.IsNullOrEmpty(animationTriggerName))
            animator.SetTrigger(animationTriggerName);

        // Play legacy Animation clip if assigned
        if (legacyAnimation != null && animationClip != null)
        {
            legacyAnimation.Stop();
            legacyAnimation.clip = animationClip;
            legacyAnimation.Play();
        }

        // Notify ObjectiveManager
        ObjectiveManager.Instance?.CompleteObjective(myObjectiveIndex);

        // Turn off lights
        if (lightsToDisable != null)
        {
            foreach (Light l in lightsToDisable)
            {
                if (l != null)
                    l.intensity = 0f;
            }
        }

        // Turn ON lights
        if (lightsToEnable != null)
        {
            foreach (Light l in lightsToEnable)
            {
                if (l != null)
                    l.intensity = enabledLightIntensity;
            }
        }

        // Change emission color with strength multiplier
        if (emissionRenderers != null)
        {
            foreach (Renderer r in emissionRenderers)
            {
                if (r != null)
                {
                    foreach (Material mat in r.materials)
                    {
                        if (mat.HasProperty("_EmissionColor"))
                        {
                            Color finalEmission = emissionOffColor * emissionOffStrength;
                            mat.SetColor("_EmissionColor", finalEmission);

                            // Ensure emission keyword is enabled
                            mat.EnableKeyword("_EMISSION");
                        }
                    }
                }
            }
        }
        
        // Unhide optional objects
        if (objectsToUnhide != null)
        {
            foreach (GameObject obj in objectsToUnhide)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }

        // Handle sound switching
        if (audioSource != null)
        {
            // Stop before sound
            audioSource.Stop();

            // Play the additional ONE-SHOT completion sound first
            if (completionOneShotSound != null)
            {
                audioSource.PlayOneShot(completionOneShotSound, completionOneShotVolume);
            }

            // Handle AFTER sound (looping or one-shot)
            if (soundAfter != null)
            {
                if (loopAfterSound)
                {
                    // Loop the after sound
                    audioSource.clip = soundAfter;
                    audioSource.loop = true;
                    audioSource.Play();
                }
                else
                {
                    // Play the after sound once
                    audioSource.loop = false;
                    audioSource.PlayOneShot(soundAfter);
                }
            }
        }
        
        // Play cutscene if assigned
        if (cutsceneToPlay != null)
        {
            cutsceneToPlay.StartCutscene();
        }

        // Load scene if assigned
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            StartCoroutine(LoadSceneAfterDelay());
        }
    }

#if UNITY_EDITOR
    // Draw interaction range in editor
    private void OnDrawGizmosSelected()
    {
        Vector3 center = interactionPoint != null ? interactionPoint.position : transform.position;
        
        // Draw interaction range sphere
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(center, interactionRange);
        
        // Draw look-at cone (approximate)
        Gizmos.color = new Color(1, 1, 0, 0.5f);
        Vector3 forward = Vector3.forward;
        float coneLength = interactionRange;
        float coneRadius = Mathf.Tan(lookAtAngle * Mathf.Deg2Rad) * coneLength;
        
        // Draw cone lines
        Gizmos.DrawLine(center, center + forward * coneLength + Vector3.up * coneRadius);
        Gizmos.DrawLine(center, center + forward * coneLength - Vector3.up * coneRadius);
        Gizmos.DrawLine(center, center + forward * coneLength + Vector3.right * coneRadius);
        Gizmos.DrawLine(center, center + forward * coneLength - Vector3.right * coneRadius);
    }
#endif
}
