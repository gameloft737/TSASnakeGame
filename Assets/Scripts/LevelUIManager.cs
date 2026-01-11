
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Defines what type of trigger condition to use
/// </summary>
public enum LevelTriggerType
{
    XPLevel,        // Triggers when player reaches a specific XP level
    WaveNumber      // Triggers when a specific wave starts
}

/// <summary>
/// Defines what action to take when the trigger fires
/// </summary>
public enum LevelUIActionType
{
    ShowSubtitle,           // Show a subtitle message
    ShowGameObject,         // Enable a GameObject
    HideGameObject,         // Disable a GameObject
    ToggleGameObject,       // Toggle a GameObject's active state
    PlayAnimation,          // Play an animation on an Animator
    InvokeUnityEvent,       // Invoke a custom UnityEvent
    FadeToBlack,            // Fade the screen to black
    FadeFromBlack,          // Fade the screen from black to clear
    FadeToBlackAndBack,     // Fade to black, hold, then fade back
    LoadScene,              // Load a new scene (with optional fade)
    FadeAndLoadScene,       // Fade to white, then load a new scene
    ShowTutorialPanel       // Show a tutorial panel that pauses the game
}

/// <summary>
/// A single UI trigger configuration
/// </summary>
[System.Serializable]
public class LevelUITrigger
{
    [Header("Trigger Condition")]
    [Tooltip("What type of trigger to use (XP Level or Wave Number)")]
    public LevelTriggerType triggerType = LevelTriggerType.XPLevel;
    
    [Tooltip("The level/wave number that triggers this action")]
    public int triggerValue = 1;
    
    [Tooltip("If true, this trigger can only fire once per game session")]
    public bool triggerOnce = true;
    
    [Tooltip("If true, waits for the Attack Selection UI to close before executing this trigger. Useful for levels 2+ where the attack menu appears.")]
    public bool waitForAttackUI = false;
    
    [Header("Action")]
    [Tooltip("What action to perform when triggered")]
    public LevelUIActionType actionType = LevelUIActionType.ShowSubtitle;
    
    [Header("Subtitle Settings (for ShowSubtitle action)")]
    [TextArea(2, 5)]
    [Tooltip("The subtitle text to display")]
    public string subtitleText = "";
    
    [Tooltip("How long to show the subtitle (0 = use default duration)")]
    public float subtitleDuration = 3f;
    
    [Header("GameObject Settings (for Show/Hide/Toggle actions)")]
    [Tooltip("The GameObject to show/hide/toggle")]
    public GameObject targetGameObject;
    
    [Header("Animation Settings (for PlayAnimation action)")]
    [Tooltip("The Animator to use")]
    public Animator targetAnimator;
    
    [Tooltip("The animation state/trigger name to play")]
    public string animationName = "";
    
    [Tooltip("If true, uses SetTrigger. If false, uses Play()")]
    public bool useAnimatorTrigger = true;
    
    [Header("Unity Event Settings (for InvokeUnityEvent action)")]
    [Tooltip("Custom event to invoke when triggered")]
    public UnityEngine.Events.UnityEvent onTriggered;
    
    [Header("Fade Settings (for Fade actions)")]
    [Tooltip("Duration of the fade effect")]
    public float fadeDuration = 1f;
    
    [Tooltip("How long to hold at full black (for FadeToBlackAndBack)")]
    public float fadeHoldDuration = 0.5f;
    
    [Tooltip("If true, shows a subtitle after the fade completes")]
    public bool showSubtitleAfterFade = false;
    
    [Tooltip("Subtitle text to show after fade completes")]
    [TextArea(2, 5)]
    public string fadeSubtitleText = "";
    
    [Tooltip("Duration to show the subtitle after fade")]
    public float fadeSubtitleDuration = 3f;
    
    [Header("Scene Loading Settings (for LoadScene/FadeAndLoadScene actions)")]
    [Tooltip("The name of the scene to load")]
    public string sceneToLoad = "";
    
    [Tooltip("If true, uses additive scene loading instead of replacing the current scene")]
    public bool additiveSceneLoad = false;
    
    [Header("Tutorial Panel Settings (for ShowTutorialPanel action)")]
    [Tooltip("(Legacy - no longer used) Title text for the tutorial panel")]
    public string tutorialTitle = "Tutorial";
    
