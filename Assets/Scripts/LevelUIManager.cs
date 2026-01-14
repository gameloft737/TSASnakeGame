
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Defines what type of trigger condition to use
/// </summary>
public enum LevelTriggerType
{
    XPLevel,        // Triggers when player reaches a specific XP level (rank)
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
    ShowTutorialPanel,      // Show a tutorial panel that pauses the game
    ShowLevelAnnouncement   // Show a level announcement with slide animation
}

/// <summary>
/// A single UI trigger configuration
/// </summary>
[System.Serializable]
public class LevelUITrigger
{
    [Header("Trigger Condition")]
    [Tooltip("What type of trigger to use (XP Level/Rank or Wave Number)")]
    public LevelTriggerType triggerType = LevelTriggerType.XPLevel;
    
    [Tooltip("The rank/wave number that triggers this action")]
    public int triggerValue = 1;
    
    [Tooltip("If true, this trigger can only fire once per game session")]
    public bool triggerOnce = true;
    
    [Tooltip("If true, waits for the Attack Selection UI to close before executing this trigger. Useful for ranks 2+ where the attack menu appears.")]
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
    
    [Tooltip("If true, makes the player invincible (takes no damage) when this trigger fires. Useful for end-game sequences.")]
    public bool disablePlayerDamage = false;
    
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
    
    [Header("Level Announcement Settings (for ShowLevelAnnouncement action)")]
    [Tooltip("The level number to announce (if 0, will auto-calculate from rank)")]
    public int announcementLevel = 0;
    
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
    /// Checks if this trigger should fire for the given rank/wave
    /// </summary>
    public bool ShouldTrigger(LevelTriggerType type, int value)
    {
        if (triggerType != type) return false;
        if (triggerOnce && hasTriggered) return false;
        return value == triggerValue;
    }
}

/// <summary>
/// Manages UI triggers based on player rank and wave progression.
/// Listens to XPManager and WaveManager events and executes configured triggers.
/// 
/// TERMINOLOGY:
/// - Rank: The XP level from XPManager (what was previously called "level")
/// - Level: A milestone reached every N ranks (configurable via ranksPerLevel)
///
/// SETUP INSTRUCTIONS:
/// 1. Create an empty GameObject in your scene and name it "LevelUIManager"
/// 2. Add this script to that GameObject
/// 3. Configure your triggers directly in the "Triggers" list
/// 4. Make sure you have SubtitleUI in your scene if using subtitle triggers
/// 5. For level announcements, assign a TextMeshProUGUI and its RectTransform
///
/// TRIGGER TYPES:
/// - XP Level (Rank): Triggers when player reaches a specific rank (from XPManager)
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
/// - ShowLevelAnnouncement: Show a level announcement with slide-in, hover, slide-out animation
/// </summary>
public class LevelUIManager : MonoBehaviour
{
    public static LevelUIManager Instance { get; private set; }
    
    [Header("Triggers")]
    [Tooltip("List of UI triggers that fire at specific ranks or waves")]
    [SerializeField] private List<LevelUITrigger> triggers = new List<LevelUITrigger>();
    
    [Header("References (Auto-found if not assigned)")]
    [SerializeField] private XPManager xpManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private AttackSelectionUI attackSelectionUI;
    
    [Header("Level Announcement UI")]
    [Tooltip("The TextMeshProUGUI component for displaying level announcements")]
    [SerializeField] private TextMeshProUGUI levelAnnouncementText;
    
    [Tooltip("The RectTransform of the level announcement (for animation)")]
    [SerializeField] private RectTransform levelAnnouncementRect;
    
    [Tooltip("How many ranks equal one level (default: 10)")]
    [SerializeField] private int ranksPerLevel = 10;
    
    [Tooltip("If true, automatically show level announcement when reaching level milestones")]
    [SerializeField] private bool autoShowLevelAnnouncements = true;
    
    [Header("Win Condition")]
    [Tooltip("The rank at which the player wins the game (0 = disabled)")]
    [SerializeField] private int winRank = 50;
    
    [Tooltip("The text to display when the player wins")]
    [SerializeField] private string winText = "You Won!";
    
    [Tooltip("If true, automatically show win announcement when reaching win rank")]
    [SerializeField] private bool autoShowWinAnnouncement = true;
    
    [Header("Level Announcement Animation Settings")]
    [Tooltip("Duration of the slide-in animation")]
    [SerializeField] private float slideInDuration = 0.5f;
    
    [Tooltip("Duration to hover in the center")]
    [SerializeField] private float hoverDuration = 2f;
    
    [Tooltip("Duration of the slide-out animation")]
    [SerializeField] private float slideOutDuration = 0.5f;
    
