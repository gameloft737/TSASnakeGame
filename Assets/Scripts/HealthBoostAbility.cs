using UnityEngine;

/// <summary>
/// Passive ability that increases maximum health.
/// Each level adds more max health.
/// </summary>
public class HealthBoostAbility : BaseAbility
{
    [Header("Health Boost Settings")]
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
    
    private void ApplyBonus()
    {
        if (PlayerStats.Instance == null)
        {
            Debug.LogWarning("HealthBoostAbility: PlayerStats not found!");
            return;
        }
        
        currentBonus = healthBonusPerLevel * currentLevel;
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