using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages the death screen UI with restart and quit buttons.
/// Replaces the automatic resume behavior with player choice.
/// </summary>
public class DeathScreenManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject deathScreenPanel;
    [SerializeField] private TextMeshProUGUI deathText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Animator deathAnimator;
    [SerializeField] private string deathTrigger = "ShowDeath";
    
    [Header("Auto-Create Buttons")]
    [Tooltip("If true, will automatically create restart and quit buttons if they don't exist")]
    [SerializeField] private bool autoCreateButtons = true;
    
    [Header("References")]
    [SerializeField] private SnakeHealth snakeHealth;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private AttackManager attackManager;
    [SerializeField] private AbilityManager abilityManager;
    [SerializeField] private XPManager xpManager;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private SnakeBody snakeBody;
    [SerializeField] private AttackSelectionUI attackSelectionUI;
    [SerializeField] private SnakePauseMenu pauseMenu;
    [SerializeField] private GameObject nonUI;
    [SerializeField] private CheckpointManager checkpointManager;
    
    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private bool reloadCurrentScene = true;
    
    [Header("Checkpoint Settings")]
    [Tooltip("If true, uses checkpoint system to restore player state instead of reloading scene")]
    [SerializeField] private bool useCheckpointSystem = true;
    
    private bool isDeathScreenActive = false;
    private Canvas deathCanvas;
    private PlayerInput playerInput;
    
    private void Start()
    {
        FindReferences();
        SetupButtons();
        
        // Hide death screen at start
        if (deathScreenPanel != null)
        {
            deathScreenPanel.SetActive(false);
        }
    }
    
    private void SetupButtons()
    {
        // Setup button listeners if buttons exist
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitClicked);
        }
        
        // Auto-create buttons if enabled and panel exists but buttons don't
        if (autoCreateButtons && deathScreenPanel != null)
        {
            if (restartButton == null)
            {
                restartButton = CreateButton(deathScreenPanel.transform, "RestartButton", "RESTART", new Vector2(0, -50), OnRestartClicked);
            }
            
            if (quitButton == null)
            {
                quitButton = CreateButton(deathScreenPanel.transform, "QuitButton", "QUIT", new Vector2(0, -120), OnQuitClicked);
            }
        }
    }
    
    private Button CreateButton(Transform parent, string name, string text, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        // Create button GameObject
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        
        // Add RectTransform
        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(200, 50);
        
        // Add Image for button background
        Image image = buttonObj.AddComponent<Image>();
        image.color = name.Contains("Restart") ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.6f, 0.2f, 0.2f);
        
        // Add Button component
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        
        // Create text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = 24;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;
        
        Debug.Log($"[DeathScreenManager] Auto-created button: {name}");
        
        return button;
    }
    
    private void FindReferences()
    {
        if (snakeHealth == null) snakeHealth = FindFirstObjectByType<SnakeHealth>();
        if (waveManager == null) waveManager = FindFirstObjectByType<WaveManager>();
        if (attackManager == null) attackManager = FindFirstObjectByType<AttackManager>();
        if (abilityManager == null) abilityManager = FindFirstObjectByType<AbilityManager>();
        if (xpManager == null) xpManager = FindFirstObjectByType<XPManager>();
        if (playerStats == null) playerStats = PlayerStats.Instance;
        if (enemySpawner == null) enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (cameraManager == null) cameraManager = FindFirstObjectByType<CameraManager>();
        if (playerMovement == null) playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (snakeBody == null) snakeBody = FindFirstObjectByType<SnakeBody>();
        if (attackSelectionUI == null) attackSelectionUI = FindFirstObjectByType<AttackSelectionUI>();
        if (pauseMenu == null) pauseMenu = SnakePauseMenu.Instance;
        if (pauseMenu == null) pauseMenu = FindFirstObjectByType<SnakePauseMenu>();
        if (checkpointManager == null) checkpointManager = CheckpointManager.Instance;
        if (checkpointManager == null) checkpointManager = FindFirstObjectByType<CheckpointManager>();
        
        // Try to get death screen panel from AttackSelectionUI if not assigned
        if (deathScreenPanel == null && attackSelectionUI != null)
        {
            // Use reflection to get the private deathScreenPanel field
            var field = typeof(AttackSelectionUI).GetField("deathScreenPanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                deathScreenPanel = field.GetValue(attackSelectionUI) as GameObject;
                if (deathScreenPanel != null)
                {
                    Debug.Log("[DeathScreenManager] Using death screen panel from AttackSelectionUI");
                }
            }
        }
    }
    
    /// <summary>
    /// Shows the death screen with restart and quit options.
    /// Called by SnakeHealth when the player dies.
    /// </summary>
    public void ShowDeathScreen()
    {
        if (isDeathScreenActive) return;
        
        isDeathScreenActive = true;
        
        // Always create our own death screen UI to ensure buttons work properly
        // Don't rely on the AttackSelectionUI's death screen panel
        if (deathScreenPanel == null || restartButton == null || quitButton == null)
        {
            CreateDeathScreenUI();
        }
        
        if (deathScreenPanel != null)
        {
            deathScreenPanel.SetActive(true);
        }
        
        if (nonUI != null)
        {
            nonUI.SetActive(false);
        }
        
        if (deathText != null)
        {
            deathText.text = "YOU'RE DEAD!";
        }
        
        if (deathAnimator != null)
        {
            deathAnimator.SetTrigger(deathTrigger);
        }
        
        // Disable pause menu while death screen is active
        if (pauseMenu == null)
        {
            pauseMenu = SnakePauseMenu.Instance;
            if (pauseMenu == null) pauseMenu = FindFirstObjectByType<SnakePauseMenu>();
        }
        if (pauseMenu != null)
        {
            pauseMenu.enabled = false;
            Debug.Log("[DeathScreenManager] Pause menu disabled");
        }
        
        // Show cursor for button interaction - ensure it's fully unlocked and visible
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Disable player input to allow UI interaction
        DisablePlayerInput();
        
        // Ensure EventSystem exists for UI interaction
        EnsureEventSystem();
        
        Debug.Log("[DeathScreenManager] Death screen shown with cursor unlocked and player input disabled");
    }
    
    private void Update()
    {
        // Continuously ensure cursor is unlocked while death screen is active
        if (isDeathScreenActive)
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            if (!Cursor.visible)
            {
                Cursor.visible = true;
            }
        }
    }
    
    /// <summary>
    /// Ensures an EventSystem exists in the scene for UI interaction.
    /// </summary>
    private void EnsureEventSystem()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystem = eventSystemObj.AddComponent<EventSystem>();
            // Use InputSystemUIInputModule for the new Input System
            eventSystemObj.AddComponent<InputSystemUIInputModule>();
            Debug.Log("[DeathScreenManager] Created EventSystem with InputSystemUIInputModule for UI interaction");
        }
        else
        {
            // Make sure the existing EventSystem has an input module
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null &&
                eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                Debug.Log("[DeathScreenManager] Added InputSystemUIInputModule to existing EventSystem");
            }
        }
    }
    
    /// <summary>
    /// Disables player input to allow UI interaction.
    /// </summary>
    private void DisablePlayerInput()
    {
        // Find PlayerInput component if not cached
        if (playerInput == null)
        {
            playerInput = FindFirstObjectByType<PlayerInput>();
        }
        
        if (playerInput != null)
        {
            playerInput.DeactivateInput();
            Debug.Log("[DeathScreenManager] Player input deactivated");
        }
        else
        {
            // If no PlayerInput component, try to disable the Snake action map directly
            // by finding any active PlayerControls instances
            Debug.Log("[DeathScreenManager] No PlayerInput found, attempting to disable input via other means");
        }
        
        // Also freeze the attack manager to prevent any attack processing
        if (attackManager != null)
        {
            attackManager.SetFrozen(true);
        }
    }
    
    /// <summary>
    /// Re-enables player input after UI interaction is complete.
    /// </summary>
    private void EnablePlayerInput()
    {
        if (playerInput != null)
        {
            playerInput.ActivateInput();
            Debug.Log("[DeathScreenManager] Player input reactivated");
        }
        
        // Unfreeze attack manager
        if (attackManager != null)
        {
            attackManager.SetFrozen(false);
        }
    }
    
    /// <summary>
    /// Creates the death screen UI from scratch if none exists.
    /// </summary>
    private void CreateDeathScreenUI()
    {
        Debug.Log("[DeathScreenManager] Creating death screen UI from scratch");
        
        // Always create a new canvas for the death screen to ensure proper layering and raycasting
        GameObject canvasObj = new GameObject("DeathScreenCanvas");
        deathCanvas = canvasObj.AddComponent<Canvas>();
        deathCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        deathCanvas.sortingOrder = 1000; // Make sure it's on top of everything
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Ensure EventSystem exists
        EnsureEventSystem();
        
        // Create death screen panel
        deathScreenPanel = new GameObject("DeathScreenPanel");
        deathScreenPanel.transform.SetParent(deathCanvas.transform, false);
        
        RectTransform panelRect = deathScreenPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Add dark background
        Image panelImage = deathScreenPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);
        
        // Create death text
        GameObject textObj = new GameObject("DeathText");
        textObj.transform.SetParent(deathScreenPanel.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchoredPosition = new Vector2(0, 100);
        textRect.sizeDelta = new Vector2(600, 100);
        
        deathText = textObj.AddComponent<TextMeshProUGUI>();
        deathText.text = "YOU'RE DEAD!";
        deathText.fontSize = 72;
        deathText.alignment = TextAlignmentOptions.Center;
        deathText.color = Color.red;
        
        // Create buttons
        restartButton = CreateButton(deathScreenPanel.transform, "RestartButton", "RESTART", new Vector2(0, -20), OnRestartClicked);
        quitButton = CreateButton(deathScreenPanel.transform, "QuitButton", "QUIT", new Vector2(0, -90), OnQuitClicked);
        
        Debug.Log("[DeathScreenManager] Death screen UI created successfully");
    }
    
    /// <summary>
    /// Hides the death screen.
    /// </summary>
    public void HideDeathScreen()
    {
        isDeathScreenActive = false;
        
        if (deathScreenPanel != null)
        {
            deathScreenPanel.SetActive(false);
        }
        
        if (nonUI != null)
        {
            nonUI.SetActive(true);
        }
        
        // Re-enable pause menu
        if (pauseMenu != null)
        {
            pauseMenu.enabled = true;
        }
        
        // Re-enable player input
        EnablePlayerInput();
        
        // Hide cursor for gameplay
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        Debug.Log("[DeathScreenManager] Death screen hidden");
    }
    
    /// <summary>
    /// Called when the restart button is clicked.
    /// Uses checkpoint system to restore player state to last level milestone.
    /// </summary>
    public void OnRestartClicked()
    {
        Debug.Log("[DeathScreenManager] Restart clicked");
        
        // Re-enable pause menu
        if (pauseMenu != null)
        {
            pauseMenu.enabled = true;
        }
        
        // Try to use checkpoint system first
        if (useCheckpointSystem)
        {
            // Find checkpoint manager if not cached
            if (checkpointManager == null)
            {
                checkpointManager = CheckpointManager.Instance;
                if (checkpointManager == null)
                    checkpointManager = FindFirstObjectByType<CheckpointManager>();
            }
            
            if (checkpointManager != null && checkpointManager.HasCheckpoint())
            {
                Debug.Log($"[DeathScreenManager] Restoring checkpoint at level {checkpointManager.GetCheckpointLevel()}");
                HideDeathScreen();
                checkpointManager.RestoreCheckpoint();
                return;
            }
            else
            {
                Debug.Log("[DeathScreenManager] No checkpoint available, falling back to scene reload");
            }
        }
        
        // Fallback: reload scene if checkpoint system is disabled or no checkpoint exists
        if (reloadCurrentScene)
        {
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }
        else
        {
            StartCoroutine(ResetGameState());
        }
    }
    
    /// <summary>
    /// Called when the quit button is clicked.
    /// Returns to the main menu or exits the game.
    /// </summary>
    public void OnQuitClicked()
    {
        Debug.Log("[DeathScreenManager] Quit clicked");
        
        // Re-enable pause menu before scene change
        if (pauseMenu != null)
        {
            pauseMenu.enabled = true;
        }
        
        // Try to load main menu scene
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            // Check if the scene exists in build settings
            bool sceneExists = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (sceneName == mainMenuSceneName)
                {
                    sceneExists = true;
                    break;
                }
            }
            
            if (sceneExists)
            {
                SceneManager.LoadScene(mainMenuSceneName);
                return;
            }
        }
        
        // If no main menu scene, quit the application
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    /// <summary>
    /// Manually resets the game state without reloading the scene.
    /// This is an alternative to scene reload if needed.
    /// </summary>
    private IEnumerator ResetGameState()
    {
        HideDeathScreen();
        
        // Clear all enemies
        if (enemySpawner != null)
        {
            enemySpawner.ClearAllEnemies();
        }
        
        // Reset XP and level
        if (xpManager != null)
        {
            xpManager.ResetXP();
        }
        
        // Reset player stats
        if (playerStats != null)
        {
            playerStats.ResetAllBonuses();
        }
        
        // Clear all attacks
        if (attackManager != null)
        {
            attackManager.ClearAllAttacks();
        }
        
        // Clear all abilities
        if (abilityManager != null)
        {
            var abilities = abilityManager.GetActiveAbilities();
            foreach (var ability in abilities)
            {
                if (ability != null)
                {
                    abilityManager.RemoveAbility(ability);
                }
            }
        }
        
        // Reset wave manager to wave 0
        if (waveManager != null)
        {
            waveManager.currentWaveIndex = 0;
        }
        
        // Wait a frame for cleanup
        yield return null;
        
        // Reset snake health
        if (snakeHealth != null)
        {
            snakeHealth.ResetHealth();
        }
        
        // Switch back to gameplay camera
        if (cameraManager != null)
        {
            cameraManager.SwitchToNormalCamera();
        }
        
        // Re-enable player movement
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
            Rigidbody rb = playerMovement.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }
        
        // Start the first wave
        if (waveManager != null)
        {
            waveManager.StartWave();
        }
        
        Debug.Log("[DeathScreenManager] Game state reset complete");
    }
    
    /// <summary>
    /// Returns whether the death screen is currently active.
    /// </summary>
    public bool IsDeathScreenActive()
    {
        return isDeathScreenActive;
    }
}