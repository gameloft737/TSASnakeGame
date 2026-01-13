using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized manager for all AppleEnemy instances.
/// Handles batch updates to improve performance with large numbers of enemies.
/// Replaces per-enemy coroutines with a single efficient update loop.
/// </summary>
public class AppleEnemyManager : MonoBehaviour
{
    private static AppleEnemyManager _instance;
    public static AppleEnemyManager Instance => _instance;
    
    [Header("Update Settings")]
    [Tooltip("Base interval for tracking updates (seconds)")]
    [SerializeField] private float baseUpdateInterval = 0.1f;
    
    [Tooltip("How many enemies to update per frame (0 = all). Scales with enemy count.")]
    [SerializeField] private int baseEnemiesPerFrame = 50;
    
    [Tooltip("Minimum percentage of enemies to update each frame (0.0-1.0)")]
    [SerializeField] private float minUpdatePercentage = 0.25f;
    
    [Tooltip("Distance beyond which enemies update less frequently")]
    [SerializeField] private float lodDistance = 30f;
    
    [Tooltip("Update interval multiplier for distant enemies")]
    [SerializeField] private float distantUpdateMultiplier = 2f;
    
    [Header("Spatial Optimization")]
    [Tooltip("How often to recalculate nearest body part cache (seconds)")]
    [SerializeField] private float spatialCacheInterval = 0.15f;
    
    [Header("NavMesh Batching")]
    [Tooltip("Max NavMesh destination updates per frame (legacy - no longer used for gating)")]
    [SerializeField] private int maxNavMeshUpdatesPerFrame = 50;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // References
    private SnakeBody snakeBody;
    private SnakeHealth snakeHealth;
    private Transform playerTransform;
    
    // Update tracking
    private int currentUpdateIndex = 0;
    private float lastSpatialCacheTime = 0f;
    private int navMeshUpdatesThisFrame = 0;
    
    // Cached body parts for spatial queries
    private List<Transform> cachedBodyParts = new List<Transform>(32);
    private Vector3[] bodyPartPositions = new Vector3[32];
    private int bodyPartCount = 0;
    
    // Per-enemy timing data (avoids per-enemy allocations)
    private Dictionary<AppleEnemy, float> lastUpdateTime = new Dictionary<AppleEnemy, float>(64);
    private Dictionary<AppleEnemy, float> updateIntervals = new Dictionary<AppleEnemy, float>(64);
    
