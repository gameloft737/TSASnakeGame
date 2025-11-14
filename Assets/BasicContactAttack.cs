using UnityEngine;
using System.Collections;

public class BasicContactAttack : MonoBehaviour
{
    [Header("Contact Attack Settings")]
    [SerializeField] private float damage;
    [SerializeField] private float damageDelay = 0.2f;
    [SerializeField] private float cooldown = 1f;
    [SerializeField] private float damageRadius = 3f;
    [SerializeField] private LayerMask enemyLayer;
    
    [SerializeField] private float lungeForce = 30f;
    
    [Header("Animation")]
    [SerializeField] private string attackTrigger = "bite";
    
    [SerializeField] private AttackManager attackManager;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private SnakeBody snakeBody;

    private float lastAttackTime = -999f;

    private void OnTriggerEnter(Collider other)
    {
        // Check cooldown
        if (Time.time < lastAttackTime + cooldown) return;

        Debug.Log(other.transform);
        AppleEnemy enemy = other.GetComponentInParent<AppleEnemy>();
            
        if (enemy != null)
        {
            lastAttackTime = Time.time;
            StartCoroutine(DealDamageAfterDelay());
        }
    }

    private IEnumerator DealDamageAfterDelay()
    {
        yield return new WaitForSeconds(damageDelay);
        
        if (attackManager != null)
        {
            attackManager.TriggerAnimation(attackTrigger);
        }
        
        // Find all enemies in radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, damageRadius, enemyLayer);
        
        foreach (Collider col in hitColliders)
        {
            AppleEnemy enemy = col.GetComponentInParent<AppleEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
        
        playerMovement.ApplyLunge(lungeForce);
        
        if (snakeBody != null)
        {
            snakeBody.ApplyForceToBody(transform.forward, lungeForce);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}