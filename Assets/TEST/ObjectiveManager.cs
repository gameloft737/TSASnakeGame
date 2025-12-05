using UnityEngine;
using TMPro;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance;

    [Header("UI")]
    public TextMeshProUGUI objectiveTextUI;

    [Header("Objectives")]
    public ObjectiveTrigger[] objectives; // Drag ObjectiveTrigger objects here

    private int currentIndex = 0;

    public int CurrentObjectiveIndex { get { return currentIndex; } }

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
            objectiveTextUI.text = "";
            return;
        }

        currentIndex = index;

        // Update top-left UI with objective name
        if (objectives[index] != null)
        {
            objectiveTextUI.text = "Objective: " + objectives[index].objectiveName;
            objectives[index].AssignObjective(index);
        }
    }

    public void CompleteObjective(int index)
    {
        // Only complete the currently active objective
        if (index != currentIndex) return;

        StartObjective(index + 1);
    }
}
