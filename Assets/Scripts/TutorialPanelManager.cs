
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages tutorial panels that can be shown after subtitles to display instructions.
/// Pauses the game and disables the pause menu while the tutorial is active.
/// 
/// SETUP INSTRUCTIONS:
/// 1. Create a Canvas for the tutorial panel (or use an existing UI Canvas)
/// 2. Create a Panel as a child of the Canvas with your tutorial content
/// 3. Add a TextMeshProUGUI for the instructions text
/// 4. Add a Button for the continue button
/// 5. Create an empty GameObject and add this script
/// 6. Assign the references in the Inspector
/// 
/// USAGE:
/// - Call TutorialPanelManager.Instance.ShowTutorial() to display the tutorial
/// - The panel will pause the game and disable the pause menu
/// - When the player clicks Continue, the game resumes
/// </summary>
public class TutorialPanelManager : MonoBehaviour
{
    public static TutorialPanelManager Instance { get; private set; }
    
    [Header("UI References")]
    [Tooltip("The main tutorial panel GameObject")]
    [SerializeField] private GameObject tutorialPanel;
    
    [Tooltip("The text component for displaying instructions")]
    [SerializeField] private TextMeshProUGUI instructionsText;
    
    [Tooltip("The continue button")]
    [SerializeField] private Button continueButton;
    
    [Tooltip("Optional: Title text component")]
    [SerializeField] private TextMeshProUGUI titleText;
    
    [Header("Default Content")]
    [Tooltip("Default title for the tutorial panel")]
    [SerializeField] private string defaultTitle = "How to Play";
    
    [Tooltip("Default instructions text")]
    [TextArea(5, 15)]
    [SerializeField] private string defaultInstructions = "Welcome to the game!\n\nUse WASD to move.\nUse the mouse to look around.\nCollect apples to grow.\nDefeat enemies to survive!";
    
    [Header("Settings")]
    [Tooltip("Should the game pause when the tutorial is shown?")]
    [SerializeField] private bool pauseGameWhenShown = true;
    
    [Tooltip("Should the pause menu be disabled when the tutorial is shown?")]
    [SerializeField] private bool disablePauseMenu = true;
    
    [Tooltip("Should the cursor be visible when the tutorial is shown?")]
    [SerializeField] private bool showCursor = true;
    
    [Header("Animation")]
    [Tooltip("Optional: Animator for the tutorial panel")]
    [SerializeField] private Animator panelAnimator;
    
    [Tooltip("Animation trigger name for showing the panel")]
    [SerializeField] private string showAnimTrigger = "Show";
    
    [Tooltip("Animation trigger name for hiding the panel")]
    [SerializeField] private string hideAnimTrigger = "Hide";
    
    [Tooltip("Time to wait for hide animation before deactivating")]
    [SerializeField] private float hideAnimDuration = 0.3f;
    
    [Header("References (Auto-found if not assigned)")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLookAt mouseLookAt;
    [SerializeField] private AttackManager attackManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private AbilityManager abilityManager;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private bool isTutorialActive = false;
    private bool wasTimeScaleZero = false;
    private Action onTutorialClosed;
    
    /// <summary>
    /// Event fired when the tutorial panel is shown
    /// </summary>
    public event Action OnTutorialShown;
    
    /// <summary>
    /// Event fired when the tutorial panel is closed
    /// </summary>
    public event Action OnTutorialClosed;
    
    /// <summary>
    /// Returns whether the tutorial panel is currently active
    /// </summary>
    public bool IsTutorialActive => isTutorialActive;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[TutorialPanelManager] Multiple instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        FindReferences();
        
        // Setup continue button
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        
        // Ensure panel starts hidden
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
        
        isTutorialActive = false;
    }
    
