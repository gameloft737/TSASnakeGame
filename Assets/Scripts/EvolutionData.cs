using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines an evolution requirement - a passive ability that unlocks additional levels
/// </summary>
[System.Serializable]
public class EvolutionRequirement
{
    [Header("Required Passive")]
    [Tooltip("The passive ability prefab required for this evolution")]
    public GameObject requiredPassivePrefab;
    
    [Tooltip("Minimum level of the passive required (0 = just needs to be owned)")]
    public int requiredPassiveLevel = 1;
    
    [Header("Evolution Unlock")]
    [Tooltip("The level that gets unlocked when this evolution is achieved")]
    public int unlockedLevel = 7;
    
    [Tooltip("Name of this evolution (for UI display)")]
    public string evolutionName = "Evolution";
    
    [TextArea(2, 4)]
    [Tooltip("Description of what this evolution does")]
    public string evolutionDescription = "Unlocks a powerful new form!";
    
    [Header("Evolution Visual Changes")]
    [Tooltip("Material to apply to the snake head when this evolution is active")]
    public Material evolutionHeadMaterial;
    
    [Tooltip("Material to apply to the snake body when this evolution is active")]
    public Material evolutionBodyMaterial;
    
    [Tooltip("Optional attachment object to enable when this evolution is active")]
    public GameObject evolutionAttachment;
}

/// <summary>
/// ScriptableObject that defines evolution paths for attacks or active abilities
/// </summary>
[CreateAssetMenu(fileName = "NewEvolutionData", menuName = "Upgrades/Evolution Data")]
public class EvolutionData : ScriptableObject
{
    [Header("Evolution Info")]
    [Tooltip("Name of the base attack/ability this evolution applies to")]
    public string baseName;
    
    [TextArea(2, 4)]
    public string evolutionSystemDescription = "Combine with passive abilities to unlock powerful evolutions!";
    
    [Header("Evolution Requirements")]
    [Tooltip("List of possible evolutions. Each passive can unlock a different evolution level.")]
    public List<EvolutionRequirement> evolutions = new List<EvolutionRequirement>();
    
    /// <summary>
    /// Checks if a specific evolution is unlocked based on owned passives
    /// </summary>
    public bool IsEvolutionUnlocked(EvolutionRequirement evolution, AbilityManager abilityManager)
    {
        if (abilityManager == null || evolution.requiredPassivePrefab == null)
            return false;
        
        int passiveLevel = abilityManager.GetAbilityLevel(evolution.requiredPassivePrefab);
        return passiveLevel >= evolution.requiredPassiveLevel;
    }
    
    /// <summary>
    /// Gets the highest unlocked level based on current passives
    /// </summary>
    public int GetMaxUnlockedLevel(int baseMaxLevel, AbilityManager abilityManager)
    {
        int maxLevel = baseMaxLevel;
        
        foreach (var evolution in evolutions)
        {
            if (IsEvolutionUnlocked(evolution, abilityManager))
            {
                maxLevel = Mathf.Max(maxLevel, evolution.unlockedLevel);
            }
        }
        
        return maxLevel;
    }
    
    /// <summary>
    /// Gets all currently unlocked evolutions
    /// </summary>
    public List<EvolutionRequirement> GetUnlockedEvolutions(AbilityManager abilityManager)
    {
        List<EvolutionRequirement> unlocked = new List<EvolutionRequirement>();
        
        foreach (var evolution in evolutions)
        {
            if (IsEvolutionUnlocked(evolution, abilityManager))
            {
                unlocked.Add(evolution);
            }
        }
        
        return unlocked;
    }
    
    /// <summary>
    /// Gets all evolutions that are not yet unlocked (for UI hints)
    /// </summary>
    public List<EvolutionRequirement> GetLockedEvolutions(AbilityManager abilityManager)
    {
        List<EvolutionRequirement> locked = new List<EvolutionRequirement>();
        
        foreach (var evolution in evolutions)
        {
            if (!IsEvolutionUnlocked(evolution, abilityManager))
            {
                locked.Add(evolution);
            }
        }
        
        return locked;
    }
    
    /// <summary>
    /// Gets the evolution requirement for a specific level
    /// </summary>
    public EvolutionRequirement GetEvolutionForLevel(int level)
    {
        foreach (var evolution in evolutions)
        {
            if (evolution.unlockedLevel == level)
            {
                return evolution;
            }
        }
        return null;
    }
}