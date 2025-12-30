using UnityEngine;

/// <summary>
/// Passive ability that increases XP gained from all sources.
/// Each level adds more XP multiplier.
/// Uses AbilityUpgradeData for level-based stats if assigned.
/// </summary>
public class XPBoostAbility : BaseAbility
{
    [Header("XP Boost Settings (Fallback if no UpgradeData)")]
    [SerializeField] private float xpMultiplierPerLevel = 0.10f; // 10% per level
    
    private float currentBonus = 0f;
    
    protected override void ActivateAbility()
    {
        base.ActivateAbility();
        ApplyBonus();
    }
    
    protected override void OnLevelUp()
    {
        base.OnLevelUp();
        // Remove old bonus and apply new one
        RemoveBonus();
        ApplyBonus();
    }
    
    /// <summary>
    /// Applies custom stats from the upgrade data
    /// </summary>
    protected override void ApplyCustomStats(AbilityLevelStats stats)
    {
        // The effectValue from upgrade data represents the XP multiplier for this level
        // We'll apply it in ApplyBonus() which is called after this
    }
    
    private void ApplyBonus()
    {
        if (PlayerStats.Instance == null)
        {
            Debug.LogWarning("XPBoostAbility: PlayerStats not found!");
            return;
        }
        
        // Use upgrade data if available, otherwise fall back to per-level calculation
        // For passive abilities, "damage" field represents the effect value (XP multiplier)
        if (upgradeData != null)
        {
            currentBonus = GetDamage();
        }
        else
        {
            currentBonus = xpMultiplierPerLevel * currentLevel;
        }
        
        PlayerStats.Instance.AddXPMultiplier(currentBonus);
        
        Debug.Log($"XPBoostAbility: Applied +{currentBonus * 100:F0}% XP boost (Level {currentLevel})");
    }
    
    private void RemoveBonus()
    {
        if (PlayerStats.Instance == null || currentBonus == 0f) return;
        
        PlayerStats.Instance.AddXPMultiplier(-currentBonus);
        currentBonus = 0f;
    }
    
    protected override void DeactivateAbility()
    {
        RemoveBonus();
        base.DeactivateAbility();
    }
}