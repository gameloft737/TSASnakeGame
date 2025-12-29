using UnityEngine;
using UnityEngine.SceneManagement; // Needed for scene switching

public class CutsceneTrigger : MonoBehaviour
{
    [Header("References")]
    public GameObject fpsController;      // Player FPS controller
    public Camera cutsceneCamera;         // Optional: Cutscene camera

    [Header("Scene Settings")]
    public string sceneToLoad;            // Name of the scene to switch to
    public float delayBeforeScene = 2f;   // Delay after entering trigger before scene loads

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // Only trigger once, and only if player enters
        if (!triggered && other.gameObject == fpsController)
        {
            triggered = true;
            StartCoroutine(HandleSceneSwitch());
        }
    }

    private System.Collections.IEnumerator HandleSceneSwitch()
    {
        // Optional: disable player and enable cutscene camera
        fpsController.SetActive(false);
        if (cutsceneCamera != null)
            cutsceneCamera.gameObject.SetActive(true);

        // Wait for delay
        yield return new WaitForSeconds(delayBeforeScene);

        // Switch scene
        if (!string.IsNullOrEmpty(sceneToLoad))
            SceneManager.LoadScene(sceneToLoad);
    }
}
