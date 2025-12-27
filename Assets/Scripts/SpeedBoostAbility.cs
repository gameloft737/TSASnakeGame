using UnityEngine;

/// <summary>
/// Passive ability that increases movement speed.
/// Each level adds more speed multiplier.
/// </summary>
public class SpeedBoostAbility : BaseAbility
{
    [Header("Speed Boost Settings")]
    [SerializeField] private float speedMultiplierPerLevel = 0.10f; // 10% per level
    
    private float currentBonus = 0f;
    private PlayerMovement playerMovement;
    private float originalMaxSpeed;
    private float originalDefaultSpeed;
    
    protected override void Awake()
    {
        base.Awake();
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        
        if (playerMovement != null)
        {
            originalMaxSpeed = playerMovement.maxSpeed;
            originalDefaultSpeed = playerMovement.defaultSpeed;
        }
    }
    
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
            Debug.LogWarning("SpeedBoostAbility: PlayerStats not found!");
            return;
        }
        
        currentBonus = speedMultiplierPerLevel * currentLevel;
        PlayerStats.Instance.AddSpeedMultiplier(currentBonus);
        
        // Also directly update PlayerMovement speeds
        UpdatePlayerSpeed();
        
        Debug.Log($"SpeedBoostAbility: Applied +{currentBonus * 100:F0}% speed boost (Level {currentLevel})");
    }
    
    private void UpdatePlayerSpeed()
    {
        if (playerMovement == null) return;
        
        float multiplier = PlayerStats.Instance != null 
            ? PlayerStats.Instance.GetSpeedMultiplier() 
            : 1f + currentBonus;
        
        playerMovement.maxSpeed = originalMaxSpeed * multiplier;
        playerMovement.defaultSpeed = originalDefaultSpeed * multiplier;
    }
    
    private void RemoveBonus()
    {
        if (PlayerStats.Instance == null || currentBonus == 0f) return;
        
        PlayerStats.Instance.AddSpeedMultiplier(-currentBonus);
        currentBonus = 0f;
        
        // Reset player speed
        if (playerMovement != null)
        {
            playerMovement.maxSpeed = originalMaxSpeed;
            playerMovement.defaultSpeed = originalDefaultSpeed;
        }
    }
    
    protected override void DeactivateAbility()
    {
        RemoveBonus();
        base.DeactivateAbility();
    }
}