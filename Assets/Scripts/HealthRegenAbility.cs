using UnityEngine;

/// <summary>
/// Passive ability that provides health regeneration over time.
/// Each level adds more health regen per second.
/// </summary>
public class HealthRegenAbility : BaseAbility
{
    [Header("Health Regen Settings")]
    [SerializeField] private float regenPerSecondPerLevel = 2f; // +2 HP/sec per level
    
    private float currentBonus = 0f;
    private SnakeHealth snakeHealth;
    
    protected override void Awake()
    {
        base.Awake();
        snakeHealth = FindFirstObjectByType<SnakeHealth>();
    }
    
    protected override void ActivateAbility()
    {
        base.ActivateAbility();
        ApplyBonus();
    }
    
    protected override void Update()
    {
        base.Update();
        
        // Apply regeneration each frame
        if (isActive && !isFrozen && snakeHealth != null && snakeHealth.IsAlive())
        {
            float regenAmount = PlayerStats.Instance != null 
                ? PlayerStats.Instance.GetHealthRegenPerSecond() * Time.deltaTime
                : currentBonus * Time.deltaTime;
            
            if (regenAmount > 0)
            {
                snakeHealth.Heal(regenAmount);
            }
        }
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
            Debug.LogWarning("HealthRegenAbility: PlayerStats not found!");
            return;
        }
        
        currentBonus = regenPerSecondPerLevel * currentLevel;
        PlayerStats.Instance.AddHealthRegen(currentBonus);
        
        Debug.Log($"HealthRegenAbility: Applied +{currentBonus:F1} HP/sec (Level {currentLevel})");
    }
    
    private void RemoveBonus()
    {
        if (PlayerStats.Instance == null || currentBonus == 0f) return;
        
        PlayerStats.Instance.AddHealthRegen(-currentBonus);
        currentBonus = 0f;
    }
    
    protected override void DeactivateAbility()
    {
        RemoveBonus();
        base.DeactivateAbility();
    }
}