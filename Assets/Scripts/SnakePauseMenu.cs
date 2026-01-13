using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the pause menu for the Snake game.
/// Press Escape to pause/unpause the game.
/// </summary>
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
        // Find references if not assigned
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
        
        // Ensure menu starts closed
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        isPaused = false;
    }
    
    private void Update()
    {
        // Check for Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Don't allow pause if tutorial panel is active
            if (TutorialPanelManager.Instance != null && TutorialPanelManager.Instance.IsTutorialActive)
            {
                return;
            }
            
            // Don't allow pause if death screen is active
            DeathScreenManager deathScreen = FindFirstObjectByType<DeathScreenManager>();
            if (deathScreen != null && deathScreen.IsDeathScreenActive())
            {
                return;
            }
            
            // Don't allow pause if another UI is open (attack selection, ability collection, etc.)
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
                // If settings panel is open, close it and go back to main pause menu
                if (IsSettingsOpen())
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
    /// Returns whether the settings panel is currently open
    /// </summary>
    public bool IsSettingsOpen()
    {
        return settingsPanel != null && settingsPanel.activeSelf;
    }
    
    /// <summary>
    /// Pauses the game and shows the pause menu
    /// </summary>
    public void PauseGame()
    {
        if (isPaused) return;
        
        isPaused = true;
        
        // Lower game music volume while paused
        if (waveManager != null)
        {
            SoundManager.SetVolume("GameMusic", waveManager.gameObject, 0.3f);
        }
        
        // Show pause menu
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[SnakePauseMenu] pauseMenuPanel is not assigned! The game will pause but no menu will be shown.");
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
        
        // Show cursor
        if (showCursorWhenOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        // Freeze player and game systems
        if (playerMovement != null)
            playerMovement.SetFrozen(true);
        if (mouseLookAt != null)
            mouseLookAt.SetFrozen(true);
        if (attackManager != null)
            attackManager.SetFrozen(true);
        if (waveManager != null)
            waveManager.PauseWave();
        
        // Freeze all abilities
        if (abilityManager != null)
        {
            foreach (BaseAbility ability in abilityManager.GetActiveAbilities())
            {
                if (ability != null)
                    ability.SetFrozen(true);
            }
        }
        
        // Freeze all enemies
        var enemies = AppleEnemy.GetAllActiveEnemies();
        foreach (AppleEnemy enemy in enemies)
        {
            if (enemy != null)
                enemy.SetFrozen(true);
        }
        
        Debug.Log("[SnakePauseMenu] Game paused");
    }
    
    /// <summary>
    /// Resumes the game and hides the pause menu
    /// </summary>
    public void ResumeGame()
    {
        if (!isPaused) return;
        
        isPaused = false;
        
        // Restore game music volume
        if (waveManager != null)
        {
            SoundManager.SetVolume("GameMusic", waveManager.gameObject, 1f);
        }
        
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
        
        // Hide cursor
        if (showCursorWhenOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        // Unfreeze player and game systems
        if (playerMovement != null)
            playerMovement.SetFrozen(false);
        if (mouseLookAt != null)
            mouseLookAt.SetFrozen(false);
        if (attackManager != null)
            attackManager.SetFrozen(false);
        if (waveManager != null)
            waveManager.ResumeWave();
        
        // Unfreeze all abilities
        if (abilityManager != null)
        {
            foreach (BaseAbility ability in abilityManager.GetActiveAbilities())
            {
                if (ability != null)
                    ability.SetFrozen(false);
            }
        }
        
        // Unfreeze all enemies
        var enemies = AppleEnemy.GetAllActiveEnemies();
        foreach (AppleEnemy enemy in enemies)
        {
            if (enemy != null)
                enemy.SetFrozen(false);
        }
        
        Debug.Log("[SnakePauseMenu] Game resumed");
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
    /// Restarts the current scene
    /// </summary>
    public void RestartGame()
    {
        // Resume time before reloading
        Time.timeScale = 1f;
        isPaused = false;
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
        // Ensure time is resumed if this object is destroyed
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