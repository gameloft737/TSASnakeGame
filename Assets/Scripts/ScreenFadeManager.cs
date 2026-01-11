using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// Manages screen fade effects (fade to black, fade from black, etc.)
/// Uses a CanvasGroup on a Panel for the fade effect.
/// 
/// SETUP INSTRUCTIONS:
/// 1. Create a Canvas in your scene (if you don't have one for UI)
/// 2. Create a Panel as a child of the Canvas
/// 3. Add a CanvasGroup component to the Panel
/// 4. Set the Panel to stretch to fill the entire screen (anchor to all corners)
/// 5. Set the Panel's Image color to black (or your desired fade color)
/// 6. Make sure the Panel is rendered on top of everything (set Sort Order on Canvas or use a separate Canvas)
/// 7. Create an empty GameObject and add this script
/// 8. Assign the Panel's CanvasGroup to the "Fade Panel" field
/// 
/// USAGE:
/// - Call ScreenFadeManager.Instance.FadeToBlack() to fade the screen to black
/// - Call ScreenFadeManager.Instance.FadeFromBlack() to fade from black to clear
/// - Use the callback parameter to execute code when the fade completes
/// </summary>
public class ScreenFadeManager : MonoBehaviour
{
    public static ScreenFadeManager Instance { get; private set; }
    
    [Header("References")]
    [Tooltip("The CanvasGroup on the fade panel. Should be a full-screen panel with a black Image.")]
    [SerializeField] private CanvasGroup fadePanel;
    
    [Tooltip("Optional: The Image component on the fade panel. Used to ensure correct color.")]
    [SerializeField] private Image fadePanelImage;
    
    [Header("Settings")]
    [Tooltip("Default duration for fade effects")]
    [SerializeField] private float defaultFadeDuration = 1f;
    
    [Tooltip("If true, starts the scene with a fade from black")]
    [SerializeField] private bool fadeInOnStart = true;
    
    [Tooltip("Delay before the initial fade from black starts")]
    [SerializeField] private float initialFadeDelay = 0.5f;
    
    [Tooltip("The color to fade to/from (usually black)")]
    [SerializeField] private Color fadeColor = Color.black;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private Coroutine activeFadeCoroutine;
    private bool isFading = false;
    private bool hasInitialized = false;
    
    /// <summary>
    /// Event fired when a fade to black completes
    /// </summary>
    public event Action OnFadeToBlackComplete;
    
    /// <summary>
    /// Event fired when a fade from black completes
    /// </summary>
    public event Action OnFadeFromBlackComplete;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[ScreenFadeManager] Instance created");
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[ScreenFadeManager] Multiple instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        // Validate fade panel
        if (fadePanel == null)
        {
            Debug.LogError("[ScreenFadeManager] Fade Panel is not assigned! Please assign a CanvasGroup on a full-screen Panel.");
            return;
        }
        
        // Try to get the Image component if not assigned
        if (fadePanelImage == null)
        {
            fadePanelImage = fadePanel.GetComponent<Image>();
            if (fadePanelImage == null)
            {
                fadePanelImage = fadePanel.GetComponentInChildren<Image>();
            }
        }
        
        // Ensure the fade panel image has the correct color
        if (fadePanelImage != null)
        {
            fadePanelImage.color = fadeColor;
            Debug.Log($"[ScreenFadeManager] Set fade panel image color to: {fadeColor}");
        }
        else
        {
            Debug.LogWarning("[ScreenFadeManager] No Image component found on fade panel. Make sure the panel has an Image with the correct color.");
        }
        
        Debug.Log($"[ScreenFadeManager] Fade Panel found: {fadePanel.gameObject.name}, fadeInOnStart: {fadeInOnStart}");
        
        // If fading in on start, make sure the panel starts fully opaque
        if (fadeInOnStart)
        {
            SetFadeAlpha(1f);
            fadePanel.gameObject.SetActive(true);
            // Ensure the panel blocks raycasts during fade
            fadePanel.blocksRaycasts = true;
            fadePanel.interactable = false;
            Debug.Log("[ScreenFadeManager] Panel set to opaque (alpha=1), active=true");
        }
        else
        {
            SetFadeAlpha(0f);
            fadePanel.gameObject.SetActive(false);
            Debug.Log("[ScreenFadeManager] Panel set to clear (alpha=0), active=false");
        }
        
