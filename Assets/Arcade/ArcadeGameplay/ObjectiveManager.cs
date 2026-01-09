using UnityEngine;
using TMPro;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance;

    [Header("Startup Subtitle")]
    [TextArea(1, 3)]
    public string startSubtitle;
    public float startSubtitleDuration = 3f;

    [Header("UI")]
    public TextMeshProUGUI objectiveTextUI;
    
    [Tooltip("Parent GameObject containing the objective UI (to hide/show)")]
    public GameObject objectiveUIContainer;

    [Header("Objectives")]
    public ObjectiveTrigger[] objectives; // Drag ObjectiveTrigger objects here

    [Header("Objective Outlines")]
    [Tooltip("Automatically find and update ObjectiveOutline components on objective triggers")]
    public bool autoUpdateOutlines = true;

    private int currentIndex = 0;
    private bool hasStarted = false;
    private ObjectiveOutline[] objectiveOutlines;

    public int CurrentObjectiveIndex { get { return currentIndex; } }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("ObjectiveManager: Instance set successfully");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Hide objective UI on start - it will be shown after cutscene ends
        HideObjectiveUI();
        
        Debug.Log("ObjectiveManager: Start() called, UI hidden. Waiting for cutscene events.");
        
        // Find all ObjectiveOutline components
        if (autoUpdateOutlines)
        {
            FindObjectiveOutlines();
        }
        
        // Don't auto-start objectives or show subtitle anymore
        // These are now triggered by CutsceneController
    }
    
    /// <summary>
    /// Find all ObjectiveOutline components in the scene
    /// </summary>
    private void FindObjectiveOutlines()
    {
        objectiveOutlines = FindObjectsOfType<ObjectiveOutline>();
        Debug.Log($"ObjectiveManager: Found {objectiveOutlines.Length} ObjectiveOutline components");
    }
    
    /// <summary>
    /// Update all objective outlines to reflect the current objective
    /// </summary>
    private void UpdateAllOutlines()
    {
        if (!autoUpdateOutlines || objectiveOutlines == null) return;
        
        foreach (ObjectiveOutline outline in objectiveOutlines)
        {
            if (outline != null)
            {
                outline.CheckAndUpdateOutline();
            }
        }
    }
    
    /// <summary>
    /// Hides the objective UI container
    /// </summary>
    public void HideObjectiveUI()
    {
        if (objectiveUIContainer != null)
        {
            objectiveUIContainer.SetActive(false);
        }
        else if (objectiveTextUI != null)
        {
            objectiveTextUI.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Shows the objective UI container
    /// </summary>
    public void ShowObjectiveUI()
    {
        Debug.Log("ObjectiveManager: ShowObjectiveUI() called");
        
        if (objectiveUIContainer != null)
        {
            objectiveUIContainer.SetActive(true);
            Debug.Log($"ObjectiveManager: objectiveUIContainer '{objectiveUIContainer.name}' set active. ActiveInHierarchy: {objectiveUIContainer.activeInHierarchy}");
            
            // Check if parent Canvas is active
            Canvas parentCanvas = objectiveUIContainer.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                Debug.Log($"ObjectiveManager: Parent Canvas '{parentCanvas.name}' activeInHierarchy: {parentCanvas.gameObject.activeInHierarchy}");
            }
        }
        else if (objectiveTextUI != null)
        {
            objectiveTextUI.gameObject.SetActive(true);
            Debug.Log($"ObjectiveManager: objectiveTextUI set active. ActiveInHierarchy: {objectiveTextUI.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning("ObjectiveManager: Both objectiveUIContainer and objectiveTextUI are null!");
        }
    }
    
    /// <summary>
    /// Called by CutsceneController when the intro cutscene starts.
    /// Shows the startup subtitle.
    /// </summary>
    public void OnCutsceneStart()
    {
        Debug.Log("ObjectiveManager: OnCutsceneStart() called");
        
        if (SubtitleUI.Instance != null)
        {
            if (!string.IsNullOrEmpty(startSubtitle))
            {
                Debug.Log($"ObjectiveManager: Showing subtitle: {startSubtitle}");
                SubtitleUI.Instance.ShowSubtitle(startSubtitle, startSubtitleDuration);
            }
            else
            {
                Debug.LogWarning("ObjectiveManager: startSubtitle is empty!");
            }
        }
        else
        {
            Debug.LogWarning("ObjectiveManager: SubtitleUI.Instance is null!");
        }
    }
    
    /// <summary>
    /// Called by CutsceneController when the intro cutscene ends.
    /// Shows the objective UI and starts the first objective.
    /// </summary>
    public void OnCutsceneEnd()
    {
        Debug.Log("ObjectiveManager: OnCutsceneEnd() called");
        
        if (!hasStarted)
        {
            hasStarted = true;
            Debug.Log("ObjectiveManager: Showing objective UI and starting first objective");
            ShowObjectiveUI();
            StartObjective(0);
        }
        else
        {
            Debug.Log("ObjectiveManager: Already started, skipping");
        }
    }


    public void StartObjective(int index)
    {
        if (index >= objectives.Length)
        {
            objectiveTextUI.text = "";
            // Disable all outlines when all objectives are complete
            UpdateAllOutlines();
            return;
        }

        currentIndex = index;

        // Update top-left UI with objective name
        if (objectives[index] != null)
        {
            objectiveTextUI.text = "Objective: " + objectives[index].objectiveName;
            objectives[index].AssignObjective(index);
        }
        
        // Update outlines to show the new current objective
        UpdateAllOutlines();
    }

    public void CompleteObjective(int index)
    {
        // Only complete the currently active objective
        if (index != currentIndex) return;

        StartObjective(index + 1);
    }
}
