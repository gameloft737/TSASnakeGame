using UnityEngine;

/// <summary>
/// Passive ability that increases maximum health.
/// Each level adds more max health.
/// Uses AbilityUpgradeData for level-based stats if assigned.
/// </summary>
public class HealthBoostAbility : BaseAbility
{
    [Header("Health Boost Settings (Fallback if no UpgradeData)")]
    [SerializeField] private float healthBonusPerLevel = 25f; // +25 max health per level
    
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
        // The effectValue from upgrade data represents the health bonus for this level
        // We'll apply it in ApplyBonus() which is called after this
    }
    
    private void ApplyBonus()
    {
        if (PlayerStats.Instance == null)
        {
            Debug.LogWarning("HealthBoostAbility: PlayerStats not found!");
            return;
        }
        
        // Use upgrade data if available, otherwise fall back to per-level calculation
        // For passive abilities, "damage" field represents the effect value (health bonus)
        if (upgradeData != null)
        {
            currentBonus = GetDamage();
        }
        else
        {
            currentBonus = healthBonusPerLevel * currentLevel;
        }
        
        PlayerStats.Instance.AddMaxHealthBonus(currentBonus);
        
        Debug.Log($"HealthBoostAbility: Applied +{currentBonus:F0} max health (Level {currentLevel})");
    }
    
    private void RemoveBonus()
    {
        if (PlayerStats.Instance == null || currentBonus == 0f) return;
        
        PlayerStats.Instance.AddMaxHealthBonus(-currentBonus);
        currentBonus = 0f;
    }
    
    protected override void DeactivateAbility()
    {
        RemoveBonus();
        base.DeactivateAbility();
    }
}