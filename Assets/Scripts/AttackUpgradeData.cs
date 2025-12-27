using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines the stats for a single level of an attack
/// </summary>
[System.Serializable]
public class AttackLevelStats
{
    [Header("Level Info")]
    public string levelName = "Level 1";
    [TextArea(2, 4)]
    public string description = "Base attack";
    
    [Header("Damage")]
    public float damage = 10f;
    
    [Header("Range")]
    [Tooltip("The range/distance of the attack")]
    public float range = 10f;
    
    [Header("Fuel System")]
    public float minFuelToActivate = 50f;
    public float fuelRechargeRate = 20f;
    public float burstFuelCost = 15f;
    public float continuousDrainRate = 10f;
    
    [Header("Attack-Specific Stats")]
    [Tooltip("Custom float values that specific attacks can use (e.g., laser width, fire spread, etc.)")]
    public List<CustomStat> customStats = new List<CustomStat>();
}

/// <summary>
/// A custom stat that can be defined per attack type
/// </summary>
[System.Serializable]
public class CustomStat
{
    public string statName;
    public float value;
}

/// <summary>
/// ScriptableObject that holds all level data for an attack
/// </summary>
[CreateAssetMenu(fileName = "NewAttackUpgradeData", menuName = "Attacks/Attack Upgrade Data")]
public class AttackUpgradeData : ScriptableObject
{
    [Header("Attack Info")]
    public string attackName;
    [TextArea(2, 4)]
    public string attackDescription;
    public Sprite attackIcon;
    
    [Header("Level Stats")]
    [Tooltip("Define stats for each level. Index 0 = Level 1, Index 1 = Level 2, etc.")]
    public List<AttackLevelStats> levels = new List<AttackLevelStats>();
    
    [Header("Max Level")]
    public int maxLevel = 5;
    
    /// <summary>
    /// Gets the stats for a specific level (1-indexed)
    /// </summary>
    public AttackLevelStats GetStatsForLevel(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, levels.Count - 1);
        if (levels.Count == 0)
        {
            Debug.LogWarning($"AttackUpgradeData '{attackName}' has no levels defined!");
            return new AttackLevelStats();
        }
        return levels[index];
    }
    
    /// <summary>
    /// Gets a custom stat value for a specific level
    /// </summary>
    public float GetCustomStat(int level, string statName, float defaultValue = 0f)
    {
        AttackLevelStats stats = GetStatsForLevel(level);
        foreach (CustomStat stat in stats.customStats)
        {
            if (stat.statName == statName)
            {
                return stat.value;
            }
        }
        return defaultValue;
    }
    
    /// <summary>
    /// Checks if the attack can be upgraded further
    /// </summary>
    public bool CanUpgrade(int currentLevel)
    {
        return currentLevel < maxLevel && currentLevel < levels.Count;
    }
}