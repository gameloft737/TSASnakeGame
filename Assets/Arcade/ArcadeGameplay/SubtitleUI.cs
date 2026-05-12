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

    [Header("Voiceline Audio")]
    [Tooltip("AudioSource used to play subtitle voicelines. If not assigned, one is created at runtime.")]
    public AudioSource voicelineAudioSource;
    [Tooltip("Default volume for voicelines (0-1). Can be overridden per ShowSubtitle call.")]
    [Range(0f, 1f)]
    public float defaultVoicelineVolume = 1f;
    [Tooltip("If true, cuts the currently-playing voiceline when a NEW subtitle with a voiceline is shown. Leave OFF so short subtitles don't cut off longer voicelines.")]
    public bool stopPreviousVoiceline = false;
    [Tooltip("If true, stops the voiceline when the subtitle's duration ends (via ClearSubtitle). Leave OFF so voicelines outlast their subtitle text.")]
    public bool stopVoicelineOnClear = false;

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
        ShowSubtitle(message, duration, null, -1f);
    }

    /// <summary>
    /// Show a subtitle message with an accompanying voiceline AudioClip.
    /// If duration <= 0, uses defaultDuration. If voicelineVolume < 0, uses defaultVoicelineVolume.
    /// </summary>
    public void ShowSubtitle(string message, float duration, AudioClip voiceline, float voicelineVolume = -1f)
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

        // Make sure this GameObject and its parents are active
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("SubtitleUI: GameObject is not active in hierarchy! Activating...");
            gameObject.SetActive(true);
        }
        
        // Check if parent Canvas is active
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && !parentCanvas.gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"SubtitleUI: Parent Canvas '{parentCanvas.name}' is not active!");
        }

        // Activate objects and set text
        backgroundPanel.gameObject.SetActive(true);
        subtitleText.gameObject.SetActive(true);
        subtitleText.text = message;
        
        // Play voiceline if provided
        if (voiceline != null)
        {
            PlayVoiceline(voiceline, voicelineVolume);
        }

        activeCoroutine = StartCoroutine(SubtitleRoutine(duration));
    }

    /// <summary>
    /// Plays the given AudioClip as a subtitle voiceline via the assigned AudioSource
    /// (or a lazily-created one). By default, voicelines are allowed to overlap and
    /// run to completion - set <see cref="stopPreviousVoiceline"/> to true to cut
    /// the previous voiceline when a new one starts.
    /// </summary>
    public void PlayVoiceline(AudioClip clip, float volume = -1f)
    {
        if (clip == null) return;

        EnsureVoicelineAudioSource();

        if (stopPreviousVoiceline && voicelineAudioSource.isPlaying)
        {
            voicelineAudioSource.Stop();
        }

        float finalVolume = volume < 0f ? defaultVoicelineVolume : Mathf.Clamp01(volume);
        // PlayOneShot layers onto anything already playing (including previous voicelines),
        // so short subtitles can't cut off longer voicelines unless stopPreviousVoiceline is enabled.
        voicelineAudioSource.PlayOneShot(clip, finalVolume);
    }

    /// <summary>
    /// Creates (or validates) a voiceline AudioSource on a persistent sibling GameObject.
    /// Critical: this GameObject is NEVER deactivated when subtitles hide, so PlayOneShot
    /// audio can finish playing even after the subtitle text/background is turned off.
    /// Prior versions added the AudioSource to this.gameObject, which was also the
    /// backgroundPanel's GameObject - so ClearSubtitle() (which calls SetActive(false) on
    /// the backgroundPanel) was silently killing audio on the shared GameObject.
    /// </summary>
    private void EnsureVoicelineAudioSource()
    {
        // If the user assigned one in the inspector, trust it (they know where it lives).
        if (voicelineAudioSource != null) return;

        // Parent the holder to this SubtitleUI's PARENT (sibling) so it doesn't get
        // deactivated when the SubtitleUI's own GameObject is toggled off. If there's
        // no parent (root object), the holder just sits in the scene root, which is fine.
        Transform holderParent = transform.parent;
        Transform holder = holderParent != null ? holderParent.Find("__SubtitleVoicelineAudio") : null;
        if (holder == null)
        {
            // Also check under ourselves in case of an older holder from a previous version
            holder = transform.Find("__VoicelineAudio");
        }

        GameObject holderGO;
        if (holder == null)
        {
            holderGO = new GameObject("__SubtitleVoicelineAudio");
            if (holderParent != null) holderGO.transform.SetParent(holderParent, false);
        }
        else
        {
            holderGO = holder.gameObject;
            // Re-parent to sibling if it was a child of ours (legacy fix)
            if (holderGO.transform.parent == transform && holderParent != null)
            {
                holderGO.transform.SetParent(holderParent, false);
            }
        }
        // Make sure it stays on even if someone toggles it off
        if (!holderGO.activeSelf) holderGO.SetActive(true);

        voicelineAudioSource = holderGO.GetComponent<AudioSource>();
        if (voicelineAudioSource == null)
        {
            voicelineAudioSource = holderGO.AddComponent<AudioSource>();
        }
        voicelineAudioSource.playOnAwake = false;
        voicelineAudioSource.loop = false;
        voicelineAudioSource.spatialBlend = 0f; // 2D
        voicelineAudioSource.volume = 1f; // base level 1 so PlayOneShot volume is used directly
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

        if (stopVoicelineOnClear && voicelineAudioSource != null && voicelineAudioSource.isPlaying)
        {
            voicelineAudioSource.Stop();
        }
    }

    // --- Optional: quick test method you can call from other scripts ---
    // Example: call SubtitleUI.Instance.TestShow() from ObjectiveManager.Start() to verify it works.
    public void TestShow()
    {
        ShowSubtitle("Subtitle system working! (test)", 2f);
    }
}
