using UnityEngine;

/// <summary>
/// Base class for all abilities in the game
/// </summary>
public abstract class BaseAbility : MonoBehaviour
{
    [Header("Level System")]
    [SerializeField] protected int currentLevel = 1;
    [SerializeField] protected int maxLevel = 3;
    
    protected bool isActive = false;
    protected bool isFrozen = false; // Whether the ability is frozen (paused)

    protected virtual void Awake()
    {
        ActivateAbility();
    }

    protected virtual void Update()
    {
        // Skip updates when frozen
        if (isFrozen) return;
        
        // Abilities are now permanent - no duration countdown
    }

    /// <summary>
    /// Levels up the ability
    /// </summary>
    public virtual bool LevelUp()
    {
        if (currentLevel >= maxLevel)
        {
            // Already max level
            return false;
        }
        
        currentLevel++;
        OnLevelUp();
        return true;
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

    // Getters
    public int GetCurrentLevel() => currentLevel;
    public int GetMaxLevel() => maxLevel;
    public bool IsActive() => isActive;
    public bool IsMaxLevel() => currentLevel >= maxLevel;
    public bool IsFrozen() => isFrozen;
}