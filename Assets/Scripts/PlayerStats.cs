using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Singleton that manages player stat modifiers from passive abilities.
/// Other scripts should query this for their final stat values.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }
    
    [Header("Base Stats (for reference only)")]
    [SerializeField] private float baseDamageMultiplier = 1f;
    [SerializeField] private float baseRangeMultiplier = 1f;
    [SerializeField] private float baseMaxHealthBonus = 0f;
    [SerializeField] private float baseHealthRegenPerSecond = 0f;
    [SerializeField] private float baseSpeedMultiplier = 1f;
    
    [Header("Current Stat Bonuses (from abilities)")]
    [SerializeField] private float damageMultiplierBonus = 0f;
    [SerializeField] private float rangeMultiplierBonus = 0f;
    [SerializeField] private float maxHealthBonus = 0f;
    [SerializeField] private float healthRegenPerSecond = 0f;
    [SerializeField] private float speedMultiplierBonus = 0f;
    
    [Header("Events")]
    public UnityEvent onStatsChanged;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    // ============ GETTERS FOR FINAL STAT VALUES ============
    
    /// <summary>
    /// Gets the total damage multiplier (base + bonuses)
    /// </summary>
    public float GetDamageMultiplier()
    {
        return baseDamageMultiplier + damageMultiplierBonus;
    }
    
    /// <summary>
    /// Gets the total range multiplier (base + bonuses)
    /// </summary>
    public float GetRangeMultiplier()
    {
        return baseRangeMultiplier + rangeMultiplierBonus;
    }
    
    /// <summary>
    /// Gets the total max health bonus
    /// </summary>
    public float GetMaxHealthBonus()
    {
        return baseMaxHealthBonus + maxHealthBonus;
    }
    
    /// <summary>
    /// Gets the health regeneration per second
    /// </summary>
    public float GetHealthRegenPerSecond()
    {
        return baseHealthRegenPerSecond + healthRegenPerSecond;
    }
    
    /// <summary>
    /// Gets the total speed multiplier (base + bonuses)
    /// </summary>
    public float GetSpeedMultiplier()
    {
        return baseSpeedMultiplier + speedMultiplierBonus;
    }
    
    // ============ METHODS TO ADD/REMOVE BONUSES ============
    
    /// <summary>
    /// Adds to the damage multiplier bonus
    /// </summary>
    public void AddDamageMultiplier(float amount)
    {
        damageMultiplierBonus += amount;
        onStatsChanged?.Invoke();
        Debug.Log($"[PlayerStats] Damage multiplier now: {GetDamageMultiplier():F2}x");
    }
    
    /// <summary>
    /// Adds to the range multiplier bonus
    /// </summary>
    public void AddRangeMultiplier(float amount)
    {
        rangeMultiplierBonus += amount;
        onStatsChanged?.Invoke();
        Debug.Log($"[PlayerStats] Range multiplier now: {GetRangeMultiplier():F2}x");
    }
    
    /// <summary>
    /// Adds to the max health bonus
    /// </summary>
    public void AddMaxHealthBonus(float amount)
    {
        maxHealthBonus += amount;
        onStatsChanged?.Invoke();
        Debug.Log($"[PlayerStats] Max health bonus now: +{GetMaxHealthBonus():F0}");
    }
    
    /// <summary>
    /// Adds to the health regen per second
    /// </summary>
    public void AddHealthRegen(float amount)
    {
        healthRegenPerSecond += amount;
        onStatsChanged?.Invoke();
        Debug.Log($"[PlayerStats] Health regen now: {GetHealthRegenPerSecond():F1}/sec");
    }
    
    /// <summary>
    /// Adds to the speed multiplier bonus
    /// </summary>
    public void AddSpeedMultiplier(float amount)
    {
        speedMultiplierBonus += amount;
        onStatsChanged?.Invoke();
        Debug.Log($"[PlayerStats] Speed multiplier now: {GetSpeedMultiplier():F2}x");
    }
    
    // ============ RESET METHOD ============
    
    /// <summary>
    /// Resets all bonuses to zero (useful for game restart)
    /// </summary>
    public void ResetAllBonuses()
    {
        damageMultiplierBonus = 0f;
        rangeMultiplierBonus = 0f;
        maxHealthBonus = 0f;
        healthRegenPerSecond = 0f;
        speedMultiplierBonus = 0f;
        onStatsChanged?.Invoke();
        Debug.Log("[PlayerStats] All bonuses reset!");
    }
}