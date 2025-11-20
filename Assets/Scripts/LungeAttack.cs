using UnityEngine;
using System.Collections.Generic;

public class LungeAttack : Attack
{
    [Header("Lunge Settings")]
    [SerializeField] private float lungeForce = 30f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float hitRadius = 1.5f;
    [SerializeField] private float hitRange = 2.5f;
    
    [Header("Animation")]
    [SerializeField] private string attackTrigger = "Lunge";
    
    [SerializeField] private Transform orientation;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private SnakeBody snakeBody;
    [SerializeField] private AttackManager attackManager;
    
    private HashSet<AppleEnemy> hitEnemies = new HashSet<AppleEnemy>();

    private void Awake()
    {
        attackType = AttackType.Burst;
    }

    protected override void OnActivate()
    {
        ExecuteLunge();
    }

    private void ExecuteLunge()
    {
        if (orientation == null || playerMovement == null) return;

        if (attackManager != null)
        {
            attackManager.TriggerAnimation(attackTrigger);
        }

        playerMovement.ApplyLunge(lungeForce);
        
        if (snakeBody != null)
        {
            snakeBody.ApplyForceToBody(orientation.forward, lungeForce);
        }
        
        hitEnemies.Clear();
        
        Collider[] hitColliders = Physics.OverlapSphere(
            orientation.position + orientation.forward * (hitRange * 0.5f), 
            hitRadius, 
            enemyLayer
        );
        
        foreach (Collider col in hitColliders)
        {
            AppleEnemy apple = col.GetComponentInParent<AppleEnemy>();
            
            if (apple != null && !hitEnemies.Contains(apple))
            {
                hitEnemies.Add(apple);
                apple.TakeDamage(damage);
                Debug.Log($"Lunge hit {apple.name} for {damage} damage!");
            }
        }
        
        if (hitEnemies.Count > 0)
        {
            Debug.Log($"Lunge attack hit {hitEnemies.Count} enemies!");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (orientation == null) return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(orientation.position + orientation.forward * (hitRange * 0.5f), hitRadius);
        Gizmos.DrawLine(orientation.position, orientation.position + orientation.forward * hitRange);
    }
}
