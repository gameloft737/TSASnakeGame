using UnityEngine;
using TMPro;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance;

    [Header("UI")]
    public TextMeshProUGUI objectiveTextUI;

    [Header("Objectives")]
    public Objective[] objectives;

    private int currentIndex = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        StartObjective(0);
    }

    public void StartObjective(int index)
    {
        if (index >= objectives.Length)
        {
            objectiveTextUI.text = "All Objectives Complete!";
            return;
        }

        currentIndex = index;

        objectiveTextUI.text = "Objective: " + objectives[index].objectiveText;

        // Register this objective's trigger
        if (objectives[index].trigger != null)
            objectives[index].trigger.AssignObjective(index);
    }

    public void CompleteObjective(int index)
    {
        if (index != currentIndex) return; // Only complete active objective

        Debug.Log($"Objective {index} complete!");

        StartObjective(index + 1);
    }

    
}
