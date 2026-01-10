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
    [Tooltip("Main AudioSource for looping sounds (before/after)")]
    public AudioSource audioSource;        // AudioSource on this object
    
    [Header("Before Sounds (While Objective is Active)")]
    [Tooltip("Sounds that play while the objective is active (before completion)")]
    public AudioClip[] soundsBefore;       // Multiple sounds that play while objective is active
    [Tooltip("Should the before sounds loop?")]
    public bool loopBeforeSounds = true;
    [Tooltip("Volume for before sounds")]
    [Range(0f, 1f)]
    public float beforeSoundsVolume = 1f;
    
    [Header("After Sounds (When Objective Completes)")]
    [Tooltip("Sounds that play when the objective is completed")]
    public AudioClip[] soundsAfter;        // Multiple sounds that play when objective completes
    [Tooltip("Should the after sounds loop?")]
    public bool loopAfterSounds = false;
    [Tooltip("Volume for after sounds")]
    [Range(0f, 1f)]
    public float afterSoundsVolume = 1f;

    [Header("One-Shot Completion Sounds")]
    [Tooltip("Additional sounds that play once on completion (played via PlayOneShot)")]
    public AudioClip[] completionOneShotSounds;
    [Tooltip("Volume for one-shot completion sounds")]
    [Range(0f, 1f)]
    public float completionOneShotVolume = 1f;
    
    [Header("Additional Audio Sources")]
    [Tooltip("Extra AudioSources for playing multiple sounds simultaneously")]
    public AudioSource[] additionalAudioSources;

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
    private AudioSource[] allAudioSources; // Combined array of main + additional audio sources

    private void Awake()
    {
        // Cache the ObjectiveOutline component if present
        objectiveOutline = GetComponent<ObjectiveOutline>();
        
        // Use this transform if no interaction point specified
        if (interactionPoint == null)
            interactionPoint = transform;
        
        // Build combined audio sources array
        BuildAudioSourcesArray();
    }
    
    /// <summary>
    /// Builds the combined array of all available audio sources
    /// </summary>
    private void BuildAudioSourcesArray()
    {
        int count = (audioSource != null ? 1 : 0) + (additionalAudioSources != null ? additionalAudioSources.Length : 0);
        allAudioSources = new AudioSource[count];
        
        int index = 0;
        if (audioSource != null)
        {
            allAudioSources[index++] = audioSource;
        }
        
        if (additionalAudioSources != null)
        {
            foreach (AudioSource source in additionalAudioSources)
            {
                if (source != null)
                {
                    allAudioSources[index++] = source;
                }
            }
        }
    }

    private void Start()
    {
        // Find the player and camera
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        playerCamera = Camera.main;
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

        // Handle sounds for new active objective
        PlayBeforeSounds();
    }
    
    /// <summary>
    /// Play all "before" sounds when the objective becomes active
    /// </summary>
    private void PlayBeforeSounds()
    {
        if (soundsBefore == null || soundsBefore.Length == 0) return;
        
        // Stop any currently playing sounds first
        StopAllSounds();
        
        // Play each before sound on an available audio source
        for (int i = 0; i < soundsBefore.Length; i++)
        {
            AudioClip clip = soundsBefore[i];
            if (clip == null) continue;
            
            AudioSource source = GetAudioSource(i);
            if (source != null)
            {
                source.clip = clip;
                source.loop = loopBeforeSounds;
                source.volume = beforeSoundsVolume;
                source.Play();
            }
            else
            {
                Debug.LogWarning($"ObjectiveTrigger [{gameObject.name}]: Not enough AudioSources to play all before sounds. Need {soundsBefore.Length}, have {allAudioSources.Length}");
                break;
            }
        }
    }
    
    /// <summary>
    /// Play all "after" sounds when the objective is completed
    /// </summary>
    private void PlayAfterSounds()
    {
        if (soundsAfter == null || soundsAfter.Length == 0) return;
        
        // Play each after sound on an available audio source
        for (int i = 0; i < soundsAfter.Length; i++)
        {
            AudioClip clip = soundsAfter[i];
            if (clip == null) continue;
            
            AudioSource source = GetAudioSource(i);
            if (source != null)
            {
                if (loopAfterSounds)
                {
                    source.clip = clip;
                    source.loop = true;
                    source.volume = afterSoundsVolume;
                    source.Play();
                }
                else
                {
                    source.loop = false;
                    source.PlayOneShot(clip, afterSoundsVolume);
                }
            }
            else
            {
                Debug.LogWarning($"ObjectiveTrigger [{gameObject.name}]: Not enough AudioSources to play all after sounds. Need {soundsAfter.Length}, have {allAudioSources.Length}");
                break;
            }
        }
    }
    
    /// <summary>
    /// Play all one-shot completion sounds
    /// </summary>
    private void PlayCompletionOneShotSounds()
    {
        if (completionOneShotSounds == null || completionOneShotSounds.Length == 0) return;
        
        // Play all one-shot sounds on the main audio source (they can overlap with PlayOneShot)
        if (audioSource != null)
        {
            foreach (AudioClip clip in completionOneShotSounds)
            {
                if (clip != null)
                {
                    audioSource.PlayOneShot(clip, completionOneShotVolume);
                }
            }
        }
    }
    
    /// <summary>
    /// Stop all sounds on all audio sources
    /// </summary>
    private void StopAllSounds()
    {
        if (allAudioSources == null) return;
        
        foreach (AudioSource source in allAudioSources)
        {
            if (source != null)
            {
                source.Stop();
            }
        }
    }
    
    /// <summary>
    /// Get an audio source by index (returns null if index is out of range)
    /// </summary>
    private AudioSource GetAudioSource(int index)
    {
        if (allAudioSources == null || index >= allAudioSources.Length)
            return null;
        return allAudioSources[index];
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
            InteractionPromptUI.Instance?.ShowMessage(interactionMessage);
        }
        else if (!canInteract && wasCanInteract)
        {
            InteractionPromptUI.Instance?.HideMessage();
        }
        
        // Check for interaction input
        if (canInteract && Input.GetKeyDown(KeyCode.E))
        {
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
            return false;
        }
        
        // Check if player is looking at the objective
        Vector3 directionToTarget = (targetPosition - playerCamera.transform.position).normalized;
        Vector3 cameraForward = playerCamera.transform.forward;
        
        float angle = Vector3.Angle(cameraForward, directionToTarget);
        if (angle > lookAtAngle)
        {
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
                    return false;
                }
            }
        }
        
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
        // Stop all before sounds
        StopAllSounds();
        
        // Play the one-shot completion sounds first
        PlayCompletionOneShotSounds();
        
        // Play the after sounds
        PlayAfterSounds();
        
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
