
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Defines what type of trigger condition to use
/// </summary>
public enum LevelTriggerType
{
    TimerLevel,    // Triggers based on timer level (every N minutes)
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
    [Tooltip("What type of trigger to use (Timer Level or Wave Number)")]
    public LevelTriggerType triggerType = LevelTriggerType.TimerLevel;
    
    [Tooltip("The timer level or wave number that triggers this action")]
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
    [Tooltip("Title text for the tutorial panel")]
    public string tutorialTitle = "Tutorial";
    
    [Tooltip("Instructions text for the tutorial panel")]
    [TextArea(3, 8)]
    public string tutorialInstructions = "";
    
    [Header("Level Announcement Settings (for ShowLevelAnnouncement action)")]
    [Tooltip("The level number to announce (if 0, will auto-calculate from timer)")]
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
/// Manages UI triggers based on timer level and wave progression.
/// Listens to WaveManager events and executes configured triggers.
/// 
/// TERMINOLOGY:
/// - Level: A milestone based on game time (every N minutes, configurable via minutesPerLevel)
///
/// SETUP INSTRUCTIONS:
/// 1. Create an empty GameObject in your scene and name it "LevelUIManager"
/// 2. Add this script to that GameObject
/// 3. Configure your triggers directly in the "Triggers" list
/// 4. Make sure you have SubtitleUI in your scene if using subtitle triggers
/// 5. For level announcements, assign a TextMeshProUGUI and its RectTransform
///
/// TRIGGER TYPES:
/// - Timer Level: Triggers when player reaches a specific timer-based level
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
    [Tooltip("List of UI triggers that fire at specific levels or waves")]
    [SerializeField] private List<LevelUITrigger> triggers = new List<LevelUITrigger>();
    
    [Header("References (Auto-found if not assigned)")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private AttackSelectionUI attackSelectionUI;
    [SerializeField] private AbilityCollector abilityCollector;
    
    [Header("Level Announcement UI")]
    [Tooltip("The TextMeshProUGUI component for displaying level announcements")]
    [SerializeField] private TextMeshProUGUI levelAnnouncementText;
    
    [Tooltip("The RectTransform of the level announcement (for animation)")]
    [SerializeField] private RectTransform levelAnnouncementRect;
    
    [Tooltip("The TextMeshProUGUI component for displaying the timer")]
    [SerializeField] private TextMeshProUGUI timerText;
    
    [Tooltip("The TextMeshProUGUI component for displaying the next level text (e.g., 'until level 2')")]
    [SerializeField] private TextMeshProUGUI nextLevelText;
    
    [Tooltip("The TextMeshProUGUI component for displaying the 10-second countdown")]
    [SerializeField] private TextMeshProUGUI countdownText;
    
    [Tooltip("The RectTransform of the countdown text (for scaling animation)")]
    [SerializeField] private RectTransform countdownRect;
    
    [Header("Environment")]
    [Tooltip("The Directional Light (Sun) to change color per level")]
    [SerializeField] private Light sunLight;
    
    [Tooltip("Array of skybox materials for each level")]
    [SerializeField] private Material[] levelSkyboxes;
    
    [Tooltip("Array of sun colors for each level")]
    [SerializeField] private Color[] levelSunColors;
    
    [Tooltip("Water material for each level")]
    [SerializeField] private Material[] levelWaterMaterials;
    
    [Tooltip("Global Volume to change profile on")]
    [SerializeField] private Volume globalVolume;
    
    [Tooltip("Array of Volume Profiles for each level")]
    [SerializeField] private VolumeProfile[] levelVolumeProfiles;
    
    [Tooltip("How many minutes equal one level (default: 3)")]
    [SerializeField] private float minutesPerLevel = 3f;
    
    [Tooltip("If true, automatically show level announcement when reaching level milestones")]
    [SerializeField] private bool autoShowLevelAnnouncements = true;
    
    [Header("Win Condition")]
    [Tooltip("The level at which the player wins the game (0 = disabled)")]
    [SerializeField] private int winLevel = 50;
    
    [Tooltip("The text to display when the player wins")]
    [SerializeField] private string winText = "You Won!";
    
    [Tooltip("If true, automatically show win announcement when reaching win level")]
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
    [SerializeField] private bool debugMode = true;
    
    // Track the last announced level to avoid duplicates
    private int lastAnnouncedLevel = 0;
    private Coroutine levelAnnouncementCoroutine;
    private bool hasWon = false;
    private float gameTime;
    
    private bool isPaused = false;
    private int lastCalculatedLevel = 0;
    private bool hasShownOneMinuteWarning = false;
    private int lastCountdownNumber = -1;
    private Coroutine countdownCoroutine;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            gameTime = 0f;
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
        
        hasShownOneMinuteWarning = false;
        lastCountdownNumber = -1;
        
        if (debugMode)
        {
            Debug.Log($"[LevelUIManager] Initialized with {triggers.Count} triggers");
        }
        
        // Check current state on start and trigger any matching triggers
        StartCoroutine(CheckInitialState());
        
        // Start timer-based level tracking
        StartCoroutine(TrackTimerBasedLevel());
    }
    
