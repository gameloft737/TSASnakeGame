using UnityEngine;
using System.Collections.Generic;

public class FireParticleDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damageTickInterval = 0.1f;
    
    [Header("References")]
    [SerializeField] private ParticleSystem fireParticles;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject criticalHitEffectPrefab;
    [SerializeField] private GameObject burnEffectPrefab;
    
    private float damagePerTick;
    private Dictionary<AppleEnemy, float> damageTimers = new Dictionary<AppleEnemy, float>();
    private Dictionary<AppleEnemy, BurnEffect> burnEffects = new Dictionary<AppleEnemy, BurnEffect>();
    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
    
    // Evolution stats
    private float lifeStealPercent = 0f;
    private float critChance = 0f;
    private float critMultiplier = 2f;
    private float burnDamagePercent = 0f;
    private float burnDuration = 0f;
    
    // Reference to snake health for life steal
    private SnakeHealth snakeHealth;

    private void Awake()
    {
        if (fireParticles == null)
        {
            fireParticles = GetComponent<ParticleSystem>();
        }
        
        // Cache snake health reference
        snakeHealth = FindFirstObjectByType<SnakeHealth>();
    }

    /// <summary>
    /// Initialize with base damage only (legacy support)
    /// </summary>
    public void Initialize(float damageAmount)
    {
        Initialize(damageAmount, 0f, 0f, 2f, 0f, 0f);
    }
    
    /// <summary>
    /// Initialize with full evolution stats
    /// </summary>
    public void Initialize(float damageAmount, float lifeSteal, float crit, float critMult, float burnDmg, float burnDur)
    {
        // Convert total damage to damage per tick
        damagePerTick = damageAmount * damageTickInterval;
        
        // Store evolution stats
        lifeStealPercent = lifeSteal;
        critChance = crit;
        critMultiplier = critMult;
        burnDamagePercent = burnDmg;
        burnDuration = burnDur;
        
        // Re-cache snake health in case it wasn't found in Awake
        if (snakeHealth == null)
        {
            snakeHealth = FindFirstObjectByType<SnakeHealth>();
        }
    }

    private void Update()
    {
        // Increment all timers and remove destroyed enemies
        var keys = new List<AppleEnemy>(damageTimers.Keys);
        foreach (var apple in keys)
        {
            if (apple == null)
            {
                damageTimers.Remove(apple);
            }
            else
            {
                damageTimers[apple] += Time.deltaTime;
            }
        }
        
        // Update burn effects
        UpdateBurnEffects();
    }
    
    /// <summary>
    /// Updates all active burn effects
    /// </summary>
    private void UpdateBurnEffects()
    {
        var burnKeys = new List<AppleEnemy>(burnEffects.Keys);
        foreach (var apple in burnKeys)
        {
            if (apple == null)
            {
                // Clean up burn effect for destroyed enemy
                if (burnEffects[apple].effectInstance != null)
                {
                    Destroy(burnEffects[apple].effectInstance);
                }
                burnEffects.Remove(apple);
                continue;
            }
            
            BurnEffect burn = burnEffects[apple];
            burn.timer += Time.deltaTime;
            burn.tickTimer += Time.deltaTime;
            
            // Apply burn damage tick
            if (burn.tickTimer >= burn.tickInterval)
            {
                float burnDamage = burn.damagePerTick;
                apple.TakeDamage(burnDamage);
                
                // Life steal from burn damage
                if (lifeStealPercent > 0 && snakeHealth != null)
                {
                    float healAmount = burnDamage * lifeStealPercent;
                    snakeHealth.Heal(healAmount);
                }
                
                burn.tickTimer = 0f;
            }
            
            // Remove expired burn
            if (burn.timer >= burn.duration)
            {
                if (burn.effectInstance != null)
                {
                    Destroy(burn.effectInstance);
                }
                burnEffects.Remove(apple);
            }
            else
            {
                burnEffects[apple] = burn;
            }
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        if (fireParticles == null) return;
        
        int numCollisionEvents = fireParticles.GetCollisionEvents(other, collisionEvents);
        if (numCollisionEvents == 0) return;
        
        AppleEnemy apple = other.GetComponentInParent<AppleEnemy>();
        if (apple == null) return;
        
        // Initialize timer if this is a new enemy
        if (!damageTimers.ContainsKey(apple))
        {
            damageTimers[apple] = 0f;
        }
        
        // Deal damage if enough time has passed
        if (damageTimers[apple] >= damageTickInterval)
        {
            float finalDamage = damagePerTick;
            bool isCrit = false;
            
            // Check for critical hit
            if (critChance > 0 && Random.value < critChance)
            {
                finalDamage *= critMultiplier;
                isCrit = true;
                
                // Spawn crit effect
                if (criticalHitEffectPrefab != null)
                {
                    Instantiate(criticalHitEffectPrefab, apple.transform.position, Quaternion.identity);
                }
            }
            
            // Deal damage
            apple.TakeDamage(finalDamage);
            
            // Life steal
            if (lifeStealPercent > 0 && snakeHealth != null)
            {
                float healAmount = finalDamage * lifeStealPercent;
                snakeHealth.Heal(healAmount);
            }
            
            // Apply burn effect
            if (burnDuration > 0 && burnDamagePercent > 0)
            {
                ApplyBurnEffect(apple);
            }
            
            damageTimers[apple] = 0f;
            
            if (isCrit)
            {
                Debug.Log($"FireParticleDamage: Critical hit! Dealt {finalDamage:F1} damage");
            }
        }
    }
    
    /// <summary>
    /// Applies or refreshes a burn effect on an enemy
    /// </summary>
    private void ApplyBurnEffect(AppleEnemy apple)
    {
        float burnDamagePerSecond = damagePerTick * burnDamagePercent / damageTickInterval;
        
        if (burnEffects.ContainsKey(apple))
        {
            // Refresh existing burn
            BurnEffect burn = burnEffects[apple];
            burn.timer = 0f; // Reset duration
            burn.damagePerTick = Mathf.Max(burn.damagePerTick, burnDamagePerSecond * burn.tickInterval); // Use higher damage
            burnEffects[apple] = burn;
        }
        else
        {
            // Apply new burn
            BurnEffect burn = new BurnEffect
            {
                duration = burnDuration,
                timer = 0f,
                tickInterval = 0.5f,
                tickTimer = 0f,
                damagePerTick = burnDamagePerSecond * 0.5f // Damage per tick (0.5s interval)
            };
            
            // Spawn burn visual effect
            if (burnEffectPrefab != null)
            {
                burn.effectInstance = Instantiate(burnEffectPrefab, apple.transform);
                burn.effectInstance.transform.localPosition = Vector3.zero;
            }
            
            burnEffects[apple] = burn;
        }
    }
    
    /// <summary>
    /// Struct to track burn effect state
    /// </summary>
    private struct BurnEffect
    {
        public float duration;
        public float timer;
        public float tickInterval;
        public float tickTimer;
        public float damagePerTick;
        public GameObject effectInstance;
    }
}