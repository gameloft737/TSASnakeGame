using UnityEngine;
using System.Collections.Generic;

public class FireParticleDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damageTickInterval = 0.1f;
    
    [Header("References")]
    [SerializeField] private ParticleSystem fireParticles;
    
    private float damagePerTick;
    private Dictionary<AppleEnemy, float> damageTimers = new Dictionary<AppleEnemy, float>();
    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    private void Awake()
    {
        if (fireParticles == null)
        {
            fireParticles = GetComponent<ParticleSystem>();
        }
    }

    public void Initialize(float damageAmount)
    {
        // Convert total damage to damage per tick
        damagePerTick = damageAmount * damageTickInterval;
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
            apple.TakeDamage(damagePerTick);
            damageTimers[apple] = 0f;
        }
    }
}