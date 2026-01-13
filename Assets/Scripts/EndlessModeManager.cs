using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// Manages endless mode functionality by disabling the ending cutscene scene transition.
///
/// SETUP INSTRUCTIONS:
/// 1. Add this script to a GameObject in your Arcade scene
/// 2. Create an Endless Mode button in your UI
/// 3. Call StartEndlessMode() from the button's OnClick event
/// 4. The regular Start button should call SetRegularMode() before MainMenuManager.OnPlayClicked()
///
/// HOW IT WORKS:
/// - Regular mode: EndingCutsceneTrigger loads its configured scene normally
/// - Endless mode: EndingCutsceneTrigger's scene name is cleared so no scene loads
/// </summary>
public class EndlessModeManager : MonoBehaviour
{
    public static EndlessModeManager Instance { get; private set; }
    
    [Header("Scene Settings")]
    [Tooltip("Scene name to load when winning in regular mode")]
    [SerializeField] private string regularModeSceneName = "Snake";
    
    [Tooltip("Scene name to load when winning in endless mode (leave empty for no scene change)")]
    [SerializeField] private string endlessModeSceneName = "";
    
    [Header("Game Start Settings")]
    [Tooltip("Reference to the MainMenuManager to start the game")]
    [SerializeField] private MainMenuManager mainMenuManager;
    
    [Header("Confirmation Panel (Optional)")]
    [Tooltip("Optional confirmation panel - if not assigned, endless mode starts immediately")]
    [SerializeField] private GameObject confirmationPanel;
    
    [Tooltip("The confirm button that starts endless mode")]
    [SerializeField] private Button confirmButton;
    
    [Tooltip("The cancel button that closes the panel")]
    [SerializeField] private Button cancelButton;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    /// <summary>
    /// Static flag that persists across scene loads to indicate if endless mode is active
    /// </summary>
    public static bool IsEndlessMode { get; private set; } = false;
    
    /// <summary>
    /// Stores the scene names configured in the manager
    /// </summary>
    private static string configuredRegularSceneName = null;
    private static string configuredEndlessSceneName = null;
    
    private void Awake()
    {
        // Simple singleton - each scene gets its own instance, but we track the current one
        // No DontDestroyOnLoad - this allows UI references to work properly
        Instance = this;
        
        // Store the configured scene names (only if not already set)
        if (configuredRegularSceneName == null)
            configuredRegularSceneName = regularModeSceneName;
        if (configuredEndlessSceneName == null)
            configuredEndlessSceneName = endlessModeSceneName;
    }
    
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void Start()
    {
        // Set up button listeners if panel exists
        SetupButtonListeners();
    }
    
    /// <summary>
    /// Called when a new scene is loaded - applies endless mode settings to EndingCutsceneTrigger
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (debugMode)
            Debug.Log($"[EndlessModeManager] Scene loaded: {scene.name}, IsEndlessMode: {IsEndlessMode}");
        
        StartCoroutine(ApplySettingsDelayed());
    }
    
    /// <summary>
    /// Sets up button click listeners
    /// </summary>
    private void SetupButtonListeners()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(ConfirmEndlessMode);
        }
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(HideConfirmationPanel);
        }
    }
    
    private IEnumerator ApplySettingsDelayed()
    {
        // Wait for scene to fully initialize
        yield return new WaitForEndOfFrame();
        yield return null;
        
        ApplyEndingCutsceneSettings();
    }
    
    /// <summary>
    /// Applies settings to the EndingCutsceneTrigger based on current mode
    /// </summary>
    private void ApplyEndingCutsceneSettings()
    {
        EndingCutsceneTrigger endingTrigger = FindFirstObjectByType<EndingCutsceneTrigger>();
        
        if (endingTrigger == null)
        {
            if (debugMode)
                Debug.Log("[EndlessModeManager] EndingCutsceneTrigger not found in scene");
            return;
        }
        
        if (IsEndlessMode)
        {
            // Set to endless mode scene name
            endingTrigger.snakeSceneName = configuredEndlessSceneName ?? "";
            
            if (debugMode)
                Debug.Log($"[EndlessModeManager] Endless mode applied - Scene set to: '{configuredEndlessSceneName}'");
        }
        else
        {
            // Set to regular mode scene name
            endingTrigger.snakeSceneName = configuredRegularSceneName ?? "Snake";
            
            if (debugMode)
                Debug.Log($"[EndlessModeManager] Regular mode applied - Scene set to: '{configuredRegularSceneName}'");
        }
    }
    
    /// <summary>
    /// Sets the game to regular mode (ending cutscene will load its scene)
    /// Call this from the regular Start button BEFORE MainMenuManager.OnPlayClicked()
    /// </summary>
    public void SetRegularMode()
    {
        IsEndlessMode = false;
        
        if (debugMode)
            Debug.Log("[EndlessModeManager] Regular mode set");
        
        ApplyEndingCutsceneSettings();
    }
    
    /// <summary>
    /// Shows the endless mode confirmation panel, or starts endless mode directly if no panel
    /// Call this from the Endless Mode button's OnClick event
    /// </summary>
    public void StartEndlessMode()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(true);
            
            if (debugMode)
                Debug.Log("[EndlessModeManager] Showing confirmation panel");
        }
        else
        {
            // No confirmation panel - start endless mode directly
            ConfirmEndlessMode();
        }
    }
    
    /// <summary>
    /// Hides the confirmation panel
    /// </summary>
    public void HideConfirmationPanel()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Confirms endless mode and starts the game
    /// </summary>
    public void ConfirmEndlessMode()
    {
        HideConfirmationPanel();
        
        IsEndlessMode = true;
        
        // Always log this for debugging
        Debug.Log($"[EndlessModeManager] Endless mode confirmed - IsEndlessMode is now: {IsEndlessMode}");
        
        ApplyEndingCutsceneSettings();
        
        // Start the game
        StartGame();
    }
    
    /// <summary>
    /// Starts the game via MainMenuManager
    /// </summary>
    private void StartGame()
    {
        if (mainMenuManager != null)
        {
            mainMenuManager.OnPlayClicked();
            return;
        }
        
        MainMenuManager foundManager = FindFirstObjectByType<MainMenuManager>();
        if (foundManager != null)
        {
            foundManager.OnPlayClicked();
            return;
        }
        
        if (debugMode)
            Debug.LogWarning("[EndlessModeManager] MainMenuManager not found - cannot start game");
    }
    
    /// <summary>
    /// Resets to regular mode (call when returning to main menu)
    /// </summary>
    public static void ResetToRegularMode()
    {
        IsEndlessMode = false;
    }
    
    /// <summary>
    /// Returns whether endless mode is active
    /// </summary>
    public static bool GetIsEndlessMode()
    {
        return IsEndlessMode;
    }
}