    [Tooltip("How far off-screen the text starts (in pixels from center)")]
    [SerializeField] private float offScreenOffset = 1000f;
    
    [Tooltip("Amplitude of the hover wobble effect")]
    [SerializeField] private float wobbleAmplitude = 10f;
    
    [Tooltip("Speed of the hover wobble effect")]
    [SerializeField] private float wobbleSpeed = 3f;
    
    [Tooltip("Easing curve for slide animations")]
    [SerializeField] private AnimationCurve slideEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    // Track the last announced level to avoid duplicates
    private int lastAnnouncedLevel = 0;
    private Coroutine levelAnnouncementCoroutine;
    private bool hasWon = false;
    
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
        
        // Check current rank on start and trigger any matching triggers
        StartCoroutine(CheckInitialState());
    }
    
    private IEnumerator CheckInitialState()
    {
        yield return null;
        
        if (xpManager != null)
        {
            int currentRank = xpManager.GetCurrentLevel();
            if (debugMode)
                Debug.Log($"[LevelUIManager] Checking initial rank: {currentRank}");
            ProcessTriggers(LevelTriggerType.XPLevel, currentRank);
        }
        
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
            Debug.LogWarning("[LevelUIManager] XPManager not found. XP Rank triggers will not work.");
            
        if (waveManager == null)
            Debug.LogWarning("[LevelUIManager] WaveManager not found. Wave triggers will not work.");
    }
    
    private void SubscribeToEvents()
    {
        XPManager.OnLeveledUp += OnRankUp;
        
        if (waveManager != null)
        {
            waveManager.OnWaveStarted.AddListener(OnWaveStarted);
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        XPManager.OnLeveledUp -= OnRankUp;
        
        if (waveManager != null)
        {
            waveManager.OnWaveStarted.RemoveListener(OnWaveStarted);
        }
    }
    
    private void OnRankUp(int newRank)
    {
        if (debugMode)
            Debug.Log($"[LevelUIManager] Player reached rank {newRank}");
            
        ProcessTriggers(LevelTriggerType.XPLevel, newRank);
        
        // Check for win condition first
        if (autoShowWinAnnouncement && winRank > 0 && newRank >= winRank && !hasWon)
        {
            hasWon = true;
            // Wait for attack selection UI to close before showing win announcement
            StartCoroutine(ShowWinAnnouncementAfterAttackUI());
            return; // Don't show level announcement if we just won
        }
        
        if (autoShowLevelAnnouncements && ranksPerLevel > 0)
        {
            int currentLevel = GetLevelFromRank(newRank);
            if (currentLevel > lastAnnouncedLevel && currentLevel > 0)
            {
                lastAnnouncedLevel = currentLevel;
                ShowLevelAnnouncement(currentLevel);
            }
        }
    }
    
    public int GetLevelFromRank(int rank)
    {
        return rank / ranksPerLevel;
    }
    
    public int GetCurrentLevel()
    {
        if (xpManager != null)
        {
            return GetLevelFromRank(xpManager.GetCurrentLevel());
        }
        return 0;
    }
    
    private void OnWaveStarted(int waveIndex)
    {
        int waveNumber = waveIndex + 1;
        
        if (debugMode)
            Debug.Log($"[LevelUIManager] Wave {waveNumber} started");
            
        ProcessTriggers(LevelTriggerType.WaveNumber, waveNumber);
    }
    
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
    
    private void ExecuteTrigger(LevelUITrigger trigger)
    {
        if (trigger == null) return;
        
        trigger.hasTriggered = true;
        
        if (debugMode)
            Debug.Log($"[LevelUIManager] Executing trigger: {trigger.actionType} at {trigger.triggerType} {trigger.triggerValue}");
        
        if (trigger.waitForAttackUI)
        {
            StartCoroutine(ExecuteTriggerAfterAttackUI(trigger));
        }
        else if (trigger.delay > 0)
        {
            StartCoroutine(ExecuteTriggerDelayed(trigger, trigger.delay));
        }
        else
        {
            ExecuteTriggerAction(trigger);
        }
    }
    
    private IEnumerator ExecuteTriggerAfterAttackUI(LevelUITrigger trigger)
    {
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            if (attackSelectionUI != null && attackSelectionUI.IsUIOpen()) break;
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (attackSelectionUI != null)
            while (attackSelectionUI.IsUIOpen()) yield return null;
        if (trigger.delay > 0) yield return new WaitForSeconds(trigger.delay);
        ExecuteTriggerAction(trigger);
    }
    
    private IEnumerator ExecuteTriggerDelayed(LevelUITrigger trigger, float delay)
    {
        yield return new WaitForSeconds(delay);
        ExecuteTriggerAction(trigger);
    }
    
    private void ExecuteTriggerAction(LevelUITrigger trigger)
    {
        // Check if this trigger should disable player damage
        if (trigger.disablePlayerDamage)
        {
            if (SnakeHealth.Instance != null)
            {
                SnakeHealth.Instance.SetInvincible(true);
                if (debugMode)
                    Debug.Log("[LevelUIManager] Player damage disabled by trigger");
            }
        }
        
        switch (trigger.actionType)
        {
            case LevelUIActionType.ShowSubtitle:
                ShowSubtitle(trigger.subtitleText, trigger.subtitleDuration);
                break;
            case LevelUIActionType.ShowGameObject:
                if (trigger.targetGameObject != null) trigger.targetGameObject.SetActive(true);
                break;
            case LevelUIActionType.HideGameObject:
                if (trigger.targetGameObject != null) trigger.targetGameObject.SetActive(false);
                break;
            case LevelUIActionType.ToggleGameObject:
                if (trigger.targetGameObject != null) trigger.targetGameObject.SetActive(!trigger.targetGameObject.activeSelf);
                break;
            case LevelUIActionType.PlayAnimation:
                if (trigger.targetAnimator != null && !string.IsNullOrEmpty(trigger.animationName))
                {
                    if (trigger.useAnimatorTrigger) trigger.targetAnimator.SetTrigger(trigger.animationName);
                    else trigger.targetAnimator.Play(trigger.animationName);
                }
                break;
            case LevelUIActionType.InvokeUnityEvent:
                trigger.onTriggered?.Invoke();
                break;
            case LevelUIActionType.FadeToBlack:
                if (ScreenFadeManager.Instance != null)
                    ScreenFadeManager.Instance.FadeToBlack(trigger.fadeDuration, () => {
                        if (trigger.showSubtitleAfterFade) ShowSubtitle(trigger.fadeSubtitleText, trigger.fadeSubtitleDuration);
                    });
                break;
            case LevelUIActionType.FadeFromBlack:
                if (ScreenFadeManager.Instance != null)
                    ScreenFadeManager.Instance.FadeFromBlack(trigger.fadeDuration, () => {
                        if (trigger.showSubtitleAfterFade) ShowSubtitle(trigger.fadeSubtitleText, trigger.fadeSubtitleDuration);
                    });
                break;
            case LevelUIActionType.FadeToBlackAndBack:
                if (ScreenFadeManager.Instance != null)
                    ScreenFadeManager.Instance.FadeToBlackAndBack(trigger.fadeDuration, trigger.fadeHoldDuration, trigger.fadeDuration, null, () => {
                        if (trigger.showSubtitleAfterFade) ShowSubtitle(trigger.fadeSubtitleText, trigger.fadeSubtitleDuration);
                    });
                break;
            case LevelUIActionType.LoadScene:
                if (!string.IsNullOrEmpty(trigger.sceneToLoad)) LoadSceneInternal(trigger.sceneToLoad, trigger.additiveSceneLoad);
                break;
            case LevelUIActionType.FadeAndLoadScene:
                if (!string.IsNullOrEmpty(trigger.sceneToLoad) && ScreenFadeManager.Instance != null)
                    ScreenFadeManager.Instance.FadeToBlack(trigger.fadeDuration, () => LoadSceneInternal(trigger.sceneToLoad, trigger.additiveSceneLoad));
                break;
            case LevelUIActionType.ShowTutorialPanel:
                if (TutorialPanelManager.Instance != null) TutorialPanelManager.Instance.ShowTutorial();
                break;
            case LevelUIActionType.ShowLevelAnnouncement:
                int level = trigger.announcementLevel > 0 ? trigger.announcementLevel : GetLevelFromRank(xpManager?.GetCurrentLevel() ?? 0);
                if (level > 0) ShowLevelAnnouncement(level);
                break;
        }
    }
    
    private void LoadSceneInternal(string sceneName, bool additive)
    {
        SceneManager.LoadScene(sceneName, additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
    }
    
    public void LoadSceneWithFade(string sceneName, float fadeDuration = 1f, bool additive = false)
    {
        if (ScreenFadeManager.Instance != null)
            ScreenFadeManager.Instance.FadeToBlack(fadeDuration, () => LoadSceneInternal(sceneName, additive));
        else LoadSceneInternal(sceneName, additive);
    }
    
    public void LoadSceneImmediate(string sceneName, bool additive = false) => LoadSceneInternal(sceneName, additive);
    
    public void ShowLevelAnnouncement(int level)
    {
        // Save checkpoint when a new level is announced
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.SaveCheckpoint(level);
            if (debugMode)
                Debug.Log($"[LevelUIManager] Saved checkpoint at level {level}");
        }
        
        ShowAnnouncement($"Level {level}");
    }
    
    /// <summary>
    /// Show the win announcement with the same animation style
    /// </summary>
    public void ShowWinAnnouncement()
    {
        ShowAnnouncement(winText);
    }
    
    /// <summary>
    /// Waits for the attack selection UI to close before showing the win announcement
    /// </summary>
    private IEnumerator ShowWinAnnouncementAfterAttackUI()
    {
        // First, wait a short time for the attack UI to potentially open
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            if (attackSelectionUI != null && attackSelectionUI.IsUIOpen()) break;
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
        }
        
        // Add a small delay after the UI closes for a smoother experience
        yield return new WaitForSeconds(0.5f);
        
        // Show the win announcement
        ShowAnnouncement(winText);
    }
    
    /// <summary>
    /// Show a custom announcement with the slide-in, wobble, slide-out animation
    /// </summary>
    public void ShowAnnouncement(string text)
    {
        if (levelAnnouncementText == null || levelAnnouncementRect == null) return;
        if (levelAnnouncementCoroutine != null) StopCoroutine(levelAnnouncementCoroutine);
        levelAnnouncementCoroutine = StartCoroutine(AnnouncementAnimation(text));
    }
    
    private IEnumerator AnnouncementAnimation(string text)
    {
        levelAnnouncementText.text = text;
        levelAnnouncementText.gameObject.SetActive(true);
        
        Vector2 centerPos = Vector2.zero;
        Vector2 startPos = new Vector2(-offScreenOffset, 0);
        Vector2 endPos = new Vector2(offScreenOffset, 0);
        
        float elapsed = 0f;
        while (elapsed < slideInDuration)
        {
            elapsed += Time.deltaTime;
            float t = slideEaseCurve.Evaluate(Mathf.Clamp01(elapsed / slideInDuration));
            levelAnnouncementRect.anchoredPosition = Vector2.Lerp(startPos, centerPos, t);
            yield return null;
        }
        levelAnnouncementRect.anchoredPosition = centerPos;
        
        elapsed = 0f;
        while (elapsed < hoverDuration)
        {
            elapsed += Time.deltaTime;
            float wobbleX = Mathf.Sin(elapsed * wobbleSpeed) * wobbleAmplitude * 0.3f;
            float wobbleY = Mathf.Sin(elapsed * wobbleSpeed * 1.3f) * wobbleAmplitude + Mathf.Sin(elapsed * wobbleSpeed * 0.7f) * wobbleAmplitude * 0.5f;
            float rotWobble = Mathf.Sin(elapsed * wobbleSpeed * 0.8f) * 2f;
            levelAnnouncementRect.localRotation = Quaternion.Euler(0, 0, rotWobble);
            levelAnnouncementRect.anchoredPosition = centerPos + new Vector2(wobbleX, wobbleY);
            yield return null;
        }
        levelAnnouncementRect.localRotation = Quaternion.identity;
        
        elapsed = 0f;
        Vector2 currentPos = levelAnnouncementRect.anchoredPosition;
        while (elapsed < slideOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = slideEaseCurve.Evaluate(Mathf.Clamp01(elapsed / slideOutDuration));
            levelAnnouncementRect.anchoredPosition = Vector2.Lerp(currentPos, endPos, t);
            yield return null;
        }
        
        levelAnnouncementText.gameObject.SetActive(false);
        levelAnnouncementRect.anchoredPosition = startPos;
        levelAnnouncementCoroutine = null;
    }
    
    private void ShowSubtitle(string text, float duration)
    {
        if (!string.IsNullOrEmpty(text) && SubtitleUI.Instance != null)
            SubtitleUI.Instance.ShowSubtitle(text, duration);
    }
    
    public void TriggerSubtitleForLevel(int level, string text, float duration = 3f) => ShowSubtitle(text, duration);
    
    public void ResetAllTriggers()
    {
        foreach (var trigger in triggers) trigger.Reset();
        lastAnnouncedLevel = 0;
        hasWon = false;
    }
    
    public int GetWinRank() => winRank;
    public void SetWinRank(int value) { winRank = Mathf.Max(0, value); }
    public bool HasWon() => hasWon;
    
    public void AddRuntimeTrigger(LevelUITrigger trigger) { if (trigger != null) triggers.Add(trigger); }
    public void RemoveRuntimeTrigger(LevelUITrigger trigger) { if (trigger != null) triggers.Remove(trigger); }
    public List<LevelUITrigger> GetTriggers() => triggers;
    public int GetRanksPerLevel() => ranksPerLevel;
    public void SetRanksPerLevel(int value) { ranksPerLevel = Mathf.Max(1, value); }
}