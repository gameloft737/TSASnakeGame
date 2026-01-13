
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages a controls panel that slides in from the top-right corner after the arcade cutscene ends.
/// Displays control hints like "WASD to move", "Mouse to look", "E to interact".
///
/// SETUP INSTRUCTIONS:
/// 1. Create a Canvas (or use existing UI Canvas)
/// 2. Create a Panel anchored to top-right corner
/// 3. Add TextMeshPro texts for each control hint
/// 4. Add this script to the panel or a manager object
/// 5. The script will automatically find CutsceneController and subscribe to OnCutsceneEnded
///
/// HIERARCHY EXAMPLE:
/// Canvas
/// └── ControlsSlideInPanel (anchor top-right, pivot top-right)
///     ├── Background (Image)
///     ├── Title Text - "Controls"
///     ├── Control1 Text - "WASD to move"
///     ├── Control2 Text - "Mouse to look"
///     └── Control3 Text - "E to interact"
/// </summary>
public class ControlsSlideInPanel : MonoBehaviour
{
    public static ControlsSlideInPanel Instance { get; private set; }
    
    [Header("Panel Reference")]
    [Tooltip("The RectTransform of the panel to slide in. If not assigned, uses this GameObject's RectTransform.")]
    [SerializeField] private RectTransform panelRectTransform;
    
    [Header("Slide Animation Settings")]
    [Tooltip("Duration of the slide-in animation in seconds")]
    [SerializeField] private float slideInDuration = 0.5f;
    
    [Tooltip("Duration of the slide-out animation in seconds")]
    [SerializeField] private float slideOutDuration = 0.3f;
    
    [Tooltip("Delay before starting the slide-in animation")]
    [SerializeField] private float slideInDelay = 0.5f;
    
