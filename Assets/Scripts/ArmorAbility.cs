using UnityEngine;

/// <summary>
/// Passive ability that reduces incoming damage.
/// Each level adds more damage reduction.
/// Uses AbilityUpgradeData for level-based stats if assigned.
/// </summary>
public class ArmorAbility : BaseAbility
{
    [Header("Armor Settings (Fallback if no UpgradeData)")]
    [SerializeField] private float damageReductionPerLevel = 0.05f; // 5% per level
    
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
        // The effectValue from upgrade data represents the damage reduction for this level
        // We'll apply it in ApplyBonus() which is called after this
    }
    
    private void ApplyBonus()
    {
        if (PlayerStats.Instance == null)
        {
            Debug.LogWarning("ArmorAbility: PlayerStats not found!");
            return;
        }
        
        // Use upgrade data if available, otherwise fall back to per-level calculation
        // For passive abilities, "damage" field represents the effect value (damage reduction)
        if (upgradeData != null)
        {
            currentBonus = GetDamage();
        }
        else
        {
            currentBonus = damageReductionPerLevel * currentLevel;
        }
        
        PlayerStats.Instance.AddDamageReduction(currentBonus);
        
        Debug.Log($"ArmorAbility: Applied +{currentBonus * 100:F0}% damage reduction (Level {currentLevel})");
    }
    
    private void RemoveBonus()
    {
        if (PlayerStats.Instance == null || currentBonus == 0f) return;
        
        PlayerStats.Instance.AddDamageReduction(-currentBonus);
        currentBonus = 0f;
    }
    
    protected override void DeactivateAbility()
    {
        RemoveBonus();
        base.DeactivateAbility();
    }
}