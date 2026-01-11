using UnityEngine;

/// <summary>
/// Component attached to body fire emitters that syncs with the main FireAttack.
/// Emits fire particles from body parts when the main fire breath is active.
/// </summary>
public class BodyFireEmitter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem fireParticles;
    [SerializeField] private FireParticleDamage particleDamage;
    
    private FireAttack mainFireAttack;
    private bool isInitialized = false;
    
    // Store original particle settings to scale from
    private float originalStartLifetime;
    private float originalStartSpeed;
    private bool hasStoredOriginals = false;
    
    private void Awake()
    {
        if (fireParticles == null)
        {
            fireParticles = GetComponent<ParticleSystem>();
        }
        
        if (particleDamage == null)
        {
            particleDamage = GetComponent<FireParticleDamage>();
        }
        
        StoreOriginalParticleSettings();
    }
    
    private void StoreOriginalParticleSettings()
    {
        if (fireParticles != null && !hasStoredOriginals)
        {
            var main = fireParticles.main;
            originalStartLifetime = main.startLifetime.constant;
            originalStartSpeed = main.startSpeed.constant;
            hasStoredOriginals = true;
        }
    }
    
    /// <summary>
    /// Initialize the body fire emitter with reference to the main fire attack
    /// </summary>
    public void Initialize(FireAttack fireAttack, float damage, float lifeSteal, float critChance, float critMultiplier, float burnDamage, float burnDuration)
    {
        mainFireAttack = fireAttack;
        
        if (particleDamage != null)
        {
            particleDamage.Initialize(damage, lifeSteal, critChance, critMultiplier, burnDamage, burnDuration);
        }
        
        isInitialized = true;
        
        // Make sure particles are stopped initially
        if (fireParticles != null && fireParticles.isPlaying)
        {
            fireParticles.Stop();
        }
    }
    
    /// <summary>
    /// Updates the damage stats (called when the main attack is upgraded)
    /// </summary>
    public void UpdateStats(float damage, float lifeSteal, float critChance, float critMultiplier, float burnDamage, float burnDuration)
    {
        if (particleDamage != null)
        {
            particleDamage.Initialize(damage, lifeSteal, critChance, critMultiplier, burnDamage, burnDuration);
        }
    }
    
    private void Update()
    {
        if (!isInitialized || mainFireAttack == null) return;
        
        // Sync with main fire attack state
        bool shouldBeActive = mainFireAttack.IsActive();
        
        if (shouldBeActive && !fireParticles.isPlaying)
        {
            ApplyRangeMultiplierToParticles();
            fireParticles.Play();
        }
        else if (!shouldBeActive && fireParticles.isPlaying)
        {
            fireParticles.Stop();
            RestoreOriginalParticleSettings();
        }
    }
    
    /// <summary>
    /// Applies the range multiplier from PlayerStats to the fire particle system
    /// </summary>
    private void ApplyRangeMultiplierToParticles()
    {
        if (fireParticles == null || !hasStoredOriginals) return;
        
        float rangeMultiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetRangeMultiplier() : 1f;
        
        var main = fireParticles.main;
        main.startLifetime = originalStartLifetime * rangeMultiplier;
        main.startSpeed = originalStartSpeed * Mathf.Sqrt(rangeMultiplier);
    }
    
    /// <summary>
    /// Restores the original particle settings
    /// </summary>
    private void RestoreOriginalParticleSettings()
    {
        if (fireParticles == null || !hasStoredOriginals) return;
        
        var main = fireParticles.main;
        main.startLifetime = originalStartLifetime;
        main.startSpeed = originalStartSpeed;
    }
    
    /// <summary>
    /// Force stop the fire particles
    /// </summary>
    public void ForceStop()
    {
        if (fireParticles != null)
        {
            fireParticles.Stop();
            RestoreOriginalParticleSettings();
        }
    }
    
    /// <summary>
    /// Gets the particle system reference
    /// </summary>
    public ParticleSystem GetParticleSystem() => fireParticles;
}