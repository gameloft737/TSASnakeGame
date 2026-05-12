using UnityEngine;
using UnityEngine.SceneManagement;

public class SnakePauseMenu : MonoBehaviour
{
    public static SnakePauseMenu Instance { get; private set; }
    
    [Header("Menu References")]
    [Tooltip("The pause menu panel to show/hide")]
    public GameObject pauseMenuPanel;
    
    [Tooltip("Optional: Settings panel within the pause menu")]
    public GameObject settingsPanel;
    
    [Tooltip("Optional: Main pause menu buttons panel")]
    public GameObject mainPausePanel;
    
    [Header("Settings")]
    [Tooltip("Should the game pause when menu is open?")]
    public bool pauseGameWhenOpen = true;
    
    [Tooltip("Should the cursor be visible when menu is open?")]
    public bool showCursorWhenOpen = true;
    
    [Header("Scene Names")]
    [Tooltip("Name of the main menu scene to load when quitting")]
    public string mainMenuSceneName = "Arcade";
    
    [Header("References")]
    [Tooltip("Reference to player movement to freeze when paused")]
    public PlayerMovement playerMovement;
    
    [Tooltip("Reference to mouse look to freeze when paused")]
    public MouseLookAt mouseLookAt;
    
    [Tooltip("Reference to attack manager to pause attacks")]
    public AttackManager attackManager;
    
    [Tooltip("Reference to wave manager to pause waves")]
    public WaveManager waveManager;
    
    [Tooltip("Reference to ability manager")]
    public AbilityManager abilityManager;
    
    private bool isPaused = false;
    private bool wasTimeScaleZero = false;
    
    public bool IsPaused => isPaused;
    
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
        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (mouseLookAt == null)
            mouseLookAt = FindFirstObjectByType<MouseLookAt>();
        if (attackManager == null)
            attackManager = FindFirstObjectByType<AttackManager>();
        if (waveManager == null)
            waveManager = FindFirstObjectByType<WaveManager>();
        if (abilityManager == null)
            abilityManager = FindFirstObjectByType<AbilityManager>();
        
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        isPaused = false;
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (TutorialPanelManager.Instance != null && TutorialPanelManager.Instance.IsTutorialActive)
            {
                return;
            }
            
            DeathScreenManager deathScreen = FindFirstObjectByType<DeathScreenManager>();
            if (deathScreen != null && deathScreen.IsDeathScreenActive())
            {
                return;
            }
            
            AttackSelectionUI attackUI = FindFirstObjectByType<AttackSelectionUI>();
            if (attackUI != null && attackUI.IsUIOpen())
            {
                return;
            }
            
            AbilityCollector abilityCollector = FindFirstObjectByType<AbilityCollector>();
            if (abilityCollector != null && abilityCollector.IsUIOpen())
            {
                return;
            }
            
            if (isPaused)
            {
                if (IsSettingsOpen())
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
    
    public bool IsSettingsOpen()
    {
        return settingsPanel != null && settingsPanel.activeSelf;
    }
    
    public void PauseGame()
    {
        if (isPaused) return;
        
        isPaused = true;
        
        if (waveManager != null)
        {
            SoundManager.SetVolume("GameMusic", waveManager.gameObject, 0.3f);
        }
        if (LevelUIManager.Instance != null)
        {
            LevelUIManager.Instance.SetLevelMusicVolume(0.3f);
        }
        
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[SnakePauseMenu] pauseMenuPanel is not assigned! The game will pause but no menu will be shown.");
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
        
        if (showCursorWhenOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        if (playerMovement != null)
            playerMovement.SetFrozen(true);
        if (mouseLookAt != null)
            mouseLookAt.SetFrozen(true);
        if (attackManager != null)
            attackManager.SetFrozen(true);
        if (waveManager != null)
            waveManager.PauseWave();
        
        if (abilityManager != null)
        {
            foreach (BaseAbility ability in abilityManager.GetActiveAbilities())
            {
                if (ability != null)
                    ability.SetFrozen(true);
            }
        }
        
        var enemies = AppleEnemy.GetAllActiveEnemies();
        foreach (AppleEnemy enemy in enemies)
        {
            if (enemy != null)
                enemy.SetFrozen(true);
        }
    }
    
    public void ResumeGame()
    {
        if (!isPaused) return;
        
        isPaused = false;
        
        if (waveManager != null)
        {
            SoundManager.SetVolume("GameMusic", waveManager.gameObject, 1f);
        }
        if (LevelUIManager.Instance != null)
        {
            LevelUIManager.Instance.SetLevelMusicVolume(1f);
        }
        
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        if (pauseGameWhenOpen && !wasTimeScaleZero)
        {
            Time.timeScale = 1f;
        }
        
        if (showCursorWhenOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        if (playerMovement != null)
            playerMovement.SetFrozen(false);
        if (mouseLookAt != null)
            mouseLookAt.SetFrozen(false);
        if (attackManager != null)
            attackManager.SetFrozen(false);
        if (waveManager != null)
            waveManager.ResumeWave();
        
        if (abilityManager != null)
        {
            foreach (BaseAbility ability in abilityManager.GetActiveAbilities())
            {
                if (ability != null)
                    ability.SetFrozen(false);
            }
        }
        
        var enemies = AppleEnemy.GetAllActiveEnemies();
        foreach (AppleEnemy enemy in enemies)
        {
            if (enemy != null)
                enemy.SetFrozen(false);
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
    
    public void RestartGame()
    {
        Time.timeScale = 1f;
        isPaused = false;
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}