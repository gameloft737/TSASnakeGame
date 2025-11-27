using UnityEngine;

/// <summary>
/// Goop ability that creates a trail of slowing particles
/// </summary>
public class GoopAbility : BaseAbility
{
    [Header("Goop Settings")]
    [SerializeField] private ParticleSystem goopParticles;
    
    private void Start()
    {
        // If no particle system is assigned, try to find one on this GameObject
        if (goopParticles == null)
        {
            goopParticles = GetComponent<ParticleSystem>();
        }
        
        // Connect the particle system to the collision script if it exists
        if (goopParticles != null)
        {
            GoopParticleCollision collisionScript = goopParticles.GetComponent<GoopParticleCollision>();
            if (collisionScript == null)
            {
                goopParticles.gameObject.AddComponent<GoopParticleCollision>();
            }
        }
    }
}