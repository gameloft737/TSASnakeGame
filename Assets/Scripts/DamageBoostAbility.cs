using UnityEngine;

/// <summary>
/// Passive ability that increases all attack damage.
/// Each level adds more damage multiplier.
/// </summary>
public class DamageBoostAbility : BaseAbility
{
    [Header("Damage Boost Settings")]
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
    
    private void ApplyBonus()
    {
        if (PlayerStats.Instance == null)
        {
            Debug.LogWarning("DamageBoostAbility: PlayerStats not found!");
            return;
        }
        
        currentBonus = damageMultiplierPerLevel * currentLevel;
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