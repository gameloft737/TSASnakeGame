using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Represents a single tutorial page with its own panel GameObject
/// </summary>
[System.Serializable]
public class TutorialPanelPage
{
    [Tooltip("The panel GameObject for this tutorial page")]
    public GameObject panelObject;
    
    [Tooltip("The Next button on this panel (optional - will auto-find if not set)")]
    public Button nextButton;
    
    [Tooltip("The Close button on this panel (optional - will auto-find if not set)")]
    public Button closeButton;
}

/// <summary>
/// Represents a single tutorial page with title and instructions (legacy support)
/// </summary>
[System.Serializable]
public class TutorialPage
{
    [Tooltip("Title for this tutorial page")]
    public string title = "Tutorial";
    
    [Tooltip("Instructions text for this tutorial page")]
    [TextArea(5, 15)]
    public string instructions = "Instructions go here...";
}

/// <summary>
/// Manages tutorial panels that can be shown after subtitles to display instructions.
/// Supports multiple separate panel GameObjects with Next button navigation.
/// Pauses the game and disables the pause menu while the tutorial is active.
/// 
/// SETUP INSTRUCTIONS:
/// 1. Create a Canvas for the tutorial panels (or use an existing UI Canvas)
/// 2. Create separate Panel GameObjects for each tutorial page
/// 3. Each panel should have its own Next button (except the last) and Close button (on the last)
/// 4. Create an empty GameObject and add this script
/// 5. Drag your panel GameObjects into the "Tutorial Panels" list
/// 
/// USAGE:
/// - Call TutorialPanelManager.Instance.ShowTutorial() to display the tutorial panels in sequence
/// - The first panel shows, click Next to go to the next panel
/// - On the last panel, click Close to resume the game
/// </summary>
public class TutorialPanelManager : MonoBehaviour
{
    public static TutorialPanelManager Instance { get; private set; }
    
    [Header("Tutorial Panels")]
    [Tooltip("List of tutorial panel pages. Each page is a separate GameObject that will be shown in sequence.")]
    [SerializeField] private List<TutorialPanelPage> tutorialPanels = new List<TutorialPanelPage>();
    
    [Header("Settings")]
    [Tooltip("Should the game pause when the tutorial is shown?")]
    [SerializeField] private bool pauseGameWhenShown = true;
    
    [Tooltip("Should the pause menu be disabled when the tutorial is shown?")]
    [SerializeField] private bool disablePauseMenu = true;
    
    [Tooltip("Should the cursor be visible when the tutorial is shown?")]
    [SerializeField] private bool showCursor = true;
    
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
    private int currentPageIndex = 0;
    
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
    
    /// <summary>
    /// Returns the current page index (0-based)
    /// </summary>
    public int CurrentPageIndex => currentPageIndex;
    
    /// <summary>
    /// Returns the total number of pages in the current tutorial
    /// </summary>
    public int TotalPages => tutorialPanels?.Count ?? 0;
    
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
        SetupPanelButtons();
        HideAllPanels();
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
    /// Setup button listeners for all tutorial panels
    /// </summary>
    private void SetupPanelButtons()
    {
        for (int i = 0; i < tutorialPanels.Count; i++)
        {
            TutorialPanelPage page = tutorialPanels[i];
            if (page.panelObject == null) continue;
            
            // Auto-find buttons if not assigned
            if (page.nextButton == null)
            {
                Button[] buttons = page.panelObject.GetComponentsInChildren<Button>(true);
                foreach (Button btn in buttons)
                {
                    if (btn.gameObject.name.ToLower().Contains("next"))
                    {
                        page.nextButton = btn;
                        break;
                    }
                }
            }
            
            if (page.closeButton == null)
            {
                Button[] buttons = page.panelObject.GetComponentsInChildren<Button>(true);
                foreach (Button btn in buttons)
                {
                    if (btn.gameObject.name.ToLower().Contains("close"))
                    {
                        page.closeButton = btn;
                        break;
                    }
                }
            }
            
            // Add listeners
            if (page.nextButton != null)
            {
                page.nextButton.onClick.RemoveAllListeners();
                page.nextButton.onClick.AddListener(() => OnNextClicked());
            }
            
            if (page.closeButton != null)
            {
                page.closeButton.onClick.RemoveAllListeners();
                page.closeButton.onClick.AddListener(() => OnCloseClicked());
            }
        }
    }
    
