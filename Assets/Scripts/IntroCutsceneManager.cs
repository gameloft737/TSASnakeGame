using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// Manages an intro cutscene sequence that plays immediately when the scene loads.
/// SEQUENCE: 1. Scene loads 2. Hide cursor 3. Play camera animation 4. Show subtitles 5. Fade panel to black 6. Load next scene
/// </summary>
public class IntroCutsceneManager : MonoBehaviour
{
    [Header("Auto Start")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private float startDelay = 0f;
    
    [Header("Camera Animation")]
    [SerializeField] private Animator cutsceneAnimator;
    [SerializeField] private string animationName = "IntroCutscene";
    [SerializeField] private AnimationClip cutsceneAnimationClip;
    [SerializeField] private float cutsceneDuration = 5f;
    
    [Header("Subtitles")]
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private Image subtitleBackground;
    [SerializeField] private SubtitleEntry[] subtitles;
    
    [Header("Fade Panel Settings")]
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private float fadeToBlackDuration = 1f;
    
    [Header("Scene Transition")]
    [SerializeField] private string nextSceneName = "";
    [SerializeField] private float delayBeforeFade = 0.5f;
    [SerializeField] private float delayAfterFade = 2f;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip cutsceneMusic;
    [Tooltip("Optional dedicated AudioSource for subtitle voicelines. If not assigned, voicelines play via PlayOneShot on 'audioSource' (music continues).")]
    [SerializeField] private AudioSource voicelineAudioSource;
    [Tooltip("If true, waits for the currently-playing voiceline to finish before loading the next scene. Prevents voicelines from being cut off by scene transitions.")]
    [SerializeField] private bool waitForVoicelineBeforeSceneLoad = true;
    [Tooltip("Max seconds to wait for a voiceline to finish before giving up and loading the next scene anyway (safety cap).")]
    [SerializeField] private float maxVoicelineWaitSeconds = 30f;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private bool isPlaying = false;
    private Coroutine cutsceneCoroutine;
    private bool subtitlesComplete = false;
    // Tracks when the currently-playing voiceline will finish so we can wait it out before scene transitions.
    private float voicelineEndTime = 0f;
    
    public event Action OnCutsceneStarted;
    public event Action OnCutsceneEnded;
    public bool IsPlaying => isPlaying;
    
    private void Start()
    {
        InitializeUI();
        HideCursor();
        if (autoStart)
        {
            if (startDelay > 0) StartCoroutine(DelayedStart());
            else StartCutscene();
        }
    }
    
    private void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    private void InitializeUI()
    {
        if (subtitleText != null) { subtitleText.text = ""; subtitleText.gameObject.SetActive(false); }
        if (subtitleBackground != null) subtitleBackground.gameObject.SetActive(false);
        if (fadePanel != null) { fadePanel.alpha = 0f; fadePanel.gameObject.SetActive(true); }
    }
    
    private IEnumerator DelayedStart() { yield return new WaitForSeconds(startDelay); StartCutscene(); }
    
    public void StartCutscene()
    {
        if (isPlaying) return;
        isPlaying = true;
        HideCursor();
        OnCutsceneStarted?.Invoke();
        cutsceneCoroutine = StartCoroutine(CutsceneSequence());
    }
    
    public void SkipCutscene()
    {
        if (!isPlaying) return;
        if (cutsceneCoroutine != null) StopCoroutine(cutsceneCoroutine);
        StartCoroutine(SkipToEnd());
    }
    
    private IEnumerator SkipToEnd()
    {
        if (fadePanel != null) fadePanel.alpha = 1f;
        HideSubtitle();
        yield return new WaitForSeconds(0.5f);
        LoadNextScene();
    }
    
    private IEnumerator CutsceneSequence()
    {
        if (audioSource != null && cutsceneMusic != null) { audioSource.clip = cutsceneMusic; audioSource.Play(); }
        
        float animDuration = cutsceneDuration;
        if (cutsceneAnimator != null && !string.IsNullOrEmpty(animationName))
        {
            cutsceneAnimator.Play(animationName);
            animDuration = GetAnimationDuration();
        }
        
        // Start subtitle sequence and track completion
        subtitlesComplete = (subtitles == null || subtitles.Length == 0);
        if (!subtitlesComplete) StartCoroutine(SubtitleSequence());
        
        // Wait for animation to complete
        yield return new WaitForSeconds(animDuration);
        
        // Wait for subtitles to complete if they're still playing
        while (!subtitlesComplete) yield return null;
        
        HideSubtitle();
        
        // Wait delay before fade
        if (delayBeforeFade > 0) yield return new WaitForSeconds(delayBeforeFade);
        
        // Fade panel to black
        yield return StartCoroutine(FadePanelToBlack());
        
        // Wait delay after fade before scene switch
        if (delayAfterFade > 0) yield return new WaitForSeconds(delayAfterFade);
        
        OnCutsceneEnded?.Invoke();
        isPlaying = false;
        LoadNextScene();
    }
    
    private IEnumerator SubtitleSequence()
    {
        foreach (SubtitleEntry entry in subtitles)
        {
            if (entry.startTime > 0) yield return new WaitForSeconds(entry.startTime);
            ShowSubtitle(entry.text);
            PlayVoiceline(entry.voiceline, entry.voicelineVolume);
            yield return new WaitForSeconds(entry.duration);
            HideSubtitle();
            if (entry.gapAfter > 0) yield return new WaitForSeconds(entry.gapAfter);
        }
        
        // Don't mark subtitles complete until the final voiceline has finished playing,
        // so the cutscene doesn't fade/transition out and cut the voiceline off.
        if (waitForVoicelineBeforeSceneLoad)
        {
            yield return WaitForVoicelineToFinish();
        }
        
        subtitlesComplete = true;
    }
    
    /// <summary>
    /// Yields until the currently-tracked voiceline has finished, capped by
    /// <see cref="maxVoicelineWaitSeconds"/> as a safety net.
    /// </summary>
    private IEnumerator WaitForVoicelineToFinish()
    {
        float cap = Time.time + Mathf.Max(0f, maxVoicelineWaitSeconds);
        while (Time.time < voicelineEndTime && Time.time < cap)
        {
            yield return null;
        }
    }
    
    private void PlayVoiceline(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }
        float vol = Mathf.Clamp01(volume);
        
        // Use PlayOneShot so voicelines layer and finish naturally instead of
        // cutting each other off when subtitles change rapidly.
        if (voicelineAudioSource != null)
        {
            // Ensure the base volume is 1 so PlayOneShot volume is applied directly
            if (voicelineAudioSource.volume <= 0.001f) voicelineAudioSource.volume = 1f;
            voicelineAudioSource.PlayOneShot(clip, vol);
        }
        else if (audioSource != null)
        {
            // Layer on top of cutscene music without stopping it
            audioSource.PlayOneShot(clip, vol);
        }
        else
        {
            // Neither AudioSource was assigned in the Inspector - create a dedicated one on this GameObject.
            voicelineAudioSource = gameObject.AddComponent<AudioSource>();
            voicelineAudioSource.playOnAwake = false;
            voicelineAudioSource.loop = false;
            voicelineAudioSource.spatialBlend = 0f; // 2D
            voicelineAudioSource.volume = 1f;
            voicelineAudioSource.PlayOneShot(clip, vol);
            Debug.LogWarning($"[IntroCutsceneManager] No AudioSource assigned in Inspector - auto-created one to play voiceline '{clip.name}'. Assign 'voicelineAudioSource' or 'audioSource' to silence this warning.");
        }
        
        // Track when this voiceline should finish so scene transitions can wait for it.
        // Extend (not overwrite) in case an earlier longer voiceline is still playing.
        float clipEnd = Time.time + clip.length;
        if (clipEnd > voicelineEndTime) voicelineEndTime = clipEnd;
    }
    
