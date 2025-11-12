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
    
    private Transform orientation;
    private PlayerMovement playerMovement;
    private SnakeBody snakeBody;
    private AttackManager attackManager;
    private HashSet<AppleEnemy> hitEnemies = new HashSet<AppleEnemy>();

    void Start()
    {
        // Get components from parent (the snake head)
        playerMovement = GetComponentInParent<PlayerMovement>();
        snakeBody = GetComponentInParent<SnakeBody>();
        attackManager = GetComponentInParent<AttackManager>();
        
        if (playerMovement != null)
        {
            orientation = playerMovement.transform.Find("Orientation");
        }
    }

    protected override void Use()
    {
        if (orientation == null || playerMovement == null) return;

        // Trigger animation through AttackManager
        if (attackManager != null)
        {
            attackManager.TriggerAnimation(attackTrigger);
        }

        // Tell player movement to apply the lunge
        playerMovement.ApplyLunge(lungeForce);
        
        // Tell snake body to apply lunge to all segments
        if (snakeBody != null)
        {
            snakeBody.ApplyForceToBody(orientation.forward, lungeForce);
        }
        
        // Clear previous hits
        hitEnemies.Clear();
        
        // Use OverlapSphere for better performance (no raycast needed)
        Collider[] hitColliders = Physics.OverlapSphere(
            orientation.position + orientation.forward * (hitRange * 0.5f), 
            hitRadius, 
            enemyLayer
        );
        
        foreach (Collider col in hitColliders)
        {
            // Try to find AppleEnemy in the hit object or its parents
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
        else
        {
            Debug.Log("Lunge attack missed!");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (orientation == null) return;
        
        Gizmos.color = Color.red;
        // Draw the actual hit detection sphere
        Gizmos.DrawWireSphere(orientation.position + orientation.forward * (hitRange * 0.5f), hitRadius);
        // Draw direction line
        Gizmos.DrawLine(orientation.position, orientation.position + orientation.forward * hitRange);
    }
}