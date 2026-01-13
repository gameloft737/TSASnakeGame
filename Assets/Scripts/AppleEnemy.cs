using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using System;

public class AppleEnemy : MonoBehaviour, IPooledObject
{
    public static event Action<AppleEnemy> OnAppleDied;
    
    private static SnakeBody s_cachedSnakeBody;
    private static SnakeHealth s_cachedSnakeHealth;
    private static bool s_referencesSearched = false;
    
    // Static list of all active AppleEnemies for efficient ally/enemy targeting
    private static List<AppleEnemy> s_allAppleEnemies = new List<AppleEnemy>(64);
    
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
    [SerializeField] private float trackingUpdateInterval = 0.1f; // Kept for legacy/fallback
    
    [Header("Performance")]
    [Tooltip("Use centralized manager for updates (better performance with many enemies)")]
    [SerializeField] private bool useManagerUpdates = true;
    
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
    private float baseMaxHealth = 0f; // Will be set from maxHealth on first use
    
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
    
    // Manager-based update tracking
    private bool usesManagerUpdates = false;
    private float lastManagerUpdateTime = 0f;
    private float pendingAgentReEnableTime = -1f; // -1 means not pending
    private bool needsDestinationUpdate = true; // Track if we need to set a destination
    
    // Classic mode state
    private bool isClassicMode = false;
    private Vector3 lastCardinalDirection = Vector3.forward; // Last movement direction (cardinal only)
    private float classicMoveTimer = 0f;
    private float classicMoveInterval = 0.3f;
    private Vector3 classicTargetPosition;
    private bool hasClassicTarget = false;
    
    // Knockback state
    private bool isKnockedBack = false;
    private Vector3 knockbackVelocity;
    private float knockbackTimer = 0f;
    private float knockbackDuration = 0f;
    
    // Ally state
    private bool isAlly = false;
    private float allyDamageMultiplier = 1f;
    private AppleEnemy currentEnemyTarget; // Target enemy apple when ally
    private Renderer[] renderers; // Cached renderers for visual changes
    private Material[] originalMaterials; // Store original material for EACH renderer
    private Color[] originalColors; // Store original color for EACH renderer
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        
        // Register this enemy in the static list for efficient lookups
        if (!s_allAppleEnemies.Contains(this))
        {
            s_allAppleEnemies.Add(this);
        }
        
        // Cache renderers for visual changes (only if not already cached)
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                // Store original materials and colors for ALL renderers
                originalMaterials = new Material[renderers.Length];
                originalColors = new Color[renderers.Length];
                
