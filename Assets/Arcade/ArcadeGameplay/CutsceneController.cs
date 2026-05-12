using UnityEngine;
using System;
using System.Collections; // Needed for IEnumerator

public class CutsceneController : MonoBehaviour
{
    public static CutsceneController Instance { get; private set; }
    
    /// <summary>
    /// Event fired when the cutscene ends and gameplay begins
    /// </summary>
    public event Action OnCutsceneEnded;
    
    [Header("Cameras")]
    public Camera mainCamera;          // Your player camera
    public Camera cutsceneCamera;      // Camera used for cutscene

    [Header("Animator")]
    public Animator cutsceneAnimator;  // Animator attached to cutscene camera
    
    [Tooltip("Name of the animation clip to play")]
    public string animationClipName = "IntroCutscene";
    
    [Tooltip("The animation clip to get the exact duration (optional - if not set, uses fallback duration)")]
    public AnimationClip cutsceneAnimationClip;

    [Header("Settings")]
    [Tooltip("If true, automatically detects when the animation ends. If false, uses the fallback duration.")]
    public bool autoDetectAnimationEnd = true;
    
    [Tooltip("Fallback duration if auto-detect is disabled or animation clip is not assigned")]
    public float fallbackDuration = 5f;

    [Header("Subtitles")]
    [TextArea(1, 3)]
    public string subtitleText;
    public float subtitleDuration = 3f;

    [Header("FPS Controller Teleport")]
    [Tooltip("If assigned, the FPS controller will teleport to this transform at the end of the cutscene")]
    public Transform cutsceneEndTransform;
    
    [Tooltip("Height offset for the player (to account for CharacterController height)")]
    public float playerHeightOffset = 0f;