        hasInitialized = true;
    }
    
    private void Start()
    {
        Debug.Log($"[ScreenFadeManager] Start called. fadeInOnStart: {fadeInOnStart}, fadePanel null: {fadePanel == null}");
        if (fadeInOnStart && fadePanel != null)
        {
            // Re-ensure the panel is set up correctly at Start (in case something changed it)
            SetFadeAlpha(1f);
            fadePanel.gameObject.SetActive(true);
            
            Debug.Log("[ScreenFadeManager] Starting initial fade sequence");
            StartCoroutine(InitialFadeSequence());
        }
    }
    
    /// <summary>
    /// Called when the script is enabled - ensures fade state is maintained
    /// </summary>
    private void OnEnable()
    {
        // If we're supposed to fade in on start and haven't completed the initial fade yet,
        // make sure the panel stays opaque
        if (fadeInOnStart && hasInitialized && fadePanel != null && !isFading)
        {
            // Only force alpha if we haven't started fading yet
            if (activeFadeCoroutine == null)
            {
                SetFadeAlpha(1f);
                fadePanel.gameObject.SetActive(true);
            }
        }
    }
    
    /// <summary>
    /// Handles the initial fade from black when the scene starts
    /// </summary>
    private IEnumerator InitialFadeSequence()
    {
        // Ensure panel is fully opaque before we start
        SetFadeAlpha(1f);
        fadePanel.gameObject.SetActive(true);
        
        Debug.Log($"[ScreenFadeManager] InitialFadeSequence started, current alpha: {GetCurrentAlpha()}, waiting {initialFadeDelay}s");
        
        // Wait for the initial delay (use realtime so it works even if game starts paused)
        // During this time, keep ensuring the panel stays opaque
        float delayElapsed = 0f;
        while (delayElapsed < initialFadeDelay)
        {
            // Keep the panel opaque during the delay
            if (fadePanel.alpha < 1f)
            {
                fadePanel.alpha = 1f;
            }
            delayElapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        Debug.Log($"[ScreenFadeManager] Starting initial fade from black, alpha before fade: {GetCurrentAlpha()}");
        
        // Fade from black
        yield return FadeFromBlackCoroutine(defaultFadeDuration, () =>
        {
            Debug.Log("[ScreenFadeManager] Initial fade complete");
        });
    }
    
    /// <summary>
    /// Fade the screen to black
    /// </summary>
    /// <param name="duration">Duration of the fade (uses default if <= 0)</param>
    /// <param name="onComplete">Callback when fade completes</param>
    public void FadeToBlack(float duration = -1f, Action onComplete = null)
    {
        if (fadePanel == null)
        {
            Debug.LogError("[ScreenFadeManager] Cannot fade - Fade Panel is not assigned!");
            onComplete?.Invoke();
            return;
        }
        
        float fadeDuration = duration > 0 ? duration : defaultFadeDuration;
        
        if (activeFadeCoroutine != null)
        {
            StopCoroutine(activeFadeCoroutine);
        }
        
        activeFadeCoroutine = StartCoroutine(FadeToBlackCoroutine(fadeDuration, onComplete));
    }
    
    /// <summary>
    /// Fade the screen from black to clear
    /// </summary>
    /// <param name="duration">Duration of the fade (uses default if <= 0)</param>
    /// <param name="onComplete">Callback when fade completes</param>
    public void FadeFromBlack(float duration = -1f, Action onComplete = null)
    {
        if (fadePanel == null)
        {
            Debug.LogError("[ScreenFadeManager] Cannot fade - Fade Panel is not assigned!");
            onComplete?.Invoke();
            return;
        }
        
        float fadeDuration = duration > 0 ? duration : defaultFadeDuration;
        
        if (activeFadeCoroutine != null)
        {
            StopCoroutine(activeFadeCoroutine);
        }
        
        activeFadeCoroutine = StartCoroutine(FadeFromBlackCoroutine(fadeDuration, onComplete));
    }
    
    /// <summary>
    /// Fade to black, then fade back from black
    /// </summary>
    /// <param name="fadeInDuration">Duration of fade to black</param>
    /// <param name="holdDuration">How long to hold at full black</param>
    /// <param name="fadeOutDuration">Duration of fade from black</param>
    /// <param name="onFadeToBlackComplete">Callback when fade to black completes (before hold)</param>
    /// <param name="onComplete">Callback when entire sequence completes</param>
    public void FadeToBlackAndBack(float fadeInDuration = -1f, float holdDuration = 0.5f, float fadeOutDuration = -1f, 
        Action onFadeToBlackComplete = null, Action onComplete = null)
    {
        if (fadePanel == null)
        {
            Debug.LogError("[ScreenFadeManager] Cannot fade - Fade Panel is not assigned!");
            onComplete?.Invoke();
            return;
        }
        
        float inDuration = fadeInDuration > 0 ? fadeInDuration : defaultFadeDuration;
        float outDuration = fadeOutDuration > 0 ? fadeOutDuration : defaultFadeDuration;
        
        if (activeFadeCoroutine != null)
        {
            StopCoroutine(activeFadeCoroutine);
        }
        
        activeFadeCoroutine = StartCoroutine(FadeToBlackAndBackCoroutine(inDuration, holdDuration, outDuration, onFadeToBlackComplete, onComplete));
    }
    
    private IEnumerator FadeToBlackCoroutine(float duration, Action onComplete)
    {
        isFading = true;
        
        Debug.Log($"[ScreenFadeManager] Fading to black over {duration}s, current alpha: {GetCurrentAlpha()}");
        
        float startAlpha = GetCurrentAlpha();
        float elapsed = 0f;
        
        // Make sure the panel is active
        fadePanel.gameObject.SetActive(true);
        
        while (elapsed < duration)
        {
            // Use unscaledDeltaTime so fades work even when game is paused
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(startAlpha, 1f, t);
            SetFadeAlpha(alpha);
            yield return null;
        }
        
        SetFadeAlpha(1f);
        isFading = false;
        activeFadeCoroutine = null;
        
        Debug.Log("[ScreenFadeManager] Fade to black complete");
        
        OnFadeToBlackComplete?.Invoke();
        onComplete?.Invoke();
    }
    
    private IEnumerator FadeFromBlackCoroutine(float duration, Action onComplete)
    {
        isFading = true;
        
        // Ensure we start from fully opaque
        float startAlpha = GetCurrentAlpha();
        if (startAlpha < 0.99f)
        {
            Debug.LogWarning($"[ScreenFadeManager] FadeFromBlack starting with alpha {startAlpha}, forcing to 1.0");
            startAlpha = 1f;
            SetFadeAlpha(1f);
        }
        
        Debug.Log($"[ScreenFadeManager] Fading from black over {duration}s, current alpha: {startAlpha}");
        
        float elapsed = 0f;
        
        // Make sure the panel is active and visible
        fadePanel.gameObject.SetActive(true);
        fadePanel.blocksRaycasts = true;
        
        while (elapsed < duration)
        {
            // Use unscaledDeltaTime so fades work even when game is paused
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Use smooth step for a nicer fade curve
            float smoothT = t * t * (3f - 2f * t);
            float alpha = Mathf.Lerp(startAlpha, 0f, smoothT);
            
            SetFadeAlpha(alpha);
            
            if (debugMode && Time.frameCount % 10 == 0)
            {
                Debug.Log($"[ScreenFadeManager] Fade progress: t={t:F2}, alpha={alpha:F2}");
            }
            
            yield return null;
        }
        
        SetFadeAlpha(0f);
        
        // Disable the panel when fully transparent
        fadePanel.blocksRaycasts = false;
        fadePanel.gameObject.SetActive(false);
        
        isFading = false;
        activeFadeCoroutine = null;
        
        Debug.Log("[ScreenFadeManager] Fade from black complete");
        
        OnFadeFromBlackComplete?.Invoke();
        onComplete?.Invoke();
    }
    
    private IEnumerator FadeToBlackAndBackCoroutine(float fadeInDuration, float holdDuration, float fadeOutDuration,
        Action onFadeToBlackComplete, Action onComplete)
    {
        // Fade to black
        yield return FadeToBlackCoroutine(fadeInDuration, null);
        
        onFadeToBlackComplete?.Invoke();
        
        // Hold at black (use WaitForSecondsRealtime so it works when paused)
        if (holdDuration > 0)
        {
            yield return new WaitForSecondsRealtime(holdDuration);
        }
        
        // Fade from black
        yield return FadeFromBlackCoroutine(fadeOutDuration, null);
        
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// Set the fade panel alpha directly
    /// </summary>
    private void SetFadeAlpha(float alpha)
    {
        if (fadePanel != null)
        {
            fadePanel.alpha = alpha;
        }
    }
    
    /// <summary>
    /// Get the current alpha of the fade panel
    /// </summary>
    private float GetCurrentAlpha()
    {
        return fadePanel != null ? fadePanel.alpha : 0f;
    }
    
    /// <summary>
    /// Check if a fade is currently in progress
    /// </summary>
    public bool IsFading => isFading;
    
    /// <summary>
    /// Immediately set the screen to fully black (no fade)
    /// </summary>
    public void SetBlack()
    {
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            SetFadeAlpha(1f);
        }
    }
    
    /// <summary>
    /// Immediately clear the fade (no fade)
    /// </summary>
    public void SetClear()
    {
        if (fadePanel != null)
        {
            SetFadeAlpha(0f);
            fadePanel.gameObject.SetActive(false);
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
            
            if (GUILayout.Button("Fade To Black"))
            {
                manager.FadeToBlack(testDuration);
            }
            
            if (GUILayout.Button("Fade From Black"))
            {
                manager.FadeFromBlack(testDuration);
            }
            
            UnityEditor.EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Fade To Black And Back"))
            {
                manager.FadeToBlackAndBack(testDuration, 0.5f, testDuration);
            }
            
            UnityEditor.EditorGUILayout.Space(5);
            
            UnityEditor.EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Set Black (Instant)"))
            {
                manager.SetBlack();
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