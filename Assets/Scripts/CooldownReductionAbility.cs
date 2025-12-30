using UnityEngine;

/// <summary>
/// Passive ability that reduces cooldowns on all abilities.
/// Each level adds more cooldown reduction.
/// Uses AbilityUpgradeData for level-based stats if assigned.
/// </summary>
public class CooldownReductionAbility : BaseAbility
{
    [Header("Cooldown Reduction Settings (Fallback if no UpgradeData)")]
    [SerializeField] private float cooldownReductionPerLevel = 0.08f; // 8% per level
    
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
        // The effectValue from upgrade data represents the cooldown reduction for this level
        // We'll apply it in ApplyBonus() which is called after this
    }
    
    private void ApplyBonus()
    {
        if (PlayerStats.Instance == null)
        {
            Debug.LogWarning("CooldownReductionAbility: PlayerStats not found!");
            return;
        }
        
        // Use upgrade data if available, otherwise fall back to per-level calculation
        // For passive abilities, "damage" field represents the effect value (cooldown reduction)
        if (upgradeData != null)
        {
            currentBonus = GetDamage();
        }
        else
        {
            currentBonus = cooldownReductionPerLevel * currentLevel;
        }
        
        PlayerStats.Instance.AddCooldownReduction(currentBonus);
        
        Debug.Log($"CooldownReductionAbility: Applied +{currentBonus * 100:F0}% cooldown reduction (Level {currentLevel})");
    }
    
    private void RemoveBonus()
    {
        if (PlayerStats.Instance == null || currentBonus == 0f) return;
        
        PlayerStats.Instance.AddCooldownReduction(-currentBonus);
        currentBonus = 0f;
    }
    
    protected override void DeactivateAbility()
    {
        RemoveBonus();
        base.DeactivateAbility();
    }
}