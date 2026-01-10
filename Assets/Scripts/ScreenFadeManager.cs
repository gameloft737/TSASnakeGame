using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// Manages screen fade effects (fade to white, fade from white, etc.)
/// Uses a full-screen UI Image to create the fade effect.
/// 
/// SETUP INSTRUCTIONS:
/// 1. Create a Canvas in your scene (if you don't have one for UI)
/// 2. Create an Image as a child of the Canvas
/// 3. Set the Image to stretch to fill the entire screen (anchor to all corners)
/// 4. Set the Image color to white with alpha = 0 (transparent)
/// 5. Make sure the Image is rendered on top of everything (set Sort Order on Canvas or use a separate Canvas)
/// 6. Create an empty GameObject and add this script
/// 7. Assign the fade Image to the "Fade Image" field
/// 
/// USAGE:
/// - Call ScreenFadeManager.Instance.FadeToWhite() to fade the screen to white
/// - Call ScreenFadeManager.Instance.FadeFromWhite() to fade from white to clear
/// - Use the callback parameter to execute code when the fade completes
/// </summary>
public class ScreenFadeManager : MonoBehaviour
{
    public static ScreenFadeManager Instance { get; private set; }
    
    [Header("References")]
    [Tooltip("The UI Image used for the fade effect. Should be a full-screen white image.")]
    [SerializeField] private Image fadeImage;
    
    [Header("Settings")]
    [Tooltip("Default duration for fade effects")]
    [SerializeField] private float defaultFadeDuration = 1f;
    
    [Tooltip("If true, starts the game with a fade from white")]
    [SerializeField] private bool fadeInOnStart = true;
    
    [Tooltip("Delay before the initial fade from white starts")]
    [SerializeField] private float initialFadeDelay = 0.5f;
    
    [Header("Initial Fade Settings")]
    [Tooltip("If true, shows a subtitle after the initial fade completes")]
    [SerializeField] private bool showSubtitleAfterInitialFade = true;
    
    [Tooltip("The subtitle text to show after initial fade")]
    [TextArea(2, 5)]
    [SerializeField] private string initialSubtitleText = "Level 1";
    
    [Tooltip("Duration to show the initial subtitle")]
    [SerializeField] private float initialSubtitleDuration = 3f;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private Coroutine activeFadeCoroutine;
    private bool isFading = false;
    
    /// <summary>
    /// Event fired when a fade to white completes
    /// </summary>
    public event Action OnFadeToWhiteComplete;
    
    /// <summary>
    /// Event fired when a fade from white completes
    /// </summary>
    public event Action OnFadeFromWhiteComplete;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[ScreenFadeManager] Multiple instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        // Validate fade image
        if (fadeImage == null)
        {
            Debug.LogError("[ScreenFadeManager] Fade Image is not assigned! Please assign a full-screen UI Image.");
            return;
        }
        
