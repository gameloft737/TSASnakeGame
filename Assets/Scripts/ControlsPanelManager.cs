using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Manages a controls panel that can be toggled with the H key.
/// Shows/hides the controls panel and updates the hint text accordingly.
///
/// SETUP INSTRUCTIONS:
/// 1. Create a Canvas (or use an existing UI Canvas)
/// 2. Create a Panel in the bottom-right corner for the controls
/// 3. Create a TextMeshPro text above the panel for the hint ("Press H to hide/unhide")
/// 4. Create an empty GameObject and add this script
/// 5. Assign the panel and hint text references in the Inspector
///
/// HIERARCHY EXAMPLE:
/// Canvas
/// ├── ControlsContainer (anchor bottom-right)
/// │   ├── HintText (TextMeshProUGUI) - "Press H to hide"
/// │   └── ControlsPanel (Panel with your controls info)
/// │       ├── Title Text - "Controls"
/// │       ├── Control 1 - "WASD - Move"
/// │       ├── Control 2 - "Mouse - Look"
/// │       └── ... more controls
/// └── ControlsPanelManager (this script)
///
/// USAGE:
/// - Press H to toggle the controls panel visibility
/// - The hint text automatically updates to show "Press H to hide" or "Press H to unhide"
/// </summary>
public class ControlsPanelManager : MonoBehaviour
{
    public static ControlsPanelManager Instance { get; private set; }
    
    [Header("UI References")]
    [Tooltip("The panel containing the controls information")]
    [SerializeField] private GameObject controlsPanel;
    
    [Tooltip("The text that shows 'Press H to hide/unhide'")]
    [SerializeField] private TextMeshProUGUI hintText;
    
    [Header("Settings")]
    [Tooltip("The key to toggle the controls panel")]
    [SerializeField] private KeyCode toggleKey = KeyCode.H;
    
    [Tooltip("Text to show when the panel is visible")]
    [SerializeField] private string hideHintText = "Press H to hide";
    
    [Tooltip("Text to show when the panel is hidden")]
    [SerializeField] private string showHintText = "Press H to unhide";
    
    [Tooltip("Should the panel start visible?")]
    [SerializeField] private bool startVisible = true;
    
    [Tooltip("Should the hint text always be visible (even when panel is hidden)?")]
    [SerializeField] private bool alwaysShowHint = true;
    
    [Header("Animation (Optional)")]
    [Tooltip("If true, the panel will fade in/out instead of instantly appearing/disappearing")]
    [SerializeField] private bool useFadeAnimation = false;
    
    [Tooltip("Duration of the fade animation")]
    [SerializeField] private float fadeDuration = 0.25f;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private bool isPanelVisible = true;
    private CanvasGroup panelCanvasGroup;
    private Coroutine fadeCoroutine;
    
    /// <summary>
    /// Returns whether the controls panel is currently visible
    /// </summary>
    public bool IsPanelVisible => isPanelVisible;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[ControlsPanelManager] Multiple instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        ValidateReferences();
        SetupCanvasGroup();
        
        // Set initial state
        isPanelVisible = startVisible;
        UpdatePanelVisibility(false); // false = no animation on start
        UpdateHintText();
        
