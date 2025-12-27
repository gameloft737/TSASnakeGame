using UnityEngine;
using UnityEngine.Events;
using System;

/// <summary>
/// Manages player XP collection and level progression
/// </summary>
public class XPManager : MonoBehaviour
{
    public static XPManager Instance { get; private set; }
    
    [Header("XP Settings")]
    [SerializeField] private int currentXP = 0;
    [SerializeField] private int xpToNextLevel = 100;
    [SerializeField] private float xpScalingFactor = 1.5f; // How much XP requirement increases per level
    
    [Header("Level Info")]
    [SerializeField] private int currentLevel = 1;
    
    [Header("Events")]
    public UnityEvent<int, int> OnXPChanged; // current XP, XP to next level
    public UnityEvent<int> OnLevelUp; // new level
    
    // Static event for other systems to listen to
    public static event Action<int> OnXPCollected;
    public static event Action<int> OnLeveledUp;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Initialize UI
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    }
    
    /// <summary>
    /// Add XP to the player
    /// </summary>
    public void AddXP(int amount)
    {
        currentXP += amount;
        OnXPCollected?.Invoke(amount);
        
        // Check for level up
        while (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }
        
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    }
    
    /// <summary>
    /// Handle level up
    /// </summary>
    private void LevelUp()
    {
        currentXP -= xpToNextLevel;
        currentLevel++;
        
        // Increase XP requirement for next level
        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * xpScalingFactor);
        
        OnLevelUp?.Invoke(currentLevel);
        OnLeveledUp?.Invoke(currentLevel);
        
        Debug.Log($"Level Up! Now level {currentLevel}. Next level requires {xpToNextLevel} XP.");
    }
    
    /// <summary>
    /// Get current XP
    /// </summary>
    public int GetCurrentXP() => currentXP;
    
    /// <summary>
    /// Get XP required for next level
    /// </summary>
    public int GetXPToNextLevel() => xpToNextLevel;
    
    /// <summary>
    /// Get current level
    /// </summary>
    public int GetCurrentLevel() => currentLevel;
    
    /// <summary>
    /// Get XP progress as a percentage (0-1)
    /// </summary>
    public float GetXPProgress() => (float)currentXP / xpToNextLevel;
    
    /// <summary>
    /// Reset XP for a new game
    /// </summary>
    public void ResetXP()
    {
        currentXP = 0;
        currentLevel = 1;
        xpToNextLevel = 100;
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    }
    
    /// <summary>
    /// Set XP directly (for loading saves, etc.)
    /// </summary>
    public void SetXP(int xp, int level, int xpRequired)
    {
        currentXP = xp;
        currentLevel = level;
        xpToNextLevel = xpRequired;
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    }
}