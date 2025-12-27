using UnityEngine;

/// <summary>
/// Passive ability that increases all attack range.
/// Each level adds more range multiplier.
/// </summary>
public class RangeBoostAbility : BaseAbility
{
    [Header("Range Boost Settings")]
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
    
    private void ApplyBonus()
    {
        if (PlayerStats.Instance == null)
        {
            Debug.LogWarning("RangeBoostAbility: PlayerStats not found!");
            return;
        }
        
        currentBonus = rangeMultiplierPerLevel * currentLevel;
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