using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class AppleEnemy : MonoBehaviour
{
    private NavMeshAgent agent;

    [Header("References")]
    [SerializeField] private SnakeBody snakeBody;
    [SerializeField] private SnakeHealth snakeHealth; // ADD THIS LINE
    [SerializeField] private Transform agentObj;
    [SerializeField] private AppleChecker appleChecker;
    [SerializeField] private ParticleSystem biteParticles;
    
    [Header("Tracking Settings")]
    [SerializeField] private float contactDistance = 1.5f;
    
    [Header("Biting Settings")]
    [SerializeField] private float contactTimeBeforeBiting = 0.5f;
    [SerializeField] private float biteDamageInterval = 0.5f;
    [SerializeField] private float minDamage = 5f;
    [SerializeField] private float maxDamage = 15f;
    
    [Header("Apple Health")]
    [SerializeField] private float maxHealth = 100f;
    
    private Transform nearestBodyPart;
    private float contactTimer = 0f;
    private float biteTimer = 0f;
    private bool isInContact = false;
    private bool isBiting = false;
    private float currentHealth;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        
        currentHealth = maxHealth;
        
        if (biteParticles != null)
        {
            biteParticles.Stop();
        }
        
        // Auto-find SnakeHealth if not assigned
        if (snakeHealth == null && snakeBody != null)
        {
            snakeHealth = snakeBody.GetComponent<SnakeHealth>();
        }
        
        StartCoroutine(TrackAndMonitorContact());
    }

    private Transform FindNearestBodyPart()
    {
        if (snakeBody == null || snakeBody.bodyParts == null || snakeBody.bodyParts.Count == 0)
            return null;

        float nearestDistance = Mathf.Infinity;
        Transform nearest = null;

        foreach (var bodyPart in snakeBody.bodyParts)
        {
            if (bodyPart != null)
            {
                float distance = Vector3.Distance(transform.position, bodyPart.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = bodyPart.transform;
                }
            }
        }

        return nearest;
    }

    private bool IsAnyBodyPartNearby(out Transform closestPart)
    {
        closestPart = null;
        
        if (snakeBody == null || snakeBody.bodyParts == null || snakeBody.bodyParts.Count == 0)
            return false;

        float closestDistance = Mathf.Infinity;

        foreach (var bodyPart in snakeBody.bodyParts)
        {
            if (bodyPart != null)
            {
                float distance = Vector3.Distance(transform.position, bodyPart.transform.position);
                if (distance <= contactDistance && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPart = bodyPart.transform;
                }
            }
        }

        return closestPart != null;
    }

    private IEnumerator TrackAndMonitorContact()
    {
        nearestBodyPart = FindNearestBodyPart();

        while (true)
        {
            if (nearestBodyPart != null)
            {
                bool touchingSnake = appleChecker != null && appleChecker.isTouching;
                Transform contactedPart;
                isInContact = IsAnyBodyPartNearby(out contactedPart) || touchingSnake;

                if (isInContact)
                {
                    // Stop moving when in contact
                    if (agent.isOnNavMesh)
                    {
                        agent.SetDestination(transform.position);
                        agent.velocity = Vector3.zero;
                    }

                    // Increment contact timer
                    contactTimer += Time.deltaTime;

                    // Check if we should start biting
                    if (contactTimer >= contactTimeBeforeBiting)
                    {
                        if (!isBiting)
                        {
                            StartBiting();
                        }
                        
                        // Increment bite timer and deal damage periodically
                        biteTimer += Time.deltaTime;
                        if (biteTimer >= biteDamageInterval)
                        {
                            DealDamage();
                            biteTimer = 0f;
                        }
                    }
                }
                else
                {
                    // Lost contact - reset everything
                    if (isBiting)
                    {
                        StopBiting();
                    }
                    
                    contactTimer = 0f;
                    biteTimer = 0f;

                    // Continue tracking
                    nearestBodyPart = FindNearestBodyPart();

                    if (nearestBodyPart != null && agent.isOnNavMesh)
                    {
                        agent.SetDestination(nearestBodyPart.position);
                    }
                }

                // Face the direction of movement (only if moving)
                if (agentObj != null && agent.velocity.sqrMagnitude > 0.01f)
                {
                    float angle = Mathf.Atan2(agent.velocity.z, agent.velocity.x) * Mathf.Rad2Deg;
                    agentObj.transform.rotation = Quaternion.Euler(0, -angle + 90, 0);
                }
            }
            else
            {
                nearestBodyPart = FindNearestBodyPart();
            }

            yield return null;
        }
    }

    private void StartBiting()
    {
        isBiting = true;
        
        if (biteParticles != null)
        {
            biteParticles.Play();
        }
        
        Debug.Log("Apple started biting!");
    }

    private void StopBiting()
    {
        isBiting = false;
        
        if (biteParticles != null)
        {
            biteParticles.Stop();
        }
        
        Debug.Log("Apple stopped biting!");
    }

    private void DealDamage()
    {
        // Random damage within range
        float damage = Random.Range(minDamage, maxDamage);
        
        // Apply damage to snake
        if (snakeHealth != null)
        {
            snakeHealth.TakeDamage(damage);
            Debug.Log($"Apple dealt {damage:F1} damage to snake!");
        }
        else
        {
            Debug.LogWarning("SnakeHealth reference is missing! Cannot deal damage.");
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        
        Debug.Log($"Apple took {damage:F1} damage! Health: {currentHealth:F1}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Apple destroyed!");
        
        if (biteParticles != null)
        {
            biteParticles.Stop();
        }
        
        Destroy(gameObject);
    }

    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isBiting ? Color.red : (isInContact ? Color.green : Color.yellow);
        Gizmos.DrawWireSphere(transform.position, contactDistance);
        
        if (nearestBodyPart != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, nearestBodyPart.position);
        }
    }
}