
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public enum LevelTriggerType
{
    TimerLevel,
    WaveNumber,
    AttackRank
}

public enum LevelUIActionType
{
    ShowSubtitle,
    ShowGameObject,
    HideGameObject,
    ToggleGameObject,
    PlayAnimation,
    InvokeUnityEvent,
    FadeToBlack,
    FadeFromBlack,
    FadeToBlackAndBack,
    LoadScene,
    FadeAndLoadScene,
    ShowTutorialPanel,
    ShowLevelAnnouncement,
    ShowTutorialPrompt
}

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
    
    [Tooltip("Optional voiceline AudioClip to play alongside the subtitle. Leave empty for text only.")]
    public AudioClip subtitleVoiceline;
    
    [Tooltip("Volume for the subtitle voiceline (0-1).")]
    [Range(0f, 1f)]
    public float subtitleVoicelineVolume = 1f;
    
    [Tooltip("Pitch multiplier for the subtitle voiceline. 1 = normal, >1 = higher pitched. Uses the Voiceline Mixer Group on LevelUIManager for pitch-without-speed if one is assigned, otherwise falls back to AudioSource.pitch (which also speeds up playback).")]
    [Range(0.01f, 3f)]
    public float subtitleVoicelinePitch = 1f;
    
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
    
    [Tooltip("Optional voiceline AudioClip to play alongside the post-fade subtitle.")]
    public AudioClip fadeSubtitleVoiceline;
    
    [Tooltip("Volume for the post-fade subtitle voiceline (0-1).")]
    [Range(0f, 1f)]
    public float fadeSubtitleVoicelineVolume = 1f;
    
    [Tooltip("Pitch multiplier for the post-fade subtitle voiceline. 1 = normal, >1 = higher pitched. Uses the Voiceline Mixer Group on LevelUIManager for pitch-without-speed if one is assigned, otherwise falls back to AudioSource.pitch (which also speeds up playback).")]
    [Range(0.01f, 3f)]
    public float fadeSubtitleVoicelinePitch = 1f;
    
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
    
    [Header("Tutorial Prompt Settings (for ShowTutorialPrompt action)")]
    [Tooltip("The type of tutorial prompt (WASD for movement, Attack for mouse click)")]
    public TutorialPromptType promptType = TutorialPromptType.WASD;
    
    [Tooltip("Custom text for the tutorial prompt (optional - uses default if empty)")]
    [TextArea(1, 2)]
    public string customPromptText = "";
    
    [Header("Optional Delay")]
    [Tooltip("Delay in seconds before executing the action")]
    public float delay = 0f;
    
    [HideInInspector]
    public bool hasTriggered = false;
    
    public void Reset()
    {
        hasTriggered = false;
    }
    
    public bool ShouldTrigger(LevelTriggerType type, int value)
    {
        if (triggerType != type) return false;
        if (triggerOnce && hasTriggered) return false;
        return value == triggerValue;
    }
}

