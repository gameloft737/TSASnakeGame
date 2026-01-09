using UnityEngine;
using System.Collections; // Needed for IEnumerator

public class CutsceneController : MonoBehaviour
{
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
        Debug.Log("CutsceneController: CutsceneRoutine started");
        
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
            Debug.Log($"CutsceneController: Playing animation '{animationClipName}'");
            cutsceneAnimator.Play(animationClipName);
            
            // Get animation duration
            if (autoDetectAnimationEnd)
            {
                animationDuration = GetAnimationDuration();
                Debug.Log($"CutsceneController: Auto-detected animation duration: {animationDuration}s");
            }
        }

        // Notify ObjectiveManager that cutscene has started (shows subtitle)
        Debug.Log($"CutsceneController: ObjectiveManager.Instance = {(ObjectiveManager.Instance != null ? "exists" : "NULL")}");
        Debug.Log($"CutsceneController: SubtitleUI.Instance = {(SubtitleUI.Instance != null ? "exists" : "NULL")}");
        
        if (ObjectiveManager.Instance != null)
        {
            Debug.Log("CutsceneController: Calling ObjectiveManager.OnCutsceneStart()");
            ObjectiveManager.Instance.OnCutsceneStart();
        }
        // Fallback: Show subtitle directly if ObjectiveManager doesn't handle it
        else if (!string.IsNullOrEmpty(subtitleText) && SubtitleUI.Instance != null)
        {
            Debug.Log("CutsceneController: Showing subtitle directly (fallback)");
            SubtitleUI.Instance.ShowSubtitle(subtitleText, subtitleDuration);
        }
        else
        {
            Debug.LogWarning("CutsceneController: No ObjectiveManager or SubtitleUI available!");
        }

        // Wait for the animation to complete
        if (autoDetectAnimationEnd && cutsceneAnimator != null)
        {
            Debug.Log($"CutsceneController: Waiting for animation to complete...");
            yield return StartCoroutine(WaitForAnimationToEnd());
        }
        else
        {
            Debug.Log($"CutsceneController: Waiting {animationDuration} seconds (fallback duration)...");
            yield return new WaitForSeconds(animationDuration);
        }

        Debug.Log("CutsceneController: Cutscene ended, transitioning to gameplay");
        
        // Cutscene ends: disable cutscene camera
        if (cutsceneCamera != null)
        {
            cutsceneCamera.gameObject.SetActive(false);
        }

        // Enable FPS camera (use SetActive to ensure it works)
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(true);
        }

        // Teleport FPS controller to cutscene end position for seamless transition
        if (fpsController != null)
        {
            TeleportPlayerToCutsceneEnd();
            fpsController.SetControl(true);
        }

        // Notify ObjectiveManager that cutscene has ended (shows objective UI)
        if (ObjectiveManager.Instance != null)
        {
            Debug.Log("CutsceneController: Calling ObjectiveManager.OnCutsceneEnd()");
            ObjectiveManager.Instance.OnCutsceneEnd();
        }
        else
        {
            Debug.LogWarning("CutsceneController: ObjectiveManager.Instance is null at cutscene end!");
        }
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
                Debug.Log("CutsceneController: Animation completed (normalizedTime >= 1)");
                break;
            }
            
            yield return null;
        }
    }

    /// <summary>
    /// Teleports the FPS controller so that its camera matches the cutscene end transform position and rotation
    /// </summary>
    private void TeleportPlayerToCutsceneEnd()
    {
        if (fpsController == null) return;

        // Determine the target transform (use cutsceneEndTransform if assigned, otherwise use cutscene camera)
        Transform targetTransform = cutsceneEndTransform != null ? cutsceneEndTransform : cutsceneCamera.transform;

        if (targetTransform == null) return;

        // Get the CharacterController to properly teleport
        CharacterController characterController = fpsController.GetComponent<CharacterController>();
        
        if (characterController != null)
        {
            // Disable CharacterController temporarily to allow position change
            characterController.enabled = false;
        }
        
        // The target transform represents where the CAMERA should be (eye level)
        // We need to position the player so that their camera ends up at this position
        
        // Get the camera's local position relative to the player
        Vector3 cameraLocalPos = Vector3.zero;
        if (fpsController.playerCamera != null)
        {
            // Get the world offset from player root to camera
            cameraLocalPos = fpsController.playerCamera.position - fpsController.transform.position;
        }
        
        // Calculate where the player root should be so that the camera ends up at targetTransform.position
        Vector3 playerPosition = targetTransform.position - cameraLocalPos;
        playerPosition.y += playerHeightOffset;
        
        // Teleport the player
        fpsController.transform.position = playerPosition;
        
        Debug.Log($"CutsceneController: Target camera position: {targetTransform.position}");
        Debug.Log($"CutsceneController: Camera offset from player: {cameraLocalPos}");
        Debug.Log($"CutsceneController: Teleported player root to: {playerPosition}");
        Debug.Log($"CutsceneController: Resulting camera position: {fpsController.playerCamera?.position}");
        
        if (characterController != null)
        {
            // Re-enable CharacterController
            characterController.enabled = true;
        }

        // Align the camera rotation to match the target transform's rotation
        fpsController.SetCameraRotation(targetTransform, false);
    }
}
