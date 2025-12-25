using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles particle collision detection and spawns goop slow effects on AppleEnemy
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class GoopParticleCollision : MonoBehaviour
{
    [Header("Goop Effect Settings")]
    [SerializeField] private GameObject goopSlowEffectPrefab;
    
    private ParticleSystem ps;
    private List<ParticleCollisionEvent> collisionEvents;
    public float effector;
    
    private void Start()
    {
        ps = GetComponent<ParticleSystem>();
        collisionEvents = new List<ParticleCollisionEvent>();
        
        Debug.Log("GoopParticleCollision: Initializing on " + gameObject.name);
        
        // Ensure collision is enabled
        var collision = ps.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.sendCollisionMessages = true;
        
        // IMPORTANT: Don't kill particles on collision!
        collision.lifetimeLoss = 0f; // Particles keep their lifetime
    }
    
    private void OnParticleCollision(GameObject other)
    {
        
        // Check if the collided object is an AppleEnemy
        AppleEnemy apple = other.GetComponent<AppleEnemy>();
        if (apple == null)
        {
            // Try checking parent
            apple = other.GetComponentInParent<AppleEnemy>();
        }
        
        if (apple != null)
        {
            
            // Check if this apple already has a goop slow effect
            GoopSlowEffect existingEffect = apple.GetComponentInChildren<GoopSlowEffect>();
            
            if (existingEffect == null)
            {
                
                // Create the goop slow effect as a child of the apple
                GameObject goopEffect;
                
                if (goopSlowEffectPrefab != null)
                {
                    goopEffect = Instantiate(goopSlowEffectPrefab, apple.transform);
                    goopEffect.GetComponent<GoopSlowEffect>().speedMultiplier = effector;
                }
                else
                {
                    goopEffect = new GameObject("GoopSlowEffect");
                    goopEffect.transform.SetParent(apple.transform);
                    goopEffect.AddComponent<GoopSlowEffect>();
                }
                
                goopEffect.transform.localPosition = Vector3.zero;
            }
            else
            {
                // Refresh the existing effect
                existingEffect.RefreshEffect();
            }
        }
    }
}