    private void ShowSubtitle(string text)
    {
        if (subtitleText != null) { subtitleText.text = text; subtitleText.gameObject.SetActive(true); }
        if (subtitleBackground != null) subtitleBackground.gameObject.SetActive(true);
    }
    
    private void HideSubtitle()
    {
        if (subtitleText != null) { subtitleText.text = ""; subtitleText.gameObject.SetActive(false); }
        if (subtitleBackground != null) subtitleBackground.gameObject.SetActive(false);
    }
    
    private IEnumerator FadePanelToBlack()
    {
        if (fadePanel == null) yield break;
        float elapsed = 0f;
        float startAlpha = fadePanel.alpha;
        while (elapsed < fadeToBlackDuration)
        {
            elapsed += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / fadeToBlackDuration);
            yield return null;
        }
        fadePanel.alpha = 1f;
    }
    
    private void LoadNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName)) { Debug.LogWarning("[IntroCutsceneManager] No next scene specified!"); return; }
        
        #if UNITY_EDITOR
        // Deselect all objects to prevent Editor inspector errors during scene transition
        UnityEditor.Selection.activeGameObject = null;
        #endif
        
        SceneManager.LoadScene(nextSceneName);
    }
    
    private float GetAnimationDuration()
    {
        if (cutsceneAnimationClip != null) return cutsceneAnimationClip.length;
        if (cutsceneAnimator != null)
        {
            RuntimeAnimatorController controller = cutsceneAnimator.runtimeAnimatorController;
            if (controller != null)
            {
                foreach (AnimationClip clip in controller.animationClips)
                {
                    if (clip.name == animationName || clip.name.Contains(animationName)) return clip.length;
                }
            }
        }
        return cutsceneDuration;
    }
    
    private void OnDestroy() { if (cutsceneCoroutine != null) StopCoroutine(cutsceneCoroutine); }
}

