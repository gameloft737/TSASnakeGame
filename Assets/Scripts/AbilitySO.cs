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
    
    [Header("Upgrade Data")]
    [Tooltip("Optional: Reference to detailed upgrade data for this ability")]
    public AbilityUpgradeData upgradeData;
    
    /// <summary>
    /// Gets the description for a specific level
    /// </summary>
    public string GetDescriptionForLevel(int level)
    {
        if (upgradeData != null)
        {
            return upgradeData.GetDescription(level);
        }
        return description;
    }
    
    /// <summary>
    /// Gets the damage value for a specific level
    /// </summary>
    public float GetDamage(int level)
    {
        if (upgradeData != null)
        {
            return upgradeData.GetDamage(level);
        }
        return 0f;
    }
    
    /// <summary>
    /// Gets the cooldown value for a specific level
    /// </summary>
    public float GetCooldown(int level)
    {
        if (upgradeData != null)
        {
            return upgradeData.GetCooldown(level);
        }
        return 0f;
    }
    
    /// <summary>
    /// Gets the duration value for a specific level
    /// </summary>
    public float GetDuration(int level)
    {
        if (upgradeData != null)
        {
            return upgradeData.GetDuration(level);
        }
        return 0f;
    }
    
    /// <summary>
    /// Gets a custom stat value for a specific level
    /// </summary>
    public float GetCustomStat(int level, string statName, float defaultValue = 0f)
    {
        if (upgradeData != null)
        {
            return upgradeData.GetCustomStat(level, statName, defaultValue);
        }
        return defaultValue;
    }
    
    /// <summary>
    /// Checks if the ability can be upgraded further
    /// </summary>
    public bool CanUpgrade(int currentLevel)
    {
        if (upgradeData != null)
        {
            return upgradeData.CanUpgrade(currentLevel);
        }
        return currentLevel < maxLevel;
    }
}