        // If fading in on start, make sure the image starts fully opaque (white)
        if (fadeInOnStart)
        {
            SetFadeAlpha(1f);
        }
        else
        {
            SetFadeAlpha(0f);
        }
    }
    
    private void Start()
    {
        if (fadeInOnStart && fadeImage != null)
        {
            StartCoroutine(InitialFadeSequence());
        }
    }
    
    /// <summary>
    /// Handles the initial fade from white when the game starts
    /// </summary>
    private IEnumerator InitialFadeSequence()
    {
        // Wait for the initial delay
        yield return new WaitForSeconds(initialFadeDelay);
        
        if (debugMode)
            Debug.Log("[ScreenFadeManager] Starting initial fade from white");
        
        // Fade from white
        yield return FadeFromWhiteCoroutine(defaultFadeDuration, () =>
        {
            if (debugMode)
                Debug.Log("[ScreenFadeManager] Initial fade complete");
            
            // Show the initial subtitle if configured
            if (showSubtitleAfterInitialFade && !string.IsNullOrEmpty(initialSubtitleText))
            {
                if (SubtitleUI.Instance != null)
                {
                    SubtitleUI.Instance.ShowSubtitle(initialSubtitleText, initialSubtitleDuration);
                    if (debugMode)
                        Debug.Log($"[ScreenFadeManager] Showing initial subtitle: {initialSubtitleText}");
                }
                else
                {
                    Debug.LogWarning("[ScreenFadeManager] SubtitleUI.Instance is null. Cannot show initial subtitle.");
                }
            }
        });
    }
    
    /// <summary>
    /// Fade the screen to white
    /// </summary>
    /// <param name="duration">Duration of the fade (uses default if <= 0)</param>
    /// <param name="onComplete">Callback when fade completes</param>
    public void FadeToWhite(float duration = -1f, Action onComplete = null)
    {
        if (fadeImage == null)
        {
            Debug.LogError("[ScreenFadeManager] Cannot fade - Fade Image is not assigned!");
            onComplete?.Invoke();
            return;
        }
        
        float fadeDuration = duration > 0 ? duration : defaultFadeDuration;
        
        if (activeFadeCoroutine != null)
        {
            StopCoroutine(activeFadeCoroutine);
        }
        
        activeFadeCoroutine = StartCoroutine(FadeToWhiteCoroutine(fadeDuration, onComplete));
    }
    
    /// <summary>
    /// Fade the screen from white to clear
    /// </summary>
    /// <param name="duration">Duration of the fade (uses default if <= 0)</param>
    /// <param name="onComplete">Callback when fade completes</param>
    public void FadeFromWhite(float duration = -1f, Action onComplete = null)
    {
        if (fadeImage == null)
        {
            Debug.LogError("[ScreenFadeManager] Cannot fade - Fade Image is not assigned!");
            onComplete?.Invoke();
            return;
        }
        
        float fadeDuration = duration > 0 ? duration : defaultFadeDuration;
        
        if (activeFadeCoroutine != null)
        {
            StopCoroutine(activeFadeCoroutine);
        }
        
        activeFadeCoroutine = StartCoroutine(FadeFromWhiteCoroutine(fadeDuration, onComplete));
    }
    
    /// <summary>
    /// Fade to white, then fade back from white
    /// </summary>
    /// <param name="fadeInDuration">Duration of fade to white</param>
    /// <param name="holdDuration">How long to hold at full white</param>
    /// <param name="fadeOutDuration">Duration of fade from white</param>
    /// <param name="onFadeToWhiteComplete">Callback when fade to white completes (before hold)</param>
    /// <param name="onComplete">Callback when entire sequence completes</param>
    public void FadeToWhiteAndBack(float fadeInDuration = -1f, float holdDuration = 0.5f, float fadeOutDuration = -1f, 
        Action onFadeToWhiteComplete = null, Action onComplete = null)
    {
        if (fadeImage == null)
        {
            Debug.LogError("[ScreenFadeManager] Cannot fade - Fade Image is not assigned!");
            onComplete?.Invoke();
            return;
        }
        
        float inDuration = fadeInDuration > 0 ? fadeInDuration : defaultFadeDuration;
        float outDuration = fadeOutDuration > 0 ? fadeOutDuration : defaultFadeDuration;
        
        if (activeFadeCoroutine != null)
        {
            StopCoroutine(activeFadeCoroutine);
        }
        
        activeFadeCoroutine = StartCoroutine(FadeToWhiteAndBackCoroutine(inDuration, holdDuration, outDuration, onFadeToWhiteComplete, onComplete));
    }
    
    private IEnumerator FadeToWhiteCoroutine(float duration, Action onComplete)
    {
        isFading = true;
        
        if (debugMode)
            Debug.Log($"[ScreenFadeManager] Fading to white over {duration}s");
        
        float startAlpha = GetCurrentAlpha();
        float elapsed = 0f;
        
        // Make sure the image is active
        fadeImage.gameObject.SetActive(true);
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(startAlpha, 1f, t);
            SetFadeAlpha(alpha);
            yield return null;
        }
        
        SetFadeAlpha(1f);
        isFading = false;
        activeFadeCoroutine = null;
        
        if (debugMode)
            Debug.Log("[ScreenFadeManager] Fade to white complete");
        
        OnFadeToWhiteComplete?.Invoke();
        onComplete?.Invoke();
    }
    
    private IEnumerator FadeFromWhiteCoroutine(float duration, Action onComplete)
    {
        isFading = true;
        
        if (debugMode)
            Debug.Log($"[ScreenFadeManager] Fading from white over {duration}s");
        
        float startAlpha = GetCurrentAlpha();
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(startAlpha, 0f, t);
            SetFadeAlpha(alpha);
            yield return null;
        }
        
        SetFadeAlpha(0f);
        
        // Optionally disable the image when fully transparent
        fadeImage.gameObject.SetActive(false);
        
        isFading = false;
        activeFadeCoroutine = null;
        
        if (debugMode)
            Debug.Log("[ScreenFadeManager] Fade from white complete");
        
        OnFadeFromWhiteComplete?.Invoke();
        onComplete?.Invoke();
    }
    
    private IEnumerator FadeToWhiteAndBackCoroutine(float fadeInDuration, float holdDuration, float fadeOutDuration,
        Action onFadeToWhiteComplete, Action onComplete)
    {
        // Fade to white
        yield return FadeToWhiteCoroutine(fadeInDuration, null);
        
        onFadeToWhiteComplete?.Invoke();
        
        // Hold at white
        if (holdDuration > 0)
        {
            yield return new WaitForSeconds(holdDuration);
        }
        
        // Fade from white
        yield return FadeFromWhiteCoroutine(fadeOutDuration, null);
        
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// Set the fade image alpha directly
    /// </summary>
    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }
    }
    
    /// <summary>
    /// Get the current alpha of the fade image
    /// </summary>
    private float GetCurrentAlpha()
    {
        return fadeImage != null ? fadeImage.color.a : 0f;
    }
    
    /// <summary>
    /// Check if a fade is currently in progress
    /// </summary>
    public bool IsFading => isFading;
    
    /// <summary>
    /// Immediately set the screen to fully white (no fade)
    /// </summary>
    public void SetWhite()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            SetFadeAlpha(1f);
        }
    }
    
    /// <summary>
    /// Immediately clear the fade (no fade)
    /// </summary>
    public void SetClear()
    {
        if (fadeImage != null)
        {
            SetFadeAlpha(0f);
            fadeImage.gameObject.SetActive(false);
        }
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(ScreenFadeManager))]
public class ScreenFadeManagerEditor : UnityEditor.Editor
{
    private float testDuration = 1f;
    
