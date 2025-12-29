using UnityEngine;

public class FireAttack : Attack
{
    [Header("Fire Settings")]
    [SerializeField] private ParticleSystem fireParticles;
    [SerializeField] private FireParticleDamage particleDamage;
    
    [Header("Animation")]
    [SerializeField] private string animationBoolName = "Fire";
    [SerializeField] private Animator animator;
    
    // Store original particle settings to scale from
    private float originalStartLifetime;
    private float originalStartSpeed;
    private bool hasStoredOriginals = false;

    private void Awake()
    {
        attackType = AttackType.Continuous;
        
        // Initialize particle damage with this attack's damage stat
        if (particleDamage != null)
        {
            particleDamage.Initialize(damage);
        }
        
        // Store original particle settings
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

    protected override void OnActivate()
    {
        if (fireParticles != null)
        {
            // Apply range multiplier to particle system range
            ApplyRangeMultiplierToParticles();
            fireParticles.Play();
        }
        
        if (animator != null)
        {
            animator.SetBool(animationBoolName, true);
        }
    }
    
    /// <summary>
    /// Applies the range multiplier from PlayerStats to the fire particle system
    /// This scales the startLifetime to make particles travel further
    /// </summary>
    private void ApplyRangeMultiplierToParticles()
    {
        if (fireParticles == null || !hasStoredOriginals) return;
        
        float rangeMultiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetRangeMultiplier() : 1f;
        
        var main = fireParticles.main;
        // Scale lifetime to increase range (particles travel further before dying)
        main.startLifetime = originalStartLifetime * rangeMultiplier;
        // Optionally also scale speed slightly for more dramatic effect
        main.startSpeed = originalStartSpeed * Mathf.Sqrt(rangeMultiplier); // Square root for balanced scaling
    }

    protected override void OnHoldUpdate()
    {
        // Keep particles playing while held
        if (fireParticles != null && !fireParticles.isPlaying)
        {
            fireParticles.Play();
        }
    }

    protected override void OnDeactivate()
    {
        if (fireParticles != null)
        {
            fireParticles.Stop();
            // Restore original settings when deactivated
            RestoreOriginalParticleSettings();
        }
        
        if (animator != null)
        {
            animator.SetBool(animationBoolName, false);
        }
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
}