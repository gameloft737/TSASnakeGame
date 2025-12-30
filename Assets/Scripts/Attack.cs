using UnityEngine;

[System.Serializable]
public class AttackVariation
{
    public string attackName;
    public GameObject attachmentObject; // Can be null if no attachment
    // Note: Material changes are now only applied through evolutions
}

public abstract class Attack : MonoBehaviour
{
    [Header("Upgrade System")]
    [SerializeField] protected AttackUpgradeData upgradeData;
    [SerializeField] protected int currentLevel = 1;
    
    [Header("Attack Stats (Base - overridden by upgrade data if assigned)")]
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected float range = 10f;
    public string attackName;
    public Sprite attackIcon; // Icon for the attack (can be set directly or from upgrade data)

    [Header("Fuel System")]
    [SerializeField] protected float minFuelToActivate = 50f;
    [SerializeField] protected float fuelRechargeRate = 20f;

    [Header("Attack Type")]
    [SerializeField] protected AttackType attackType = AttackType.Burst;
    [SerializeField] protected float burstFuelCost = 15f;
    [SerializeField] protected float continuousDrainRate = 10f;
    
    [Header("Visual Variation")]
    [SerializeField] protected AttackVariation visualVariation;
    
    // Track if we're at an evolution level for material changes
    private bool hasAppliedEvolutionVisuals = false;

    public enum AttackType
    {
        Burst,
        Continuous
    }

    protected const float MAX_FUEL = 100f;
    protected static float sharedFuel = MAX_FUEL;
    protected bool isActive = false;

    protected static int activeAttackCount = 0;
    protected static Attack currentActiveAttack = null;
    
    // Pause state tracking
    protected static bool isPaused = false;
    
    protected virtual void Start()
    {
        // Apply initial level stats
        ApplyLevelStats();
    }

    protected virtual void Update()
    {
        // Don't update fuel if paused
        if (isPaused) return;
        
        // Only the current active attack handles recharging
        if (currentActiveAttack == this && !IsAnyAttackActive() && sharedFuel < MAX_FUEL)
        {
            sharedFuel = Mathf.Min(MAX_FUEL, sharedFuel + fuelRechargeRate * Time.deltaTime);
        }
    }

    private static bool IsAnyAttackActive()
    {
        return activeAttackCount > 0;
    }

    public bool CanActivate()
    {
        return sharedFuel >= minFuelToActivate && !isPaused;
    }

    public bool TryActivate()
    {
        if (CanActivate())
        {
            isActive = true;
            activeAttackCount += 1;
            OnActivate();

            if (attackType == AttackType.Burst)
            {
                sharedFuel = Mathf.Max(0f, sharedFuel - burstFuelCost);
                StopUsing();
            }

            return true;
        }
        return false;
    }

    public void HoldUpdate()
    {
        // Don't update fuel if paused
        if (!isActive || isPaused) return;

        OnHoldUpdate();

        if (attackType == AttackType.Continuous)
        {
            sharedFuel = Mathf.Max(0f, sharedFuel - continuousDrainRate * Time.deltaTime);

            if (sharedFuel <= 0f)
            {
                StopUsing();
            }
        }
    }

    public void StopUsing()
    {
        if (isActive)
        {
            isActive = false;
            activeAttackCount = Mathf.Max(0, activeAttackCount - 1);
            OnDeactivate();
        }
    }

    // Called by AttackManager when this becomes the active attack
    public void SetAsCurrentAttack()
    {
        currentActiveAttack = this;
    }
    
    // Called by WaveManager to set pause state
    public static void SetPaused(bool paused)
    {
        isPaused = paused;
        
        // Force stop all active attacks when pausing
        if (paused && currentActiveAttack != null)
        {
            currentActiveAttack.StopUsing();
        }
        
        // Refill fuel to max when unpausing
        if (!paused)
        {
            sharedFuel = MAX_FUEL;
        }
    }

