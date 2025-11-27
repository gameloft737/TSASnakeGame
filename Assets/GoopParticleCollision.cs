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
        
        Debug.Log("GoopParticleCollision: Collision enabled = " + collision.enabled);
        Debug.Log("GoopParticleCollision: Send messages = " + collision.sendCollisionMessages);
        Debug.Log("GoopParticleCollision: Lifetime loss = " + collision.lifetimeLoss);
    }
    
    private void OnParticleCollision(GameObject other)
    {
        Debug.Log("GOOP COLLISION DETECTED with: " + other.name);
        
        // Check if the collided object is an AppleEnemy
        AppleEnemy apple = other.GetComponent<AppleEnemy>();
        if (apple == null)
        {
            Debug.Log("No AppleEnemy on " + other.name + ", checking parent...");
            // Try checking parent
            apple = other.GetComponentInParent<AppleEnemy>();
        }
        
        if (apple != null)
        {
            Debug.Log("FOUND APPLE: " + apple.gameObject.name);
            
            // Check if this apple already has a goop slow effect
            GoopSlowEffect existingEffect = apple.GetComponentInChildren<GoopSlowEffect>();
            
            if (existingEffect == null)
            {
                Debug.Log("Creating NEW GoopSlowEffect on " + apple.gameObject.name);
                
                // Create the goop slow effect as a child of the apple
                GameObject goopEffect;
                
                if (goopSlowEffectPrefab != null)
                {
                    goopEffect = Instantiate(goopSlowEffectPrefab, apple.transform);
                    Debug.Log("Instantiated from prefab");
                }
                else
                {
                    goopEffect = new GameObject("GoopSlowEffect");
                    goopEffect.transform.SetParent(apple.transform);
                    goopEffect.AddComponent<GoopSlowEffect>();
                    Debug.Log("Created new GameObject with GoopSlowEffect component");
                }
                
                goopEffect.transform.localPosition = Vector3.zero;
                Debug.Log("GoopSlowEffect created successfully!");
            }
            else
            {
                Debug.Log("Refreshing EXISTING GoopSlowEffect");
                // Refresh the existing effect
                existingEffect.RefreshEffect();
            }
        }
        else
        {
            Debug.Log("NO APPLE FOUND on " + other.name + " or its parent!");
        }
    }
}