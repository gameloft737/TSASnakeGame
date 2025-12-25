using UnityEngine;

public class FireAttack : Attack
{
    [Header("Fire Settings")]
    [SerializeField] private ParticleSystem fireParticles;
    [SerializeField] private FireParticleDamage particleDamage;
    
    [Header("Animation")]
    [SerializeField] private string animationBoolName = "Fire";
    [SerializeField] private Animator animator;

    private void Awake()
    {
        attackType = AttackType.Continuous;
        
        // Initialize particle damage with this attack's damage stat
        if (particleDamage != null)
        {
            particleDamage.Initialize(damage);
        }
    }

    protected override void OnActivate()
    {
        if (fireParticles != null)
        {
            fireParticles.Play();
        }
        
        if (animator != null)
        {
            animator.SetBool(animationBoolName, true);
        }
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
        }
        
        if (animator != null)
        {
            animator.SetBool(animationBoolName, false);
        }
    }
}