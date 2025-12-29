using UnityEngine;

/// <summary>
/// Passive ability that increases maximum health by a percentage.
/// Each level adds +10% max health.
/// Uses AbilityUpgradeData for level-based stats if assigned.
/// </summary>
public class HealthBoostAbility : BaseAbility
{
    [Header("Health Boost Settings (Fallback if no UpgradeData)")]
    [SerializeField] private float healthPercentPerLevel = 0.1f; // +10% max health per level
    
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
        // The effectValue from upgrade data represents the health percent bonus for this level
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
        // For passive abilities, "damage" field represents the effect value (health percent as decimal, e.g., 0.1 = 10%)
        if (upgradeData != null)
        {
            // If upgrade data provides a value > 1, assume it's a percentage (e.g., 10 = 10%)
            float rawValue = GetDamage();
            currentBonus = rawValue > 1f ? rawValue / 100f : rawValue;
        }
        else
        {
            currentBonus = healthPercentPerLevel * currentLevel;
        }
        
        PlayerStats.Instance.AddMaxHealthPercentBonus(currentBonus);
        
        Debug.Log($"HealthBoostAbility: Applied +{currentBonus * 100:F0}% max health (Level {currentLevel})");
    }
    
    private void RemoveBonus()
    {
        if (PlayerStats.Instance == null || currentBonus == 0f) return;
        
        PlayerStats.Instance.AddMaxHealthPercentBonus(-currentBonus);
        currentBonus = 0f;
    }
    
    protected override void DeactivateAbility()
    {
        RemoveBonus();
        base.DeactivateAbility();
    }
}