[System.Serializable]
public class SubtitleEntry
{
    [TextArea(1, 3)] public string text = "";
    public float startTime = 0f;
    public float duration = 3f;
    public float gapAfter = 0.5f;
    [Tooltip("Optional voiceline AudioClip to play when this subtitle appears.")]
    public AudioClip voiceline;
    [Tooltip("Volume for the voiceline (0-1).")]
    [Range(0f, 1f)]
    public float voicelineVolume = 1f;
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(IntroCutsceneManager))]
public class IntroCutsceneManagerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        IntroCutsceneManager manager = (IntroCutsceneManager)target;
        DrawDefaultInspector();
        
        UnityEditor.EditorGUILayout.Space(15);
        UnityEditor.EditorGUILayout.LabelField("Testing Tools", UnityEditor.EditorStyles.boldLabel);
        
        if (Application.isPlaying)
        {
            UnityEditor.EditorGUILayout.BeginVertical("box");
            UnityEditor.EditorGUILayout.LabelField("Status:", manager.IsPlaying ? "Playing" : "Idle");
            if (!manager.IsPlaying) { if (GUILayout.Button("Start Cutscene")) manager.StartCutscene(); }
            else { if (GUILayout.Button("Skip Cutscene")) manager.SkipCutscene(); }
            UnityEditor.EditorGUILayout.EndVertical();
        }
        else
        {
            UnityEditor.EditorGUILayout.HelpBox("Enter Play Mode to test.", UnityEditor.MessageType.Info);
        }
        
        UnityEditor.EditorGUILayout.Space(10);
        UnityEditor.EditorGUILayout.HelpBox(
            "SETUP:\n" +
            "1. Create Canvas with: fade Panel (CanvasGroup, black Image child), subtitle Text\n" +
            "2. Optional: Camera with Animator\n" +
            "3. Configure subtitles array\n" +
            "4. Set next scene name\n" +
            "5. Cursor is hidden automatically",
            UnityEditor.MessageType.Info);
    }
}
#endif