    // Getters
    public float GetFuelPercentage() => sharedFuel / MAX_FUEL;
    public float GetCurrentFuel() => sharedFuel;
    public float GetMaxFuel() => MAX_FUEL;
    public float GetMinFuelToActivate() => minFuelToActivate;
    public bool IsActive() => isActive;
    
    /// <summary>
    /// Gets the final damage value (base damage * damage multiplier from PlayerStats)
    /// </summary>
    public float GetDamage()
    {
        float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetDamageMultiplier() : 1f;
        return damage * multiplier;
    }
    
    /// <summary>
    /// Gets the final range value (base range * range multiplier from PlayerStats)
    /// </summary>
    public float GetRange()
    {
        float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetRangeMultiplier() : 1f;
        return range * multiplier;
    }
    
    /// <summary>
    /// Gets the base damage without multipliers (for UI display)
    /// </summary>
    public float GetBaseDamage() => damage;
    
    /// <summary>
    /// Gets the base range without multipliers (for UI display)
    /// </summary>
    public float GetBaseRange() => range;
    
    public AttackType GetAttackType() => attackType;
    public AttackVariation GetVisualVariation() => visualVariation;
    public int GetCurrentLevel() => currentLevel;
    public int GetMaxLevel() => upgradeData != null ? upgradeData.maxLevel : 1;
    public AttackUpgradeData GetUpgradeData() => upgradeData;
    
    /// <summary>
    /// Gets the effective max level considering evolutions
    /// </summary>
    public int GetEffectiveMaxLevel(AbilityManager abilityManager = null)
    {
        if (upgradeData == null) return 1;
        
        // Try to find AbilityManager if not provided
        if (abilityManager == null)
        {
            abilityManager = Object.FindFirstObjectByType<AbilityManager>();
        }
        
        return upgradeData.GetEffectiveMaxLevel(abilityManager);
    }
    
    /// <summary>
    /// Checks if this attack can be upgraded (considers evolutions)
    /// </summary>
    public bool CanUpgrade()
    {
        if (upgradeData == null) return false;
        
        // Find AbilityManager to check for evolutions
        AbilityManager abilityManager = Object.FindFirstObjectByType<AbilityManager>();
        return upgradeData.CanUpgradeWithEvolutions(currentLevel, abilityManager);
    }
    
    /// <summary>
    /// Checks if this attack can be upgraded without considering evolutions
    /// </summary>
    public bool CanUpgradeBase()
    {
        if (upgradeData == null) return false;
        return upgradeData.CanUpgrade(currentLevel);
    }
    
    /// <summary>
    /// Checks if the current level is an evolution level AND the evolution is unlocked
    /// </summary>
    public bool IsAtEvolutionLevel()
    {
        if (upgradeData == null) return false;
        
        // First check if this is an evolution level
        if (!upgradeData.IsEvolutionLevel(currentLevel))
            return false;
        
        // Now verify the evolution is actually unlocked
        EvolutionRequirement evolution = upgradeData.GetEvolutionForLevel(currentLevel);
        if (evolution == null)
            return false;
        
        // Check if the player has the required passive
        AbilityManager abilityManager = Object.FindFirstObjectByType<AbilityManager>();
        if (upgradeData.evolutionData == null || abilityManager == null)
            return false;
        
        return upgradeData.evolutionData.IsEvolutionUnlocked(evolution, abilityManager);
    }
    
    /// <summary>
    /// Gets the evolution requirement for the next level (if it's an evolution)
    /// </summary>
    public EvolutionRequirement GetNextEvolution()
    {
        if (upgradeData == null) return null;
        return upgradeData.GetEvolutionForLevel(currentLevel + 1);
    }
    
