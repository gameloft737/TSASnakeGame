using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using System;

public class AppleEnemy : MonoBehaviour
{
    public static event Action<AppleEnemy> OnAppleDied;
    
    private NavMeshAgent agent;

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
    
    public bool isMetal = false;
    private Transform nearestBodyPart;
    private float contactTimer = 0f;
    private float biteTimer = 0f;
    private bool isInContact = false;
    private bool isBiting = false;
    private float currentHealth;
    private Coroutine reEnableAgentCoroutine;
    private bool wasInContactLastFrame = false;
    private Vector3 lastValidVelocity;
    private bool isInitialized = false;

    private bool isDead = false;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        
        currentHealth = maxHealth;
        
        if (biteParticles != null)
        {
            biteParticles.Stop();
        }
        
        // Auto-find if not already initialized by spawner
        if (!isInitialized)
        {
            Debug.Log("ayyyy");
            if (snakeBody == null)
            {
                snakeBody = FindFirstObjectByType<SnakeBody>();
            }
            
            if (snakeHealth == null)
            {
                snakeHealth = FindFirstObjectByType<SnakeHealth>();
            }
        }
        
        lastValidVelocity = agentObj != null ? agentObj.forward : transform.forward;
        
        StartCoroutine(TrackAndMonitorContact());
    }

    public void Initialize(SnakeBody body, SnakeHealth health)
    {
        snakeBody = body;
        snakeHealth = health;
        isInitialized = true;
    }

    void LateUpdate()
    {
        // Smooth rotation based on movement direction
        if (agentObj != null && agent.enabled)
        {
            Vector3 velocity = agent.velocity;
            
            if (velocity.sqrMagnitude > minVelocityForRotation * minVelocityForRotation)
            {
                lastValidVelocity = velocity.normalized;
            }
            
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

                    contactTimer += Time.deltaTime;

                    if (contactTimer >= contactTimeBeforeBiting)
                    {
                        if (!isBiting)
                        {
                            StartBiting();
                        }
                        
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
                    if (wasInContactLastFrame)
                    {
                        if (reEnableAgentCoroutine != null)
                        {
                            StopCoroutine(reEnableAgentCoroutine);
                        }
                        reEnableAgentCoroutine = StartCoroutine(ReEnableAgentAfterDelay());
                    }
                    
                    if (isBiting)
                    {
                        StopBiting();
                    }
                    
                    contactTimer = 0f;
                    biteTimer = 0f;

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
        
        // Face the nearest body part before biting
        if (nearestBodyPart != null && agentObj != null)
        {
            Vector3 directionToTarget = (nearestBodyPart.position - transform.position).normalized;
            if (directionToTarget.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                agentObj.rotation = targetRotation;
                lastValidVelocity = directionToTarget;
            }
        }
        
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
        float damage = UnityEngine.Random.Range(minDamage, maxDamage);
        
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


    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (biteParticles != null)
        {
            biteParticles.Stop();
        }
        
        OnAppleDied?.Invoke(this);
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