using UnityEngine;

/// <summary>
/// Base class for all abilities in the game
/// </summary>
public abstract class BaseAbility : MonoBehaviour
{
    [Header("Level System")]
    [SerializeField] protected int currentLevel = 1;
    [SerializeField] protected int maxLevel = 3;
    [SerializeField] protected float baseDuration = 10f;
    [SerializeField] protected float durationPerLevel = 5f;
    
    protected float remainingDuration;
    protected bool isActive = false;

    protected virtual void Awake()
    {
        remainingDuration = GetTotalDuration();
        ActivateAbility();
    }

    protected virtual void Update()
    {
        if (isActive)
        {
            remainingDuration -= Time.deltaTime;
            if (remainingDuration <= 0)
            {
                DeactivateAbility();
            }
        }
    }

    /// <summary>
    /// Levels up the ability and extends duration
    /// </summary>
    public virtual bool LevelUp()
    {
        if (currentLevel >= maxLevel)
        {
            // Already max level, just extend duration
            ExtendDuration();
            return false;
        }
        
        currentLevel++;
        ExtendDuration();
        OnLevelUp();
        return true;
    }
    
    /// <summary>
    /// Extends the ability duration
    /// </summary>
    protected virtual void ExtendDuration()
    {
        remainingDuration += GetTotalDuration();
    }

    /// <summary>
    /// Gets total duration based on level
    /// </summary>
    protected float GetTotalDuration()
    {
        return baseDuration + (durationPerLevel * (currentLevel - 1));
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
    /// Called when ability duration expires
    /// </summary>
    protected virtual void DeactivateAbility()
    {
        isActive = false;
        // Could destroy or disable here
    }

    // Getters
    public int GetCurrentLevel() => currentLevel;
    public int GetMaxLevel() => maxLevel;
    public float GetRemainingDuration() => remainingDuration;
    public bool IsActive() => isActive;
    public bool IsMaxLevel() => currentLevel >= maxLevel;
}