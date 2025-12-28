using UnityEngine;
using UnityEngine.Events;
using System;

public class XPManager : MonoBehaviour
{
    public static XPManager Instance { get; private set; }
    
    [Header("XP Settings")]
    [SerializeField] private int currentXP = 0;
    [SerializeField] private int xpToNextLevel = 100;
    [SerializeField] private float xpScalingFactor = 1.5f;
    
    [Header("Level Info")]
    [SerializeField] private int currentLevel = 1;
    
    [Header("Events")]
    public UnityEvent<int, int> OnXPChanged;
    public UnityEvent<int> OnLevelUp;
    
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
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    }
    
    public void AddXP(int amount)
    {
        currentXP += amount;
        OnXPCollected?.Invoke(amount);
        
        while (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }
        
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    }
    
    private void LevelUp()
    {
        currentXP -= xpToNextLevel;
        currentLevel++;
        
        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * xpScalingFactor);
        
        OnLevelUp?.Invoke(currentLevel);
        OnLeveledUp?.Invoke(currentLevel);
        
        Debug.Log($"Level Up! Now level {currentLevel}. Next level requires {xpToNextLevel} XP.");
    Debug.Log($"OnLevelUp listeners: {OnLevelUp?.GetPersistentEventCount()}");
    }
    
    public int GetCurrentXP() => currentXP;
    public int GetXPToNextLevel() => xpToNextLevel;
    public int GetCurrentLevel() => currentLevel;
    public float GetXPProgress() => (float)currentXP / xpToNextLevel;
    
    public void ResetXP()
    {
        currentXP = 0;
        currentLevel = 1;
        xpToNextLevel = 100;
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    }
    
    public void SetXP(int xp, int level, int xpRequired)
    {
        currentXP = xp;
        currentLevel = level;
        xpToNextLevel = xpRequired;
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    }
}