    private void FindReferences()
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
    }
    
    /// <summary>
    /// Show the tutorial panel with default content
    /// </summary>
    public void ShowTutorial(Action onClosed = null)
    {
        ShowTutorial(defaultTitle, defaultInstructions, onClosed);
    }
    
    /// <summary>
    /// Show the tutorial panel with custom content
    /// </summary>
    /// <param name="title">The title to display (optional)</param>
    /// <param name="instructions">The instructions text to display</param>
    /// <param name="onClosed">Callback when the tutorial is closed</param>
    public void ShowTutorial(string title, string instructions, Action onClosed = null)
    {
        if (isTutorialActive)
        {
            if (debugMode)
                Debug.Log("[TutorialPanelManager] Tutorial already active, ignoring show request");
            return;
        }
        
        isTutorialActive = true;
        onTutorialClosed = onClosed;
        
        if (debugMode)
            Debug.Log($"[TutorialPanelManager] Showing tutorial: {title}");
        
        // Set content
        if (titleText != null && !string.IsNullOrEmpty(title))
        {
            titleText.text = title;
        }
        
        if (instructionsText != null)
        {
            instructionsText.text = instructions;
        }
        
        // Show panel
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            
            // Play show animation if available
            if (panelAnimator != null && !string.IsNullOrEmpty(showAnimTrigger))
            {
                panelAnimator.SetTrigger(showAnimTrigger);
            }
        }
        
        // Disable pause menu
        if (disablePauseMenu)
        {
            DisablePauseMenus();
        }
        
        // Pause the game
        if (pauseGameWhenShown)
        {
            PauseGame();
        }
        
        // Show cursor
        if (showCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        OnTutorialShown?.Invoke();
    }
    
    /// <summary>
    /// Show the tutorial panel after a delay (useful for showing after subtitles)
    /// </summary>
    public void ShowTutorialAfterDelay(float delay, string title = null, string instructions = null, Action onClosed = null)
    {
        StartCoroutine(ShowTutorialDelayed(delay, title, instructions, onClosed));
    }
    
    private IEnumerator ShowTutorialDelayed(float delay, string title, string instructions, Action onClosed)
    {
        yield return new WaitForSeconds(delay);
        
        if (!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(instructions))
        {
            ShowTutorial(
                string.IsNullOrEmpty(title) ? defaultTitle : title,
                string.IsNullOrEmpty(instructions) ? defaultInstructions : instructions,
                onClosed
            );
        }
        else
        {
            ShowTutorial(onClosed);
        }
    }
    
    /// <summary>
    /// Called when the continue button is clicked
    /// </summary>
    private void OnContinueClicked()
    {
        HideTutorial();
    }
    
    /// <summary>
    /// Hide the tutorial panel and resume the game
    /// </summary>
    public void HideTutorial()
    {
        if (!isTutorialActive)
        {
            return;
        }
        
        if (debugMode)
            Debug.Log("[TutorialPanelManager] Hiding tutorial");
        
        // Play hide animation if available
        if (panelAnimator != null && !string.IsNullOrEmpty(hideAnimTrigger))
        {
            panelAnimator.SetTrigger(hideAnimTrigger);
            StartCoroutine(HidePanelAfterAnimation());
        }
        else
        {
            // No animation, hide immediately
            CompleteTutorialHide();
        }
    }
    
    private IEnumerator HidePanelAfterAnimation()
    {
        yield return new WaitForSecondsRealtime(hideAnimDuration);
        CompleteTutorialHide();
    }
    
    private void CompleteTutorialHide()
    {
        isTutorialActive = false;
        
        // Hide panel
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
        
        // Re-enable pause menu
        if (disablePauseMenu)
        {
            EnablePauseMenus();
        }
        
        // Resume the game
        if (pauseGameWhenShown)
        {
            ResumeGame();
        }
        
        // Hide cursor
        if (showCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        // Invoke callback
        onTutorialClosed?.Invoke();
        onTutorialClosed = null;
        
        OnTutorialClosed?.Invoke();
    }
    
    private void DisablePauseMenus()
    {
        // Disable SnakePauseMenu
        if (SnakePauseMenu.Instance != null)
        {
            // SnakePauseMenu doesn't have a disable method, so we'll need to track this differently
            // For now, we'll rely on the tutorial being active check
        }
        
        // Disable SnakeScenePauseManager
        if (SnakeScenePauseManager.Instance != null)
        {
            SnakeScenePauseManager.Instance.DisablePausing();
            if (debugMode)
                Debug.Log("[TutorialPanelManager] Disabled SnakeScenePauseManager");
        }
    }
    
    private void EnablePauseMenus()
    {
        // Re-enable SnakeScenePauseManager
        if (SnakeScenePauseManager.Instance != null)
        {
            SnakeScenePauseManager.Instance.EnablePausing();
            if (debugMode)
                Debug.Log("[TutorialPanelManager] Enabled SnakeScenePauseManager");
        }
    }
    
    private void PauseGame()
    {
        wasTimeScaleZero = Time.timeScale == 0f;
        Time.timeScale = 0f;
        
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
        AppleEnemy[] enemies = FindObjectsByType<AppleEnemy>(FindObjectsSortMode.None);
        foreach (AppleEnemy enemy in enemies)
        {
            if (enemy != null)
                enemy.SetFrozen(true);
        }
        
        if (debugMode)
            Debug.Log("[TutorialPanelManager] Game paused for tutorial");
    }
    
    private void ResumeGame()
    {
        if (!wasTimeScaleZero)
        {
            Time.timeScale = 1f;
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
        AppleEnemy[] enemies = FindObjectsByType<AppleEnemy>(FindObjectsSortMode.None);
        foreach (AppleEnemy enemy in enemies)
        {
            if (enemy != null)
                enemy.SetFrozen(false);
        }
        
        if (debugMode)
            Debug.Log("[TutorialPanelManager] Game resumed after tutorial");
    }
    
    private void OnDestroy()
    {
        // Ensure game is resumed if this object is destroyed while tutorial is active
        if (isTutorialActive)
        {
            Time.timeScale = 1f;
            EnablePauseMenus();
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(TutorialPanelManager))]
public class TutorialPanelManagerEditor : UnityEditor.Editor
{
    private string testTitle = "Test Tutorial";
    private string testInstructions = "This is a test tutorial.\n\nClick Continue to close.";
    
    public override void OnInspectorGUI()
    {
        TutorialPanelManager manager = (TutorialPanelManager)target;
        
        DrawDefaultInspector();
        
        UnityEditor.EditorGUILayout.Space(15);
        UnityEditor.EditorGUILayout.LabelField("Testing Tools", UnityEditor.EditorStyles.boldLabel);
        
        if (Application.isPlaying)
        {
            UnityEditor.EditorGUILayout.BeginVertical("box");
            
            testTitle = UnityEditor.EditorGUILayout.TextField("Test Title", testTitle);
            UnityEditor.EditorGUILayout.LabelField("Test Instructions:");
            testInstructions = UnityEditor.EditorGUILayout.TextArea(testInstructions, GUILayout.Height(60));
            
            if (GUILayout.Button("Show Test Tutorial"))
            {
                manager.ShowTutorial(testTitle, testInstructions);
            }
            
            if (manager.IsTutorialActive)
            {
                if (GUILayout.Button("Hide Tutorial"))
                {
                    manager.HideTutorial();
                }
            }
            
            UnityEditor.EditorGUILayout.EndVertical();
        }
        else
        {
            UnityEditor.EditorGUILayout.HelpBox("Enter Play Mode to access testing tools.", UnityEditor.MessageType.Info);
        }
    }
}
#endif