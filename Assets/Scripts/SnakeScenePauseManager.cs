using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the pause menu for the Snake game scene.
/// Press Escape to pause/unpause the game.
/// This is a simplified pause menu without main menu functionality.
/// </summary>
public class SnakeScenePauseManager : MonoBehaviour
{
    public static SnakeScenePauseManager Instance { get; private set; }
    
    [Header("Menu References")]
    [Tooltip("The pause menu panel to show/hide")]
    public GameObject pauseMenuPanel;
    
    [Tooltip("Optional: The Canvas containing the pause menu (will be found automatically if not set)")]
    public Canvas pauseMenuCanvas;
    
    [Tooltip("Optional: Settings panel within the pause menu")]
    public GameObject settingsPanel;
    
    [Tooltip("Optional: Main pause menu buttons panel")]
    public GameObject mainPausePanel;
    
    [Header("Settings")]
    [Tooltip("Should the game pause when menu is open?")]
    public bool pauseGameWhenOpen = true;
    
    [Tooltip("Should the cursor be visible when menu is open?")]
    public bool showCursorWhenOpen = true;
    
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
    private bool canPause = true; // Pausing is enabled by default for snake scene
    
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
        
        // Find and configure the Canvas if not assigned
        if (pauseMenuCanvas == null && pauseMenuPanel != null)
        {
            pauseMenuCanvas = pauseMenuPanel.GetComponentInParent<Canvas>();
            if (pauseMenuCanvas == null)
            {
                // Check if the panel itself has a Canvas
                pauseMenuCanvas = pauseMenuPanel.GetComponent<Canvas>();
            }
        }
        
        // Configure Canvas for proper rendering
        if (pauseMenuCanvas != null)
        {
            // Ensure it renders on top of everything
            pauseMenuCanvas.sortingOrder = 100;
            Debug.Log($"[SnakeScenePauseManager] Canvas found: {pauseMenuCanvas.name}, sortingOrder set to {pauseMenuCanvas.sortingOrder}");
        }
        else if (pauseMenuPanel != null)
        {
            Debug.LogWarning("[SnakeScenePauseManager] No Canvas found for pause menu! Make sure the pause menu panel is under a Canvas.");
        }
        
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
            // Don't allow pausing if disabled
            if (!canPause)
            {
                return;
            }
            
            // Don't allow pause if tutorial panel is active
            if (TutorialPanelManager.Instance != null && TutorialPanelManager.Instance.IsTutorialActive)
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
    /// Call this to enable pausing
    /// </summary>
    public void EnablePausing()
    {
        canPause = true;
    }
    
    /// <summary>
    /// Call this to disable pausing (e.g., during cutscenes)
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
            
            // Ensure Canvas is enabled and on top
            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.enabled = true;
                pauseMenuCanvas.sortingOrder = 100;
            }
            
            // Check for CanvasGroup and ensure it's visible
            CanvasGroup canvasGroup = pauseMenuPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            
            Debug.Log($"[SnakeScenePauseManager] Pause menu activated. Active: {pauseMenuPanel.activeSelf}, ActiveInHierarchy: {pauseMenuPanel.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning("[SnakeScenePauseManager] pauseMenuPanel is not assigned! The game will pause but no menu will be shown.");
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
        
        Debug.Log("[SnakeScenePauseManager] Game paused");
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
        
        Debug.Log("[SnakeScenePauseManager] Game resumed");
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
        // Resume time and audio before reloading
        Time.timeScale = 1f;
        AudioListener.pause = false;
        isPaused = false;
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}