    private IEnumerator CheckInitialState()
    {
        yield return null;
        
        if (waveManager != null)
        {
            int currentWave = waveManager.GetCurrentWaveIndex() + 1;
            if (debugMode)
                Debug.Log($"[LevelUIManager] Checking initial wave: {currentWave}");
            ProcessTriggers(LevelTriggerType.WaveNumber, currentWave);
        }
        
        // Initialize timer-based level on start (start at level 1)
        if (minutesPerLevel > 0)
        {
            int initialLevel = 1;
            lastCalculatedLevel = initialLevel;
            lastAnnouncedLevel = initialLevel;
            
            // Trigger level 1 immediately
            ShowLevelAnnouncement(initialLevel);
            ProcessTriggers(LevelTriggerType.TimerLevel, initialLevel);
            
            if (debugMode)
                Debug.Log($"[LevelUIManager] Initial timer-based level: {initialLevel}");
        }
    }
    
    private IEnumerator TrackTimerBasedLevel()
    {
        Debug.Log("[LevelUIManager] TrackTimerBasedLevel started");
        
        // Track when we last checked UI state
        bool wasPaused = false;
        bool wasApplePaused = false;
        float uiPauseStartTime = 0f;
        float applePauseStartTime = 0f;
        
        while (minutesPerLevel > 0)
        {
            yield return new WaitForSeconds(0.1f);
            
            bool uiOpen = (attackSelectionUI != null && attackSelectionUI.IsUIOpen()) || 
                        (abilityCollector != null && abilityCollector.IsUIOpen());
            
            // Also pause when death screen is open
            var deathScreen = FindFirstObjectByType<DeathScreenManager>();
            if (deathScreen != null && deathScreen.IsDeathScreenActive())
                continue;
            
            if (uiOpen) continue;
            
            gameTime += 0.1f;
            
            int currentLevel = Mathf.FloorToInt(gameTime / (minutesPerLevel * 60f)) + 1;
            if (currentLevel < 1) currentLevel = 1;
            
            float timeInLevel = gameTime % (minutesPerLevel * 60f);
            float timeLeft = (minutesPerLevel * 60f) - timeInLevel;
            if (timeLeft < 0) timeLeft = 0;
            
            int minutes = Mathf.FloorToInt(timeLeft / 60f);
            int seconds = Mathf.FloorToInt(timeLeft % 60f);
            
            if (timerText != null)
                timerText.text = $"{minutes}:{seconds:D2}";
            
            if (nextLevelText != null)
            {
                int nextLevel = currentLevel + 1;
                if (winLevel > 0 && nextLevel >= winLevel)
                    nextLevelText.text = "end";
                else
                    nextLevelText.text = $"until level {nextLevel}";
            }
            
            // 1 minute warning
            if (timeLeft <= 60f && timeLeft > 0.5f && !hasShownOneMinuteWarning)
            {
                hasShownOneMinuteWarning = true;
                ShowAnnouncement("1 Minute Left");
            }
            
            // 10 second countdown
            int countdownNumber = Mathf.Clamp((int)timeLeft, 1, 10);
            bool shouldShowCountdown = (timeLeft <= 10f && timeLeft > 0f);
            
            if (shouldShowCountdown && countdownNumber != lastCountdownNumber)
            {
                lastCountdownNumber = countdownNumber;
                if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
                countdownCoroutine = StartCoroutine(ShowCountdownNumber(countdownNumber));
            }
            
            // Level up
            if (currentLevel > lastCalculatedLevel)
            {
                hasShownOneMinuteWarning = false;
                lastCountdownNumber = -1;
                lastCalculatedLevel = currentLevel;
                
                StartCoroutine(LevelTransition(currentLevel));
                ProcessTriggers(LevelTriggerType.TimerLevel, currentLevel);
                
                if (CheckpointManager.Instance != null)
                    CheckpointManager.Instance.SaveCheckpoint(currentLevel);
            }
            
            // Win condition
            if (winLevel > 0 && currentLevel >= winLevel && !hasWon)
            {
                hasWon = true;
                ShowWinAnnouncement();
                
                if (timerText != null)
                    timerText.gameObject.SetActive(false);
                if (nextLevelText != null)
                    nextLevelText.gameObject.SetActive(false);
            }
        }
    }
      
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    private void FindReferences()
    {
        if (waveManager == null)
            waveManager = FindFirstObjectByType<WaveManager>();
            
        if (attackSelectionUI == null)
            attackSelectionUI = FindFirstObjectByType<AttackSelectionUI>();
            
        if (abilityCollector == null)
            abilityCollector = FindFirstObjectByType<AbilityCollector>();
            
        if (waveManager == null)
            Debug.LogWarning("[LevelUIManager] WaveManager not found. Wave triggers will not work.");
    }
    