    [Tooltip("(Legacy - no longer used) Instructions text for the tutorial panel")]
    [TextArea(3, 8)]
    public string tutorialInstructions = "";
    
    [Header("Optional Delay")]
    [Tooltip("Delay in seconds before executing the action")]
    public float delay = 0f;
    
    // Runtime state
    [HideInInspector]
    public bool hasTriggered = false;
    
    /// <summary>
    /// Resets the trigger state (call when restarting the game)
    /// </summary>
    public void Reset()
    {
        hasTriggered = false;
    }
    
    /// <summary>
    /// Checks if this trigger should fire for the given level/wave
    /// </summary>
    public bool ShouldTrigger(LevelTriggerType type, int value)
    {
        if (triggerType != type) return false;
        if (triggerOnce && hasTriggered) return false;
        return value == triggerValue;
    }
}

/// <summary>
/// Manages UI triggers based on player level and wave progression.
/// Listens to XPManager and WaveManager events and executes configured triggers.
///
/// SETUP INSTRUCTIONS:
/// 1. Create an empty GameObject in your scene and name it "LevelUIManager"
/// 2. Add this script to that GameObject
/// 3. Configure your triggers directly in the "Triggers" list
/// 4. Make sure you have SubtitleUI in your scene if using subtitle triggers
///
/// TRIGGER TYPES:
/// - XP Level: Triggers when player reaches a specific XP level (from XPManager)
/// - Wave Number: Triggers when a specific wave starts (from WaveManager)
///
/// ACTION TYPES:
/// - ShowSubtitle: Display a subtitle message using SubtitleUI
/// - ShowGameObject: Enable a specific GameObject
/// - HideGameObject: Disable a specific GameObject
/// - ToggleGameObject: Toggle a GameObject's active state
/// - PlayAnimation: Play an animation on an Animator
/// - InvokeUnityEvent: Call a custom UnityEvent
/// - FadeToBlack: Fade the screen to black using ScreenFadeManager
/// - FadeFromBlack: Fade the screen from black to clear
/// - FadeToBlackAndBack: Fade to black, hold, then fade back (with optional subtitle after)
/// - LoadScene: Load a new scene immediately
/// - FadeAndLoadScene: Fade to white, then load a new scene
/// - ShowTutorialPanel: Show a tutorial panel that pauses the game
/// </summary>
public class LevelUIManager : MonoBehaviour
{
    public static LevelUIManager Instance { get; private set; }
    
    [Header("Triggers")]
    [Tooltip("List of UI triggers that fire at specific levels or waves")]
    [SerializeField] private List<LevelUITrigger> triggers = new List<LevelUITrigger>();
    
    [Header("References (Auto-found if not assigned)")]
    [SerializeField] private XPManager xpManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private AttackSelectionUI attackSelectionUI;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[LevelUIManager] Multiple instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        FindReferences();
        SubscribeToEvents();
        
        // Reset triggers on start
        foreach (var trigger in triggers)
        {
            trigger.Reset();
        }
        
        if (debugMode)
        {
            Debug.Log($"[LevelUIManager] Initialized with {triggers.Count} triggers");
        }
        
