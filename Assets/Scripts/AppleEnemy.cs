using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using System;

public class AppleEnemy : MonoBehaviour
{
    public static event Action<AppleEnemy> OnAppleDied;
    
    private static SnakeBody s_cachedSnakeBody;
    private static SnakeHealth s_cachedSnakeHealth;
    private static bool s_referencesSearched = false;
    
    [Header("References")]
    [SerializeField] private Transform agentObj;
    [SerializeField] private AppleChecker appleChecker;
    [SerializeField] private ParticleSystem biteParticles;
    [SerializeField] private GameObject deathObjectPrefab;
    [SerializeField] private GameObject xpDropPrefab;
    
    [Header("XP Drop Settings")]
    [SerializeField] private int minXPDrop = 5;
    [SerializeField] private int maxXPDrop = 15;
    [SerializeField] private int xpDropCount = 3;
    
    [Header("Tracking Settings")]
    [SerializeField] private float contactDistance = 1.5f;
    [SerializeField] private float trackingUpdateInterval = 0.1f;
    
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float minVelocityForRotation = 0.1f;
    
    [Header("Biting Settings")]
    [SerializeField] private float contactTimeBeforeBiting = 0.5f;
    [SerializeField] private float biteDamageInterval = 0.5f;
    [SerializeField] private float minDamage = 5f;
    [SerializeField] private float maxDamage = 15f;
    [SerializeField] private float agentReEnableDelay = 0.5f;
    
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    
    public bool isMetal = false;
    
    private NavMeshAgent agent;
    private SnakeBody snakeBody;
    private SnakeHealth snakeHealth;
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
    private bool isFrozen = false;
    private Vector3 frozenVelocity;
    private bool wasAgentEnabled;
    private float contactDistanceSqr;
    private WaitForSeconds trackingWait;
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        
        currentHealth = maxHealth;
        contactDistanceSqr = contactDistance * contactDistance;
        trackingWait = new WaitForSeconds(trackingUpdateInterval);
        
        if (biteParticles) biteParticles.Stop();
        
        if (!isInitialized)
        {
            if (!s_referencesSearched)
            {
                s_cachedSnakeBody = FindFirstObjectByType<SnakeBody>();
                s_cachedSnakeHealth = FindFirstObjectByType<SnakeHealth>();
                s_referencesSearched = true;
            }
            snakeBody = s_cachedSnakeBody;
            snakeHealth = s_cachedSnakeHealth;
        }
        