    /// <summary>
    /// Hide all tutorial panels
    /// </summary>
    private void HideAllPanels()
    {
        foreach (TutorialPanelPage page in tutorialPanels)
        {
            if (page.panelObject != null)
            {
                page.panelObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Show the tutorial using the configured panel GameObjects
    /// </summary>
    public void ShowTutorial(Action onClosed = null)
    {
        if (isTutorialActive)
        {
            if (debugMode)
                Debug.Log("[TutorialPanelManager] Tutorial already active, ignoring show request");
            return;
        }
        
        if (tutorialPanels.Count == 0)
        {
            Debug.LogWarning("[TutorialPanelManager] No tutorial panels configured!");
            return;
        }
        
        isTutorialActive = true;
        onTutorialClosed = onClosed;
        currentPageIndex = 0;
        
        if (debugMode)
            Debug.Log("[TutorialPanelManager] Showing tutorial with " + tutorialPanels.Count + " panels");
        
        // Hide all panels first
        HideAllPanels();
        
        // Show the first panel
        DisplayCurrentPanel();
        
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
    /// Show the tutorial panel with a single page (legacy support for LevelUIManager)
    /// </summary>
    public void ShowTutorial(string title, string instructions, Action onClosed = null)
    {
        // For legacy support, just show the panel-based tutorial
        ShowTutorial(onClosed);
    }
    
    /// <summary>
    /// Display the current panel and hide others
    /// </summary>
    private void DisplayCurrentPanel()
    {
        if (tutorialPanels == null || currentPageIndex < 0 || currentPageIndex >= tutorialPanels.Count)
            return;
        
        // Hide all panels
        HideAllPanels();
        
        // Show current panel
        TutorialPanelPage currentPage = tutorialPanels[currentPageIndex];
        if (currentPage.panelObject != null)
        {
            currentPage.panelObject.SetActive(true);
        }
        
        if (debugMode)
            Debug.Log("[TutorialPanelManager] Displaying panel " + (currentPageIndex + 1) + "/" + tutorialPanels.Count);
    }
    
    /// <summary>
    /// Called when the Next button is clicked
    /// </summary>
    private void OnNextClicked()
    {
        if (currentPageIndex < tutorialPanels.Count - 1)
        {
            currentPageIndex++;
            DisplayCurrentPanel();
            
            if (debugMode)
                Debug.Log("[TutorialPanelManager] Advanced to panel " + (currentPageIndex + 1));
        }
    }
    
    /// <summary>
    /// Called when the Close button is clicked
    /// </summary>
    private void OnCloseClicked()
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
        
        CompleteTutorialHide();
    }
    
    private void CompleteTutorialHide()
    {
        isTutorialActive = false;
        currentPageIndex = 0;
        
        // Hide all panels
        HideAllPanels();
        
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
        if (SnakeScenePauseManager.Instance != null)
        {
            SnakeScenePauseManager.Instance.DisablePausing();
            if (debugMode)
                Debug.Log("[TutorialPanelManager] Disabled SnakeScenePauseManager");
        }
    }
    
    private void EnablePauseMenus()
    {
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
        
        if (debugMode)
            Debug.Log("[TutorialPanelManager] Game paused for tutorial");
    }
    
    private void ResumeGame()
    {
        if (!wasTimeScaleZero)
        {
            Time.timeScale = 1f;
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
        
        if (debugMode)
            Debug.Log("[TutorialPanelManager] Game resumed after tutorial");
    }
    
    private void OnDestroy()
    {
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
    public override void OnInspectorGUI()
    {
        TutorialPanelManager manager = (TutorialPanelManager)target;
        
        DrawDefaultInspector();
        
        UnityEditor.EditorGUILayout.Space(15);
        UnityEditor.EditorGUILayout.LabelField("Testing Tools", UnityEditor.EditorStyles.boldLabel);
        
        if (Application.isPlaying)
        {
            UnityEditor.EditorGUILayout.BeginVertical("box");
            
            UnityEditor.EditorGUILayout.LabelField("Current State:", manager.IsTutorialActive ? "Active" : "Inactive");
            if (manager.IsTutorialActive)
            {
                UnityEditor.EditorGUILayout.LabelField("Page:", (manager.CurrentPageIndex + 1) + " / " + manager.TotalPages);
            }
            
            UnityEditor.EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Show Tutorial"))
            {
                manager.ShowTutorial();
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