        // Check current level on start and trigger any matching triggers
        // This handles the case where player starts at level 1 and has a level 1 trigger
        StartCoroutine(CheckInitialState());
    }
    
    /// <summary>
    /// Checks the initial level/wave state and triggers any matching triggers
    /// Uses a coroutine to wait one frame so all systems are initialized
    /// </summary>
    private IEnumerator CheckInitialState()
    {
        // Wait one frame for all systems to initialize
        yield return null;
        
        // Check current XP level
        if (xpManager != null)
        {
            int currentLevel = xpManager.GetCurrentLevel();
            if (debugMode)
                Debug.Log($"[LevelUIManager] Checking initial level: {currentLevel}");
            ProcessTriggers(LevelTriggerType.XPLevel, currentLevel);
        }
        
        // Check current wave (wave 0 = wave 1 in display)
        if (waveManager != null)
        {
            int currentWave = waveManager.GetCurrentWaveIndex() + 1;
            if (debugMode)
                Debug.Log($"[LevelUIManager] Checking initial wave: {currentWave}");
            ProcessTriggers(LevelTriggerType.WaveNumber, currentWave);
        }
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    private void FindReferences()
    {
        if (xpManager == null)
            xpManager = FindFirstObjectByType<XPManager>();
            
        if (waveManager == null)
            waveManager = FindFirstObjectByType<WaveManager>();
            
        if (attackSelectionUI == null)
            attackSelectionUI = FindFirstObjectByType<AttackSelectionUI>();
            
        if (xpManager == null)
            Debug.LogWarning("[LevelUIManager] XPManager not found. XP Level triggers will not work.");
            
        if (waveManager == null)
            Debug.LogWarning("[LevelUIManager] WaveManager not found. Wave triggers will not work.");
    }
    
    private void SubscribeToEvents()
    {
        // Subscribe to XP level up events
        XPManager.OnLeveledUp += OnLevelUp;
        
        // Subscribe to wave start events
        if (waveManager != null)
        {
            waveManager.OnWaveStarted.AddListener(OnWaveStarted);
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        // Unsubscribe from XP level up events
        XPManager.OnLeveledUp -= OnLevelUp;
        
        // Unsubscribe from wave start events
        if (waveManager != null)
        {
            waveManager.OnWaveStarted.RemoveListener(OnWaveStarted);
        }
    }
    
    /// <summary>
    /// Called when the player levels up
    /// </summary>
    private void OnLevelUp(int newLevel)
    {
        if (debugMode)
            Debug.Log($"[LevelUIManager] Player reached level {newLevel}");
            
        ProcessTriggers(LevelTriggerType.XPLevel, newLevel);
    }
    
    /// <summary>
    /// Called when a new wave starts
    /// </summary>
    private void OnWaveStarted(int waveIndex)
    {
        // Wave index is 0-based, but we display as 1-based
        int waveNumber = waveIndex + 1;
        
        if (debugMode)
            Debug.Log($"[LevelUIManager] Wave {waveNumber} started");
            
        ProcessTriggers(LevelTriggerType.WaveNumber, waveNumber);
    }
    
    /// <summary>
    /// Process all triggers for the given type and value
    /// </summary>
    private void ProcessTriggers(LevelTriggerType type, int value)
    {
        foreach (var trigger in triggers)
        {
            if (trigger.ShouldTrigger(type, value))
            {
                ExecuteTrigger(trigger);
            }
        }
    }
    
    /// <summary>
    /// Execute a single trigger's action
    /// </summary>
    private void ExecuteTrigger(LevelUITrigger trigger)
    {
        if (trigger == null) return;
        
        // Mark as triggered
        trigger.hasTriggered = true;
        
        if (debugMode)
            Debug.Log($"[LevelUIManager] Executing trigger: {trigger.actionType} at {trigger.triggerType} {trigger.triggerValue}");
        
        // Check if we need to wait for attack UI to close
        if (trigger.waitForAttackUI)
        {
            StartCoroutine(ExecuteTriggerAfterAttackUI(trigger));
        }
        // Execute with delay if specified
        else if (trigger.delay > 0)
        {
            StartCoroutine(ExecuteTriggerDelayed(trigger, trigger.delay));
        }
        else
        {
            ExecuteTriggerAction(trigger);
        }
    }
    
    /// <summary>
    /// Waits for the Attack Selection UI to close before executing the trigger
    /// </summary>
    private IEnumerator ExecuteTriggerAfterAttackUI(LevelUITrigger trigger)
    {
        if (debugMode)
            Debug.Log($"[LevelUIManager] Waiting for Attack UI to close before executing trigger: {trigger.actionType}");
        
        // First, wait for the attack UI to open (it might not be open yet when level up happens)
        // Give it a small window to open
        float waitForOpenTime = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < waitForOpenTime)
        {
            if (attackSelectionUI != null && attackSelectionUI.IsUIOpen())
            {
                break; // UI is open, proceed to wait for it to close
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Now wait for the attack UI to close
        if (attackSelectionUI != null)
        {
            while (attackSelectionUI.IsUIOpen())
            {
                yield return null;
            }
            
            if (debugMode)
                Debug.Log($"[LevelUIManager] Attack UI closed, now executing trigger: {trigger.actionType}");
        }
        
        // Apply any additional delay after the UI closes
        if (trigger.delay > 0)
        {
            yield return new WaitForSeconds(trigger.delay);
        }
        
        ExecuteTriggerAction(trigger);
    }
    
    private IEnumerator ExecuteTriggerDelayed(LevelUITrigger trigger, float delay)
    {
        yield return new WaitForSeconds(delay);
        ExecuteTriggerAction(trigger);
    }
    
    /// <summary>
    /// Execute the actual action for a trigger
    /// </summary>
    private void ExecuteTriggerAction(LevelUITrigger trigger)
    {
        switch (trigger.actionType)
        {
            case LevelUIActionType.ShowSubtitle:
                ShowSubtitle(trigger.subtitleText, trigger.subtitleDuration);
                break;
                
            case LevelUIActionType.ShowGameObject:
                if (trigger.targetGameObject != null)
                {
                    trigger.targetGameObject.SetActive(true);
                    if (debugMode)
                        Debug.Log($"[LevelUIManager] Showing GameObject: {trigger.targetGameObject.name}");
                }
                else
                {
                    Debug.LogWarning("[LevelUIManager] ShowGameObject trigger has no target GameObject assigned!");
                }
                break;
                
            case LevelUIActionType.HideGameObject:
                if (trigger.targetGameObject != null)
                {
                    trigger.targetGameObject.SetActive(false);
                    if (debugMode)
                        Debug.Log($"[LevelUIManager] Hiding GameObject: {trigger.targetGameObject.name}");
                }
                else
                {
                    Debug.LogWarning("[LevelUIManager] HideGameObject trigger has no target GameObject assigned!");
                }
                break;
                
            case LevelUIActionType.ToggleGameObject:
                if (trigger.targetGameObject != null)
                {
                    bool newState = !trigger.targetGameObject.activeSelf;
                    trigger.targetGameObject.SetActive(newState);
                    if (debugMode)
                        Debug.Log($"[LevelUIManager] Toggled GameObject: {trigger.targetGameObject.name} to {newState}");
                }
                else
                {
                    Debug.LogWarning("[LevelUIManager] ToggleGameObject trigger has no target GameObject assigned!");
                }
                break;
                
            case LevelUIActionType.PlayAnimation:
                if (trigger.targetAnimator != null && !string.IsNullOrEmpty(trigger.animationName))
                {
                    if (trigger.useAnimatorTrigger)
                    {
                        trigger.targetAnimator.SetTrigger(trigger.animationName);
                    }
                    else
                    {
                        trigger.targetAnimator.Play(trigger.animationName);
                    }
                    if (debugMode)
                        Debug.Log($"[LevelUIManager] Playing animation: {trigger.animationName}");
                }
                else
                {
                    Debug.LogWarning("[LevelUIManager] PlayAnimation trigger has no Animator or animation name assigned!");
                }
                break;
                
            case LevelUIActionType.InvokeUnityEvent:
                trigger.onTriggered?.Invoke();
                if (debugMode)
                    Debug.Log("[LevelUIManager] Invoked UnityEvent");
                break;
                
            case LevelUIActionType.FadeToBlack:
                if (ScreenFadeManager.Instance != null)
                {
                    ScreenFadeManager.Instance.FadeToBlack(trigger.fadeDuration, () =>
                    {
                        if (trigger.showSubtitleAfterFade && !string.IsNullOrEmpty(trigger.fadeSubtitleText))
                        {
                            ShowSubtitle(trigger.fadeSubtitleText, trigger.fadeSubtitleDuration);
                        }
                    });
                    if (debugMode)
                        Debug.Log($"[LevelUIManager] Fading to black over {trigger.fadeDuration}s");
                }
                else
                {
                    Debug.LogWarning("[LevelUIManager] ScreenFadeManager.Instance is null! Make sure ScreenFadeManager exists in the scene.");
                }
                break;
                
            case LevelUIActionType.FadeFromBlack:
                if (ScreenFadeManager.Instance != null)
                {
                    ScreenFadeManager.Instance.FadeFromBlack(trigger.fadeDuration, () =>
                    {
                        if (trigger.showSubtitleAfterFade && !string.IsNullOrEmpty(trigger.fadeSubtitleText))
                        {
                            ShowSubtitle(trigger.fadeSubtitleText, trigger.fadeSubtitleDuration);
                        }
                    });
                    if (debugMode)
                        Debug.Log($"[LevelUIManager] Fading from black over {trigger.fadeDuration}s");
                }
                else
                {
                    Debug.LogWarning("[LevelUIManager] ScreenFadeManager.Instance is null! Make sure ScreenFadeManager exists in the scene.");
                }
                break;
                
            case LevelUIActionType.FadeToBlackAndBack:
                if (ScreenFadeManager.Instance != null)
                {
                    ScreenFadeManager.Instance.FadeToBlackAndBack(
                        trigger.fadeDuration,
                        trigger.fadeHoldDuration,
                        trigger.fadeDuration,
                        null, // onFadeToBlackComplete
                        () =>
                        {
                            if (trigger.showSubtitleAfterFade && !string.IsNullOrEmpty(trigger.fadeSubtitleText))
                            {
                                ShowSubtitle(trigger.fadeSubtitleText, trigger.fadeSubtitleDuration);
                            }
                        });
                    if (debugMode)
                        Debug.Log($"[LevelUIManager] Fading to black and back over {trigger.fadeDuration}s each, hold {trigger.fadeHoldDuration}s");
                }
                else
                {
                    Debug.LogWarning("[LevelUIManager] ScreenFadeManager.Instance is null! Make sure ScreenFadeManager exists in the scene.");
                }
                break;
                
            case LevelUIActionType.LoadScene:
                if (!string.IsNullOrEmpty(trigger.sceneToLoad))
                {
                    LoadSceneInternal(trigger.sceneToLoad, trigger.additiveSceneLoad);
                    if (debugMode)
                        Debug.Log($"[LevelUIManager] Loading scene: {trigger.sceneToLoad}");
                }
                else
                {
                    Debug.LogWarning("[LevelUIManager] LoadScene trigger has no scene name assigned!");
                }
                break;
                
            case LevelUIActionType.FadeAndLoadScene:
                if (!string.IsNullOrEmpty(trigger.sceneToLoad))
                {
                    if (ScreenFadeManager.Instance != null)
                    {
                        ScreenFadeManager.Instance.FadeToBlack(trigger.fadeDuration, () =>
                        {
                            LoadSceneInternal(trigger.sceneToLoad, trigger.additiveSceneLoad);
                        });
                        if (debugMode)
                            Debug.Log($"[LevelUIManager] Fading to black then loading scene: {trigger.sceneToLoad}");
                    }
                    else
                    {
                        // Fallback: load scene without fade
                        Debug.LogWarning("[LevelUIManager] ScreenFadeManager.Instance is null! Loading scene without fade.");
                        LoadSceneInternal(trigger.sceneToLoad, trigger.additiveSceneLoad);
                    }
                }
                else
                {
                    Debug.LogWarning("[LevelUIManager] FadeAndLoadScene trigger has no scene name assigned!");
                }
                break;
                
            case LevelUIActionType.ShowTutorialPanel:
                if (TutorialPanelManager.Instance != null)
                {
                    TutorialPanelManager.Instance.ShowTutorial();
                    if (debugMode)
                        Debug.Log("[LevelUIManager] Showing tutorial panel");
                }
                else
                {
                    Debug.LogWarning("[LevelUIManager] TutorialPanelManager.Instance is null! Make sure TutorialPanelManager exists in the scene.");
                }
                break;
        }
    }
    
    /// <summary>
    /// Internal method to load a scene
    /// </summary>
    private void LoadSceneInternal(string sceneName, bool additive)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[LevelUIManager] Cannot load scene - scene name is empty!");
            return;
        }
        
        LoadSceneMode mode = additive ? LoadSceneMode.Additive : LoadSceneMode.Single;
        
        try
        {
            SceneManager.LoadScene(sceneName, mode);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LevelUIManager] Failed to load scene '{sceneName}': {e.Message}");
        }
    }
    
    /// <summary>
    /// Public method to load a scene with fade (can be called from other scripts)
    /// </summary>
    public void LoadSceneWithFade(string sceneName, float fadeDuration = 1f, bool additive = false)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[LevelUIManager] Cannot load scene - scene name is empty!");
            return;
        }
        
        if (ScreenFadeManager.Instance != null)
        {
            ScreenFadeManager.Instance.FadeToBlack(fadeDuration, () =>
            {
                LoadSceneInternal(sceneName, additive);
            });
        }
        else
        {
            Debug.LogWarning("[LevelUIManager] ScreenFadeManager not found. Loading scene without fade.");
            LoadSceneInternal(sceneName, additive);
        }
    }
    
    /// <summary>
    /// Public method to load a scene immediately (can be called from other scripts)
    /// </summary>
    public void LoadSceneImmediate(string sceneName, bool additive = false)
    {
        LoadSceneInternal(sceneName, additive);
    }
    
    /// <summary>
    /// Show a subtitle using the SubtitleUI system
    /// </summary>
    private void ShowSubtitle(string text, float duration)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("[LevelUIManager] Attempted to show empty subtitle");
            return;
        }
        
        if (SubtitleUI.Instance != null)
        {
            SubtitleUI.Instance.ShowSubtitle(text, duration);
            if (debugMode)
                Debug.Log($"[LevelUIManager] Showing subtitle: \"{text}\" for {duration}s");
        }
        else
        {
            Debug.LogWarning("[LevelUIManager] SubtitleUI.Instance is null! Make sure SubtitleUI exists in the scene.");
        }
    }
    
    /// <summary>
    /// Manually trigger a subtitle at a specific level (useful for testing)
    /// </summary>
    public void TriggerSubtitleForLevel(int level, string text, float duration = 3f)
    {
        if (debugMode)
            Debug.Log($"[LevelUIManager] Manually triggering subtitle for level {level}");
            
        ShowSubtitle(text, duration);
    }
    
    /// <summary>
    /// Reset all triggers (call when restarting the game)
    /// </summary>
    public void ResetAllTriggers()
    {
        foreach (var trigger in triggers)
        {
            trigger.Reset();
        }
        
        if (debugMode)
            Debug.Log("[LevelUIManager] All triggers have been reset");
    }
    
    /// <summary>
    /// Add a runtime trigger (useful for dynamic content)
    /// </summary>
    public void AddRuntimeTrigger(LevelUITrigger trigger)
    {
        if (trigger != null)
        {
            triggers.Add(trigger);
            if (debugMode)
                Debug.Log($"[LevelUIManager] Added runtime trigger: {trigger.actionType} at {trigger.triggerType} {trigger.triggerValue}");
        }
    }
    
    /// <summary>
    /// Remove a runtime trigger
    /// </summary>
    public void RemoveRuntimeTrigger(LevelUITrigger trigger)
    {
        if (trigger != null && triggers.Contains(trigger))
        {
            triggers.Remove(trigger);
            if (debugMode)
                Debug.Log("[LevelUIManager] Removed runtime trigger");
        }
    }
    
    /// <summary>
    /// Get all triggers
    /// </summary>
    public List<LevelUITrigger> GetTriggers() => triggers;
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(LevelUIManager))]
public class LevelUIManagerEditor : UnityEditor.Editor
{
    private int testLevel = 1;
    private int testWave = 1;
    private string testSubtitle = "Test subtitle message";
    private float testDuration = 3f;
    