    // Cached reference to FPS controller
    private EasyPeasyFirstPersonController.FirstPersonController fpsController;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[CutsceneController] Multiple instances detected. Using first instance.");
        }
    }

    /// <summary>
    /// Call this method to start the cutscene
    /// </summary>
    public void StartCutscene()
    {
        // Cache the FPS controller reference
        if (mainCamera != null)
            fpsController = mainCamera.GetComponentInParent<EasyPeasyFirstPersonController.FirstPersonController>();
        
        // Disable player control
        if (fpsController != null)
        {
            fpsController.SetControl(false);
        }

        StartCoroutine(CutsceneRoutine());
    }

    // Note: StartCutscene() is now called by MainMenuManager after the menu fades out.
    // If you want the cutscene to auto-start without a menu, uncomment the line below:
    // void Start()
    // {
    //     StartCutscene();
    // }

    private IEnumerator CutsceneRoutine()
    {
        // Disable FPS camera and player movement
        // Use gameObject.SetActive to ensure it works even if MainMenuManager disabled the GameObject
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(false);
        }
        if (fpsController != null)
            fpsController.SetControl(false); // disable movement & look

        // Enable cutscene camera
        if (cutsceneCamera != null)
        {
            cutsceneCamera.gameObject.SetActive(true);
        }

        // Play cutscene animation if assigned
        float animationDuration = fallbackDuration;
        
        if (cutsceneAnimator != null && !string.IsNullOrEmpty(animationClipName))
        {
            cutsceneAnimator.Play(animationClipName);
            
            // Get animation duration
            if (autoDetectAnimationEnd)
            {
                animationDuration = GetAnimationDuration();
            }
        }

        // Notify ObjectiveManager that cutscene has started (shows subtitle)
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.OnCutsceneStart();
        }
        // Fallback: Show subtitle directly if ObjectiveManager doesn't handle it
        else if (!string.IsNullOrEmpty(subtitleText) && SubtitleUI.Instance != null)
        {
            SubtitleUI.Instance.ShowSubtitle(subtitleText, subtitleDuration);
        }
        else
        {
            Debug.LogWarning("CutsceneController: No ObjectiveManager or SubtitleUI available!");
        }

        // Wait for the animation to complete
        if (autoDetectAnimationEnd && cutsceneAnimator != null)
        {
            yield return StartCoroutine(WaitForAnimationToEnd());
        }
        else
        {
            yield return new WaitForSeconds(animationDuration);
        }

        // ONE-FRAME FREEZE: Pause the cutscene animator on its final pose and
        // hold for a frame. This eliminates any residual motion in the final
        // keyframe (e.g. non-zero velocity at the last frame of the clip) which
        // would otherwise read as a "jitter" in the moment we swap cameras.
        if (cutsceneAnimator != null)
        {
            cutsceneAnimator.speed = 0f;
        }
        yield return null; // hold the frozen pose for exactly one frame
        
        // SEAMLESS CAMERA SWITCH:
        // The previous implementation disabled the cutscene camera, enabled the FPS
        // camera, and THEN teleported the player. That left a one-frame window
        // where the FPS camera rendered at its OLD position -> visible jitter/pop.
        //
        // The new order is:
        //   1. Teleport + align the FPS player (while the FPS camera is still
        //      disabled, so no rendering happens at the wrong pose).
        //   2. Enable the FPS camera AND disable the cutscene camera in the same
        //      frame, after the transform is already correct.
        //   3. Restore control.
        
        // 1. Move the FPS controller into position while its camera is still off.
        if (fpsController != null)
        {
            TeleportPlayerToCutsceneEnd();
        }
        
        // 2. Swap cameras in a single frame. Enable the FPS camera first so we
        //    never have zero active cameras (which can cause a black flash in
        //    some render pipelines), then disable the cutscene camera.
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(true);
        }
        if (cutsceneCamera != null)
        {
            cutsceneCamera.gameObject.SetActive(false);
        }
        
        // Force a physics/transform sync so the FPS camera's first rendered frame
        // uses the new position rather than a stale one from before the teleport.
        Physics.SyncTransforms();
        
        // 3. Re-enable input now that the camera is already looking at the right spot.
        if (fpsController != null)
        {
            fpsController.SetControl(true);
        }
        
        // Reapply sensitivity settings after camera switch
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ReapplySensitivity();
        }

        // Notify ObjectiveManager that cutscene has ended (shows objective UI)
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.OnCutsceneEnd();
        }
        else
        {
            Debug.LogWarning("CutsceneController: ObjectiveManager.Instance is null at cutscene end!");
        }
        
        // Fire the OnCutsceneEnded event for any listeners (e.g., ControlsSlideInPanel)
        OnCutsceneEnded?.Invoke();
    }

    /// <summary>
    /// Gets the duration of the cutscene animation
    /// </summary>
    private float GetAnimationDuration()
    {
        // First, try to use the assigned animation clip
        if (cutsceneAnimationClip != null)
        {
            return cutsceneAnimationClip.length;
        }
        
        // Try to find the clip in the animator
        if (cutsceneAnimator != null)
        {
            // Get all clips from the animator's runtime controller
            AnimatorClipInfo[] clipInfo = cutsceneAnimator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length > 0)
            {
                return clipInfo[0].clip.length;
            }
            
            // Alternative: search through the animator controller's clips
            RuntimeAnimatorController controller = cutsceneAnimator.runtimeAnimatorController;
            if (controller != null)
            {
                foreach (AnimationClip clip in controller.animationClips)
                {
                    if (clip.name == animationClipName || clip.name.Contains(animationClipName))
                    {
                        return clip.length;
                    }
                }
            }
        }
        
        // Fallback to the configured duration
        Debug.LogWarning($"CutsceneController: Could not find animation clip '{animationClipName}', using fallback duration");
        return fallbackDuration;
    }

    /// <summary>
    /// Waits for the current animation to finish playing
    /// </summary>
    private IEnumerator WaitForAnimationToEnd()
    {
        if (cutsceneAnimator == null)
        {
            yield return new WaitForSeconds(fallbackDuration);
            yield break;
        }

        // Wait one frame for the animator to start playing
        yield return null;

        // Get the current animator state info
        AnimatorStateInfo stateInfo = cutsceneAnimator.GetCurrentAnimatorStateInfo(0);
        
        // Wait until the animation is no longer playing or has completed
        while (true)
        {
            stateInfo = cutsceneAnimator.GetCurrentAnimatorStateInfo(0);
            
            // Check if the animation has completed (normalizedTime >= 1 means it finished)
            // Also check if it's still the same animation state
            if (stateInfo.normalizedTime >= 1f && !cutsceneAnimator.IsInTransition(0))
            {
                break;
            }
            
            yield return null;
        }
    }

    /// <summary>
    /// Teleports the FPS controller so that its camera matches the cutscene end transform position and rotation.
    /// Order matters: we must (1) reset transient camera state, (2) apply rotation, (3) then measure the
    /// camera offset and position the player. Doing it in any other order leaves a mismatch between the
    /// cutscene camera pose and the FPS camera pose on the swap frame -> visible jitter.
    /// </summary>
    private void TeleportPlayerToCutsceneEnd()
    {
        if (fpsController == null) return;

        // Determine the target transform (use cutsceneEndTransform if assigned, otherwise use cutscene camera)
        Transform targetTransform = cutsceneEndTransform != null ? cutsceneEndTransform : cutsceneCamera.transform;

        if (targetTransform == null) return;

        CharacterController characterController = fpsController.GetComponent<CharacterController>();
        if (characterController != null)
        {
            // Disable CharacterController temporarily to allow direct transform writes.
            characterController.enabled = false;
        }

        // STEP 1: Reset all transient camera state (head bob, recoil, tilt, FOV, cameraParent local
        // pose) BEFORE measuring the camera offset. Otherwise cameraParent.localPosition.y might be
        // mid-bob or crouched, and our offset calculation below will be based on that stale pose.
        // After this call, cameraParent is at its rest local pose (localPosition.y = originalHeight,
        // localRotation = identity).
        fpsController.ResetCameraTransientState();

        // STEP 2: Apply rotation from the cutscene camera. This rotates the player root (yaw) and
        // playerCamera.localRotation (pitch). It MUST happen before we measure playerCamera.position,
        // because rotating the player root also rotates the camera in world space around the player
        // origin. Measuring the offset before rotation and moving after would leave a position
        // mismatch equal to the yaw-rotated radius.
        fpsController.SetCameraRotation(targetTransform, false);

        // Force transforms to update so playerCamera.position reflects the new rotation.
        Physics.SyncTransforms();

        // STEP 3: Now that the camera is at its rest local pose and correct rotation, compute the
        // world-space offset from the player root to the camera. Moving the player root by
        // (targetPos - offset) places the camera exactly at targetPos.
        Vector3 cameraWorldOffset = Vector3.zero;
        if (fpsController.playerCamera != null)
        {
            cameraWorldOffset = fpsController.playerCamera.position - fpsController.transform.position;
        }

        Vector3 playerPosition = targetTransform.position - cameraWorldOffset;
        playerPosition.y += playerHeightOffset;
        fpsController.transform.position = playerPosition;

        // One more sync so the camera's world position is up-to-date before the render swap.
        Physics.SyncTransforms();

        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }
}
