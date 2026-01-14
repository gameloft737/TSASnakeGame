using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generic object pooling system for Unity GameObjects.
/// Reduces garbage collection by reusing objects instead of instantiating/destroying.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    private static ObjectPool _instance;
    public static ObjectPool Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject poolObj = new GameObject("ObjectPool");
                _instance = poolObj.AddComponent<ObjectPool>();
                DontDestroyOnLoad(poolObj);
            }
            return _instance;
        }
    }
    
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int initialSize = 10;
        public int maxSize = 50;
        public bool expandable = true;
    }
    
    /// <summary>
    /// Statistics for a pool - useful for debugging and optimization
    /// </summary>
    public struct PoolStats
    {
        public string tag;
        public int totalCreated;
        public int activeCount;
        public int availableCount;
        public int peakActiveCount;
        public int maxSize;
    }
    
    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>(16);
    private Dictionary<string, Pool> poolConfigs = new Dictionary<string, Pool>(16);
    private Dictionary<string, Transform> poolParents = new Dictionary<string, Transform>(16);
    private Dictionary<string, int> activeCount = new Dictionary<string, int>(16);
    private Dictionary<string, int> totalCreated = new Dictionary<string, int>(16);
    private Dictionary<string, int> peakActiveCount = new Dictionary<string, int>(16);
    private Dictionary<GameObject, string> prefabToTag = new Dictionary<GameObject, string>(16);
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }
    
    /// <summary>
    /// Creates a new pool for a prefab with the given tag
    /// </summary>
    public void CreatePool(string tag, GameObject prefab, int initialSize = 10, int maxSize = 50, bool expandable = true)
    {
        if (poolDictionary.ContainsKey(tag))
        {
            #if UNITY_EDITOR
            Debug.LogWarning($"Pool with tag '{tag}' already exists!");
            #endif
            return;
        }
        
        Pool pool = new Pool
        {
            tag = tag,
            prefab = prefab,
            initialSize = initialSize,
            maxSize = maxSize,
            expandable = expandable
        };
        
        poolConfigs[tag] = pool;
        poolDictionary[tag] = new Queue<GameObject>(initialSize);
        activeCount[tag] = 0;
        totalCreated[tag] = 0;
        peakActiveCount[tag] = 0;
        prefabToTag[prefab] = tag;
        
        // Create parent object for organization
        GameObject parent = new GameObject($"Pool_{tag}");
        parent.transform.SetParent(transform);
        poolParents[tag] = parent.transform;
        
        // Pre-instantiate objects
        for (int i = 0; i < initialSize; i++)
        {
            CreateNewPooledObject(tag);
        }
    }
    
    /// <summary>
    /// Prewarms a pool by creating objects up to the specified count.
    /// Call this during loading screens to avoid hitches during gameplay.
    /// </summary>
    public void PrewarmPool(string tag, int count)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            #if UNITY_EDITOR
            Debug.LogWarning($"Cannot prewarm pool '{tag}' - pool doesn't exist!");
            #endif
            return;
        }
        
        Pool config = poolConfigs[tag];
        int currentTotal = poolDictionary[tag].Count + activeCount[tag];
        int toCreate = Mathf.Min(count - currentTotal, config.maxSize - currentTotal);
        
        for (int i = 0; i < toCreate; i++)
        {
            CreateNewPooledObject(tag);
        }
    }
    
    /// <summary>
    /// Prewarms a pool by prefab reference
    /// </summary>
    public void PrewarmPool(GameObject prefab, int count)
    {
        if (prefab == null) return;
        
        if (!prefabToTag.TryGetValue(prefab, out string tag))
        {
            tag = prefab.name + "_Pool";
            CreatePool(tag, prefab, count, Mathf.Max(count * 2, 50), true);
            return;
        }
        
        PrewarmPool(tag, count);
    }
    
    /// <summary>
    /// Creates a new object for the pool
    /// </summary>
    private GameObject CreateNewPooledObject(string tag)
    {
        if (!poolConfigs.ContainsKey(tag)) return null;
        
        Pool pool = poolConfigs[tag];
        GameObject obj = Instantiate(pool.prefab, poolParents[tag]);
        obj.SetActive(false);
        
        // Add pooled object component for tracking
        PooledObject pooledObj = obj.GetComponent<PooledObject>();
        if (pooledObj == null)
        {
            pooledObj = obj.AddComponent<PooledObject>();
        }
        pooledObj.poolTag = tag;
        
        poolDictionary[tag].Enqueue(obj);
        totalCreated[tag]++;
        return obj;
    }
    
    /// <summary>
    /// Gets an object from the pool by prefab reference.
    /// If no pool exists for this prefab, creates one automatically.
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;
        
        // Check if we have a pool for this prefab
        if (!prefabToTag.TryGetValue(prefab, out string tag))
        {
            // Auto-create a pool for this prefab
            tag = prefab.name + "_Pool";
            CreatePool(tag, prefab, 10, 100, true);
        }
        
        return Spawn(tag, position, rotation);
    }
    
    /// <summary>
    /// Gets an object from the pool by tag
    /// </summary>
    public GameObject Spawn(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            #if UNITY_EDITOR
            Debug.LogWarning($"Pool with tag '{tag}' doesn't exist!");
            #endif
            return null;
        }
        
        Queue<GameObject> pool = poolDictionary[tag];
        Pool config = poolConfigs[tag];
        
        GameObject obj = null;
        
        // Try to get an inactive object from the pool
        while (pool.Count > 0)
        {
            obj = pool.Dequeue();
            if (obj != null)
            {
                break;
            }
        }
        
        // If no object available, try to create a new one
        if (obj == null)
        {
            int totalCount = activeCount[tag] + pool.Count;
            if (config.expandable && totalCount < config.maxSize)
            {
                obj = CreateNewPooledObject(tag);
                if (obj != null)
                {
                    pool.Dequeue();
                }
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogWarning($"Pool '{tag}' is exhausted! Consider increasing maxSize.");
                #endif
                return null;
            }
        }
        
        if (obj != null)
        {
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            activeCount[tag]++;
            
            // Track peak usage for optimization
            if (activeCount[tag] > peakActiveCount[tag])
            {
                peakActiveCount[tag] = activeCount[tag];
            }
            
            // Notify the object it was spawned
            IPooledObject pooledInterface = obj.GetComponent<IPooledObject>();
            if (pooledInterface != null)
            {
                pooledInterface.OnSpawnFromPool();
            }
        }
        
        return obj;
    }
    
    /// <summary>
    /// Returns an object to the pool
    /// </summary>
    public void Despawn(GameObject obj)
    {
        if (obj == null) return;
        
        PooledObject pooledObj = obj.GetComponent<PooledObject>();
        if (pooledObj == null || string.IsNullOrEmpty(pooledObj.poolTag))
        {
            // Not a pooled object, just destroy it
            Destroy(obj);
            return;
        }
        
        string tag = pooledObj.poolTag;
        
        if (!poolDictionary.ContainsKey(tag))
        {
            Destroy(obj);
            return;
        }
        
        // Notify the object it's being despawned
        IPooledObject pooledInterface = obj.GetComponent<IPooledObject>();
        if (pooledInterface != null)
        {
            pooledInterface.OnReturnToPool();
        }
        
        obj.SetActive(false);
        obj.transform.SetParent(poolParents[tag]);
        poolDictionary[tag].Enqueue(obj);
        activeCount[tag] = Mathf.Max(0, activeCount[tag] - 1);
    }
    
    /// <summary>
    /// Despawns an object after a delay
    /// </summary>
    public void DespawnAfterDelay(GameObject obj, float delay)
    {
        if (obj == null) return;
        StartCoroutine(DespawnDelayedCoroutine(obj, delay));
    }
    
    private System.Collections.IEnumerator DespawnDelayedCoroutine(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        Despawn(obj);
    }
    
    /// <summary>
    /// Gets the number of active objects in a pool
    /// </summary>
    public int GetActiveCount(string tag)
    {
        return activeCount.ContainsKey(tag) ? activeCount[tag] : 0;
    }
    
    /// <summary>
    /// Gets the number of available objects in a pool
    /// </summary>
    public int GetAvailableCount(string tag)
    {
        return poolDictionary.ContainsKey(tag) ? poolDictionary[tag].Count : 0;
    }
    
    /// <summary>
    /// Clears all inactive objects from a pool (objects in the queue)
    /// </summary>
    public void ClearPool(string tag)
    {
        if (!poolDictionary.ContainsKey(tag)) return;
        
        Queue<GameObject> pool = poolDictionary[tag];
        while (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        
        activeCount[tag] = 0;
    }
    
    /// <summary>
    /// Despawns all active objects from all pools.
    /// Call this when loading a new scene or restarting the game.
    /// </summary>
    public void DespawnAllActive()
    {
        // Find all active pooled objects in the scene
        PooledObject[] allPooledObjects = FindObjectsByType<PooledObject>(FindObjectsSortMode.None);
        
        int despawnedCount = 0;
        foreach (PooledObject pooledObj in allPooledObjects)
        {
            if (pooledObj != null && pooledObj.gameObject.activeInHierarchy)
            {
                Despawn(pooledObj.gameObject);
                despawnedCount++;
            }
        }
        
        Debug.Log($"[ObjectPool] Despawned {despawnedCount} active pooled objects");
    }
    
    /// <summary>
    /// Despawns all active objects from a specific pool.
    /// </summary>
    public void DespawnAllActive(string tag)
    {
        if (!poolDictionary.ContainsKey(tag)) return;
        
        // Find all active pooled objects with this tag
        PooledObject[] allPooledObjects = FindObjectsByType<PooledObject>(FindObjectsSortMode.None);
        
        int despawnedCount = 0;
        foreach (PooledObject pooledObj in allPooledObjects)
        {
            if (pooledObj != null && pooledObj.poolTag == tag && pooledObj.gameObject.activeInHierarchy)
            {
                Despawn(pooledObj.gameObject);
                despawnedCount++;
            }
        }
        
        Debug.Log($"[ObjectPool] Despawned {despawnedCount} active objects from pool '{tag}'");
    }
    
    /// <summary>
    /// Clears all pools completely (both active and inactive objects).
    /// Use with caution - this destroys all pooled objects.
    /// </summary>
    public void ClearAllPools()
    {
        // First despawn all active objects
        DespawnAllActive();
        
        // Then clear all pool queues
        foreach (var kvp in poolDictionary)
        {
            ClearPool(kvp.Key);
        }
        
        Debug.Log("[ObjectPool] Cleared all pools");
    }
    
    /// <summary>
    /// Checks if a pool exists
    /// </summary>
    public bool HasPool(string tag)
    {
        return poolDictionary.ContainsKey(tag);
    }
    
    /// <summary>
    /// Gets the total number of objects created for a pool
    /// </summary>
    public int GetTotalCreated(string tag)
    {
        return totalCreated.ContainsKey(tag) ? totalCreated[tag] : 0;
    }
    
    /// <summary>
    /// Gets the peak active count for a pool (useful for optimization)
    /// </summary>
    public int GetPeakActiveCount(string tag)
    {
        return peakActiveCount.ContainsKey(tag) ? peakActiveCount[tag] : 0;
    }
    
    /// <summary>
    /// Gets statistics for a specific pool
    /// </summary>
    public PoolStats GetPoolStats(string tag)
    {
        PoolStats stats = new PoolStats();
        stats.tag = tag;
        
        if (poolDictionary.ContainsKey(tag))
        {
            stats.totalCreated = totalCreated[tag];
            stats.activeCount = activeCount[tag];
            stats.availableCount = poolDictionary[tag].Count;
            stats.peakActiveCount = peakActiveCount[tag];
            stats.maxSize = poolConfigs[tag].maxSize;
        }
        
        return stats;
    }
    
    /// <summary>
    /// Gets statistics for all pools
    /// </summary>
    public List<PoolStats> GetAllPoolStats()
    {
        List<PoolStats> allStats = new List<PoolStats>(poolDictionary.Count);
        
        foreach (var kvp in poolDictionary)
        {
            allStats.Add(GetPoolStats(kvp.Key));
        }
        
        return allStats;
    }
    
    /// <summary>
    /// Logs all pool statistics to the console (editor only)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void LogAllPoolStats()
    {
        Debug.Log("=== Object Pool Statistics ===");
        foreach (var kvp in poolDictionary)
        {
            PoolStats stats = GetPoolStats(kvp.Key);
            Debug.Log($"Pool '{stats.tag}': Active={stats.activeCount}, Available={stats.availableCount}, Peak={stats.peakActiveCount}, Total={stats.totalCreated}, Max={stats.maxSize}");
        }
    }
}

/// <summary>
/// Component added to pooled objects for tracking
/// </summary>
public class PooledObject : MonoBehaviour
{
    public string poolTag;
    
    /// <summary>
    /// Returns this object to its pool
    /// </summary>
    public void ReturnToPool()
    {
        ObjectPool.Instance.Despawn(gameObject);
    }
}

/// <summary>
/// Interface for objects that need to respond to pool events
/// </summary>
public interface IPooledObject
{
    void OnSpawnFromPool();
    void OnReturnToPool();
}