    public override void OnInspectorGUI()
    {
        LevelUIManager manager = (LevelUIManager)target;
        
        DrawDefaultInspector();
        
        UnityEditor.EditorGUILayout.Space(15);
        UnityEditor.EditorGUILayout.LabelField("Testing Tools", UnityEditor.EditorStyles.boldLabel);
        
        // Only show testing tools in play mode
        if (Application.isPlaying)
        {
            UnityEditor.EditorGUILayout.BeginVertical("box");
            
            // Test subtitle
            UnityEditor.EditorGUILayout.LabelField("Test Subtitle", UnityEditor.EditorStyles.boldLabel);
            testSubtitle = UnityEditor.EditorGUILayout.TextField("Message", testSubtitle);
            testDuration = UnityEditor.EditorGUILayout.FloatField("Duration", testDuration);
            
            if (GUILayout.Button("Show Test Subtitle"))
            {
                manager.TriggerSubtitleForLevel(0, testSubtitle, testDuration);
            }
            
            UnityEditor.EditorGUILayout.Space(10);
            
            // Reset triggers
            if (GUILayout.Button("Reset All Triggers"))
            {
                manager.ResetAllTriggers();
            }
            
            UnityEditor.EditorGUILayout.EndVertical();
        }
        else
        {
            UnityEditor.EditorGUILayout.HelpBox("Enter Play Mode to access testing tools.", UnityEditor.MessageType.Info);
        }
    }
}
#endif