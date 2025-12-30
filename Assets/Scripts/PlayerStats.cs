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
    [SerializeField] private float baseCooldownReduction = 0f;
    [SerializeField] private float baseCritChance = 0f;
    [SerializeField] private float baseCritMultiplier = 2f; // 2x damage on crit by default
    [SerializeField] private float baseLifesteal = 0f;
    [SerializeField] private float baseDamageReduction = 0f;
    [SerializeField] private float baseXPMultiplier = 1f;
    
    [Header("Current Stat Bonuses (from abilities)")]
    [SerializeField] private float damageMultiplierBonus = 0f;
    [SerializeField] private float rangeMultiplierBonus = 0f;
    [SerializeField] private float maxHealthBonus = 0f;
    [SerializeField] private float maxHealthPercentBonus = 0f; // Percentage bonus (0.1 = 10%)
    [SerializeField] private float healthRegenPerSecond = 0f;
    [SerializeField] private float speedMultiplierBonus = 0f;
    [SerializeField] private float cooldownReductionBonus = 0f; // Percentage (0.1 = 10% CDR)
    [SerializeField] private float critChanceBonus = 0f; // Percentage (0.1 = 10% crit chance)
    [SerializeField] private float critMultiplierBonus = 0f; // Additional crit damage multiplier
    [SerializeField] private float lifestealBonus = 0f; // Percentage of damage healed (0.1 = 10%)
    [SerializeField] private float damageReductionBonus = 0f; // Percentage damage reduction (0.1 = 10%)
    [SerializeField] private float xpMultiplierBonus = 0f; // Additional XP multiplier
    
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
    /// Gets the total max health bonus (flat amount)
    /// </summary>
    public float GetMaxHealthBonus()
    {
        return baseMaxHealthBonus + maxHealthBonus;
    }
    
    /// <summary>
    /// Gets the total max health percentage bonus (e.g., 0.2 = 20% increase)
    /// </summary>
    public float GetMaxHealthPercentBonus()
    {
        return maxHealthPercentBonus;
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
    
    /// <summary>
    /// Gets the total cooldown reduction (capped at 0.75 = 75%)
    /// </summary>
    public float GetCooldownReduction()
    {
        return Mathf.Min(baseCooldownReduction + cooldownReductionBonus, 0.75f);
    }
    
    /// <summary>
    /// Gets the cooldown multiplier (1 - CDR, so 0.25 at max CDR)
    /// </summary>
    public float GetCooldownMultiplier()
    {
        return 1f - GetCooldownReduction();
    }
    
    /// <summary>
    /// Gets the total critical hit chance (capped at 1.0 = 100%)
    /// </summary>
    public float GetCritChance()
    {
        return Mathf.Min(baseCritChance + critChanceBonus, 1f);
    }
    
    /// <summary>
    /// Gets the total critical hit damage multiplier
    /// </summary>
    public float GetCritMultiplier()
    {
        return baseCritMultiplier + critMultiplierBonus;
    }
    
    /// <summary>
    /// Rolls for a critical hit and returns the damage multiplier (1 for normal, crit multiplier for crit)
    /// </summary>
    public float RollCritMultiplier()
    {
        if (Random.value < GetCritChance())
        {
            return GetCritMultiplier();
        }
        return 1f;
    }
    
    /// <summary>
    /// Gets the total lifesteal percentage
    /// </summary>
    public float GetLifesteal()
    {
        return baseLifesteal + lifestealBonus;
    }
    
    /// <summary>
    /// Gets the total damage reduction (capped at 0.75 = 75%)
    /// </summary>
    public float GetDamageReduction()
    {
        return Mathf.Min(baseDamageReduction + damageReductionBonus, 0.75f);
    }
    
    /// <summary>
    /// Gets the damage multiplier after reduction (1 - DR)
    /// </summary>
    public float GetDamageReductionMultiplier()
    {
        return 1f - GetDamageReduction();
    }
    
    /// <summary>
    /// Gets the total XP multiplier
    /// </summary>
    public float GetXPMultiplier()
    {
        return baseXPMultiplier + xpMultiplierBonus;
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
    /// Adds to the max health bonus (flat amount)
    /// </summary>
    public void AddMaxHealthBonus(float amount)
    {
        maxHealthBonus += amount;
        onStatsChanged?.Invoke();
        Debug.Log($"[PlayerStats] Max health bonus now: +{GetMaxHealthBonus():F0}");
    }
    
    /// <summary>
    /// Adds to the max health percentage bonus (e.g., 0.1 = +10%)
    /// </summary>
    public void AddMaxHealthPercentBonus(float percent)
    {
        maxHealthPercentBonus += percent;
        onStatsChanged?.Invoke();
        Debug.Log($"[PlayerStats] Max health percent bonus now: +{GetMaxHealthPercentBonus() * 100:F0}%");
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
    
    /// <summary>
    /// Adds to the cooldown reduction bonus
    /// </summary>
    public void AddCooldownReduction(float amount)
    {
        cooldownReductionBonus += amount;
        onStatsChanged?.Invoke();
        Debug.Log($"[PlayerStats] Cooldown reduction now: {GetCooldownReduction() * 100:F0}%");
    }
    
    /// <summary>
    /// Adds to the critical hit chance bonus
    /// </summary>
    public void AddCritChance(float amount)
    {
        critChanceBonus += amount;
        onStatsChanged?.Invoke();
        Debug.Log($"[PlayerStats] Crit chance now: {GetCritChance() * 100:F0}%");
    }
    
    /// <summary>
    /// Adds to the critical hit multiplier bonus
    /// </summary>
    public void AddCritMultiplier(float amount)
    {
        critMultiplierBonus += amount;
        onStatsChanged?.Invoke();
        Debug.Log($"[PlayerStats] Crit multiplier now: {GetCritMultiplier():F2}x");
    }
    
    /// <summary>
    /// Adds to the lifesteal bonus
    /// </summary>
    public void AddLifesteal(float amount)
    {
        lifestealBonus += amount;
        onStatsChanged?.Invoke();
        Debug.Log($"[PlayerStats] Lifesteal now: {GetLifesteal() * 100:F0}%");
    }
    
    /// <summary>
    /// Adds to the damage reduction bonus
    /// </summary>
    public void AddDamageReduction(float amount)
    {
        damageReductionBonus += amount;
        onStatsChanged?.Invoke();
        Debug.Log($"[PlayerStats] Damage reduction now: {GetDamageReduction() * 100:F0}%");
    }
    
    /// <summary>
    /// Adds to the XP multiplier bonus
    /// </summary>
    public void AddXPMultiplier(float amount)
    {
        xpMultiplierBonus += amount;
        onStatsChanged?.Invoke();
        Debug.Log($"[PlayerStats] XP multiplier now: {GetXPMultiplier():F2}x");
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
        maxHealthPercentBonus = 0f;
        healthRegenPerSecond = 0f;
        speedMultiplierBonus = 0f;
        cooldownReductionBonus = 0f;
        critChanceBonus = 0f;
        critMultiplierBonus = 0f;
        lifestealBonus = 0f;
        damageReductionBonus = 0f;
        xpMultiplierBonus = 0f;
        onStatsChanged?.Invoke();
        Debug.Log("[PlayerStats] All bonuses reset!");
    }
}