using UnityEngine;

public enum AbilityType
{
    Passive,
    Active
}

[CreateAssetMenu(fileName = "New Ability", menuName = "Abilities/Ability Data")]
public class AbilitySO : ScriptableObject
{
    public string abilityName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;
    public GameObject abilityPrefab;
    
    [Header("Ability Classification")]
    public AbilityType abilityType = AbilityType.Passive;
    
    [Header("Level Settings")]
    [Tooltip("Maximum level this ability can reach")]
    public int maxLevel = 3;
}