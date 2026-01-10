using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Manages the main menu UI overlay in the Arcade scene.
/// When the game starts, the menu is displayed over the arcade scene view.
/// When Play is clicked, the UI fades out and the intro cutscene begins seamlessly.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("The main menu canvas/panel containing all menu UI")]
    public GameObject mainMenuPanel;
    
    [Tooltip("The settings panel (child of main menu or separate)")]
    public GameObject settingsPanel;
    
    [Header("Buttons")]
    public Button playButton;
    public Button settingsButton;
    public Button settingsBackButton;
    public Button quitButton;
    
    [Header("Fade Settings")]
    [Tooltip("Canvas Group for fading the entire menu UI")]
    public CanvasGroup menuCanvasGroup;
    
    [Tooltip("Duration of the fade out animation")]
    public float fadeOutDuration = 0.5f;
    
    [Header("Cameras")]
    [Tooltip("The intro/menu camera that shows during the main menu")]
    public Camera introCamera;
    
    [Tooltip("The FPS camera (player's camera) - disabled during menu")]
    public Camera fpsCamera;
    
    [Header("Cutscene Integration")]
    [Tooltip("Reference to the CutsceneController that plays the intro")]
    public CutsceneController introCutsceneController;
    
    [Tooltip("If true, automatically starts the cutscene after fade")]
    public bool autoStartCutscene = true;
    
    [Header("FPS Controller")]
    [Tooltip("Reference to the FPS controller to disable during menu")]
    public EasyPeasyFirstPersonController.FirstPersonController fpsController;
    
    [Header("Audio")]
    [Tooltip("Optional: Sound to play when clicking Play")]
    public AudioClip playButtonSound;
    
    [Tooltip("Optional: Sound to play for button clicks")]
    public AudioClip buttonClickSound;
    
    [Header("Menu Music")]
    [Tooltip("The main menu music clip")]
    public AudioClip menuMusic;
    
    [Tooltip("Volume for menu music")]
    [Range(0f, 1f)]
    public float menuMusicVolume = 0.5f;
    
    [Tooltip("Should menu music loop?")]
    public bool loopMenuMusic = true;
    
    [Tooltip("Fade out duration for menu music when starting game")]
    public float menuMusicFadeOutDuration = 1f;
    
    [Header("Game Audio Management")]
    [Tooltip("Audio sources to mute during menu (will be unmuted when game starts)")]
    public AudioSource[] gameAudioSources;
    
    [Tooltip("If true, automatically finds and mutes all AudioSources in scene except menu")]
    public bool autoFindGameAudioSources = true;
    
    [Tooltip("Tags to exclude from auto-muting (e.g., 'MenuAudio')")]
    public string[] excludeFromMuteTags;
    
    private AudioSource audioSource;
    private AudioSource menuMusicSource;
    private bool isTransitioning = false;
    private AudioSource[] cachedGameAudioSources;
    
    private void Awake()
    {
        // Get or add AudioSource for UI sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        
        // Create a separate AudioSource for menu music
        menuMusicSource = gameObject.AddComponent<AudioSource>();
        menuMusicSource.playOnAwake = false;
        menuMusicSource.loop = loopMenuMusic;
        menuMusicSource.volume = menuMusicVolume;
    }
    
    private void Start()
    {
        // Pause the game while in menu (stops animations, physics, etc.)
        Time.timeScale = 0f;
        
        // Setup cameras - intro camera active, FPS camera disabled
        SetupCamerasForMenu();
        
        // Initialize menu state
        ShowMainMenu();
        
        // Disable FPS controller while in menu
        if (fpsController != null)
        {
            fpsController.SetControl(false);
            fpsController.SetCursorVisibility(true);
        }
        
        // Disable the cutscene controller's auto-start if it has one
        if (introCutsceneController != null)
        {
            introCutsceneController.enabled = false;
        }
        
        // Setup button listeners
        SetupButtonListeners();
        
        // Setup audio - mute game sounds and play menu music
        SetupMenuAudio();
    }
    
    /// <summary>
    /// Sets up audio for the menu state - plays menu music and mutes game audio
    /// </summary>
    private void SetupMenuAudio()
    {
        // Find all game audio sources if auto-find is enabled
        if (autoFindGameAudioSources)
        {
            FindGameAudioSources();
        }
        else
        {
            cachedGameAudioSources = gameAudioSources;
        }
        
        // Mute all game audio sources
        MuteGameAudio(true);
        
        // Start playing menu music
        PlayMenuMusic();
    }
    
    /// <summary>
    /// Finds all AudioSources in the scene except menu-related ones
    /// </summary>
    private void FindGameAudioSources()
    {
        AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        System.Collections.Generic.List<AudioSource> gameSources = new System.Collections.Generic.List<AudioSource>();
        
        foreach (AudioSource source in allSources)
        {
            // Skip our own audio sources
            if (source == audioSource || source == menuMusicSource)
                continue;
            
            // Skip sources with excluded tags
            bool isExcluded = false;
            if (excludeFromMuteTags != null)
            {
                foreach (string tag in excludeFromMuteTags)
                {
                    if (!string.IsNullOrEmpty(tag) && source.gameObject.CompareTag(tag))
                    {
                        isExcluded = true;
                        break;
                    }
                }
            }
            
            if (!isExcluded)
            {
                gameSources.Add(source);
            }
        }
        
        cachedGameAudioSources = gameSources.ToArray();
        Debug.Log($"MainMenuManager: Found {cachedGameAudioSources.Length} game audio sources to manage");
    }
    
    /// <summary>
    /// Mutes or unmutes all game audio sources
    /// </summary>
    private void MuteGameAudio(bool mute)
    {
        if (cachedGameAudioSources == null) return;
        
        foreach (AudioSource source in cachedGameAudioSources)
        {
            if (source != null)
            {
                source.mute = mute;
            }
        }
        
        Debug.Log($"MainMenuManager: {(mute ? "Muted" : "Unmuted")} {cachedGameAudioSources.Length} game audio sources");
    }
    
    /// <summary>
    /// Starts playing the menu music
    /// </summary>
    private void PlayMenuMusic()
    {
        if (menuMusic != null && menuMusicSource != null)
        {
            menuMusicSource.clip = menuMusic;
            menuMusicSource.volume = menuMusicVolume;
            menuMusicSource.loop = loopMenuMusic;
            menuMusicSource.Play();
            Debug.Log("MainMenuManager: Started playing menu music");
        }
    }
    
    /// <summary>
    /// Stops the menu music (with optional fade)
    /// </summary>
    private void StopMenuMusic(bool fade = true)
    {
        if (menuMusicSource != null && menuMusicSource.isPlaying)
        {
            if (fade && menuMusicFadeOutDuration > 0)
            {
                StartCoroutine(FadeOutMenuMusic());
            }
            else
            {
                menuMusicSource.Stop();
            }
        }
    }
    
    /// <summary>
    /// Coroutine to fade out menu music
    /// </summary>
    private IEnumerator FadeOutMenuMusic()
    {
        if (menuMusicSource == null) yield break;
        
        float startVolume = menuMusicSource.volume;
        float elapsed = 0f;
        
        while (elapsed < menuMusicFadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time since game might be paused
            menuMusicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / menuMusicFadeOutDuration);
            yield return null;
        }
        
        menuMusicSource.Stop();
        menuMusicSource.volume = startVolume; // Reset volume for potential replay
        Debug.Log("MainMenuManager: Menu music faded out");
    }
    
    /// <summary>
    /// Sets up cameras for the main menu state - intro camera on, FPS camera off
    /// </summary>
    private void SetupCamerasForMenu()
    {
        // Enable intro camera for menu background
        if (introCamera != null)
        {
            introCamera.gameObject.SetActive(true);
        }
        
        // Disable FPS camera during menu
        if (fpsCamera != null)
        {
            fpsCamera.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Switches from intro camera to FPS camera for gameplay
    /// </summary>
    private void SwitchToGameplayCamera()
    {
        // Disable intro camera
        if (introCamera != null)
        {
            introCamera.gameObject.SetActive(false);
        }
        
        // Enable FPS camera for gameplay
        if (fpsCamera != null)
        {
            fpsCamera.gameObject.SetActive(true);
        }
    }
    
    private void SetupButtonListeners()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }
        
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }
        
        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.AddListener(OnSettingsBackClicked);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
    }
    
    /// <summary>
    /// Shows the main menu panel and hides settings
    /// </summary>
    public void ShowMainMenu()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
        
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 1f;
            menuCanvasGroup.interactable = true;
            menuCanvasGroup.blocksRaycasts = true;
        }
        
        // Show cursor for menu interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    /// <summary>
    /// Called when the Play button is clicked
    /// </summary>
    public void OnPlayClicked()
    {
        if (isTransitioning) return;
        
        PlaySound(playButtonSound ?? buttonClickSound);
        StartCoroutine(TransitionToGame());
    }
    
    /// <summary>
    /// Called when the Settings button is clicked
    /// </summary>
    public void OnSettingsClicked()
    {
        PlaySound(buttonClickSound);
        
        // Hide main menu panel when showing settings
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
        
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// Called when the Back button in settings is clicked
    /// </summary>
    public void OnSettingsBackClicked()
    {
        PlaySound(buttonClickSound);
        
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// Called when the Quit button is clicked
    /// </summary>
    public void OnQuitClicked()
    {
        PlaySound(buttonClickSound);
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    /// <summary>
    /// Coroutine that handles the transition from menu to gameplay
    /// </summary>
    private IEnumerator TransitionToGame()
    {
        isTransitioning = true;
        
        // Start fading out menu music
        StopMenuMusic(true);
        
        // Fade out the menu UI using unscaled time (since Time.timeScale is 0)
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;
            
            float elapsed = 0f;
            float startAlpha = menuCanvasGroup.alpha;
            
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime; // Use unscaled time since game is paused
                menuCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
            
            menuCanvasGroup.alpha = 0f;
        }
        
        // Hide the menu panel completely
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
        
        // Unmute all game audio sources
        MuteGameAudio(false);
        
        // Lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Resume time before starting cutscene
        Time.timeScale = 1f;
        
        // Start the intro cutscene
        if (autoStartCutscene && introCutsceneController != null)
        {
            // The cutscene controller will handle camera switching
            introCutsceneController.enabled = true;
            introCutsceneController.StartCutscene();
        }
        else
        {
            // No cutscene - switch directly to gameplay camera
            SwitchToGameplayCamera();
            
            if (fpsController != null)
            {
                // Enable player control
                fpsController.SetControl(true);
            }
        }
        
        isTransitioning = false;
    }
    
    /// <summary>
    /// Plays a sound effect if available
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// Public method to show the menu again (e.g., from pause)
    /// </summary>
    public void ShowMenu()
    {
        StopAllCoroutines();
        isTransitioning = false;
        
        // Pause the game again
        Time.timeScale = 0f;
        
        // Switch back to intro camera
        SetupCamerasForMenu();
        
        if (fpsController != null)
        {
            fpsController.SetControl(false);
        }
        
        // Mute game audio and play menu music again
        MuteGameAudio(true);
        PlayMenuMusic();
        
        ShowMainMenu();
    }
    
    private void OnDestroy()
    {
        // Ensure time is restored if this object is destroyed
        Time.timeScale = 1f;
    }
}