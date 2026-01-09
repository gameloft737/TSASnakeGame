using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Ending Cutscene Trigger - Handles the transition from FPS gameplay to ending.
///
/// When the player enters this trigger:
/// 1. Disables player control
/// 2. Forces the FPS camera to look at a target transform
/// 3. Animates the FPS camera FOV through multiple keyframes
/// 4. Fades to white immediately after FOV animation
/// 5. Loads the snake scene
/// </summary>
public class EndingCutsceneTrigger : MonoBehaviour
{
    [System.Serializable]
    public class FOVKeyframe
    {
        [Tooltip("The FOV value at this keyframe")]
        public float fov = 60f;
        
        [Tooltip("Duration to reach this FOV from the previous keyframe (in seconds)")]
        public float duration = 1f;
        
        [Tooltip("Easing curve for this transition (0-1 on both axes)")]
        public AnimationCurve easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        public FOVKeyframe() { }
        
        public FOVKeyframe(float fov, float duration)
        {
            this.fov = fov;
            this.duration = duration;
            this.easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
    }

    [Header("Player References")]
    [Tooltip("The FPS Controller GameObject (player)")]
    public GameObject fpsController;
    
    [Tooltip("The main FPS camera")]
    public Camera fpsCamera;

    [Header("Look At Target")]
    [Tooltip("The transform the camera will look at when triggered")]
    public Transform lookAtTarget;
    
    [Tooltip("How fast the camera rotates to look at the target (degrees per second). Set to 0 for instant.")]
    public float lookAtSpeed = 90f;
    
    [Tooltip("If true, smoothly rotates to target. If false, snaps instantly.")]
    public bool smoothLookAt = true;

    [Header("FOV Animation")]
    [Tooltip("Enable FOV animation during the cutscene")]
    public bool animateFOV = true;
    
    [Tooltip("List of FOV keyframes. The animation will transition through each one in order.\n" +
             "Example: Start at 60, zoom out to 90, then zoom in to 30")]
    public List<FOVKeyframe> fovKeyframes = new List<FOVKeyframe>()
    {
        new FOVKeyframe(60f, 0f),    // Starting FOV (duration 0 = instant)
        new FOVKeyframe(90f, 1.5f),  // Zoom out
        new FOVKeyframe(30f, 1.5f)   // Zoom in
    };

    [Header("Fade Settings")]
    [Tooltip("The Image component used for the fade effect (should be a full-screen white image)")]
    public Image fadeImage;
    
    [Tooltip("Duration of the fade to white effect")]
    public float fadeDuration = 1.5f;
    
    [Tooltip("Color to fade to (default is white)")]
    public Color fadeColor = Color.white;
    
    [Tooltip("How many seconds before the FOV animation ends should the fade start?\n" +
             "Example: 0.5 means fade starts 0.5 seconds before FOV animation completes")]
    public float fadeStartOffset = 0.5f;
    
    [Tooltip("Delay after fade completes before loading scene")]
    public float postFadeDelay = 0.5f;

    [Header("Scene Settings")]
    [Tooltip("Name of the snake scene to load")]
    public string snakeSceneName = "SnakeScene";

    [Header("Audio (Optional)")]
    [Tooltip("Optional audio source for ending music/sound")]
    public AudioSource endingAudioSource;
    
    [Tooltip("Optional audio clip to play during the ending")]
    public AudioClip endingAudioClip;

    // Private variables
    private bool hasTriggered = false;
    private EasyPeasyFirstPersonController.FirstPersonController fpsControllerScript;
    private float originalFOV;

