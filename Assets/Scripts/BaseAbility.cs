using UnityEngine;

/// <summary>
/// Base class for all abilities in the game
/// </summary>
public abstract class BaseAbility : MonoBehaviour
{
    [Header("Level System")]
    [SerializeField] protected int currentLevel = 1;
    [SerializeField] protected int maxLevel = 3;
    
    [Header("Upgrade Data")]
    [SerializeField] protected AbilityUpgradeData upgradeData;
    
    protected bool isActive = false;
    protected bool isFrozen = false; // Whether the ability is frozen (paused)

    protected virtual void Awake()
    {
        // If upgrade data is assigned, sync max level from it
        if (upgradeData != null)
        {
            maxLevel = upgradeData.maxLevel;
        }
        
        ActivateAbility();
    }

    protected virtual void Update()
    {
        // Skip updates when frozen
        if (isFrozen) return;
        
        // Abilities are now permanent - no duration countdown
    }

    /// <summary>
    /// Levels up the ability (considers evolutions)
    /// </summary>
    public virtual bool LevelUp()
    {
        // Use CanUpgrade which properly checks evolution requirements
        if (!CanUpgrade())
        {
            return false;
        }
        
        // Check if we have level data for the next level
        if (upgradeData != null && currentLevel >= upgradeData.levels.Count)
        {
            Debug.LogWarning($"{GetType().Name}: No level data for level {currentLevel + 1}");
            return false;
        }
        
        currentLevel++;
        ApplyLevelStats();
        OnLevelUp();
        
        // Check if this is an evolution level (now properly checks if evolution is unlocked)
        if (IsAtEvolutionLevel())
        {
            OnEvolutionUnlocked();
        }
        
        return true;
    }
    
    /// <summary>
    /// Sets the upgrade data for this ability
    /// </summary>
    public virtual void SetUpgradeData(AbilityUpgradeData data)
    {
        upgradeData = data;
        if (upgradeData != null)
        {
            maxLevel = upgradeData.maxLevel;
        }
    }
    
    /// <summary>
    /// Gets the upgrade data for this ability
    /// </summary>
    public AbilityUpgradeData GetUpgradeData() => upgradeData;
    
    /// <summary>
    /// Applies stats from the upgrade data for the current level
    /// Override this in derived classes to apply specific stats
    /// </summary>
    protected virtual void ApplyLevelStats()
    {
        if (upgradeData == null) return;
        
        AbilityLevelStats stats = upgradeData.GetStatsForLevel(currentLevel);
        ApplyCustomStats(stats);
    }
    
    /// <summary>
    /// Override this to apply custom stats from the level data
    /// </summary>
    protected virtual void ApplyCustomStats(AbilityLevelStats stats)
    {
        // Override in derived classes to apply specific stats
    }
    
    /// <summary>
    /// Gets the damage value for the current level from upgrade data
    /// </summary>
    protected float GetDamage()
    {
        if (upgradeData != null)
        {
            return upgradeData.GetDamage(currentLevel);
        }
        return 0f;
    }
    
    /// <summary>
    /// Gets the cooldown value for the current level from upgrade data
    /// </summary>
    protected float GetCooldown()
    {
        if (upgradeData != null)
        {
            return upgradeData.GetCooldown(currentLevel);
        }
        return 0f;
    }
    
    /// <summary>
    /// Gets the duration value for the current level from upgrade data
    /// </summary>
    protected float GetDuration()
    {
        if (upgradeData != null)
        {
            return upgradeData.GetDuration(currentLevel);
        }
        return 0f;
    }
    
    /// <summary>
    /// Gets a custom stat value for the current level
    /// </summary>
    protected float GetCustomStat(string statName, float defaultValue = 0f)
    {
        if (upgradeData != null)
        {
            return upgradeData.GetCustomStat(currentLevel, statName, defaultValue);
        }
        return defaultValue;
    }
    
    /// <summary>
    /// Gets the description for the current level
    /// </summary>
    public string GetDescription()
    {
        if (upgradeData != null)
        {
            return upgradeData.GetDescription(currentLevel);
        }
        return "";
    }
    
    /// <summary>
    /// Gets the description for a specific level
    /// </summary>
    public string GetDescriptionForLevel(int level)
    {
        if (upgradeData != null)
        {
            return upgradeData.GetDescription(level);
        }
        return "";
    }

    /// <summary>
    /// Called when ability levels up (override for custom behavior)
    /// </summary>
    protected virtual void OnLevelUp()
    {
        Debug.Log($"{GetType().Name} leveled up to {currentLevel}!");
    }

    /// <summary>
    /// Called when ability is first activated
    /// </summary>
    protected virtual void ActivateAbility()
    {
        isActive = true;
        ApplyLevelStats();
    }

    /// <summary>
    /// Called when ability is deactivated
    /// </summary>
    protected virtual void DeactivateAbility()
    {
        isActive = false;
    }

    /// <summary>
    /// Freezes or unfreezes the ability
    /// </summary>
    public virtual void SetFrozen(bool frozen)
    {
        isFrozen = frozen;
    }
    
    /// <summary>
    /// Checks if the ability can be upgraded further (considers evolutions)
    /// </summary>
    public bool CanUpgrade()
    {
        if (upgradeData != null)
        {
            AbilityManager abilityManager = Object.FindFirstObjectByType<AbilityManager>();
            return upgradeData.CanUpgradeWithEvolutions(currentLevel, abilityManager);
        }
        return currentLevel < maxLevel;
    }
    
    /// <summary>
    /// Checks if the ability can be upgraded without considering evolutions
    /// </summary>
    public bool CanUpgradeBase()
    {
        if (upgradeData != null)
        {
            return upgradeData.CanUpgrade(currentLevel);
        }
        return currentLevel < maxLevel;
    }
    
    /// <summary>
    /// Gets the effective max level considering evolutions
    /// </summary>
    public int GetEffectiveMaxLevel()
    {
        if (upgradeData != null)
        {
            AbilityManager abilityManager = Object.FindFirstObjectByType<AbilityManager>();
            return upgradeData.GetEffectiveMaxLevel(abilityManager);
        }
        return maxLevel;
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
    /// Called when an evolution level is reached
    /// </summary>
    protected virtual void OnEvolutionUnlocked()
    {
        Debug.Log($"{GetType().Name} evolved to level {currentLevel}!");
    }

    // Getters
    public int GetCurrentLevel() => currentLevel;
    public int GetMaxLevel() => maxLevel;
    public int GetBaseMaxLevel() => upgradeData != null ? upgradeData.maxLevel : maxLevel;
    public bool IsActive() => isActive;
    public bool IsMaxLevel() => currentLevel >= GetEffectiveMaxLevel();
    public bool IsFrozen() => isFrozen;
}