    public override void OnInspectorGUI()
    {
        ScreenFadeManager manager = (ScreenFadeManager)target;
        
        DrawDefaultInspector();
        
        UnityEditor.EditorGUILayout.Space(15);
        UnityEditor.EditorGUILayout.LabelField("Testing Tools", UnityEditor.EditorStyles.boldLabel);
        
        if (Application.isPlaying)
        {
            UnityEditor.EditorGUILayout.BeginVertical("box");
            
            testDuration = UnityEditor.EditorGUILayout.FloatField("Test Duration", testDuration);
            
            UnityEditor.EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Fade To White"))
            {
                manager.FadeToWhite(testDuration);
            }
            
            if (GUILayout.Button("Fade From White"))
            {
                manager.FadeFromWhite(testDuration);
            }
            
            UnityEditor.EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Fade To White And Back"))
            {
                manager.FadeToWhiteAndBack(testDuration, 0.5f, testDuration);
            }
            
            UnityEditor.EditorGUILayout.Space(5);
            
            UnityEditor.EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Set White (Instant)"))
            {
                manager.SetWhite();
            }
            
            if (GUILayout.Button("Set Clear (Instant)"))
            {
                manager.SetClear();
            }
            
            UnityEditor.EditorGUILayout.EndHorizontal();
            
            UnityEditor.EditorGUILayout.EndVertical();
        }
        else
        {
            UnityEditor.EditorGUILayout.HelpBox("Enter Play Mode to access testing tools.", UnityEditor.MessageType.Info);
        }
    }
}
#endif