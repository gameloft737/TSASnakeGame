using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the pause menu that opens when pressing Escape.
/// Pauses the game and shows the settings/pause menu.
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }
    
    [Header("Menu References")]
    [Tooltip("The pause menu panel to show/hide")]
    public GameObject pauseMenuPanel;
    
    [Tooltip("Optional: Settings panel within the pause menu")]
    public GameObject settingsPanel;
    
    [Tooltip("Optional: Main pause menu buttons panel")]
    public GameObject mainPausePanel;
    
    [Header("Main Menu Reference")]
    [Tooltip("Reference to MainMenuManager to check if we're in main menu")]
    public MainMenuManager mainMenuManager;
    
    [Header("Settings")]
    [Tooltip("Should the game pause when menu is open?")]
    public bool pauseGameWhenOpen = true;
    
    [Tooltip("Should the cursor be visible when menu is open?")]
    public bool showCursorWhenOpen = true;
    
    [Header("Scene Names")]
    [Tooltip("Name of the main menu scene to load when quitting")]
    public string mainMenuSceneName = "MainMenu";
    
    [Header("FPS Controller Reference")]
    [Tooltip("Reference to disable player controls when paused")]
    public EasyPeasyFirstPersonController.FirstPersonController fpsController;
    
    private bool isPaused = false;
    private bool wasTimeScaleZero = false;
    private bool canPause = false; // Only true after game has started
    
    public bool IsPaused => isPaused;
    public bool CanPause => canPause;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // Find FPS controller if not assigned
        if (fpsController == null)
        {
            fpsController = FindFirstObjectByType<EasyPeasyFirstPersonController.FirstPersonController>();
        }
        
        // Find MainMenuManager if not assigned
        if (mainMenuManager == null)
        {
            mainMenuManager = FindFirstObjectByType<MainMenuManager>();
        }
        
        // Ensure menu starts closed
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        isPaused = false;
        canPause = false; // Start with pausing disabled until game starts
    }
    
    private void Update()
    {
        // Check for Escape or P key
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            // Don't allow pausing if we're in the main menu
            if (!canPause || IsInMainMenu())
            {
                return;
            }
            
            if (isPaused)
            {
                // If settings panel is open, close it first and return to main pause menu
                if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    CloseSettings();
                }
                else
                {
                    // Otherwise, resume the game
                    ResumeGame();
                }
            }
            else
            {
                PauseGame();
            }
        }
    }
    
    /// <summary>
    /// Checks if we're currently in the main menu state
    /// </summary>
    private bool IsInMainMenu()
    {
        // Check if MainMenuManager exists and its panel is active
        if (mainMenuManager != null && mainMenuManager.mainMenuPanel != null)
        {
            return mainMenuManager.mainMenuPanel.activeSelf;
        }
        return false;
    }
    
    /// <summary>
    /// Call this when the game actually starts (after main menu transition)
    /// </summary>
    public void EnablePausing()
    {
        canPause = true;
    }
    
    /// <summary>
    /// Call this to disable pausing (e.g., during cutscenes or main menu)
    /// </summary>
    public void DisablePausing()
    {
        canPause = false;
    }
    
    /// <summary>
    /// Pauses the game and shows the pause menu
    /// </summary>
    public void PauseGame()
    {
        if (isPaused) return;
        
        isPaused = true;
        
        // Show pause menu
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
        
        // Show main pause panel, hide settings
        if (mainPausePanel != null)
        {
            mainPausePanel.SetActive(true);
        }
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        
        // Pause time
        if (pauseGameWhenOpen)
        {
            wasTimeScaleZero = Time.timeScale == 0f;
            Time.timeScale = 0f;
        }
        
        // Pause audio
        AudioListener.pause = true;
        
        // Show cursor
        if (showCursorWhenOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        // Disable player controls
        if (fpsController != null)
        {
            fpsController.SetControl(false);
        }
    }
    
    /// <summary>
    /// Resumes the game and hides the pause menu
    /// </summary>
    public void ResumeGame()
    {
        if (!isPaused) return;
        
        isPaused = false;
        
        // Hide pause menu
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        // Resume time
        if (pauseGameWhenOpen && !wasTimeScaleZero)
        {
            Time.timeScale = 1f;
        }
        
        // Resume audio
        AudioListener.pause = false;
        
        // Hide cursor
        if (showCursorWhenOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        // Enable player controls
        if (fpsController != null)
        {
            fpsController.SetControl(true);
        }
    }
    
    /// <summary>
    /// Opens the settings panel within the pause menu
    /// </summary>
    public void OpenSettings()
    {
        if (mainPausePanel != null)
        {
            mainPausePanel.SetActive(false);
        }
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// Closes settings and returns to main pause menu
    /// </summary>
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        if (mainPausePanel != null)
        {
            mainPausePanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// Quits to main menu
    /// </summary>
    public void QuitToMainMenu()
    {
        // Resume time before loading new scene
        Time.timeScale = 1f;
        isPaused = false;
        
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
    
    /// <summary>
    /// Quits the application
    /// </summary>
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    private void OnDestroy()
    {
        // Ensure time and audio are resumed if this object is destroyed
        if (isPaused && pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}