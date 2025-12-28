using UnityEngine;

/// <summary>
/// Passive ability that increases all attack range.
/// Each level adds more range multiplier.
/// Uses AbilityUpgradeData for level-based stats if assigned.
/// </summary>
public class RangeBoostAbility : BaseAbility
{
    [Header("Range Boost Settings (Fallback if no UpgradeData)")]
    [SerializeField] private float rangeMultiplierPerLevel = 0.20f; // 20% per level
    
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
        // The effectValue from upgrade data represents the range multiplier for this level
        // We'll apply it in ApplyBonus() which is called after this
    }
    
    private void ApplyBonus()
    {
        if (PlayerStats.Instance == null)
        {
            Debug.LogWarning("RangeBoostAbility: PlayerStats not found!");
            return;
        }
        
        // Use upgrade data if available, otherwise fall back to per-level calculation
        // For passive abilities, "damage" field represents the effect value (range multiplier)
        if (upgradeData != null)
        {
            currentBonus = GetDamage();
        }
        else
        {
            currentBonus = rangeMultiplierPerLevel * currentLevel;
        }
        
        PlayerStats.Instance.AddRangeMultiplier(currentBonus);
        
        Debug.Log($"RangeBoostAbility: Applied +{currentBonus * 100:F0}% range boost (Level {currentLevel})");
    }
    
    private void RemoveBonus()
    {
        if (PlayerStats.Instance == null || currentBonus == 0f) return;
        
        PlayerStats.Instance.AddRangeMultiplier(-currentBonus);
        currentBonus = 0f;
    }
    
    protected override void DeactivateAbility()
    {
        RemoveBonus();
        base.DeactivateAbility();
    }
}