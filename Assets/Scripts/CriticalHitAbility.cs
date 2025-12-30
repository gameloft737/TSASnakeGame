using UnityEngine;

/// <summary>
/// Passive ability that increases critical hit chance and damage.
/// Each level adds more crit chance and optionally crit damage.
/// Uses AbilityUpgradeData for level-based stats if assigned.
/// </summary>
public class CriticalHitAbility : BaseAbility
{
    [Header("Critical Hit Settings (Fallback if no UpgradeData)")]
    [SerializeField] private float critChancePerLevel = 0.05f; // 5% per level
    [SerializeField] private float critMultiplierPerLevel = 0.1f; // +0.1x crit damage per level
    
    private float currentCritChanceBonus = 0f;
    private float currentCritMultiplierBonus = 0f;
    
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
        // The effectValue from upgrade data represents the crit chance for this level
        // Custom stats can be used for crit multiplier bonus
    }
    
    private void ApplyBonus()
    {
        if (PlayerStats.Instance == null)
        {
            Debug.LogWarning("CriticalHitAbility: PlayerStats not found!");
            return;
        }
        
        // Use upgrade data if available, otherwise fall back to per-level calculation
        if (upgradeData != null)
        {
            // "damage" field represents crit chance
            currentCritChanceBonus = GetDamage();
            // Use custom stat for crit multiplier if available
            currentCritMultiplierBonus = GetCustomStat("critMultiplier", critMultiplierPerLevel * currentLevel);
        }
        else
        {
            currentCritChanceBonus = critChancePerLevel * currentLevel;
            currentCritMultiplierBonus = critMultiplierPerLevel * currentLevel;
        }
        
        PlayerStats.Instance.AddCritChance(currentCritChanceBonus);
        PlayerStats.Instance.AddCritMultiplier(currentCritMultiplierBonus);
        
        Debug.Log($"CriticalHitAbility: Applied +{currentCritChanceBonus * 100:F0}% crit chance, +{currentCritMultiplierBonus:F1}x crit damage (Level {currentLevel})");
    }
    
    private void RemoveBonus()
    {
        if (PlayerStats.Instance == null) return;
        
        if (currentCritChanceBonus != 0f)
        {
            PlayerStats.Instance.AddCritChance(-currentCritChanceBonus);
            currentCritChanceBonus = 0f;
        }
        
        if (currentCritMultiplierBonus != 0f)
        {
            PlayerStats.Instance.AddCritMultiplier(-currentCritMultiplierBonus);
            currentCritMultiplierBonus = 0f;
        }
    }
    
    protected override void DeactivateAbility()
    {
        RemoveBonus();
        base.DeactivateAbility();
    }
}