using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class SubtitleUI : MonoBehaviour
{
    public static SubtitleUI Instance;

    [Header("References (assign in Inspector)")]
    public TextMeshProUGUI subtitleText;   // The TMP text object (child of background)
    public Image backgroundPanel;          // The background Image (panel)

    [Header("Settings")]
    public float defaultDuration = 3f;

    private Coroutine activeCoroutine;

    private void Awake()
    {
        // Singleton assignment
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Multiple SubtitleUI instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        // Safety checks
        if (backgroundPanel == null)
            Debug.LogWarning("SubtitleUI: backgroundPanel is not assigned in the inspector.");
        if (subtitleText == null)
            Debug.LogWarning("SubtitleUI: subtitleText is not assigned in the inspector.");

        // Ensure both objects exist before toggling
        if (backgroundPanel != null)
            backgroundPanel.gameObject.SetActive(false);

        if (subtitleText != null)
            subtitleText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Show a subtitle message for a duration. If duration <= 0, uses defaultDuration.
    /// </summary>
    public void ShowSubtitle(string message, float duration = -1f)
    {
        if (Instance == null)
        {
            Debug.LogWarning("SubtitleUI.Instance is null. Make sure a SubtitleUI exists in the scene.");
            return;
        }

        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning("SubtitleUI.ShowSubtitle called with empty message.");
            return;
        }

        if (backgroundPanel == null || subtitleText == null)
        {
            Debug.LogWarning("SubtitleUI: Missing references. Assign backgroundPanel and subtitleText in the inspector.");
            return;
        }

        // Stop previous coroutine if active
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);

        // Activate objects and set text
        backgroundPanel.gameObject.SetActive(true);
        subtitleText.gameObject.SetActive(true);
        subtitleText.text = message;

        activeCoroutine = StartCoroutine(SubtitleRoutine(duration));
    }

    private IEnumerator SubtitleRoutine(float duration)
    {
        float waitTime = (duration > 0f) ? duration : defaultDuration;
        yield return new WaitForSeconds(waitTime);

        ClearSubtitle();
    }

    /// <summary>
    /// Clear / hide subtitle immediately.
    /// </summary>
    public void ClearSubtitle()
    {
        // If the objects were not assigned, just return
        if (subtitleText != null)
            subtitleText.text = "";

        if (subtitleText != null)
            subtitleText.gameObject.SetActive(false);

        if (backgroundPanel != null)
            backgroundPanel.gameObject.SetActive(false);

        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }
    }

    // --- Optional: quick test method you can call from other scripts ---
    // Example: call SubtitleUI.Instance.TestShow() from ObjectiveManager.Start() to verify it works.
    public void TestShow()
    {
        ShowSubtitle("Subtitle system working! (test)", 2f);
    }
}
