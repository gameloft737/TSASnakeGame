using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines the stats for a single level of an ability
/// </summary>
[System.Serializable]
public class AbilityLevelStats
{
    [Header("Level Info")]
    public string levelName = "Level 1";
    [TextArea(2, 4)]
    public string description = "Base ability";
    
    [Header("Core Stats")]
    [Tooltip("Damage dealt by this ability (for turret projectiles, bombs, etc.)")]
    public float damage = 10f;
    
    [Tooltip("Cooldown time in seconds between uses (for turret shoot interval, bomb cooldown, etc.)")]
    public float cooldown = 1f;
    
    [Tooltip("Duration of the effect in seconds (for goop puddles, buffs, etc.)")]
    public float duration = 5f;
    
    [Header("Ability-Specific Stats")]
    [Tooltip("Custom float values that specific abilities can use (e.g., projectileCount, explosionRadius)")]
    public List<AbilityCustomStat> customStats = new List<AbilityCustomStat>();
}

/// <summary>
/// A custom stat that can be defined per ability type
/// </summary>
[System.Serializable]
public class AbilityCustomStat
{
    public string statName;
    public float value;
}

/// <summary>
/// ScriptableObject that holds all level data for an ability
/// </summary>
[CreateAssetMenu(fileName = "NewAbilityUpgradeData", menuName = "Abilities/Ability Upgrade Data")]
public class AbilityUpgradeData : ScriptableObject
{
    [Header("Ability Info")]
    public string abilityName;
    [TextArea(2, 4)]
    public string abilityDescription;
    public Sprite abilityIcon;
    
    [Header("Ability Classification")]
    public AbilityType abilityType = AbilityType.Passive;
    
    [Header("Level Stats")]
    [Tooltip("Define stats for each level. Index 0 = Level 1, Index 1 = Level 2, etc. Include evolution levels here too!")]
    public List<AbilityLevelStats> levels = new List<AbilityLevelStats>();
    
    [Header("Max Level")]
    [Tooltip("Base max level without evolutions")]
    public int maxLevel = 5;
    
    [Header("Evolution System")]
    [Tooltip("Optional: Evolution data that unlocks additional levels when paired with passives (for active abilities)")]
    public EvolutionData evolutionData;
    
    /// <summary>
    /// Gets the stats for a specific level (1-indexed)
    /// </summary>
    public AbilityLevelStats GetStatsForLevel(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, levels.Count - 1);
        if (levels.Count == 0)
        {
            Debug.LogWarning($"AbilityUpgradeData '{abilityName}' has no levels defined!");
            return new AbilityLevelStats();
        }
        return levels[index];
    }
    
    /// <summary>
    /// Gets a custom stat value for a specific level
    /// </summary>
    public float GetCustomStat(int level, string statName, float defaultValue = 0f)
    {
        AbilityLevelStats stats = GetStatsForLevel(level);
        foreach (AbilityCustomStat stat in stats.customStats)
        {
            if (stat.statName == statName)
            {
                return stat.value;
            }
        }
        return defaultValue;
    }
    
    /// <summary>
    /// Gets the damage for a specific level
    /// </summary>
    public float GetDamage(int level)
    {
        return GetStatsForLevel(level).damage;
    }
    
    /// <summary>
    /// Gets the cooldown for a specific level
    /// </summary>
    public float GetCooldown(int level)
    {
        return GetStatsForLevel(level).cooldown;
    }
    
    /// <summary>
    /// Gets the duration for a specific level
    /// </summary>
    public float GetDuration(int level)
    {
        return GetStatsForLevel(level).duration;
    }
    
    /// <summary>
    /// Gets the description for a specific level
    /// </summary>
    public string GetDescription(int level)
    {
        return GetStatsForLevel(level).description;
    }
    
    /// <summary>
    /// Checks if the ability can be upgraded further (without considering evolutions)
    /// </summary>
    public bool CanUpgrade(int currentLevel)
    {
        return currentLevel < maxLevel && currentLevel < levels.Count;
    }
    
    /// <summary>
    /// Checks if the ability can be upgraded further (considering evolutions)
    /// </summary>
    public bool CanUpgradeWithEvolutions(int currentLevel, AbilityManager abilityManager)
    {
        int effectiveMaxLevel = GetEffectiveMaxLevel(abilityManager);
        return currentLevel < effectiveMaxLevel && currentLevel < levels.Count;
    }
    
    /// <summary>
    /// Gets the effective max level considering unlocked evolutions
    /// </summary>
    public int GetEffectiveMaxLevel(AbilityManager abilityManager)
    {
        if (evolutionData == null || abilityManager == null)
            return maxLevel;
        
        return evolutionData.GetMaxUnlockedLevel(maxLevel, abilityManager);
    }
    
    /// <summary>
    /// Gets all unlocked evolutions for this ability
    /// </summary>
    public List<EvolutionRequirement> GetUnlockedEvolutions(AbilityManager abilityManager)
    {
        if (evolutionData == null)
            return new List<EvolutionRequirement>();
        
        return evolutionData.GetUnlockedEvolutions(abilityManager);
    }
    
    /// <summary>
    /// Gets all locked evolutions for this ability (for UI hints)
    /// </summary>
    public List<EvolutionRequirement> GetLockedEvolutions(AbilityManager abilityManager)
    {
        if (evolutionData == null)
            return new List<EvolutionRequirement>();
        
        return evolutionData.GetLockedEvolutions(abilityManager);
    }
    
    /// <summary>
    /// Checks if a specific level is an evolution level
    /// </summary>
    public bool IsEvolutionLevel(int level)
    {
        return level > maxLevel;
    }
    
    /// <summary>
    /// Gets the evolution requirement for a specific level
    /// </summary>
    public EvolutionRequirement GetEvolutionForLevel(int level)
    {
        if (evolutionData == null)
            return null;
        
        return evolutionData.GetEvolutionForLevel(level);
    }
}