    // Reusable lists to avoid allocations
    private List<AppleEnemy> enemiesToRemove = new List<AppleEnemy>(16);
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }
    
    private void Start()
    {
        FindReferences();
        SnakeBody.OnBodyPartsInitialized += OnSnakeInitialized;
    }
    
    private void OnDestroy()
    {
        SnakeBody.OnBodyPartsInitialized -= OnSnakeInitialized;
        if (_instance == this)
        {
            _instance = null;
        }
    }
    
    private void OnSnakeInitialized()
    {
        FindReferences();
    }
    
    private void FindReferences()
    {
        if (snakeBody == null)
        {
            snakeBody = FindFirstObjectByType<SnakeBody>();
        }
        
        if (snakeBody != null)
        {
            playerTransform = snakeBody.transform;
            if (snakeHealth == null)
            {
                snakeHealth = snakeBody.GetComponent<SnakeHealth>();
            }
            
            // Update static references for AppleEnemy
            AppleEnemy.SetSnakeReferences(snakeBody, snakeHealth);
        }
    }
    
    private void Update()
    {
        // Reset per-frame counters
        navMeshUpdatesThisFrame = 0;
        
        // Update spatial cache periodically
        if (Time.time - lastSpatialCacheTime >= spatialCacheInterval)
        {
            UpdateSpatialCache();
            lastSpatialCacheTime = Time.time;
        }
        
        // Periodically validate the enemy list to catch any desync issues
        // Do this every 5 seconds to avoid performance impact
        if (Time.frameCount % 300 == 0)
        {
            ValidateEnemyList();
        }
        
        // Batch update enemies
        BatchUpdateEnemies();
    }
    
    /// <summary>
    /// Updates the cached body part positions for efficient spatial queries
    /// </summary>
    private void UpdateSpatialCache()
    {
        cachedBodyParts.Clear();
        
        if (snakeBody == null || snakeBody.bodyParts == null)
        {
            bodyPartCount = 0;
            return;
        }
        
        var bodyParts = snakeBody.bodyParts;
        bodyPartCount = bodyParts.Count;
        
        // Ensure array is large enough
        if (bodyPartPositions.Length < bodyPartCount)
        {
            bodyPartPositions = new Vector3[bodyPartCount * 2];
        }
        
        for (int i = 0; i < bodyPartCount; i++)
        {
            var part = bodyParts[i];
            if (part != null)
            {
                cachedBodyParts.Add(part.transform);
                bodyPartPositions[i] = part.transform.position;
            }
        }
    }
    
    /// <summary>
    /// Batch updates enemies, spreading work across frames
    /// </summary>
    private void BatchUpdateEnemies()
    {
        var allEnemies = AppleEnemy.GetAllActiveEnemies();
        int totalEnemies = allEnemies.Count;
        
        if (totalEnemies == 0) return;
        
        // Determine how many to update this frame
        // For large enemy counts, we need to update more enemies per frame
        // to ensure all enemies get updated within a reasonable time
        int minToUpdate = Mathf.Max(1, Mathf.CeilToInt(totalEnemies * minUpdatePercentage));
        int toUpdate = baseEnemiesPerFrame > 0 ? Mathf.Max(minToUpdate, Mathf.Min(baseEnemiesPerFrame, totalEnemies)) : totalEnemies;
        
        // For very large enemy counts (100+), increase the update rate
        if (totalEnemies > 100)
        {
            toUpdate = Mathf.Max(toUpdate, totalEnemies / 4); // Update at least 25% per frame
        }
        
        // Clean up any null entries in our tracking dictionaries (do this less frequently)
        if (Time.frameCount % 30 == 0)
        {
            CleanupDeadEnemies(allEnemies);
        }
        
        float currentTime = Time.time;
        int updated = 0;
        int checked_count = 0;
        int skippedNull = 0;
        
        // Start from where we left off last frame
        // Iterate through ALL enemies, updating those that need it
        for (int i = 0; i < totalEnemies; i++)
        {
            // Stop if we've updated enough this frame
            if (updated >= toUpdate) break;
            
            int index = (currentUpdateIndex + i) % totalEnemies;
            AppleEnemy enemy = allEnemies[index];
            checked_count++;
            
            if (enemy == null)
            {
                skippedNull++;
                continue;
            }
            
            if (!enemy.gameObject.activeInHierarchy) continue;
            
            // Check if this enemy needs an update based on its interval
            float interval = GetUpdateInterval(enemy);
            float lastUpdate = GetLastUpdateTime(enemy);
            float timeSinceUpdate = currentTime - lastUpdate;
            
            // Force update if significantly overdue (more than 1.5x interval)
            // This prevents enemies from getting "stuck" when there are many enemies
            bool forceUpdate = timeSinceUpdate >= interval * 1.5f;
            
            if (timeSinceUpdate >= interval || forceUpdate)
            {
                UpdateEnemy(enemy, currentTime);
                lastUpdateTime[enemy] = currentTime;
                updated++;
            }
        }
        
        // Move the starting index based on how many we checked
        // This ensures we cycle through ALL enemies over time
        currentUpdateIndex = (currentUpdateIndex + checked_count) % Mathf.Max(1, totalEnemies);
        
        // If we found null entries, clean them up from the list
        if (skippedNull > 0 && Time.frameCount % 10 == 0)
        {
            CleanupDeadEnemies(allEnemies);
        }
        
        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[AppleEnemyManager] Enemies: {totalEnemies}, Updated: {updated}, Checked: {checked_count}, Null: {skippedNull}, NavMesh updates: {navMeshUpdatesThisFrame}");
        }
    }
    
    /// <summary>
    /// Gets the update interval for an enemy based on distance (LOD)
    /// </summary>
    private float GetUpdateInterval(AppleEnemy enemy)
    {
        if (!updateIntervals.TryGetValue(enemy, out float interval))
        {
            interval = baseUpdateInterval;
            updateIntervals[enemy] = interval;
        }
        
        // Apply LOD - distant enemies update less frequently
        if (playerTransform != null)
        {
            float distSqr = (enemy.transform.position - playerTransform.position).sqrMagnitude;
            if (distSqr > lodDistance * lodDistance)
            {
                return interval * distantUpdateMultiplier;
            }
        }
        
        return interval;
    }
    
    private float GetLastUpdateTime(AppleEnemy enemy)
    {
        if (!lastUpdateTime.TryGetValue(enemy, out float time))
        {
            time = 0f;
            lastUpdateTime[enemy] = time;
        }
        return time;
    }
    
    /// <summary>
    /// Updates a single enemy's tracking and contact logic
    /// </summary>
    private void UpdateEnemy(AppleEnemy enemy, float currentTime)
    {
        // Let the enemy handle its own update logic through a new method
        enemy.ManagerUpdate(currentTime, this);
    }
    
    /// <summary>
    /// Finds the nearest body part to a position using cached data
    /// </summary>
    public Transform FindNearestBodyPart(Vector3 position)
    {
        if (bodyPartCount == 0) return null;
        
        float nearestDistSqr = float.MaxValue;
        Transform nearest = null;
        
        for (int i = 0; i < bodyPartCount && i < cachedBodyParts.Count; i++)
        {
            Transform part = cachedBodyParts[i];
            if (part == null) continue;
            
            float distSqr = (bodyPartPositions[i] - position).sqrMagnitude;
            if (distSqr < nearestDistSqr)
            {
                nearestDistSqr = distSqr;
                nearest = part;
            }
        }
        
        return nearest;
    }
    
    /// <summary>
    /// Checks if any body part is within the given distance squared
    /// </summary>
    public bool IsAnyBodyPartNearby(Vector3 position, float distanceSqr, out Transform closest)
    {
        closest = null;
        float closestDistSqr = float.MaxValue;
        
        for (int i = 0; i < bodyPartCount && i < cachedBodyParts.Count; i++)
        {
            Transform part = cachedBodyParts[i];
            if (part == null) continue;
            
            float distSqr = (bodyPartPositions[i] - position).sqrMagnitude;
            if (distSqr <= distanceSqr && distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = part;
            }
        }
        
        return closest != null;
    }
    
    /// <summary>
    /// Requests a NavMesh destination update. Returns true if the update was allowed this frame.
    /// </summary>
    public bool RequestNavMeshUpdate()
    {
        if (navMeshUpdatesThisFrame >= maxNavMeshUpdatesPerFrame)
        {
            return false;
        }
        
        navMeshUpdatesThisFrame++;
        return true;
    }
    
    /// <summary>
    /// Cleans up tracking data for dead/destroyed enemies
    /// </summary>
    private void CleanupDeadEnemies(List<AppleEnemy> allEnemies)
    {
        enemiesToRemove.Clear();
        
        foreach (var kvp in lastUpdateTime)
        {
            if (kvp.Key == null || !allEnemies.Contains(kvp.Key))
            {
                enemiesToRemove.Add(kvp.Key);
            }
        }
        
        for (int i = 0; i < enemiesToRemove.Count; i++)
        {
            AppleEnemy enemy = enemiesToRemove[i];
            lastUpdateTime.Remove(enemy);
            updateIntervals.Remove(enemy);
        }
        
        // Also clean up null entries from the static list
        for (int i = allEnemies.Count - 1; i >= 0; i--)
        {
            if (allEnemies[i] == null)
            {
                allEnemies.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// Registers an enemy with the manager
    /// </summary>
    public void RegisterEnemy(AppleEnemy enemy)
    {
        if (enemy == null) return;
        
        // Always update the registration (in case of re-spawn from pool)
        lastUpdateTime[enemy] = Time.time;
        updateIntervals[enemy] = baseUpdateInterval;
    }
    
    /// <summary>
    /// Unregisters an enemy from the manager
    /// </summary>
    public void UnregisterEnemy(AppleEnemy enemy)
    {
        if (enemy == null) return;
        
        lastUpdateTime.Remove(enemy);
        updateIntervals.Remove(enemy);
    }
    
    /// <summary>
    /// Forces an immediate update for all enemies.
    /// Call this if enemies appear to be stuck or unresponsive.
    /// </summary>
    public void ForceUpdateAllEnemies()
    {
        var allEnemies = AppleEnemy.GetAllActiveEnemies();
        float currentTime = Time.time;
        
        foreach (var enemy in allEnemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                UpdateEnemy(enemy, currentTime);
                lastUpdateTime[enemy] = currentTime;
            }
        }
        
        Debug.Log($"[AppleEnemyManager] Force updated {allEnemies.Count} enemies");
    }
    
    /// <summary>
    /// Validates and fixes the enemy list, removing any invalid entries
    /// and ensuring all active enemies are properly registered.
    /// </summary>
    public void ValidateEnemyList()
    {
        var allEnemies = AppleEnemy.GetAllActiveEnemies();
        int removed = 0;
        int registered = 0;
        
        // Remove null entries
        for (int i = allEnemies.Count - 1; i >= 0; i--)
        {
            if (allEnemies[i] == null)
            {
                allEnemies.RemoveAt(i);
                removed++;
            }
        }
        
        // Ensure all active enemies are registered with the manager
        foreach (var enemy in allEnemies)
        {
            if (enemy != null && !lastUpdateTime.ContainsKey(enemy))
            {
                RegisterEnemy(enemy);
                registered++;
            }
        }
        
        if (removed > 0 || registered > 0)
        {
            Debug.Log($"[AppleEnemyManager] ValidateEnemyList: Removed {removed} null entries, registered {registered} untracked enemies");
        }
    }
    
    /// <summary>
    /// Gets the snake body reference
    /// </summary>
    public SnakeBody GetSnakeBody() => snakeBody;
    
    /// <summary>
    /// Gets the snake health reference
    /// </summary>
    public SnakeHealth GetSnakeHealth() => snakeHealth;
    
    #if UNITY_EDITOR
    private void OnGUI()
    {
        if (!showDebugInfo) return;
        
        var allEnemies = AppleEnemy.GetAllActiveEnemies();
        GUI.Label(new Rect(10, 10, 300, 25), $"Active Enemies: {allEnemies.Count}");
        GUI.Label(new Rect(10, 35, 300, 25), $"Cached Body Parts: {bodyPartCount}");
        GUI.Label(new Rect(10, 60, 300, 25), $"NavMesh Updates/Frame: {navMeshUpdatesThisFrame}/{maxNavMeshUpdatesPerFrame}");
    }
    #endif
}