        lastValidVelocity = agentObj ? agentObj.forward : transform.forward;
        StartCoroutine(TrackAndMonitorContact());
    }
    
    public static void SetSnakeReferences(SnakeBody body, SnakeHealth health)
    {
        s_cachedSnakeBody = body;
        s_cachedSnakeHealth = health;
        s_referencesSearched = true;
    }
    
    public static void ClearCachedReferences()
    {
        s_cachedSnakeBody = null;
        s_cachedSnakeHealth = null;
        s_referencesSearched = false;
    }

    public void Initialize(SnakeBody body, SnakeHealth health)
    {
        snakeBody = body;
        snakeHealth = health;
        isInitialized = true;
    }

    void LateUpdate()
    {
        if (isFrozen || !agentObj || !agent.enabled) return;
        
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

    private Transform FindNearestBodyPart()
    {
        if (!snakeBody || snakeBody.bodyParts == null || snakeBody.bodyParts.Count == 0)
            return null;

        float nearestDistanceSqr = float.MaxValue;
        Transform nearest = null;
        Vector3 myPos = transform.position;

        var bodyParts = snakeBody.bodyParts;
        int count = bodyParts.Count;
        for (int i = 0; i < count; i++)
        {
            var bodyPart = bodyParts[i];
            if (bodyPart)
            {
                float distanceSqr = (bodyPart.transform.position - myPos).sqrMagnitude;
                if (distanceSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    nearest = bodyPart.transform;
                }
            }
        }

        return nearest;
    }

    private bool IsAnyBodyPartNearby(out Transform closestPart)
    {
        closestPart = null;
        
        if (!snakeBody || snakeBody.bodyParts == null || snakeBody.bodyParts.Count == 0)
            return false;

        float closestDistanceSqr = float.MaxValue;
        Vector3 myPos = transform.position;

        var bodyParts = snakeBody.bodyParts;
        int count = bodyParts.Count;
        for (int i = 0; i < count; i++)
        {
            var bodyPart = bodyParts[i];
            if (bodyPart)
            {
                float distanceSqr = (bodyPart.transform.position - myPos).sqrMagnitude;
                if (distanceSqr <= contactDistanceSqr && distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestPart = bodyPart.transform;
                }
            }
        }

        return closestPart != null;
    }

    private IEnumerator TrackAndMonitorContact()
    {
        nearestBodyPart = FindNearestBodyPart();
        float lastUpdateTime = Time.time;

        while (true)
        {
            if (isFrozen)
            {
                yield return trackingWait;
                lastUpdateTime = Time.time;
                continue;
            }
            
            float deltaTime = Time.time - lastUpdateTime;
            lastUpdateTime = Time.time;
            
            if (nearestBodyPart)
            {
                bool touchingSnake = appleChecker && appleChecker.isTouching;
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

                    contactTimer += deltaTime;

                    if (contactTimer >= contactTimeBeforeBiting)
                    {
                        if (!isBiting) StartBiting();
                        
                        biteTimer += deltaTime;
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
                        if (reEnableAgentCoroutine != null) StopCoroutine(reEnableAgentCoroutine);
                        reEnableAgentCoroutine = StartCoroutine(ReEnableAgentAfterDelay());
                    }
                    
                    if (isBiting) StopBiting();
                    
                    contactTimer = 0f;
                    biteTimer = 0f;

                    nearestBodyPart = FindNearestBodyPart();

                    if (nearestBodyPart && agent.enabled && agent.isOnNavMesh)
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

            yield return trackingWait;
        }
    }

    private IEnumerator ReEnableAgentAfterDelay()
    {
        yield return new WaitForSeconds(agentReEnableDelay);
        
        if (!isInContact && !agent.enabled)
        {
            agent.enabled = true;
            
            nearestBodyPart = FindNearestBodyPart();
            if (nearestBodyPart && agent.isOnNavMesh)
            {
                agent.SetDestination(nearestBodyPart.position);
            }
        }
        
        reEnableAgentCoroutine = null;
    }

    private void StartBiting()
    {
        isBiting = true;
        
        if (nearestBodyPart && agentObj)
        {
            Vector3 directionToTarget = (nearestBodyPart.position - transform.position).normalized;
            if (directionToTarget.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                agentObj.rotation = targetRotation;
                lastValidVelocity = directionToTarget;
            }
        }
        
        if (biteParticles) biteParticles.Play();
    }

    private void StopBiting()
    {
        isBiting = false;
        if (biteParticles) biteParticles.Stop();
    }

    private void DealDamage()
    {
        float damage = UnityEngine.Random.Range(minDamage, maxDamage);
        if (snakeHealth) snakeHealth.TakeDamage(damage);
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0) Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (biteParticles) biteParticles.Stop();
        if (deathObjectPrefab) Instantiate(deathObjectPrefab, transform.position, transform.rotation);
        
        SpawnXPDrops();
        OnAppleDied?.Invoke(this);
        Destroy(gameObject);
    }
    
    private void SpawnXPDrops()
    {
        if (!xpDropPrefab)
        {
            Debug.LogWarning("XP Drop Prefab not assigned on " + gameObject.name);
            return;
        }
        
        for (int i = 0; i < xpDropCount; i++)
        {
            Vector3 spawnOffset = new Vector3(
                UnityEngine.Random.Range(-0.5f, 0.5f),
                0.5f,
                UnityEngine.Random.Range(-0.5f, 0.5f)
            );
            
            GameObject xpDrop = Instantiate(xpDropPrefab, transform.position + spawnOffset, Quaternion.identity);
            
            XPDrop xpDropScript = xpDrop.GetComponent<XPDrop>();
            if (xpDropScript)
            {
                int xpValue = UnityEngine.Random.Range(minXPDrop, maxXPDrop + 1);
                xpDropScript.Initialize(xpValue);
            }
        }
    }

    public float GetHealthPercentage() => currentHealth / maxHealth;

    public void SetFrozen(bool frozen)
    {
        if (frozen && !isFrozen)
        {
            wasAgentEnabled = agent.enabled;
            if (agent.enabled && agent.isOnNavMesh)
            {
                frozenVelocity = agent.velocity;
                agent.velocity = Vector3.zero;
                agent.isStopped = true;
                agent.enabled = false; // Fully disable the agent to prevent any movement
            }
            
            if (biteParticles && biteParticles.isPlaying) biteParticles.Pause();
        }
        else if (!frozen && isFrozen)
        {
            // Re-enable the agent if it was enabled before freezing
            if (wasAgentEnabled)
            {
                agent.enabled = true;
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.velocity = frozenVelocity;
                    
                    // Re-acquire target after unfreezing
                    nearestBodyPart = FindNearestBodyPart();
                    if (nearestBodyPart)
                    {
                        agent.SetDestination(nearestBodyPart.position);
                    }
                }
            }
            
            if (biteParticles && isBiting) biteParticles.Play();
        }
        
        isFrozen = frozen;
    }

    public bool IsFrozen() => isFrozen;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isBiting ? Color.red : (isInContact ? Color.green : Color.yellow);
        Gizmos.DrawWireSphere(transform.position, contactDistance);
        
        if (nearestBodyPart)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, nearestBodyPart.position);
        }
    }
}