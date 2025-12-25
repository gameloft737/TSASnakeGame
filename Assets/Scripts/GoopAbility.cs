using UnityEngine;

/// <summary>
/// Goop ability that creates a trail of slowing particles
/// </summary>
public class GoopAbility : BaseAbility
{
    [Header("Goop Settings")]
    [SerializeField] private ParticleSystem goopParticles;
    [SerializeField] private GoopParticleCollision collisionScript;
    public float effector = 0.5f; // Default slow multiplier
    
    [Header("Level Progression")]
    [SerializeField] private float effectorDecreasePerLevel = 0.15f; // How much slower per level
    
    protected override void Awake()
    {
        base.Awake(); // Call base to initialize duration system
    }
    
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
            collisionScript = goopParticles.GetComponent<GoopParticleCollision>();
            if (collisionScript == null)
            {
                collisionScript = goopParticles.gameObject.AddComponent<GoopParticleCollision>();
            }
            collisionScript.effector = effector;
        }
        else
        {
            Debug.LogWarning("GoopAbility: No ParticleSystem found!");
        }
    }
    
    protected override void ActivateAbility()
    {
        base.ActivateAbility();
        
        // Enable particle system
        if (goopParticles != null)
        {
            goopParticles.Play();
            Debug.Log($"GoopAbility: Activated at level {currentLevel} with effector {effector}");
        }
    }
    
    protected override void DeactivateAbility()
    {
        base.DeactivateAbility();
        
        // Stop particle system
        if (goopParticles != null)
        {
            goopParticles.Stop();
            Debug.Log("GoopAbility: Deactivated - particles stopped");
        }
        
        // Destroy the ability
        Destroy(gameObject);
    }
    
    protected override void OnLevelUp()
    {
        base.OnLevelUp();
        
        // Make goop more effective (slower enemies)
        effector -= effectorDecreasePerLevel;
        
        // Clamp to reasonable values (don't go below 0.1 = 10% speed)
        effector = Mathf.Max(effector, 0.1f);
        
        // Update collision script
        if (collisionScript != null)
        {
            collisionScript.effector = effector;
            Debug.Log($"GoopAbility: Leveled up! New slow multiplier: {effector}");
        }
    }
    
    private void OnDestroy()
    {
        // Clean up particle system
        if (goopParticles != null)
        {
            goopParticles.Stop();
        }
    }
}