    private void Start()
    {
        // Cache the FPS controller script
        if (fpsController != null)
        {
            fpsControllerScript = fpsController.GetComponent<EasyPeasyFirstPersonController.FirstPersonController>();
        }

        // Initialize fade image
        if (fadeImage != null)
        {
            // Set initial state - fully transparent
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            fadeImage.gameObject.SetActive(false);
        }

        // Store original FOV
        if (fpsCamera != null)
        {
            originalFOV = fpsCamera.fieldOfView;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the player entered the trigger
        if (hasTriggered) return;

        // Check if it's the player (either by reference or by tag)
        bool isPlayer = false;
        
        if (fpsController != null && other.gameObject == fpsController)
        {
            isPlayer = true;
        }
        else if (other.CompareTag("Player"))
        {
            isPlayer = true;
            // If fpsController wasn't assigned, try to get it from the collider
            if (fpsController == null)
            {
                fpsController = other.gameObject;
                fpsControllerScript = fpsController.GetComponent<EasyPeasyFirstPersonController.FirstPersonController>();
            }
        }
        else if (other.transform.root.CompareTag("Player"))
        {
            isPlayer = true;
            if (fpsController == null)
            {
                fpsController = other.transform.root.gameObject;
                fpsControllerScript = fpsController.GetComponent<EasyPeasyFirstPersonController.FirstPersonController>();
            }
        }

        if (isPlayer)
        {
            // Try to get the FPS camera if not assigned
            if (fpsCamera == null && fpsControllerScript != null)
            {
                fpsCamera = fpsControllerScript.playerCamera?.GetComponent<Camera>();
            }
            
            hasTriggered = true;
            StartCoroutine(EndingCutsceneSequence());
        }
    }

    private IEnumerator EndingCutsceneSequence()
    {
        Debug.Log("EndingCutsceneTrigger: Starting ending cutscene sequence");

        // Play ending audio if assigned
        if (endingAudioSource != null && endingAudioClip != null)
        {
            endingAudioSource.clip = endingAudioClip;
            endingAudioSource.Play();
        }

        // Step 1: Disable player control
        DisablePlayerControl();

        // Step 2: Force camera to look at target while animating FOV
        // The fade will start automatically during the FOV animation (based on fadeStartOffset)
        yield return StartCoroutine(LookAtTargetAndAnimateFOVWithFade());

        // Step 3: Wait for fade to complete if it hasn't already
        // (fade runs in parallel with the end of FOV animation)
        while (isFading)
        {
            yield return null;
        }

        // Step 4: Wait a moment after fade
        yield return new WaitForSeconds(postFadeDelay);

        // Step 5: Load the snake scene
        LoadSnakeScene();
    }
    
    private bool isFading = false;
    private bool fadeStarted = false;

    private void DisablePlayerControl()
    {
        Debug.Log("EndingCutsceneTrigger: Disabling player control");

        // Disable FPS controller movement and look
        if (fpsControllerScript != null)
        {
            fpsControllerScript.SetControl(false);
        }

        // Lock and hide cursor for cinematic feel
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Gets the total duration of all FOV keyframe animations
    /// </summary>
    public float GetTotalFOVDuration()
    {
        float total = 0f;
        foreach (var keyframe in fovKeyframes)
        {
            total += keyframe.duration;
        }
        return total;
    }

    private IEnumerator LookAtTargetAndAnimateFOVWithFade()
    {
        Debug.Log("EndingCutsceneTrigger: Starting look at target and FOV animation with fade");

        fadeStarted = false;
        
        if (fpsCamera == null)
        {
            Debug.LogWarning("EndingCutsceneTrigger: No FPS camera assigned!");
            yield return new WaitForSeconds(GetTotalFOVDuration());
            yield break;
        }

        // Get the camera's transform (or the parent that controls rotation)
        Transform cameraTransform = fpsCamera.transform;
        Transform playerBody = fpsController?.transform;

        // Set starting FOV from first keyframe
        if (animateFOV && fovKeyframes.Count > 0)
        {
            fpsCamera.fieldOfView = fovKeyframes[0].fov;
        }

        // Calculate total duration and when to start fade
        float totalDuration = GetTotalFOVDuration();
        float fadeStartTime = Mathf.Max(0, totalDuration - fadeStartOffset);
        float elapsedTotal = 0f;
        
        Debug.Log($"EndingCutsceneTrigger: Total FOV duration: {totalDuration}s, Fade starts at: {fadeStartTime}s");

        // Animate through each keyframe
        for (int i = 0; i < fovKeyframes.Count - 1; i++)
        {
            FOVKeyframe fromKeyframe = fovKeyframes[i];
            FOVKeyframe toKeyframe = fovKeyframes[i + 1];
            
            float startFOV = fromKeyframe.fov;
            float endFOV = toKeyframe.fov;
            float duration = toKeyframe.duration;
            AnimationCurve curve = toKeyframe.easingCurve;
            
            Debug.Log($"EndingCutsceneTrigger: FOV transition {i + 1}/{fovKeyframes.Count - 1}: {startFOV} -> {endFOV} over {duration}s");
            
            if (duration <= 0)
            {
                // Instant transition
                if (animateFOV)
                {
                    fpsCamera.fieldOfView = endFOV;
                }
                continue;
            }
            
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                elapsedTotal += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Check if it's time to start the fade
                if (!fadeStarted && elapsedTotal >= fadeStartTime)
                {
                    fadeStarted = true;
                    StartCoroutine(FadeToWhite());
                    Debug.Log($"EndingCutsceneTrigger: Starting fade at {elapsedTotal}s (offset: {fadeStartOffset}s before end)");
                }

                // Animate FOV using the keyframe's easing curve
                if (animateFOV)
                {
                    float curveValue = curve.Evaluate(t);
                    fpsCamera.fieldOfView = Mathf.Lerp(startFOV, endFOV, curveValue);
                }

                // Look at target (continues throughout all keyframes)
                UpdateLookAtTarget(cameraTransform, playerBody);

                yield return null;
            }

            // Ensure we end at the exact target FOV for this keyframe
            if (animateFOV)
            {
                fpsCamera.fieldOfView = endFOV;
            }
        }

        // If fade hasn't started yet (e.g., fadeStartOffset was larger than total duration), start it now
        if (!fadeStarted)
        {
            fadeStarted = true;
            StartCoroutine(FadeToWhite());
        }

        Debug.Log("EndingCutsceneTrigger: Look at and FOV animation complete");
    }

    private void UpdateLookAtTarget(Transform cameraTransform, Transform playerBody)
    {
        if (lookAtTarget == null) return;
        
        Vector3 directionToTarget = lookAtTarget.position - cameraTransform.position;
        
        if (directionToTarget.sqrMagnitude <= 0.001f) return;
        
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        
        if (smoothLookAt && lookAtSpeed > 0)
        {
            // Smooth rotation over time
            float rotationStep = lookAtSpeed * Time.deltaTime;
            
            if (playerBody != null)
            {
                // Rotate player body for horizontal (Y axis)
                Vector3 flatDirection = new Vector3(directionToTarget.x, 0, directionToTarget.z);
                if (flatDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion horizontalTarget = Quaternion.LookRotation(flatDirection);
                    playerBody.rotation = Quaternion.RotateTowards(playerBody.rotation, horizontalTarget, rotationStep);
                }
                
                // Rotate camera for vertical (X axis) - local rotation
                float targetPitch = -Mathf.Asin(directionToTarget.normalized.y) * Mathf.Rad2Deg;
                float currentPitch = cameraTransform.localEulerAngles.x;
                if (currentPitch > 180) currentPitch -= 360;
                float newPitch = Mathf.MoveTowards(currentPitch, targetPitch, rotationStep);
                cameraTransform.localRotation = Quaternion.Euler(newPitch, 0, 0);
            }
            else
            {
                // Just rotate the camera directly
                cameraTransform.rotation = Quaternion.RotateTowards(cameraTransform.rotation, targetRotation, rotationStep);
            }
        }
        else
        {
            // Instant snap to target
            if (playerBody != null)
            {
                Vector3 flatDirection = new Vector3(directionToTarget.x, 0, directionToTarget.z);
                if (flatDirection.sqrMagnitude > 0.001f)
                {
                    playerBody.rotation = Quaternion.LookRotation(flatDirection);
                }
                
                float targetPitch = -Mathf.Asin(directionToTarget.normalized.y) * Mathf.Rad2Deg;
                cameraTransform.localRotation = Quaternion.Euler(targetPitch, 0, 0);
            }
            else
            {
                cameraTransform.rotation = targetRotation;
            }
        }
    }

    private IEnumerator FadeToWhite()
    {
        Debug.Log("EndingCutsceneTrigger: Starting fade to white");
        isFading = true;

        if (fadeImage == null)
        {
            Debug.LogWarning("EndingCutsceneTrigger: No fade image assigned, skipping fade");
            isFading = false;
            yield break;
        }

        // Activate and prepare the fade image
        fadeImage.gameObject.SetActive(true);
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);

        // Fade in (from transparent to opaque)
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }

        // Ensure fully opaque at the end
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);

        Debug.Log("EndingCutsceneTrigger: Fade to white complete");
        isFading = false;
    }

    private void LoadSnakeScene()
    {
        Debug.Log($"EndingCutsceneTrigger: Loading scene '{snakeSceneName}'");

        if (!string.IsNullOrEmpty(snakeSceneName))
        {
            SceneManager.LoadScene(snakeSceneName);
        }
        else
        {
            Debug.LogError("EndingCutsceneTrigger: No scene name specified!");
        }
    }

    /// <summary>
    /// Call this method to manually trigger the ending cutscene (useful for testing)
    /// </summary>
    public void TriggerEndingCutscene()
    {
        if (!hasTriggered)
        {
            hasTriggered = true;
            StartCoroutine(EndingCutsceneSequence());
        }
    }

    /// <summary>
    /// Reset the trigger (useful for testing)
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
        
        // Reset FOV to original
        if (fpsCamera != null)
        {
            fpsCamera.fieldOfView = originalFOV;
        }
    }

    // Visualize the trigger in the editor
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Orange with transparency
            
            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw connection lines to referenced objects when selected
        Gizmos.color = Color.yellow;
        
        if (fpsController != null)
        {
            Gizmos.DrawLine(transform.position, fpsController.transform.position);
        }
        
        if (lookAtTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, lookAtTarget.position);
            Gizmos.DrawWireSphere(lookAtTarget.position, 0.5f);
        }
    }
}