    [Tooltip("Animation curve for the slide-in effect")]
    [SerializeField] private AnimationCurve slideInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Tooltip("Animation curve for the slide-out effect")]
    [SerializeField] private AnimationCurve slideOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Position Settings")]
    [Tooltip("How far off-screen the panel starts (positive value moves it to the right)")]
    [SerializeField] private float offScreenOffset = 400f;
    
    [Tooltip("If true, uses the panel's current position in the scene as the visible position. If false, uses visiblePositionX.")]
    [SerializeField] private bool useCurrentPositionAsVisible = true;
    
    [Tooltip("The final X position when fully visible (only used if useCurrentPositionAsVisible is false)")]
    [SerializeField] private float visiblePositionX = 0f;
    
    [Header("Auto-Hide Settings")]
    [Tooltip("If true, the panel will automatically hide after a duration")]
    [SerializeField] private bool autoHide = true;
    
    [Tooltip("Time in seconds before the panel auto-hides")]
    [SerializeField] private float autoHideDelay = 8f;
    
    [Header("Toggle Settings")]
    [Tooltip("Key to toggle the panel visibility after it has appeared")]
    [SerializeField] private KeyCode toggleKey = KeyCode.H;
    
    [Tooltip("If true, allows toggling the panel with the toggle key")]
    [SerializeField] private bool allowToggle = true;
    
    [Header("Cutscene Integration")]
    [Tooltip("If assigned, will automatically slide in when the cutscene ends")]
    [SerializeField] private CutsceneController cutsceneController;
    
    [Tooltip("If true, will try to find CutsceneController automatically")]
    [SerializeField] private bool autoFindCutsceneController = true;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;
    private bool isVisible = false;
    private bool hasAppeared = false;
    private Coroutine slideCoroutine;
    private Coroutine autoHideCoroutine;
    
    /// <summary>
    /// Returns whether the panel is currently visible
    /// </summary>
    public bool IsVisible => isVisible;
    
    /// <summary>
    /// Returns whether the panel has appeared at least once
    /// </summary>
    public bool HasAppeared => hasAppeared;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[ControlsSlideInPanel] Multiple instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        // Get RectTransform if not assigned
        if (panelRectTransform == null)
        {
            panelRectTransform = GetComponent<RectTransform>();
        }
    }
    
    private void Start()
    {
        ValidateReferences();
        SetupPositions();
        SetupCutsceneIntegration();
        
        // Start hidden
        SetPanelPosition(hiddenPosition);
        
        if (debugMode)
            Debug.Log("[ControlsSlideInPanel] Initialized. Panel hidden off-screen.");
    }
    
    private void Update()
    {
        // Allow toggling after the panel has appeared at least once
        if (allowToggle && hasAppeared && Input.GetKeyDown(toggleKey))
        {
            TogglePanel();
        }
    }
    
    private void ValidateReferences()
    {
        if (panelRectTransform == null)
        {
            Debug.LogError("[ControlsSlideInPanel] Panel RectTransform is not assigned and could not be found!");
        }
    }
    
    private void SetupPositions()
    {
        if (panelRectTransform == null) return;
        
        // Use the panel's current position as the visible position (where it's placed in the scene)
        // This allows designers to position the panel exactly where they want it in the editor
        if (useCurrentPositionAsVisible)
        {
            visiblePosition = panelRectTransform.anchoredPosition;
        }
        else
        {
            visiblePosition = new Vector2(visiblePositionX, panelRectTransform.anchoredPosition.y);
        }
        
        // Hidden position is offset to the right (off-screen)
        hiddenPosition = new Vector2(visiblePosition.x + offScreenOffset, visiblePosition.y);
        
        if (debugMode)
            Debug.Log($"[ControlsSlideInPanel] Positions set - Hidden: {hiddenPosition}, Visible: {visiblePosition}");
    }
    
    private void SetupCutsceneIntegration()
    {
        // Try to find CutsceneController if not assigned
        if (cutsceneController == null && autoFindCutsceneController)
        {
            cutsceneController = FindFirstObjectByType<CutsceneController>();
        }
        
        // Subscribe to cutscene end event
        if (cutsceneController != null)
        {
            cutsceneController.OnCutsceneEnded += OnCutsceneEnded;
            
            if (debugMode)
                Debug.Log("[ControlsSlideInPanel] Subscribed to CutsceneController.OnCutsceneEnded");
        }
        else if (debugMode)
        {
            Debug.Log("[ControlsSlideInPanel] No CutsceneController found. Call SlideIn() manually.");
        }
    }
    
    private void OnCutsceneEnded()
    {
        if (debugMode)
            Debug.Log("[ControlsSlideInPanel] Cutscene ended. Triggering slide-in.");
        
        SlideIn();
    }
    
    /// <summary>
    /// Slide the panel in from the right
    /// </summary>
    public void SlideIn()
    {
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }
        
        slideCoroutine = StartCoroutine(SlideInCoroutine());
    }
    
    /// <summary>
    /// Slide the panel out to the right
    /// </summary>
    public void SlideOut()
    {
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }
        
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
        
        slideCoroutine = StartCoroutine(SlideOutCoroutine());
    }
    
    /// <summary>
    /// Toggle the panel visibility
    /// </summary>
    public void TogglePanel()
    {
        if (isVisible)
        {
            SlideOut();
        }
        else
        {
            SlideIn();
        }
    }
    
    private IEnumerator SlideInCoroutine()
    {
        // Wait for delay
        if (slideInDelay > 0)
        {
            yield return new WaitForSeconds(slideInDelay);
        }
        
        if (debugMode)
            Debug.Log("[ControlsSlideInPanel] Starting slide-in animation");
        
        float elapsed = 0f;
        Vector2 startPos = panelRectTransform.anchoredPosition;
        
        while (elapsed < slideInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideInDuration;
            float curveValue = slideInCurve.Evaluate(t);
            
            Vector2 newPos = Vector2.Lerp(startPos, visiblePosition, curveValue);
            SetPanelPosition(newPos);
            
            yield return null;
        }
        
        SetPanelPosition(visiblePosition);
        isVisible = true;
        hasAppeared = true;
        slideCoroutine = null;
        
        if (debugMode)
            Debug.Log("[ControlsSlideInPanel] Slide-in complete");
        
        // Start auto-hide timer if enabled
        if (autoHide && autoHideDelay > 0)
        {
            autoHideCoroutine = StartCoroutine(AutoHideCoroutine());
        }
    }
    
    private IEnumerator SlideOutCoroutine()
    {
        if (debugMode)
            Debug.Log("[ControlsSlideInPanel] Starting slide-out animation");
        
        float elapsed = 0f;
        Vector2 startPos = panelRectTransform.anchoredPosition;
        
        while (elapsed < slideOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideOutDuration;
            float curveValue = slideOutCurve.Evaluate(t);
            
            Vector2 newPos = Vector2.Lerp(startPos, hiddenPosition, curveValue);
            SetPanelPosition(newPos);
            
            yield return null;
        }
        
        SetPanelPosition(hiddenPosition);
        isVisible = false;
        slideCoroutine = null;
        
        if (debugMode)
            Debug.Log("[ControlsSlideInPanel] Slide-out complete");
    }
    
    private IEnumerator AutoHideCoroutine()
    {
        yield return new WaitForSeconds(autoHideDelay);
        
        if (isVisible)
        {
            if (debugMode)
                Debug.Log("[ControlsSlideInPanel] Auto-hiding panel");
            
            SlideOut();
        }
        
        autoHideCoroutine = null;
    }
    
    private void SetPanelPosition(Vector2 position)
    {
        if (panelRectTransform != null)
        {
            panelRectTransform.anchoredPosition = position;
        }
    }
    
    /// <summary>
    /// Immediately show the panel without animation
    /// </summary>
    public void ShowImmediate()
    {
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
            slideCoroutine = null;
        }
        
        SetPanelPosition(visiblePosition);
        isVisible = true;
        hasAppeared = true;
    }
    
    /// <summary>
    /// Immediately hide the panel without animation
    /// </summary>
    public void HideImmediate()
    {
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
            slideCoroutine = null;
        }
        
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
        
        SetPanelPosition(hiddenPosition);
        isVisible = false;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (cutsceneController != null)
        {
            cutsceneController.OnCutsceneEnded -= OnCutsceneEnded;
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(ControlsSlideInPanel))]
public class ControlsSlideInPanelEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        ControlsSlideInPanel panel = (ControlsSlideInPanel)target;
        
        DrawDefaultInspector();
        
        UnityEditor.EditorGUILayout.Space(15);
        UnityEditor.EditorGUILayout.LabelField("Testing Tools", UnityEditor.EditorStyles.boldLabel);
        
        if (Application.isPlaying)
        {
            UnityEditor.EditorGUILayout.BeginVertical("box");
            
            UnityEditor.EditorGUILayout.LabelField("Current State:", panel.IsVisible ? "Visible" : "Hidden");
            UnityEditor.EditorGUILayout.LabelField("Has Appeared:", panel.HasAppeared ? "Yes" : "No");
            
            UnityEditor.EditorGUILayout.Space(5);
            
            UnityEditor.EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Slide In"))
            {
                panel.SlideIn();
            }
            if (GUILayout.Button("Slide Out"))
            {
                panel.SlideOut();
            }
            UnityEditor.EditorGUILayout.EndHorizontal();
            
            UnityEditor.EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Show Immediate"))
            {
                panel.ShowImmediate();
            }
            if (GUILayout.Button("Hide Immediate"))
            {
                panel.HideImmediate();
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
            "1. Create a Panel anchored to top-right (pivot: top-right)\n" +
            "2. Add TextMeshPro texts for controls:\n" +
            "   - 'WASD to move'\n" +
            "   - 'Mouse to look'\n" +
            "   - 'E to interact'\n" +
            "3. Add this script to the panel\n" +
            "4. CutsceneController will be found automatically\n" +
            "5. Panel will slide in when arcade cutscene ends",
            UnityEditor.MessageType.Info);
    }
}
#endif