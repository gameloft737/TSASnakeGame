using UnityEngine;

[System.Serializable]
public class Objective
{
    [TextArea] public string objectiveText;
    public ObjectiveTrigger trigger;   // Assigned in Inspector
}