    /// <summary>
    /// Upgrades the attack to the next level
    /// </summary>
    public bool TryUpgrade()
    {
        if (!CanUpgrade()) return false;
        
        currentLevel++;
        ApplyLevelStats();
        OnUpgrade();
        
        // Check if this is an evolution level and apply visuals
        if (IsAtEvolutionLevel())
        {
            OnEvolutionReached();
            
            // Notify AttackManager to apply evolution visuals
            AttackManager attackManager = Object.FindFirstObjectByType<AttackManager>();
            if (attackManager != null)
            {
                attackManager.RefreshEvolutionVisuals();
            }
        }
        
        Debug.Log($"{attackName} upgraded to level {currentLevel}!");
        return true;
    }
    
    /// <summary>
    /// Called when an evolution level is reached
    /// </summary>
    protected virtual void OnEvolutionReached()
    {
        hasAppliedEvolutionVisuals = true;
        Debug.Log($"{attackName} evolved to level {currentLevel}!");
    }
    
    /// <summary>
    /// Gets the evolution requirement for the current level (if it's an evolution)
    /// </summary>
    public EvolutionRequirement GetCurrentEvolution()
    {
        if (upgradeData == null) return null;
        return upgradeData.GetEvolutionForLevel(currentLevel);
    }
    
    /// <summary>
    /// Returns whether this attack has applied evolution visuals
    /// </summary>
    public bool HasEvolutionVisuals() => hasAppliedEvolutionVisuals;
    
    /// <summary>
    /// Sets the attack to a specific level (validates evolution requirements)
    /// </summary>
    public void SetLevel(int level)
    {
        if (upgradeData == null) return;
        
        // Get the effective max level considering evolutions
        AbilityManager abilityManager = Object.FindFirstObjectByType<AbilityManager>();
        int effectiveMaxLevel = upgradeData.GetEffectiveMaxLevel(abilityManager);
        
        // Clamp to effective max level (which considers unlocked evolutions)
        currentLevel = Mathf.Clamp(level, 1, effectiveMaxLevel);
        ApplyLevelStats();
        
        // Check if this is an evolution level and apply visuals
        if (IsAtEvolutionLevel())
        {
            OnEvolutionReached();
            
            // Notify AttackManager to apply evolution visuals
            AttackManager attackManager = Object.FindFirstObjectByType<AttackManager>();
            if (attackManager != null)
            {
                attackManager.RefreshEvolutionVisuals();
            }
        }
    }
    
    /// <summary>
    /// Applies the stats from the current level
    /// </summary>
    protected virtual void ApplyLevelStats()
    {
        if (upgradeData == null) return;
        
        AttackLevelStats stats = upgradeData.GetStatsForLevel(currentLevel);
        
        // Apply base stats
        damage = stats.damage;
        range = stats.range;
        minFuelToActivate = stats.minFuelToActivate;
        fuelRechargeRate = stats.fuelRechargeRate;
        burstFuelCost = stats.burstFuelCost;
        continuousDrainRate = stats.continuousDrainRate;
        
        // Apply icon from upgrade data if not set directly
        if (attackIcon == null && upgradeData.attackIcon != null)
        {
            attackIcon = upgradeData.attackIcon;
        }
        
        // Let child classes apply their custom stats
        ApplyCustomStats(stats);
    }
    
    /// <summary>
    /// Gets a custom stat value from the current level
    /// </summary>
    protected float GetCustomStat(string statName, float defaultValue = 0f)
    {
        if (upgradeData == null) return defaultValue;
        return upgradeData.GetCustomStat(currentLevel, statName, defaultValue);
    }
    
    /// <summary>
    /// Override in child classes to apply attack-specific custom stats
    /// </summary>
    protected virtual void ApplyCustomStats(AttackLevelStats stats) { }
    
    /// <summary>
    /// Called when the attack is upgraded - override for custom upgrade effects
    /// </summary>
    protected virtual void OnUpgrade() { }

    // Override these in child classes
    protected virtual void OnActivate() { }
    protected virtual void OnHoldUpdate() { }
    protected virtual void OnDeactivate() { }
}