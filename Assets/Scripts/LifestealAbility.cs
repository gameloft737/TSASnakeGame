using UnityEngine;

/// <summary>
/// Passive ability that heals the player based on damage dealt.
/// Each level adds more lifesteal percentage.
/// Uses AbilityUpgradeData for level-based stats if assigned.
/// </summary>
public class LifestealAbility : BaseAbility
{
    [Header("Lifesteal Settings (Fallback if no UpgradeData)")]
    [SerializeField] private float lifestealPerLevel = 0.05f; // 5% per level
    
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
        // The effectValue from upgrade data represents the lifesteal percentage for this level
        // We'll apply it in ApplyBonus() which is called after this
    }
    
    private void ApplyBonus()
    {
        if (PlayerStats.Instance == null)
        {
            Debug.LogWarning("LifestealAbility: PlayerStats not found!");
            return;
        }
        
        // Use upgrade data if available, otherwise fall back to per-level calculation
        // For passive abilities, "damage" field represents the effect value (lifesteal percentage)
        if (upgradeData != null)
        {
            currentBonus = GetDamage();
        }
        else
        {
            currentBonus = lifestealPerLevel * currentLevel;
        }
        
        PlayerStats.Instance.AddLifesteal(currentBonus);
        
        Debug.Log($"LifestealAbility: Applied +{currentBonus * 100:F0}% lifesteal (Level {currentLevel})");
    }
    
    private void RemoveBonus()
    {
        if (PlayerStats.Instance == null || currentBonus == 0f) return;
        
        PlayerStats.Instance.AddLifesteal(-currentBonus);
        currentBonus = 0f;
    }
    
    protected override void DeactivateAbility()
    {
        RemoveBonus();
        base.DeactivateAbility();
    }
}