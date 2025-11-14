using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class AppleEnemy : MonoBehaviour
{
    private NavMeshAgent agent;
    private Rigidbody rb;

    [Header("References")]
    [SerializeField] private SnakeBody snakeBody;
    [SerializeField] private SnakeHealth snakeHealth;
    [SerializeField] private Transform agentObj;
    [SerializeField] private AppleChecker appleChecker;
    [SerializeField] private ParticleSystem biteParticles;
    
    [Header("Tracking Settings")]
    [SerializeField] private float contactDistance = 1.5f;
    
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float minVelocityForRotation = 0.1f;
    
    [Header("Biting Settings")]
    [SerializeField] private float contactTimeBeforeBiting = 0.5f;
    [SerializeField] private float biteDamageInterval = 0.5f;
    [SerializeField] private float minDamage = 5f;
    [SerializeField] private float maxDamage = 15f;
    [SerializeField] private float agentReEnableDelay = 0.5f;
    
    [Header("Apple Health")]
    [SerializeField] private float maxHealth = 100f;
    
    [Header("Physics")]
    [SerializeField] private float groundingForce = 20f;
    
    private Transform nearestBodyPart;
    private float contactTimer = 0f;
    private float biteTimer = 0f;
    private bool isInContact = false;
    private bool isBiting = false;
    private float currentHealth;
    private Coroutine reEnableAgentCoroutine;
    private bool wasInContactLastFrame = false;
    private Vector3 lastValidVelocity;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        
        rb = GetComponent<Rigidbody>();
        
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
        
        lastValidVelocity = agentObj != null ? agentObj.forward : transform.forward;
        
        StartCoroutine(TrackAndMonitorContact());
    }

    void FixedUpdate()
    {
        // Apply constant downward force to prevent flying
        if (rb != null)
        {
            rb.AddForce(Vector3.down * groundingForce, ForceMode.Force);
        }
    }

    void LateUpdate()
    {
        // Smooth rotation based on movement direction
        if (agentObj != null && agent.enabled)
        {
            Vector3 velocity = agent.velocity;
            
            // Only rotate if moving fast enough
            if (velocity.sqrMagnitude > minVelocityForRotation * minVelocityForRotation)
            {
                // Update last valid velocity
                lastValidVelocity = velocity.normalized;
            }
            
            // Always smoothly rotate towards last valid direction
            if (lastValidVelocity.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lastValidVelocity);
                agentObj.rotation = Quaternion.Slerp(agentObj.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
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
                    // Just entered contact - disable agent immediately
                    if (!wasInContactLastFrame)
                    {
                        if (reEnableAgentCoroutine != null)
                        {
                            StopCoroutine(reEnableAgentCoroutine);
                            reEnableAgentCoroutine = null;
                        }
                        
                        if (agent.enabled && agent.isOnNavMesh)
                        {
                            agent.velocity = Vector3.zero;
                            agent.enabled = false;
                        }
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
                    // Just left contact - schedule agent re-enable
                    if (wasInContactLastFrame)
                    {
                        if (reEnableAgentCoroutine != null)
                        {
                            StopCoroutine(reEnableAgentCoroutine);
                        }
                        reEnableAgentCoroutine = StartCoroutine(ReEnableAgentAfterDelay());
                    }
                    
                    // Lost contact - reset biting
                    if (isBiting)
                    {
                        StopBiting();
                    }
                    
                    contactTimer = 0f;
                    biteTimer = 0f;

                    // Continue tracking (only if agent is enabled)
                    nearestBodyPart = FindNearestBodyPart();

                    if (nearestBodyPart != null && agent.enabled && agent.isOnNavMesh)
                    {
                        agent.SetDestination(nearestBodyPart.position);
                    }
                }

                wasInContactLastFrame = isInContact;
            }
            else
            {
                nearestBodyPart = FindNearestBodyPart();
            }

            yield return null;
        }
    }

    private IEnumerator ReEnableAgentAfterDelay()
    {
        yield return new WaitForSeconds(agentReEnableDelay);
        
        if (!isInContact && !agent.enabled)
        {
            agent.enabled = true;
            
            // Set new destination immediately
            nearestBodyPart = FindNearestBodyPart();
            if (nearestBodyPart != null && agent.isOnNavMesh)
            {
                agent.SetDestination(nearestBodyPart.position);
            }
        }
        
        reEnableAgentCoroutine = null;
    }

    private void StartBiting()
    {
        isBiting = true;
        
        if (biteParticles != null)
        {
            biteParticles.Play();
        }
    }

    private void StopBiting()
    {
        isBiting = false;
        
        if (biteParticles != null)
        {
            biteParticles.Stop();
        }
    }

    private void DealDamage()
    {
        // Random damage within range
        float damage = Random.Range(minDamage, maxDamage);
        
        // Apply damage to snake
        if (snakeHealth != null)
        {
            snakeHealth.TakeDamage(damage);
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        
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