        if (debugMode)
            Debug.Log($"[ControlsPanelManager] Initialized. Panel visible: {isPanelVisible}");
    }
    
    private void Update()
    {
        // Check for toggle key press
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePanel();
        }
    }
    
    /// <summary>
    /// Validate that required references are assigned
    /// </summary>
    private void ValidateReferences()
    {
        if (controlsPanel == null)
        {
            Debug.LogError("[ControlsPanelManager] Controls Panel is not assigned! Please assign it in the Inspector.");
        }
        
        if (hintText == null)
        {
            Debug.LogWarning("[ControlsPanelManager] Hint Text is not assigned. The hint text feature will be disabled.");
        }
    }
    
    /// <summary>
    /// Setup CanvasGroup for fade animation if needed
    /// </summary>
    private void SetupCanvasGroup()
    {
        if (useFadeAnimation && controlsPanel != null)
        {
            panelCanvasGroup = controlsPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = controlsPanel.AddComponent<CanvasGroup>();
            }
        }
    }
    
    /// <summary>
    /// Toggle the controls panel visibility
    /// </summary>
    public void TogglePanel()
    {
        isPanelVisible = !isPanelVisible;
        UpdatePanelVisibility(useFadeAnimation);
        UpdateHintText();
        
        if (debugMode)
            Debug.Log($"[ControlsPanelManager] Panel toggled. Now visible: {isPanelVisible}");
    }
    
    /// <summary>
    /// Show the controls panel
    /// </summary>
    public void ShowPanel()
    {
        if (!isPanelVisible)
        {
            isPanelVisible = true;
            UpdatePanelVisibility(useFadeAnimation);
            UpdateHintText();
            
            if (debugMode)
                Debug.Log("[ControlsPanelManager] Panel shown");
        }
    }
    
    /// <summary>
    /// Hide the controls panel
    /// </summary>
    public void HidePanel()
    {
        if (isPanelVisible)
        {
            isPanelVisible = false;
            UpdatePanelVisibility(useFadeAnimation);
            UpdateHintText();
            
            if (debugMode)
                Debug.Log("[ControlsPanelManager] Panel hidden");
        }
    }
    
    /// <summary>
    /// Update the panel visibility with optional animation
    /// </summary>
    private void UpdatePanelVisibility(bool animate)
    {
        if (controlsPanel == null) return;
        
        if (animate && panelCanvasGroup != null)
        {
            // Stop any existing fade
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            
            fadeCoroutine = StartCoroutine(FadePanel(isPanelVisible));
        }
        else
        {
            // Instant show/hide
            controlsPanel.SetActive(isPanelVisible);
            
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = isPanelVisible ? 1f : 0f;
            }
        }
    }
    
    /// <summary>
    /// Coroutine to fade the panel in/out
    /// </summary>
    private System.Collections.IEnumerator FadePanel(bool fadeIn)
    {
        if (panelCanvasGroup == null) yield break;
        
        // Make sure panel is active for fade
        controlsPanel.SetActive(true);
        
        float startAlpha = panelCanvasGroup.alpha;
        float targetAlpha = fadeIn ? 1f : 0f;
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time so it works when game is paused
            float t = elapsed / fadeDuration;
            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }
        
        panelCanvasGroup.alpha = targetAlpha;
        
        // If fading out, disable the panel after fade completes
        if (!fadeIn)
        {
            controlsPanel.SetActive(false);
        }
        
        fadeCoroutine = null;
    }
    
    /// <summary>
    /// Update the hint text based on panel visibility
    /// </summary>
    private void UpdateHintText()
    {
        if (hintText == null) return;
        
        // Update the text
        hintText.text = isPanelVisible ? hideHintText : showHintText;
        
        // Show/hide hint based on settings
        if (!alwaysShowHint)
        {
            hintText.gameObject.SetActive(isPanelVisible);
        }
        else
        {
            hintText.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Set the toggle key at runtime
    /// </summary>
    public void SetToggleKey(KeyCode newKey)
    {
        toggleKey = newKey;
        
        // Update hint text to reflect new key
        hideHintText = $"Press {newKey} to hide";
        showHintText = $"Press {newKey} to unhide";
        UpdateHintText();
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(ControlsPanelManager))]
public class ControlsPanelManagerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        ControlsPanelManager manager = (ControlsPanelManager)target;
        
        DrawDefaultInspector();
        
        UnityEditor.EditorGUILayout.Space(15);
        UnityEditor.EditorGUILayout.LabelField("Testing Tools", UnityEditor.EditorStyles.boldLabel);
        
        if (Application.isPlaying)
        {
            UnityEditor.EditorGUILayout.BeginVertical("box");
            
            UnityEditor.EditorGUILayout.LabelField("Current State:", manager.IsPanelVisible ? "Visible" : "Hidden");
            
            UnityEditor.EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Toggle Panel"))
            {
                manager.TogglePanel();
            }
            
            UnityEditor.EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Show Panel"))
            {
                manager.ShowPanel();
            }
            if (GUILayout.Button("Hide Panel"))
            {
                manager.HidePanel();
            }
            UnityEditor.EditorGUILayout.EndHorizontal();
            
            UnityEditor.EditorGUILayout.EndVertical();
        }
        else
        {
            UnityEditor.EditorGUILayout.HelpBox("Enter Play Mode to access testing tools.", UnityEditor.MessageType.Info);
        }
        
        UnityEditor.EditorGUILayout.Space(10);
        UnityEditor.EditorGUILayout.HelpBox(
            "SETUP GUIDE:\n" +
            "1. Create a Panel in your Canvas for the controls\n" +
            "2. Create a TextMeshPro text for the hint\n" +
            "3. Assign both to this script\n" +
            "4. Press H in-game to toggle visibility",
            UnityEditor.MessageType.Info);
    }
}
#endif