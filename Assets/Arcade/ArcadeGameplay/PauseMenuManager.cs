using UnityEngine;
using UnityEngine.SceneManagement;

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
    private bool canPause = false;
    
    public bool IsPaused => isPaused;
    public bool CanPause => canPause;
    
    private void Awake()
    {
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
        if (fpsController == null)
        {
            fpsController = FindFirstObjectByType<EasyPeasyFirstPersonController.FirstPersonController>();
        }
        
        if (mainMenuManager == null)
        {
            mainMenuManager = FindFirstObjectByType<MainMenuManager>();
        }
        
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        isPaused = false;
        canPause = false;
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (!canPause || IsInMainMenu())
            {
                return;
            }
            
            if (isPaused)
            {
                if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    CloseSettings();
                }
                else
                {
                    ResumeGame();
                }
            }
            else
            {
                PauseGame();
            }
        }
    }
    
    private bool IsInMainMenu()
    {
        if (mainMenuManager != null && mainMenuManager.mainMenuPanel != null)
        {
            return mainMenuManager.mainMenuPanel.activeSelf;
        }
        return false;
    }
    
    public void EnablePausing()
    {
        canPause = true;
    }
    
    public void DisablePausing()
    {
        canPause = false;
    }
    
    public void PauseGame()
    {
        if (isPaused) return;
        
        isPaused = true;
        
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
        
        if (mainPausePanel != null)
        {
            mainPausePanel.SetActive(true);
        }
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        
        if (pauseGameWhenOpen)
        {
            wasTimeScaleZero = Time.timeScale == 0f;
            Time.timeScale = 0f;
        }
        
        AudioListener.pause = true;
        
        if (showCursorWhenOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        if (fpsController != null)
        {
            fpsController.SetControl(false);
        }
    }
    
    public void ResumeGame()
    {
        if (!isPaused) return;
        
        isPaused = false;
        
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        if (pauseGameWhenOpen && !wasTimeScaleZero)
        {
            Time.timeScale = 1f;
        }
        
        AudioListener.pause = false;
        
        if (showCursorWhenOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        if (fpsController != null)
        {
            fpsController.SetControl(true);
        }
    }
    
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
    
    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
    
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
        if (isPaused && pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}