                for (int i = 0; i < renderers.Length; i++)
                {
                    originalMaterials[i] = renderers[i].material;
                    // Check for both _Color and _BaseColor (URP/HDRP compatibility)
                    if (originalMaterials[i].HasProperty("_Color"))
                    {
                        originalColors[i] = originalMaterials[i].color;
                    }
                    else if (originalMaterials[i].HasProperty("_BaseColor"))
                    {
                        originalColors[i] = originalMaterials[i].GetColor("_BaseColor");
                    }
                    else
                    {
                        originalColors[i] = Color.white;
                    }
                }
            }
        }
        
        // Ensure base max health is set
        EnsureBaseHealthInitialized();
        
        // Only set currentHealth if it hasn't been set by Initialize
        if (currentHealth <= 0)
        {
            currentHealth = maxHealth;
        }
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
        
        // Check if we should use manager-based updates (better performance)
        usesManagerUpdates = useManagerUpdates && AppleEnemyManager.Instance != null;
        
        if (usesManagerUpdates)
        {
            // Register with manager - it will handle our updates
            AppleEnemyManager.Instance.RegisterEnemy(this);
            // Find initial target using optimized method
            nearestBodyPart = FindNearestBodyPartOptimized();
        }
        else
        {
            // Fallback to legacy coroutine-based updates
            StartCoroutine(TrackAndMonitorContact());
        }
    }
    
    /// <summary>
    /// Called when this object is spawned from the object pool.
    /// Resets all state to prepare for reuse.
    /// </summary>
    public void OnSpawnFromPool()
    {
        // CRITICAL: Reset death state FIRST before anything else
        isDead = false;
        
        // Reset health - ensure base health is initialized before setting current health
        EnsureBaseHealthInitialized();
        // Reset maxHealth to base value (will be scaled by Initialize() if needed)
        maxHealth = baseMaxHealth;
        currentHealth = maxHealth;
        
        // Reset contact/biting state
        contactTimer = 0f;
        biteTimer = 0f;
        isInContact = false;
        isBiting = false;
        wasInContactLastFrame = false;
        
        // Reset freeze state
        isFrozen = false;
        wasAgentEnabled = true;
        
        // Reset knockback state
        isKnockedBack = false;
        knockbackVelocity = Vector3.zero;
        knockbackTimer = 0f;
        knockbackDuration = 0f;
        
        // Reset ally state
        isAlly = false;
        allyDamageMultiplier = 1f;
        currentEnemyTarget = null;
        
        // Reset classic mode state
        isClassicMode = false;
        classicMoveTimer = 0f;
        hasClassicTarget = false;
        
        // Reset destination tracking
        needsDestinationUpdate = true;
        
        // Reset manager update tracking
        lastManagerUpdateTime = 0f;
        pendingAgentReEnableTime = -1f;
        
        // Reset initialization flag so references get set properly
        isInitialized = false;
        
        // CRITICAL: Always ensure we're in the static list
        // Remove first to avoid duplicates, then add
        s_allAppleEnemies.Remove(this);
        s_allAppleEnemies.Add(this);
        
        // Restore original visuals
        RestoreOriginalVisuals();
        
        // Get the agent component if not cached
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }
        
        // Re-enable agent
        if (agent != null)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.velocity = Vector3.zero;
            }
        }
        
        // Stop any particles
        if (biteParticles) biteParticles.Stop();
        
        // Get references
        if (!s_referencesSearched)
        {
            s_cachedSnakeBody = FindFirstObjectByType<SnakeBody>();
            s_cachedSnakeHealth = FindFirstObjectByType<SnakeHealth>();
            s_referencesSearched = true;
        }
        snakeBody = s_cachedSnakeBody;
        snakeHealth = s_cachedSnakeHealth;
        
        // Stop all coroutines before starting new ones
        StopAllCoroutines();
        reEnableAgentCoroutine = null;
        
        // Check if we should use manager-based updates
        usesManagerUpdates = useManagerUpdates && AppleEnemyManager.Instance != null;
        
        if (usesManagerUpdates)
        {
            // Register with manager
            AppleEnemyManager.Instance.RegisterEnemy(this);
            nearestBodyPart = FindNearestBodyPartOptimized();
        }
        else
        {
            // Find initial target
            nearestBodyPart = FindNearestBodyPart();
            // Start tracking coroutine only if not using manager
            StartCoroutine(TrackAndMonitorContact());
        }
        
        lastValidVelocity = agentObj ? agentObj.forward : transform.forward;
        
        #if UNITY_EDITOR
        Debug.Log($"[AppleEnemy] OnSpawnFromPool complete for {gameObject.name}: isDead={isDead}, health={currentHealth}/{maxHealth}, inList={s_allAppleEnemies.Contains(this)}");
        #endif
    }
    
    /// <summary>
    /// Called when this object is returned to the object pool.
    /// Cleans up state before deactivation.
    /// </summary>
    public void OnReturnToPool()
    {
        // Stop all coroutines
        StopAllCoroutines();
        reEnableAgentCoroutine = null;
        pendingAgentReEnableTime = -1f;
        
        // Unregister from manager if using it
        if (usesManagerUpdates && AppleEnemyManager.Instance != null)
        {
            AppleEnemyManager.Instance.UnregisterEnemy(this);
        }
        
        // Unregister from static list
        s_allAppleEnemies.Remove(this);
        
        // Stop particles
        if (biteParticles) biteParticles.Stop();
        
        // Disable agent
        if (agent != null && agent.enabled)
        {
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }
        
        // Clear references
        nearestBodyPart = null;
        currentEnemyTarget = null;
    }
    
    void OnDestroy()
    {
        // Unregister from static list when destroyed (safety net for non-pooled destruction)
        s_allAppleEnemies.Remove(this);
    }
    
    void OnDisable()
    {
        // Also unregister when disabled (for pooling)
        s_allAppleEnemies.Remove(this);
        
        // Unregister from manager when disabled
        if (usesManagerUpdates && AppleEnemyManager.Instance != null)
        {
            AppleEnemyManager.Instance.UnregisterEnemy(this);
        }
    }
    
    void OnEnable()
    {
        // Re-register when enabled (for pooling)
        // Remove first to avoid duplicates, then add
        s_allAppleEnemies.Remove(this);
        s_allAppleEnemies.Add(this);
        
        // Re-register with manager if we use manager updates
        if (usesManagerUpdates && AppleEnemyManager.Instance != null)
        {
            AppleEnemyManager.Instance.RegisterEnemy(this);
        }
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
    
    /// <summary>
    /// Gets the static list of all active AppleEnemies.
    /// Use this instead of FindObjectsByType for better performance.
    /// </summary>
    public static List<AppleEnemy> GetAllActiveEnemies()
    {
        return s_allAppleEnemies;
    }
    
    /// <summary>
    /// Gets the count of all active AppleEnemies without allocating.
    /// </summary>
    public static int GetActiveEnemyCount()
    {
        return s_allAppleEnemies.Count;
    }

    public void Initialize(SnakeBody body, SnakeHealth health)
    {
        snakeBody = body;
        snakeHealth = health;
        isInitialized = true;
    }
    
    /// <summary>
    /// Initialize with a health multiplier for wave scaling
    /// </summary>
    public void Initialize(SnakeBody body, SnakeHealth health, float healthMultiplier)
    {
        snakeBody = body;
        snakeHealth = health;
        isInitialized = true;
        
        // Ensure baseMaxHealth is set before applying multiplier
        EnsureBaseHealthInitialized();
        
        // Apply health multiplier (only if > 1, otherwise keep base health)
        if (healthMultiplier > 1f)
        {
            maxHealth = baseMaxHealth * healthMultiplier;
            currentHealth = maxHealth;
        }
        else
        {
            // Keep at base health for multiplier of 1 or less
            maxHealth = baseMaxHealth;
            currentHealth = maxHealth;
        }
    }
    
    /// <summary>
    /// Ensures baseMaxHealth is initialized from the serialized maxHealth value
    /// </summary>
    private void EnsureBaseHealthInitialized()
    {
        if (baseMaxHealth <= 0)
        {
            baseMaxHealth = maxHealth;
        }
    }
    
    /// <summary>
    /// Set the health multiplier (can be called after spawn)
    /// </summary>
    public void SetHealthMultiplier(float multiplier)
    {
        EnsureBaseHealthInitialized();
        
        if (multiplier > 1f)
        {
            maxHealth = baseMaxHealth * multiplier;
            currentHealth = maxHealth;
        }
        else
        {
            maxHealth = baseMaxHealth;
            currentHealth = maxHealth;
        }
    }

    void Update()
    {
        // Handle knockback movement
        if (isKnockedBack && !isFrozen && !isDead)
        {
            knockbackTimer += Time.deltaTime;
            
            // Move the enemy during knockback
            transform.position += knockbackVelocity * Time.deltaTime;
            
            // End knockback after duration
            if (knockbackTimer >= knockbackDuration)
            {
                isKnockedBack = false;
                knockbackVelocity = Vector3.zero;
                
                // Re-enable agent movement
                if (agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    nearestBodyPart = FindNearestBodyPart();
                    if (nearestBodyPart)
                    {
                        // In classic mode, restrict to cardinal directions
                        if (isClassicMode)
                        {
                            SetNextCardinalDestination();
                        }
                        else
                        {
                            agent.SetDestination(nearestBodyPart.position);
                        }
                    }
                }
            }
        }
        
        // Handle classic mode step-by-step movement
        if (isClassicMode && !isFrozen && !isDead && !isKnockedBack && !isInContact)
        {
            classicMoveTimer += Time.deltaTime;
            
            // Check if we've reached our current cardinal target or need a new one
            if (classicMoveTimer >= classicMoveInterval || !hasClassicTarget || HasReachedClassicTarget())
            {
                classicMoveTimer = 0f;
                SetNextCardinalDestination();
            }
        }
        
        // CRITICAL: When using manager updates, we need to handle contact detection in Update
        // because ManagerUpdate may not be called frequently enough with many enemies.
        // This ensures enemies properly detect contact and deal/receive damage even when
        // the manager is batching updates across many frames.
        if (usesManagerUpdates && !isFrozen && !isDead && !isKnockedBack)
        {
            // Check contact state every frame for responsiveness
            // Use distance-based check as primary (more reliable than physics callbacks with many enemies)
            Transform contactedPart;
            bool wasInContact = isInContact;
            
            // Distance-based contact detection (reliable even with many enemies)
            isInContact = IsAnyBodyPartNearbyOptimized(out contactedPart);
            
            // AppleChecker (physics-based) as secondary confirmation
            // Only use it if distance check says we're NOT in contact, as a backup
            if (!isInContact && appleChecker != null && appleChecker.isTouching)
            {
                isInContact = true;
            }
            
            if (isInContact)
            {
                // Just entered contact - disable agent
                if (!wasInContact)
                {
                    pendingAgentReEnableTime = -1f; // Cancel any pending re-enable
                    needsDestinationUpdate = false;
                    
                    if (agent.enabled && agent.isOnNavMesh)
                    {
                        agent.velocity = Vector3.zero;
                        agent.enabled = false;
                    }
                }
                
                // Update contact timer
                contactTimer += Time.deltaTime;
                
                if (contactTimer >= contactTimeBeforeBiting)
                {
                    if (!isBiting) StartBiting();
                    
                    // Handle bite damage timing
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
                if (wasInContact)
                {
                    pendingAgentReEnableTime = Time.time + agentReEnableDelay;
                    needsDestinationUpdate = true;
                }
                
                if (isBiting) StopBiting();
                
                contactTimer = 0f;
                biteTimer = 0f;
                
                // Clear enemy target when not in contact
                currentEnemyTarget = null;
            }
            
            wasInContactLastFrame = isInContact;
            
            // Handle pending agent re-enable
            if (pendingAgentReEnableTime > 0 && Time.time >= pendingAgentReEnableTime)
            {
                pendingAgentReEnableTime = -1f;
                if (!isInContact && !agent.enabled)
                {
                    agent.enabled = true;
                    needsDestinationUpdate = true;
                }
            }
        }
    }
    
    /// <summary>
    /// Checks if the enemy has reached its current classic mode target position
    /// </summary>
    private bool HasReachedClassicTarget()
    {
        if (!hasClassicTarget) return true;
        
        Vector3 toTarget = classicTargetPosition - transform.position;
        toTarget.y = 0;
        return toTarget.sqrMagnitude < 0.25f; // Within 0.5 units
    }
    
    /// <summary>
    /// Sets the next cardinal direction destination for classic mode movement
    /// </summary>
    private void SetNextCardinalDestination()
    {
        nearestBodyPart = FindNearestBodyPart();
        if (nearestBodyPart == null || !agent.enabled || !agent.isOnNavMesh)
        {
            hasClassicTarget = false;
            return;
        }
        
        // Get the cardinal direction to the target
        Vector3 cardinalDir = GetCardinalDirectionTo(nearestBodyPart.position);
        lastCardinalDirection = cardinalDir;
        
        // Calculate the next step position (move one "step" in the cardinal direction)
        float stepDistance = agent.speed * classicMoveInterval * 1.5f; // Move a bit more than one interval's worth
        classicTargetPosition = transform.position + cardinalDir * stepDistance;
        classicTargetPosition.y = transform.position.y; // Keep same height
        
        hasClassicTarget = true;
        agent.SetDestination(classicTargetPosition);
    }
    
    void LateUpdate()
    {
        if (isFrozen || !agentObj) return;
        
        // Skip rotation update during knockback
        if (isKnockedBack) return;
        
        if (!agent.enabled) return;
        
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
        // If we're an ally, find nearest enemy apple instead
        if (isAlly)
        {
            return FindNearestEnemyApple();
        }
        
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
    
    /// <summary>
    /// Optimized version that uses the manager's cached spatial data
    /// </summary>
    private Transform FindNearestBodyPartOptimized()
    {
        // If we're an ally, find nearest enemy apple instead
        if (isAlly)
        {
            return FindNearestEnemyApple();
        }
        
        // Use manager's cached data if available
        if (usesManagerUpdates && AppleEnemyManager.Instance != null)
        {
            return AppleEnemyManager.Instance.FindNearestBodyPart(transform.position);
        }
        
        // Fallback to regular method
        return FindNearestBodyPart();
    }
    
    /// <summary>
    /// Optimized version of IsAnyBodyPartNearby that uses manager's cached data
    /// </summary>
    private bool IsAnyBodyPartNearbyOptimized(out Transform closestPart)
    {
        closestPart = null;
        
        // If we're an ally, check for nearby enemy apples instead
        if (isAlly)
        {
            bool foundEnemy = IsAnyEnemyAppleNearby(out closestPart);
            if (!foundEnemy)
            {
                currentEnemyTarget = null;
            }
            return foundEnemy;
        }
        
        // First check for nearby ally apples (enemies can attack allies)
        float closestDistanceSqr = float.MaxValue;
        Vector3 myPos = transform.position;
        
        int appleCount = s_allAppleEnemies.Count;
        for (int i = 0; i < appleCount; i++)
        {
            AppleEnemy apple = s_allAppleEnemies[i];
            if (apple == this || apple == null || !apple.isAlly || apple.isFrozen || apple.isDead)
                continue;
            
            float distanceSqr = (apple.transform.position - myPos).sqrMagnitude;
            if (distanceSqr <= contactDistanceSqr && distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestPart = apple.transform;
                currentEnemyTarget = apple;
            }
        }
        
        if (closestPart != null)
        {
            return true;
        }
        
        // Clear enemy target since we're not targeting an ally apple
        currentEnemyTarget = null;
        
        // Use manager's cached data for body parts if available
        if (usesManagerUpdates && AppleEnemyManager.Instance != null)
        {
            return AppleEnemyManager.Instance.IsAnyBodyPartNearby(myPos, contactDistanceSqr, out closestPart);
        }
        
        // Fallback: check snake body parts directly (avoid calling IsAnyBodyPartNearby to prevent recursion)
        if (!snakeBody || snakeBody.bodyParts == null || snakeBody.bodyParts.Count == 0)
            return false;

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
    
    /// <summary>
    /// Called by AppleEnemyManager to update this enemy.
    /// This handles navigation updates only - contact detection and damage are handled in Update()
    /// for frame-accurate responsiveness even when manager updates are batched.
    /// </summary>
    public void ManagerUpdate(float currentTime, AppleEnemyManager manager)
    {
        if (isFrozen || isDead) return;
        
        lastManagerUpdateTime = currentTime;
        
        // Find target if we don't have one
        if (nearestBodyPart == null)
        {
            nearestBodyPart = FindNearestBodyPartOptimized();
            needsDestinationUpdate = true;
        }
        
        // If we still don't have a target, skip this update
        if (nearestBodyPart == null)
        {
            return;
        }
        
        // Only handle navigation when not in contact
        // Contact detection and damage are handled in Update() for responsiveness
        if (!isInContact)
        {
            // Update target periodically
            Transform newTarget = FindNearestBodyPartOptimized();
            if (newTarget != nearestBodyPart)
            {
                nearestBodyPart = newTarget;
                needsDestinationUpdate = true;
            }
            
            // Try to set destination if we need one and agent is ready
            if (needsDestinationUpdate && nearestBodyPart && agent.enabled && agent.isOnNavMesh)
            {
                if (isClassicMode)
                {
                    SetNextCardinalDestination();
                    needsDestinationUpdate = false;
                }
                else
                {
                    agent.SetDestination(nearestBodyPart.position);
                    needsDestinationUpdate = false;
                }
            }
        }
    }
    
    /// <summary>
    /// Gets the contact distance squared for external queries
    /// </summary>
    public float GetContactDistanceSqr() => contactDistanceSqr;
    
    /// <summary>
    /// Finds the nearest enemy apple (non-ally) for ally targeting
    /// Uses static list instead of FindObjectsByType for better performance
    /// </summary>
    private Transform FindNearestEnemyApple()
    {
        float nearestDistanceSqr = float.MaxValue;
        Transform nearest = null;
        Vector3 myPos = transform.position;
        
        // Use static list instead of FindObjectsByType
        int count = s_allAppleEnemies.Count;
        for (int i = 0; i < count; i++)
        {
            AppleEnemy apple = s_allAppleEnemies[i];
            // Skip self, allies, frozen, and dead apples
            if (apple == this || apple == null || apple.isAlly || apple.isFrozen || apple.isDead)
                continue;
            
            float distanceSqr = (apple.transform.position - myPos).sqrMagnitude;
            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearest = apple.transform;
                currentEnemyTarget = apple;
            }
        }
        
        return nearest;
    }

    private bool IsAnyBodyPartNearby(out Transform closestPart)
    {
        closestPart = null;
        
        // If we're an ally, check for nearby enemy apples instead
        if (isAlly)
        {
            return IsAnyEnemyAppleNearby(out closestPart);
        }
        
        float closestDistanceSqr = float.MaxValue;
        Vector3 myPos = transform.position;
        
        // First check for nearby ally apples (enemies can attack allies)
        // Use static list instead of FindObjectsByType
        int appleCount = s_allAppleEnemies.Count;
        for (int i = 0; i < appleCount; i++)
        {
            AppleEnemy apple = s_allAppleEnemies[i];
            // Only target ally apples
            if (apple == this || apple == null || !apple.isAlly || apple.isFrozen || apple.isDead)
                continue;
            
            float distanceSqr = (apple.transform.position - myPos).sqrMagnitude;
            if (distanceSqr <= contactDistanceSqr && distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestPart = apple.transform;
                currentEnemyTarget = apple;
            }
        }
        
        // If we found an ally apple nearby, return true
        if (closestPart != null)
        {
            return true;
        }
        
        // Otherwise check for snake body parts
        if (!snakeBody || snakeBody.bodyParts == null || snakeBody.bodyParts.Count == 0)
            return false;

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
                    currentEnemyTarget = null; // Not targeting an apple
                }
            }
        }

        return closestPart != null;
    }
    
    /// <summary>
    /// Checks if any enemy apple is nearby (for ally targeting)
    /// Uses static list instead of FindObjectsByType for better performance
    /// </summary>
    private bool IsAnyEnemyAppleNearby(out Transform closestPart)
    {
        closestPart = null;
        
        float closestDistanceSqr = float.MaxValue;
        Vector3 myPos = transform.position;
        
        // Use static list instead of FindObjectsByType
        int count = s_allAppleEnemies.Count;
        for (int i = 0; i < count; i++)
        {
            AppleEnemy apple = s_allAppleEnemies[i];
            // Skip self, allies, frozen, and dead apples
            if (apple == this || apple == null || apple.isAlly || apple.isFrozen || apple.isDead)
                continue;
            
            float distanceSqr = (apple.transform.position - myPos).sqrMagnitude;
            if (distanceSqr <= contactDistanceSqr && distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestPart = apple.transform;
                currentEnemyTarget = apple;
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
                        // In classic mode, use step-by-step cardinal movement
                        if (isClassicMode)
                        {
                            SetNextCardinalDestination();
                        }
                        else
                        {
                            agent.SetDestination(nearestBodyPart.position);
                        }
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
                // In classic mode, use step-by-step cardinal movement
                if (isClassicMode)
                {
                    SetNextCardinalDestination();
                }
                else
                {
                    agent.SetDestination(nearestBodyPart.position);
                }
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
        // Safety check: only deal damage if we're actually in contact
        if (!isInContact)
        {
            return;
        }
        
        float damage = UnityEngine.Random.Range(minDamage, maxDamage);
        
        // If we're an ally, damage the enemy apple instead
        if (isAlly)
        {
            if (currentEnemyTarget != null && !currentEnemyTarget.isDead)
            {
                // Verify the target is still in range before dealing damage
                float distSqr = (currentEnemyTarget.transform.position - transform.position).sqrMagnitude;
                if (distSqr <= contactDistanceSqr)
                {
                    // Allies deal damage to enemy apples using regular TakeDamage
                    currentEnemyTarget.TakeDamage(damage * allyDamageMultiplier);
                }
            }
        }
        else
        {
            // Enemy apples can damage the snake OR ally apples
            // Check if we're targeting an ally apple (via contact)
            if (currentEnemyTarget != null && currentEnemyTarget.isAlly && !currentEnemyTarget.isDead)
            {
                // Verify the target is still in range before dealing damage
                float distSqr = (currentEnemyTarget.transform.position - transform.position).sqrMagnitude;
                if (distSqr <= contactDistanceSqr)
                {
                    // Enemy apple attacking an ally - use TakeDamageFromEnemy
                    currentEnemyTarget.TakeDamageFromEnemy(damage);
                }
            }
            else if (snakeHealth)
            {
                // Verify we're actually near a body part before damaging the snake
                Transform closestPart;
                if (IsAnyBodyPartNearbyOptimized(out closestPart) && closestPart != null)
                {
                    // Normal behavior - damage the snake
                    snakeHealth.TakeDamage(damage);
                }
            }
        }
    }

    /// <summary>
    /// Take damage from player attacks/abilities. Allies are immune to this.
    /// </summary>
    public void TakeDamage(float damage)
    {
        // Safety check - don't process damage if already dead or inactive
        if (isDead)
        {
            #if UNITY_EDITOR
            Debug.LogWarning($"[AppleEnemy] TakeDamage called on dead enemy {gameObject.name}, forcing cleanup");
            #endif
            // Force cleanup - this enemy should not be active
            if (gameObject.activeInHierarchy)
            {
                // This is a zombie enemy - force despawn it
                if (ObjectPool.Instance != null)
                {
                    ObjectPool.Instance.Despawn(gameObject);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            return;
        }
        
        // Safety check - if not in the static list, re-add ourselves
        if (!s_allAppleEnemies.Contains(this))
        {
            #if UNITY_EDITOR
            Debug.LogWarning($"[AppleEnemy] {gameObject.name} was not in static list during TakeDamage, re-adding");
            #endif
            s_allAppleEnemies.Add(this);
        }
        
        // Allies cannot be damaged by player attacks/abilities
        if (isAlly)
        {
            #if UNITY_EDITOR
            Debug.Log($"[AppleEnemy] TakeDamage blocked - {gameObject.name} is an ally");
            #endif
            return;
        }
        
        currentHealth -= damage;
        #if UNITY_EDITOR
        Debug.Log($"[AppleEnemy] {gameObject.name} took {damage} damage, health: {currentHealth}/{maxHealth}");
        #endif
        if (currentHealth <= 0) Die();
    }
    
    /// <summary>
    /// Take damage from enemy apples. Only allies can take this damage.
    /// </summary>
    public void TakeDamageFromEnemy(float damage)
    {
        // Only allies can be damaged by enemy apples
        if (!isAlly) return;
        
        currentHealth -= damage;
        if (currentHealth <= 0) Die();
    }
    
    /// <summary>
    /// Apply knockback force to push the enemy away. Allies are immune to knockback from player.
    /// </summary>
    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        // Allies cannot be knocked back by player attacks
        if (isDead || isFrozen || isAlly) return;
        
        // Normalize direction and apply force
        knockbackVelocity = direction.normalized * force;
        knockbackDuration = duration;
        knockbackTimer = 0f;
        isKnockedBack = true;
        
        // Temporarily disable agent during knockback
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // Play bite sound when apple dies
        SoundManager.Play("Bite", gameObject);
        
        if (biteParticles) biteParticles.Stop();
        if (deathObjectPrefab) Instantiate(deathObjectPrefab, transform.position, transform.rotation);
        
        SpawnXPDrops();
        OnAppleDied?.Invoke(this);
        
        // Use object pool if available, otherwise destroy
        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.Despawn(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
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
            
            Vector3 spawnPosition = transform.position + spawnOffset;
            
            // Try to use object pool first, fall back to Instantiate
            GameObject xpDrop = null;
            if (ObjectPool.Instance != null)
            {
                xpDrop = ObjectPool.Instance.Spawn(xpDropPrefab, spawnPosition, Quaternion.identity);
            }
            else
            {
                xpDrop = Instantiate(xpDropPrefab, spawnPosition, Quaternion.identity);
            }
            
            XPDrop xpDropScript = xpDrop.GetComponent<XPDrop>();
            if (xpDropScript)
            {
                int xpValue = UnityEngine.Random.Range(minXPDrop, maxXPDrop + 1);
                xpDropScript.InitializeXPDrop(xpValue);
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
                        // In classic mode, use step-by-step cardinal movement
                        if (isClassicMode)
                        {
                            SetNextCardinalDestination();
                        }
                        else
                        {
                            agent.SetDestination(nearestBodyPart.position);
                        }
                    }
                }
            }
            
            if (biteParticles && isBiting) biteParticles.Play();
        }
        
        isFrozen = frozen;
    }

    public bool IsFrozen() => isFrozen;
    
    /// <summary>
    /// Sets classic mode - restricts movement to cardinal directions only (no diagonals)
    /// Uses step-by-step movement in cardinal directions like classic Snake
    /// </summary>
    public void SetClassicMode(bool enabled, float cellSize = 1f, float moveInterval = 0.3f)
    {
        isClassicMode = enabled;
        classicMoveInterval = moveInterval;
        classicMoveTimer = 0f;
        hasClassicTarget = false;
        
        if (enabled)
        {
            // Immediately set a cardinal destination
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                SetNextCardinalDestination();
            }
            Debug.Log($"[AppleEnemy] Classic mode enabled - movement restricted to 4 directions");
        }
        else
        {
            // Resume normal pathfinding
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                nearestBodyPart = FindNearestBodyPart();
                if (nearestBodyPart != null)
                {
                    agent.SetDestination(nearestBodyPart.position);
                }
            }
            Debug.Log($"[AppleEnemy] Classic mode disabled");
        }
    }
    
    /// <summary>
    /// Gets a cardinal direction (N, E, S, W) from a target position
    /// </summary>
    private Vector3 GetCardinalDirectionTo(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;
        
        // Determine which cardinal direction to move (prioritize larger axis)
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
        {
            // Move horizontally (East or West)
            return direction.x > 0 ? Vector3.right : Vector3.left;
        }
        else
        {
            // Move vertically (North or South)
            return direction.z > 0 ? Vector3.forward : Vector3.back;
        }
    }
    
    
    /// <summary>
    /// Returns whether this apple is in classic mode
    /// </summary>
    public bool IsClassicMode() => isClassicMode;
    
    /// <summary>
    /// Returns whether this apple is an ally
    /// </summary>
    public bool IsAlly() => isAlly;
    
    /// <summary>
    /// Sets this apple as an ally or reverts it back to an enemy
    /// </summary>
    public void SetAsAlly(bool ally, float damageMultiplier = 1f, float healthMultiplier = 1f)
    {
        isAlly = ally;
        allyDamageMultiplier = damageMultiplier;
        
        if (ally)
        {
            // Apply health multiplier for allies
            if (healthMultiplier > 1f)
            {
                maxHealth *= healthMultiplier;
                currentHealth = maxHealth;
            }
            
            // Clear current target and find new enemy target
            currentEnemyTarget = null;
            nearestBodyPart = FindNearestBodyPart();
            
            // Only set destination if agent is initialized (Start() has run)
            if (agent != null && agent.enabled && agent.isOnNavMesh && nearestBodyPart != null)
            {
                // In classic mode, use step-by-step cardinal movement
                if (isClassicMode)
                {
                    SetNextCardinalDestination();
                }
                else
                {
                    agent.SetDestination(nearestBodyPart.position);
                }
            }
        }
        else
        {
            // Revert to enemy behavior
            currentEnemyTarget = null;
            nearestBodyPart = FindNearestBodyPart();
            
            // Restore original visuals
            RestoreOriginalVisuals();
            
            // Only set destination if agent is initialized (Start() has run)
            if (agent != null && agent.enabled && agent.isOnNavMesh && nearestBodyPart != null)
            {
                // In classic mode, use step-by-step cardinal movement
                if (isClassicMode)
                {
                    SetNextCardinalDestination();
                }
                else
                {
                    agent.SetDestination(nearestBodyPart.position);
                }
            }
        }
    }
    
    /// <summary>
    /// Sets a material for the ally visual
    /// </summary>
    public void SetAllyMaterial(Material material)
    {
        if (renderers == null || material == null) return;
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.material = material;
            }
        }
    }
    
    /// <summary>
    /// Sets a tint color for the ally visual.
    /// When using white, this makes the ally completely white by removing textures.
    /// </summary>
    public void SetAllyTint(Color tintColor)
    {
        if (renderers == null) return;
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                Material mat = renderer.material;
                
                // If the tint is white (or very close to white), make the object completely white
                // by removing the main texture and setting the color to white
                bool isWhite = tintColor.r >= 0.99f && tintColor.g >= 0.99f && tintColor.b >= 0.99f;
                
                if (isWhite)
                {
                    // Remove the main texture to show solid color
                    if (mat.HasProperty("_MainTex"))
                    {
                        mat.SetTexture("_MainTex", null);
                    }
                    if (mat.HasProperty("_BaseMap"))
                    {
                        mat.SetTexture("_BaseMap", null);
                    }
                }
                
                // Check for both _Color and _BaseColor (URP/HDRP compatibility)
                if (mat.HasProperty("_Color"))
                {
                    mat.color = tintColor;
                }
                else if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", tintColor);
                }
            }
        }
    }
    
    /// <summary>
    /// Restores the original visual appearance
    /// </summary>
    private void RestoreOriginalVisuals()
    {
        if (renderers == null || originalMaterials == null) return;
        
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && i < originalMaterials.Length && originalMaterials[i] != null)
            {
                renderers[i].material = originalMaterials[i];
                Material mat = renderers[i].material;
                
                // Check for both _Color and _BaseColor (URP/HDRP compatibility)
                if (mat.HasProperty("_Color"))
                {
                    mat.color = originalColors[i];
                }
                else if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", originalColors[i]);
                }
            }
        }
    }

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