public enum TutorialPromptType
{
    WASD,
    Attack
}

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
    
    [Header("Voiceline Audio (for trigger voicelines)")]
    [Tooltip("Optional AudioMixerGroup to route trigger voicelines through. Assign a group with a Pitch Shifter effect whose 'Pitch' parameter is EXPOSED under the name below to get pitch changes WITHOUT speed changes. Leave empty to fall back to AudioSource.pitch (which also changes speed).")]
    [SerializeField] private AudioMixerGroup voicelineMixerGroup;
    
    [Tooltip("The exposed parameter name on the mixer that controls the Pitch Shifter's 'Pitch' (multiplier, 1 = unchanged). Only used when Voiceline Mixer Group is assigned.")]
    [SerializeField] private string voicelinePitchParameter = "VoicelinePitch";
    
    // Dedicated AudioSource for trigger voicelines, created on demand.
    // Kept separate from SubtitleUI's voiceline source so we can route this
    // one through the pitch-shifter mixer group without affecting other callers.
    private AudioSource triggerVoicelineSource;
    
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
    
    [Tooltip("Sound name (as registered in SoundManager) to play as background music for each level. Index 0 = Level 1, Index 1 = Level 2, etc. Leave an entry blank to keep the previous track playing.")]
    [SerializeField] private string[] levelMusicNames;
    
    [Tooltip("How long (seconds) to fade OUT the previous level's music. Longer = smoother.")]
    [SerializeField] private float levelMusicFadeOutDuration = 2.0f;
    
    [Tooltip("How long (seconds) to fade IN the new level's music. Longer = smoother.")]
    [SerializeField] private float levelMusicFadeInDuration = 2.0f;
    
    [Tooltip("Optional sound name (as registered in SoundManager) to play as a one-shot transition SFX between level tracks. Leave blank to use the Transition Clip below instead.")]
    [SerializeField] private string levelMusicTransitionSfx = "";
    
    [Tooltip("Drag an AudioClip here to play as the one-shot transition SFX between level tracks. This works without needing to register the sound in SoundManager. Takes priority over the name field above if set.")]
    [SerializeField] private AudioClip levelMusicTransitionClip;
    
    [Tooltip("Volume for the transition SFX clip (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float levelMusicTransitionVolume = 1f;
    
    [Tooltip("Delay (seconds) after the fade-out begins before starting the new track's fade-in. Use this to let the transition SFX breathe. 0 = fully overlap (true crossfade).")]
    [SerializeField] private float levelMusicTransitionGap = 0.5f;
    
    // Tracks the currently-playing level music name so we can stop it on switch
    private string currentLevelMusicName = null;
    // Running crossfade coroutine - cancelled if a new switch happens mid-fade
    private Coroutine levelMusicCrossfadeCoroutine = null;
    // Dedicated AudioSource for the transition SFX clip (created on demand)
    private AudioSource levelMusicTransitionSource = null;
    
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
    
    [Tooltip("Drag an AudioClip here to play as a one-shot sound effect when the 'You Win' text appears. Works without needing to register the sound in SoundManager.")]
    [SerializeField] private AudioClip winSound;
    
    [Tooltip("Volume for the win sound. Supports values above 1 to amplify the clip (may clip/distort at very high values).")]
    [Range(0f, 5f)]
    [SerializeField] private float winSoundVolume = 1f;
    
    [Tooltip("Volume (0-1) to duck the currently-playing level music to while the win sound plays. 0.1 = 10%. Set to 1 to disable ducking.")]
    [Range(0f, 1f)]
    [SerializeField] private float winSoundMusicDuckVolume = 0.1f;
    
    [Tooltip("Drag an AudioClip here to play as a one-shot sound effect each time a countdown number (10..1) appears. The pitch rises as the number counts down: 10 plays at normal pitch, 1 plays at the highest pitch.")]
    [SerializeField] private AudioClip countdownSound;
    
    [Tooltip("Volume for the countdown sound. Supports values above 1 to amplify the clip (may clip/distort at very high values).")]
    [Range(0f, 5f)]
    [SerializeField] private float countdownSoundVolume = 1f;
    
    [Tooltip("Pitch to play the countdown sound at when the number is 10 (the start of the countdown). Higher pitch = faster playback.")]
    [Range(0.01f, 10f)]
    [SerializeField] private float countdownSoundStartPitch = 1f;
    
    [Tooltip("Pitch to play the countdown sound at when the number is 1 (the end of the countdown). Higher than start pitch to make it rise. Higher pitch = faster playback.")]
    [Range(0.01f, 10f)]
    [SerializeField] private float countdownSoundEndPitch = 2f;
    
    [Tooltip("Global speed multiplier applied on top of the pitch. 1 = normal (pitch controls speed), 2 = twice as fast, 0.5 = half speed. This scales pitch so you can tune playback speed independently.")]
    [Range(0.01f, 10f)]
    [SerializeField] private float countdownSoundSpeed = 1f;
    
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
    
    [Header("Tutorial Prompts")]
    [Tooltip("Text to show for attack prompt after rank 1")]
    [SerializeField] private string attackPromptText = "Click to use attack";
    
    [Tooltip("The TextMeshProUGUI component for the tutorial prompt")]
    [SerializeField] private TextMeshProUGUI tutorialPromptText;
    
    [Tooltip("The RectTransform of the tutorial prompt for animation")]
    [SerializeField] private RectTransform tutorialPromptRect;
    
    [Tooltip("Font size for the tutorial prompt")]
    [SerializeField] private float tutorialPromptFontSize = 36f;
    
    [Tooltip("If true, shows attack prompt after player reaches rank 1 (first attack upgrade)")]
    [SerializeField] private bool showAttackPromptOnRank1 = true;
    
    [Tooltip("Animation curve for prompt fade")]
    [SerializeField] private AnimationCurve promptFadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    private int lastAnnouncedLevel = 0;
    private Coroutine levelAnnouncementCoroutine;
    private bool hasWon = false;
    private float gameTime;
    // Cached references to avoid FindFirstObjectByType being called every 0.1s inside the timer coroutine.
    private DeathScreenManager _cachedDeathScreen;
    // Cache last-rendered timer values to avoid rebuilding strings/canvas every 0.1s when unchanged.
    private int _lastTimerMinutes = int.MinValue;
    private int _lastTimerSeconds = int.MinValue;
    private int _lastNextLevelShown = int.MinValue;
    
    private bool wasdPromptActive = false;
    private bool attackPromptActive = false;
    private bool wasdInputDismissed = false;
    private bool mouseInputDismissed = false;
    private Coroutine tutorialPromptCoroutine;
    
    private bool isPaused = false;
    private int lastCalculatedLevel = 0;
    private bool hasShownOneMinuteWarning = false;
    private int lastCountdownNumber = -1;
    private Coroutine countdownCoroutine;
    
    private void Awake()
    {
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
        
        foreach (var trigger in triggers)
        {
            trigger.Reset();
        }
        
        hasShownOneMinuteWarning = false;
        lastCountdownNumber = -1;
        
        if (debugMode)
        {
        }
        
        StartCoroutine(CheckInitialState());
        
        StartCoroutine(TrackTimerBasedLevel());
        
        InitializeTutorialPrompts();
    }
    
    private void Update()
    {
        CheckTutorialInputForDismiss();
    }
    
    private void InitializeTutorialPrompts()
    {
        if (tutorialPromptText != null)
        {
            tutorialPromptText.fontSize = tutorialPromptFontSize;
            tutorialPromptText.gameObject.SetActive(false);
        }
        
        wasdPromptActive = false;
        attackPromptActive = false;
        wasdInputDismissed = false;
        mouseInputDismissed = false;
        hasAttackUIClosedOnce = false;
        
        AttackManager.OnAttacksChanged += OnAttacksChangedForPrompt;
        AttackManager.OnFuelUIVisible += OnFuelUIVisible;
        
        StartCoroutine(CheckForAttackPromptAfterUI());
    }
    
    private bool hasAttackUIClosedOnce = false;
    
    private IEnumerator CheckForAttackPromptAfterUI()
    {
        yield return new WaitForSeconds(2f);
        
        AttackSelectionUI attackSelectionUI = FindFirstObjectByType<AttackSelectionUI>();
        if (attackSelectionUI != null)
        {
            int waitCount = 0;
            while (attackSelectionUI.IsUIOpen() && waitCount < 300)
            {
                yield return null;
                waitCount++;
            }
        }
        else
        {
        }
        
        hasAttackUIClosedOnce = true;
        
        yield return new WaitForSeconds(1f);
        
        CheckForExistingAttack();
    }
    
    private void OnAttacksChangedForPrompt()
    {
        if (!hasAttackUIClosedOnce)
        {
            return;
        }
        
        AttackManager attackManager = FindFirstObjectByType<AttackManager>();
        if (attackManager != null && attackManager.GetCurrentAttack() != null)
        {
            int attackLevel = attackManager.GetCurrentAttack().GetCurrentLevel();
            
            ProcessTriggers(LevelTriggerType.AttackRank, attackLevel);
            
            if (showAttackPromptOnRank1 && !attackPromptActive && !mouseInputDismissed && attackLevel >= 2)
            {
                attackPromptActive = true;
                if (tutorialPromptText != null)
                {
                    string promptText = string.IsNullOrEmpty(attackPromptText) ? "Click to use attack" : attackPromptText;
                    tutorialPromptText.text = promptText;
                    tutorialPromptText.fontSize = tutorialPromptFontSize;
                    tutorialPromptText.gameObject.SetActive(true);
                    
                    var canvasGroup = tutorialPromptText.GetComponent<CanvasGroup>();
                    if (canvasGroup == null) canvasGroup = tutorialPromptText.gameObject.AddComponent<CanvasGroup>();
                    canvasGroup.alpha = 1f;
                }
            }
        }
    }
    
    private void CheckForExistingAttack()
    {
        OnAttacksChangedForPrompt();
    }
    
    private void OnFuelUIVisible()
    {
        if (!showAttackPromptOnRank1 || attackPromptActive || mouseInputDismissed) return;
        
        StartCoroutine(ShowAttackPromptDelayed());
    }
    
    private IEnumerator ShowAttackPromptDelayed()
    {
        yield return new WaitForSeconds(1f);
        
        if (!showAttackPromptOnRank1 || attackPromptActive || mouseInputDismissed) yield break;
        
        attackPromptActive = true;
        if (tutorialPromptText != null)
        {
            string promptText = string.IsNullOrEmpty(attackPromptText) ? "Click to use attack" : attackPromptText;
            tutorialPromptText.text = promptText;
            tutorialPromptText.fontSize = tutorialPromptFontSize;
            tutorialPromptText.gameObject.SetActive(true);
            
            var canvasGroup = tutorialPromptText.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = tutorialPromptText.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
        }
    }
    
    private void CheckTutorialInputForDismiss()
    {
        if (wasdPromptActive && !wasdInputDismissed && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D)))
        {
            wasdInputDismissed = true;
            wasdPromptActive = false;
            UpdateTutorialPromptVisibility();
        }
        
        if (attackPromptActive && !mouseInputDismissed && Input.GetMouseButtonDown(0))
        {
            mouseInputDismissed = true;
            attackPromptActive = false;
            UpdateTutorialPromptVisibility();
        }
    }
    
    private void UpdateTutorialPromptVisibility()
    {
        if (!wasdPromptActive && !attackPromptActive && tutorialPromptText != null)
        {
            tutorialPromptText.gameObject.SetActive(false);
        }
    }
    
    private IEnumerator CheckInitialState()
    {
        yield return null;
        
        if (waveManager != null)
        {
            int currentWave = waveManager.GetCurrentWaveIndex() + 1;
            ProcessTriggers(LevelTriggerType.WaveNumber, currentWave);
        }
        
        if (minutesPerLevel > 0)
        {
            int initialLevel = 1;
            lastCalculatedLevel = initialLevel;
            lastAnnouncedLevel = initialLevel;
            
            ShowLevelAnnouncement(initialLevel);
            ProcessTriggers(LevelTriggerType.TimerLevel, initialLevel);
            PlayLevelMusic(initialLevel);
        }
    }
    
    private IEnumerator TrackTimerBasedLevel()
    {
        bool wasPaused = false;
        bool wasApplePaused = false;
        float uiPauseStartTime = 0f;
        float applePauseStartTime = 0f;
        
        while (minutesPerLevel > 0)
        {
            yield return new WaitForSeconds(0.1f);
            
            bool uiOpen = (attackSelectionUI != null && attackSelectionUI.IsUIOpen()) || 
                        (abilityCollector != null && abilityCollector.IsUIOpen());
            
            var deathScreen = _cachedDeathScreen;
            if (deathScreen == null)
            {
                deathScreen = FindFirstObjectByType<DeathScreenManager>();
                _cachedDeathScreen = deathScreen;
            }
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
            
            // Only rebuild timer string when displayed seconds/minutes actually change.
            if (timerText != null && (minutes != _lastTimerMinutes || seconds != _lastTimerSeconds))
            {
                timerText.text = $"{minutes}:{seconds:D2}";
                _lastTimerMinutes = minutes;
                _lastTimerSeconds = seconds;
            }
            
            if (nextLevelText != null)
            {
                int nextLevel = currentLevel + 1;
                int shownKey = (winLevel > 0 && nextLevel >= winLevel) ? -1 : nextLevel;
                if (shownKey != _lastNextLevelShown)
                {
                    if (shownKey == -1)
                        nextLevelText.text = "end";
                    else
                        nextLevelText.text = $"until level {nextLevel}";
                    _lastNextLevelShown = shownKey;
                }
            }
            
            if (timeLeft <= 60f && timeLeft > 0.5f && !hasShownOneMinuteWarning)
            {
                hasShownOneMinuteWarning = true;
                ShowAnnouncement("1 Minute Left");
            }
            
            int countdownNumber = Mathf.Clamp((int)timeLeft, 1, 10);
            bool shouldShowCountdown = (timeLeft <= 10f && timeLeft > 0f);
            
            if (shouldShowCountdown && countdownNumber != lastCountdownNumber)
            {
                lastCountdownNumber = countdownNumber;
                if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
                countdownCoroutine = StartCoroutine(ShowCountdownNumber(countdownNumber));
            }
            
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
        AttackManager.OnAttacksChanged -= OnAttacksChangedForPrompt;
        AttackManager.OnFuelUIVisible -= OnFuelUIVisible;
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
        if (trigger.disablePlayerDamage)
        {
            if (SnakeHealth.Instance != null)
            {
                SnakeHealth.Instance.SetInvincible(true);
            }
        }
        
        switch (trigger.actionType)
        {
            case LevelUIActionType.ShowSubtitle:
                ShowSubtitle(trigger.subtitleText, trigger.subtitleDuration, trigger.subtitleVoiceline, trigger.subtitleVoicelineVolume, trigger.subtitleVoicelinePitch);
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
                        if (trigger.showSubtitleAfterFade) ShowSubtitle(trigger.fadeSubtitleText, trigger.fadeSubtitleDuration, trigger.fadeSubtitleVoiceline, trigger.fadeSubtitleVoicelineVolume, trigger.fadeSubtitleVoicelinePitch);
                    });
                break;
            case LevelUIActionType.FadeFromBlack:
                if (ScreenFadeManager.Instance != null)
                    ScreenFadeManager.Instance.FadeFromBlack(trigger.fadeDuration, () => {
                        if (trigger.showSubtitleAfterFade) ShowSubtitle(trigger.fadeSubtitleText, trigger.fadeSubtitleDuration, trigger.fadeSubtitleVoiceline, trigger.fadeSubtitleVoicelineVolume, trigger.fadeSubtitleVoicelinePitch);
                    });
                break;
            case LevelUIActionType.FadeToBlackAndBack:
                if (ScreenFadeManager.Instance != null)
                    ScreenFadeManager.Instance.FadeToBlackAndBack(trigger.fadeDuration, trigger.fadeHoldDuration, trigger.fadeDuration, null, () => {
                        if (trigger.showSubtitleAfterFade) ShowSubtitle(trigger.fadeSubtitleText, trigger.fadeSubtitleDuration, trigger.fadeSubtitleVoiceline, trigger.fadeSubtitleVoicelineVolume, trigger.fadeSubtitleVoicelinePitch);
                    });
                break;
            case LevelUIActionType.LoadScene:
                if (!string.IsNullOrEmpty(trigger.sceneToLoad)) LoadSceneInternal(trigger.sceneToLoad, trigger.additiveSceneLoad);
                break;
            case LevelUIActionType.FadeAndLoadScene:
                if (!string.IsNullOrEmpty(trigger.sceneToLoad) && ScreenFadeManager.Instance != null)
                {
                    // Play the level-transition SFX at the instant the fade begins,
                    // so scene transitions get the same audio hit as level changes.
                    PlayLevelTransitionSfx();
                    ScreenFadeManager.Instance.FadeToBlack(trigger.fadeDuration, () => LoadSceneInternal(trigger.sceneToLoad, trigger.additiveSceneLoad));
                }
                break;
            case LevelUIActionType.ShowTutorialPanel:
                if (TutorialPanelManager.Instance != null) TutorialPanelManager.Instance.ShowTutorial();
                break;
            case LevelUIActionType.ShowLevelAnnouncement:
                break;
            case LevelUIActionType.ShowTutorialPrompt:
                ShowTutorialPromptByTrigger(trigger.promptType, trigger.customPromptText);
                break;
        }
    }
    
    private void ShowTutorialPromptByTrigger(TutorialPromptType promptType, string customText)
    {
        if (promptType == TutorialPromptType.Attack && !hasAttackUIClosedOnce) return;
        
        string promptText = "";
        
        switch (promptType)
        {
            case TutorialPromptType.WASD:
                promptText = string.IsNullOrEmpty(customText) ? "WASD to move" : customText;
                wasdPromptActive = true;
                break;
            case TutorialPromptType.Attack:
                promptText = string.IsNullOrEmpty(customText) ? "Click to use attack" : customText;
                attackPromptActive = true;
                break;
        }
        
        if (tutorialPromptText != null && !string.IsNullOrEmpty(promptText))
        {
            tutorialPromptText.text = promptText;
            tutorialPromptText.fontSize = tutorialPromptFontSize;
            tutorialPromptText.gameObject.SetActive(true);
            
            var canvasGroup = tutorialPromptText.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = tutorialPromptText.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
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
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.SaveCheckpoint(level);
        }
        
        ShowAnnouncement($"Level {level}");
    }
    
    public void ShowWinAnnouncement()
    {
        ShowAnnouncement(winText);
        PlayWinSound();
    }
    
    /// <summary>
    /// Plays the directly-assigned <see cref="winSound"/> as a one-shot.
    /// Supports volumes above 1 by layering multiple AudioSources, so the
    /// SFX plays reliably even if this GameObject is disabled during a
    /// scene transition. Does NOT depend on SoundManager.
    /// </summary>
    private void PlayWinSound()
    {
        if (winSound == null) return;
        
        // Duck the currently-playing level music so the win SFX is clearly audible
        if (!string.IsNullOrEmpty(currentLevelMusicName))
        {
            SoundManager.SetVolume(currentLevelMusicName, gameObject, Mathf.Clamp01(winSoundMusicDuckVolume));
        }
        
        // Unity's AudioSource.volume is clamped to [0,1]. To get louder-than-1,
        // spawn multiple stacked AudioSources (each at full volume) to sum.
        float totalVolume = Mathf.Max(0f, winSoundVolume);
        int fullLayers = Mathf.FloorToInt(totalVolume);
        float remainder = totalVolume - fullLayers;
        int layerCount = fullLayers + (remainder > 0.001f ? 1 : 0);
        if (layerCount <= 0) return;
        
        Vector3 pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        float lifetime = winSound.length + 0.1f;
        
        for (int i = 0; i < layerCount; i++)
        {
            GameObject go = new GameObject($"WinSFX_L{i}");
            go.transform.position = pos;
            
            AudioSource src = go.AddComponent<AudioSource>();
            src.clip = winSound;
            bool isLast = (i == layerCount - 1);
            src.volume = (isLast && remainder > 0.001f) ? Mathf.Clamp01(remainder) : 1f;
            src.spatialBlend = 0f; // 2D
            src.Play();
            
            Destroy(go, lifetime);
        }
    }
    
    /// <summary>
    /// Plays the countdown SFX with a pitch that increases as the countdown
    /// number decreases. At number == 10 the pitch equals
    /// <see cref="countdownSoundStartPitch"/>, at number == 1 it equals
    /// <see cref="countdownSoundEndPitch"/>. The final pitch is multiplied
    /// by <see cref="countdownSoundSpeed"/> to tune playback speed.
    /// Volumes above 1 are achieved by layering multiple AudioSources.
    /// </summary>
    private void PlayCountdownSound(int number)
    {
        if (countdownSound == null) return;
        
        // Map number 10 -> 0 (start pitch), number 1 -> 1 (end pitch).
        float clamped = Mathf.Clamp(number, 1, 10);
        float t = (10f - clamped) / 9f;
        float pitch = Mathf.Lerp(countdownSoundStartPitch, countdownSoundEndPitch, t) * Mathf.Max(0.01f, countdownSoundSpeed);
        if (Mathf.Abs(pitch) < 0.01f) pitch = 0.01f * Mathf.Sign(pitch == 0f ? 1f : pitch);
        
        // Unity's AudioSource.volume is clamped to [0,1]. To get louder-than-1,
        // spawn multiple stacked AudioSources (each at full volume) to sum.
        float totalVolume = Mathf.Max(0f, countdownSoundVolume);
        int fullLayers = Mathf.FloorToInt(totalVolume);
        float remainder = totalVolume - fullLayers;
        int layerCount = fullLayers + (remainder > 0.001f ? 1 : 0);
        if (layerCount <= 0) return;
        
        Vector3 pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        float lifetime = countdownSound.length / Mathf.Max(0.01f, Mathf.Abs(pitch)) + 0.1f;
        
        for (int i = 0; i < layerCount; i++)
        {
            GameObject go = new GameObject($"CountdownSFX_{number}_L{i}");
            go.transform.position = pos;
            
            AudioSource src = go.AddComponent<AudioSource>();
            src.clip = countdownSound;
            // Last layer gets the remainder volume (if any); others play at full.
            bool isLast = (i == layerCount - 1);
            src.volume = (isLast && remainder > 0.001f) ? Mathf.Clamp01(remainder) : 1f;
            src.pitch = pitch;
            src.spatialBlend = 0f; // 2D
            src.Play();
            
            Destroy(go, lifetime);
        }
    }
    
    private IEnumerator ShowCountdownNumber(int number)
    {
        if (countdownText == null || countdownRect == null) yield break;
        
        PlayCountdownSound(number);
        
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
        if (level == 3)
            ShowAnnouncement("Final Level");
        else if (level == 4)
        {
            ShowAnnouncement("You Win!");
            PlayWinSound();
        }
        else
            ShowLevelAnnouncement(level);
        
        if ((level == 3 || level == 4) && (levelSkyboxes == null || levelSkyboxes.Length < level))
        {
            yield break;
        }
        
        yield return new WaitForSeconds(0.5f);
        
        if (ScreenFadeManager.Instance != null)
        {
            // Kick off the transition SFX at the same instant the fade-to-white starts
            // so the "hit" of the sound lines up with the white flash.
            PlayLevelTransitionSfx();
            
            ScreenFadeManager.Instance.FadeToWhite(0.5f);
            yield return new WaitForSeconds(0.5f);
            
            ChangeEnvironment(level);
            
            ScreenFadeManager.Instance.FadeFromWhite(0.5f);
        }
        else
        {
            PlayLevelTransitionSfx();
            ChangeEnvironment(level);
        }
    }
    
    /// <summary>
    /// Plays the one-shot level-transition SFX. Prefers the directly-assigned
    /// AudioClip, falls back to the named SoundManager sound. Safe to call
    /// standalone (e.g. from LevelTransition) or indirectly via CrossfadeLevelMusic.
    /// </summary>
    private void PlayLevelTransitionSfx()
    {
        if (levelMusicTransitionClip != null)
        {
            PlayTransitionClip();
        }
        else if (!string.IsNullOrEmpty(levelMusicTransitionSfx))
        {
            SoundManager.Play(levelMusicTransitionSfx, gameObject);
        }
    }
    
    public void ChangeEnvironment(int level)
    {
        int index = level - 1;
        if (index < 0) index = 0;
        
        if (levelSkyboxes != null && levelSkyboxes.Length > index && levelSkyboxes[index] != null)
        {
            RenderSettings.skybox = levelSkyboxes[index];
        }
        
        if (sunLight != null && levelSunColors != null && levelSunColors.Length > index)
        {
            sunLight.color = levelSunColors[index];
        }
        
        if (levelWaterMaterials != null && levelWaterMaterials.Length > index && levelWaterMaterials[index] != null)
        {
            FindAndSetWater(levelWaterMaterials[index]);
        }
        
        if (globalVolume != null && levelVolumeProfiles != null && levelVolumeProfiles.Length > index && levelVolumeProfiles[index] != null)
        {
            globalVolume.profile = levelVolumeProfiles[index];
        }
        
        PlayLevelMusic(level);
    }
    
    /// <summary>
    /// Switches background music to the track configured for this level.
    /// Crossfades out the previous track and fades in the new one over the
    /// configured durations. Optionally plays a one-shot transition SFX
    /// during the switch. Safe to call rapidly - an in-progress crossfade
    /// is cancelled and replaced.
    /// </summary>
    public void PlayLevelMusic(int level)
    {
        if (levelMusicNames == null || levelMusicNames.Length == 0) return;
        
        int index = level - 1;
        if (index < 0) index = 0;
        if (index >= levelMusicNames.Length) return;
        
        string newMusic = levelMusicNames[index];
        if (string.IsNullOrEmpty(newMusic)) return;
        
        // Already playing this track - nothing to do
        if (newMusic == currentLevelMusicName) return;
        
        // Cancel any in-flight crossfade so we don't stack coroutines
        if (levelMusicCrossfadeCoroutine != null)
        {
            StopCoroutine(levelMusicCrossfadeCoroutine);
            levelMusicCrossfadeCoroutine = null;
        }
        
        string previousMusic = currentLevelMusicName;
        currentLevelMusicName = newMusic;
        levelMusicCrossfadeCoroutine = StartCoroutine(CrossfadeLevelMusic(previousMusic, newMusic, level));
    }
    
    private IEnumerator CrossfadeLevelMusic(string previousMusic, string newMusic, int level)
    {
        float fadeOut = Mathf.Max(0f, levelMusicFadeOutDuration);
        float fadeIn = Mathf.Max(0f, levelMusicFadeInDuration);
        float gap = Mathf.Max(0f, levelMusicTransitionGap);
        
        // 1. Start fading out the previous track (if any) + legacy GameMusic
        if (!string.IsNullOrEmpty(previousMusic))
        {
            if (fadeOut > 0f)
                SoundManager.FadeOut(previousMusic, gameObject, fadeOut);
            else
                SoundManager.Stop(previousMusic, gameObject);
        }
        if (previousMusic != "GameMusic" && newMusic != "GameMusic")
        {
            // In case anything else started the legacy single track
            if (fadeOut > 0f)
                SoundManager.FadeOut("GameMusic", gameObject, fadeOut);
            else
                SoundManager.Stop("GameMusic", gameObject);
        }
        
        // NOTE: The transition SFX is no longer triggered here. It's played by
        // LevelTransition() at the instant the fade-to-white starts so the "hit"
        // of the SFX lands on the white flash. The crossfade coroutine is still
        // responsible for the music fade in/out.
        
        // 3. Wait the configured gap before bringing in the new track
        //    (0 = true crossfade, >0 = brief silence / SFX-only window)
        if (gap > 0f)
            yield return new WaitForSecondsRealtime(gap);
        
        // 4. Fade in the new track
        if (fadeIn > 0f)
            SoundManager.PlayWithFadeIn(newMusic, gameObject, fadeIn);
        else
            SoundManager.Play(newMusic, gameObject);
        
        levelMusicCrossfadeCoroutine = null;
    }
    
    /// <summary>
    /// Plays the directly-assigned <see cref="levelMusicTransitionClip"/> as a
    /// one-shot through a dedicated AudioSource on this GameObject. The source
    /// is created on demand and reused. Does NOT depend on SoundManager.
    /// </summary>
    private void PlayTransitionClip()
    {
        if (levelMusicTransitionClip == null) return;
        
        if (levelMusicTransitionSource == null)
        {
            levelMusicTransitionSource = gameObject.AddComponent<AudioSource>();
            levelMusicTransitionSource.playOnAwake = false;
            levelMusicTransitionSource.loop = false;
            levelMusicTransitionSource.spatialBlend = 0f; // 2D
        }
        
        // PlayOneShot lets overlapping transitions stack without cutting each other
        levelMusicTransitionSource.PlayOneShot(levelMusicTransitionClip, Mathf.Clamp01(levelMusicTransitionVolume));
    }
    
    /// <summary>
    /// Returns the sound name of the level music currently playing (may be null/empty
    /// if no level track has started yet). Useful for ducking volume when menus open.
    /// </summary>
    public string GetCurrentLevelMusicName() => currentLevelMusicName;
    
    /// <summary>
    /// Sets the volume (0-1) of whatever level music is currently playing.
    /// Used by menus (pause, attack select, ability collector) to duck music.
    /// </summary>
    public void SetLevelMusicVolume(float volume)
    {
        if (string.IsNullOrEmpty(currentLevelMusicName)) return;
        SoundManager.SetVolume(currentLevelMusicName, gameObject, volume);
    }
    
    // Cache the water renderers so repeated level changes don't re-scan every Renderer
    // in the scene (which was O(N) with string.Contains per renderer).
    private Renderer[] _cachedWaterRenderers;

    private void FindAndSetWater(Material waterMat)
    {
        if (waterMat == null) return;

        if (_cachedWaterRenderers == null)
        {
            var all = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var found = new System.Collections.Generic.List<Renderer>();
            for (int i = 0; i < all.Length; i++)
            {
                var r = all[i];
                if (r != null && r.gameObject.name.Contains("Water"))
                {
                    found.Add(r);
                }
            }
            _cachedWaterRenderers = found.ToArray();
        }

        for (int i = 0; i < _cachedWaterRenderers.Length; i++)
        {
            var rend = _cachedWaterRenderers[i];
            if (rend != null)
            {
                rend.sharedMaterial = waterMat;
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
    
    private IEnumerator ShowWinAnnAnnouncementAfterAttackUI()
    {
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            if (attackSelectionUI != null && attackSelectionUI.IsUIOpen()) break;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (attackSelectionUI != null)
        {
            while (attackSelectionUI.IsUIOpen())
            {
                yield return null;
            }
        }
        
        yield return new WaitForSeconds(0.5f);
        
        ShowAnnouncement(winText);
        PlayWinSound();
    }
    
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
        ShowSubtitle(text, duration, null, -1f, 1f);
    }
    
    private void ShowSubtitle(string text, float duration, AudioClip voiceline, float voicelineVolume)
    {
        ShowSubtitle(text, duration, voiceline, voicelineVolume, 1f);
    }
    
    private void ShowSubtitle(string text, float duration, AudioClip voiceline, float voicelineVolume, float voicelinePitch)
    {
        // Show the subtitle text (no voiceline passed - we play voicelines ourselves
        // so pitch can be controlled by LevelUIManager's mixer group).
        if (SubtitleUI.Instance != null && !string.IsNullOrEmpty(text))
        {
            SubtitleUI.Instance.ShowSubtitle(text, duration);
        }
        
        // Play the voiceline through our own pitched AudioSource (if one is configured)
        // so higher pitch doesn't also speed up playback.
        if (voiceline != null)
        {
            PlayTriggerVoiceline(voiceline, voicelineVolume, voicelinePitch);
        }
    }
    
    /// <summary>
    /// Plays a trigger voiceline through a dedicated AudioSource owned by this
    /// LevelUIManager. If <see cref="voicelineMixerGroup"/> is assigned (and has
    /// a Pitch Shifter effect whose 'Pitch' parameter is exposed under
    /// <see cref="voicelinePitchParameter"/>), the pitch is applied via that mixer
    /// parameter so playback speed is NOT affected. If no mixer group is assigned,
    /// the code falls back to setting <see cref="AudioSource.pitch"/>, which also
    /// speeds up/slows down playback.
    /// </summary>
    private void PlayTriggerVoiceline(AudioClip clip, float volume, float pitch)
    {
        if (clip == null) return;
        
        if (triggerVoicelineSource == null)
        {
            triggerVoicelineSource = gameObject.AddComponent<AudioSource>();
            triggerVoicelineSource.playOnAwake = false;
            triggerVoicelineSource.loop = false;
            triggerVoicelineSource.spatialBlend = 0f; // 2D
            triggerVoicelineSource.volume = 1f;
        }
        
        // Route through the pitch-shifter mixer group if one is configured.
        if (voicelineMixerGroup != null && triggerVoicelineSource.outputAudioMixerGroup != voicelineMixerGroup)
        {
            triggerVoicelineSource.outputAudioMixerGroup = voicelineMixerGroup;
        }
        
        float clampedPitch = Mathf.Clamp(pitch, 0.01f, 3f);
        float finalVolume = volume < 0f ? 1f : Mathf.Clamp01(volume);
        
        if (voicelineMixerGroup != null && !string.IsNullOrEmpty(voicelinePitchParameter))
        {
            // Pitch-shift via mixer: keeps speed unchanged.
            voicelineMixerGroup.audioMixer.SetFloat(voicelinePitchParameter, clampedPitch);
            triggerVoicelineSource.pitch = 1f;
        }
        else
        {
            // No mixer configured - fall back to AudioSource.pitch (also speeds playback).
            triggerVoicelineSource.pitch = clampedPitch;
        }
        
        triggerVoicelineSource.PlayOneShot(clip, finalVolume);
        
        if (debugMode)
        {
            bool viaMixer = voicelineMixerGroup != null && !string.IsNullOrEmpty(voicelinePitchParameter);
        }
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