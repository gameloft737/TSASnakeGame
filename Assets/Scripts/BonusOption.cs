using UnityEngine;

/// <summary>
/// Represents a bonus option that can be selected when all abilities are maxed out.
/// These are one-time effects like healing or temporary speed boosts.
/// </summary>
[System.Serializable]
public class BonusOption
{
    public string optionName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;
    public BonusType bonusType;
    
    [Header("Heal Settings")]
    [Tooltip("Amount of health to restore (for Heal type)")]
    public float healAmount = 25f;
    [Tooltip("If true, heal amount is a percentage of max health")]
    public bool healIsPercentage = true;
    
    [Header("Speed Boost Settings")]
    [Tooltip("Speed multiplier (for SpeedBoost type)")]
    public float speedMultiplier = 1.5f;
    [Tooltip("Duration of the speed boost in seconds")]
    public float speedBoostDuration = 5f;
}

public enum BonusType
{
    Heal,
    SpeedBoost
}