    private void SubscribeToEvents()
    {
        if (waveManager != null)
        {
            waveManager.OnWaveStarted.AddListener(OnWaveStarted);
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        if (waveManager != null)
        {
            waveManager.OnWaveStarted.RemoveListener(OnWaveStarted);
        }
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
        Debug.Log($"[LevelUIManager] ProcessTriggers called with type: {type}, value: {value}, triggers count: {triggers.Count}");
        foreach (var trigger in triggers)
        {
            if (trigger.ShouldTrigger(type, value))
            {
                Debug.Log($"[LevelUIManager] Trigger matched! type: {trigger.triggerType}, value: {trigger.triggerValue}, action: {trigger.actionType}");
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
                // Skip - announcement is handled by ShowLevelAnnouncement() in level change code
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
        Debug.Log($"[LevelUIManager] ShowLevelAnnouncement called with level: {level}");
        
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
    
    private IEnumerator ShowCountdownNumber(int number)
    {
        if (countdownText == null || countdownRect == null) yield break;
        
        countdownText.text = number.ToString();
        countdownText.fontSize = 10;
        countdownText.gameObject.SetActive(true);
        
        float duration = 0.8f;
        float elapsed = 0f;
        float baseFontSize = 10f;
        float maxFontSize = 200f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(baseFontSize, maxFontSize, t);
            
            countdownText.fontSize = scale;
            
            yield return null;
        }
        
        countdownText.gameObject.SetActive(false);
    }
    
    private IEnumerator LevelTransition(int level)
    {
        // Show level announcement
        if (level == 3)
            ShowAnnouncement("Final Level");
        else if (level == 4)
            ShowAnnouncement("You Win!");
        else
            ShowLevelAnnouncement(level);
        
        // Skip fade for win levels if no skyboxes configured
        if ((level == 3 || level == 4) && (levelSkyboxes == null || levelSkyboxes.Length < level))
        {
            yield break;
        }
        
        // Do environment transition
        yield return new WaitForSeconds(0.5f);
        
        // Fade to white
        if (ScreenFadeManager.Instance != null)
        {
            ScreenFadeManager.Instance.FadeToWhite(0.5f);
            yield return new WaitForSeconds(0.5f);
            
            // Change environment during white flash
            ChangeEnvironment(level);
            
            // Fade back
            ScreenFadeManager.Instance.FadeFromWhite(0.5f);
        }
        else
        {
            ChangeEnvironment(level);
        }
    }
    
    public void ChangeEnvironment(int level)
    {
        int index = level - 1; // 0-based indexing
        if (index < 0) index = 0;
        
        // Change skybox if available
        if (levelSkyboxes != null && levelSkyboxes.Length > index && levelSkyboxes[index] != null)
        {
            RenderSettings.skybox = levelSkyboxes[index];
        }
        
        // Change sun color if available
        if (sunLight != null && levelSunColors != null && levelSunColors.Length > index)
        {
            sunLight.color = levelSunColors[index];
        }
        
        // Change water material if available
        if (levelWaterMaterials != null && levelWaterMaterials.Length > index && levelWaterMaterials[index] != null)
        {
            FindAndSetWater(levelWaterMaterials[index]);
        }
        
        // Change Volume Profile if available
        if (globalVolume != null && levelVolumeProfiles != null && levelVolumeProfiles.Length > index && levelVolumeProfiles[index] != null)
        {
            globalVolume.profile = levelVolumeProfiles[index];
        }
        
        if (debugMode)
            Debug.Log($"[LevelUIManager] Environment changed for level {level}");
    }
    
    private void FindAndSetWater(Material waterMat)
    {
        if (waterMat == null) return;
        
        // Find all objects with Water in name and set their material
        var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        foreach (var rend in renderers)
        {
            if (rend != null && rend.gameObject.name.Contains("Water"))
            {
                rend.material = waterMat;
            }
        }
    }
    
    public int GetTimerBasedLevel()
    {
        if (minutesPerLevel > 0)
        {
            return Mathf.FloorToInt(gameTime / (minutesPerLevel * 60f));
        }
        return 0;
    }
    
    public void SetGameStartTime(float startTime)
    {
        gameTime = 0f;
    }
    
    public float GetGameStartTime() => 0f;
    
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
        gameTime = 0f;
    }
    
    public void ResetTimerForCurrentLevel()
    {
        gameTime = 0f;
        hasWon = false;
        lastCalculatedLevel = 1;
        hasShownOneMinuteWarning = false;
        lastCountdownNumber = -1;
        
        if (timerText != null)
            timerText.text = "3:00";
        
        if (nextLevelText != null)
            nextLevelText.text = "until level 2";
    }
    
    public int GetWinLevel() => winLevel;
    public void SetWinLevel(int value) { winLevel = Mathf.Max(0, value); }
    public bool HasWon() => hasWon;
    
    public void AddRuntimeTrigger(LevelUITrigger trigger) { if (trigger != null) triggers.Add(trigger); }
    public void RemoveRuntimeTrigger(LevelUITrigger trigger) { if (trigger != null) triggers.Remove(trigger); }
    public List<LevelUITrigger> GetTriggers() => triggers;
    public float GetMinutesPerLevel() => minutesPerLevel;
    public void SetMinutesPerLevel(float value) { minutesPerLevel = Mathf.Max(0.1f, value); }
}