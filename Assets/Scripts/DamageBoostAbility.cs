using UnityEngine;

/// <summary>
/// Passive ability that increases all attack damage.
/// Each level adds more damage multiplier.
/// Uses AbilityUpgradeData for level-based stats if assigned.
/// </summary>
public class DamageBoostAbility : BaseAbility
{
    [Header("Damage Boost Settings (Fallback if no UpgradeData)")]
    [SerializeField] private float damageMultiplierPerLevel = 0.15f; // 15% per level
    
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
        // The effectValue from upgrade data represents the damage multiplier for this level
        // We'll apply it in ApplyBonus() which is called after this
    }
    
    private void ApplyBonus()
    {
        if (PlayerStats.Instance == null)
        {
            Debug.LogWarning("DamageBoostAbility: PlayerStats not found!");
            return;
        }
        
        // Use upgrade data if available, otherwise fall back to per-level calculation
        // For passive abilities, "damage" field represents the effect value (damage multiplier)
        if (upgradeData != null)
        {
            currentBonus = GetDamage();
        }
        else
        {
            currentBonus = damageMultiplierPerLevel * currentLevel;
        }
        
        PlayerStats.Instance.AddDamageMultiplier(currentBonus);
        
        Debug.Log($"DamageBoostAbility: Applied +{currentBonus * 100:F0}% damage boost (Level {currentLevel})");
    }
    
    private void RemoveBonus()
    {
        if (PlayerStats.Instance == null || currentBonus == 0f) return;
        
        PlayerStats.Instance.AddDamageMultiplier(-currentBonus);
        currentBonus = 0f;
    }
    
    protected override void DeactivateAbility()
    {
        RemoveBonus();
        base.DeactivateAbility();
    }
}