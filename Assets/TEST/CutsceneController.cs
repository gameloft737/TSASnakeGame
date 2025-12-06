using UnityEngine;
using System.Collections; // Needed for IEnumerator

public class CutsceneController : MonoBehaviour
{
    [Header("Cameras")]
    public Camera mainCamera;          // Your player camera
    public Camera cutsceneCamera;      // Camera used for cutscene

    [Header("Animator")]
    public Animator cutsceneAnimator;  // Animator attached to cutscene camera

    [Header("Settings")]
    public float cutsceneDuration = 5f;

    [Header("Subtitles")]
    [TextArea(1, 3)]
    public string subtitleText;
    public float subtitleDuration = 3f;

    [Header("FPS Camera Sync Options")]
    public bool snapFPSCameraToCutsceneStart = false; // Check this to enable
    public Transform cutsceneStartTransform;         // Optional: specify exact start point

    /// <summary>
    /// Call this method to start the cutscene
    /// </summary>
    /// 
    /// 
    public void StartCutscene()
    {
        // Disable player control
    var fpsController = mainCamera.GetComponentInParent<EasyPeasyFirstPersonController.FirstPersonController>();
    if (fpsController != null)
    {
        fpsController.SetControl(false);

        // Move FPS player to cutscene start
        fpsController.SetCameraRotation(cutsceneCamera.transform, true);
    }

        StartCoroutine(CutsceneRoutine());
    }

    void Start()
    {
        StartCutscene(); // Automatically starts the cutscene
    }


private IEnumerator CutsceneRoutine()
{
    // Disable FPS camera and player movement
    if (mainCamera != null) mainCamera.enabled = false;
    var fpsController = mainCamera.GetComponentInParent<EasyPeasyFirstPersonController.FirstPersonController>();
    if (fpsController != null)
        fpsController.SetControl(false); // disable movement & look

    // Enable cutscene camera
    if (cutsceneCamera != null) cutsceneCamera.enabled = true;

    // Play cutscene animation if assigned
    if (cutsceneAnimator != null)
        cutsceneAnimator.Play("YourAnimationClipName");

    // Show subtitle if assigned
    if (!string.IsNullOrEmpty(subtitleText) && SubtitleUI.Instance != null)
        SubtitleUI.Instance.ShowSubtitle(subtitleText, subtitleDuration);

    // Wait for the cutscene duration
    yield return new WaitForSeconds(cutsceneDuration);

    // Cutscene ends: disable cutscene camera
    if (cutsceneCamera != null) cutsceneCamera.enabled = false;

    // Enable FPS camera
    if (mainCamera != null) mainCamera.enabled = true;

    // Sync FPS controller rotation to match cutscene camera
    if (fpsController != null && cutsceneCamera != null)
        fpsController.SetCameraRotation(cutsceneCamera.transform);

    // Re-enable player movement & look
    if (fpsController != null)
        fpsController.SetControl(true);

        // Enable FPS camera
if (mainCamera != null) mainCamera.enabled = true;

// Sync rotation but don't move player (player is already there)
if (fpsController != null)
{
    fpsController.SetCameraRotation(cutsceneCamera.transform, false);
    fpsController.SetControl(true);
}

}



}
