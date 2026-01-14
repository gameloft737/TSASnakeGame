using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages spawning of ability drops in the game world
/// </summary>
public class DropManager : MonoBehaviour
{
    [Header("Drop Prefab")]
    [SerializeField] private GameObject dropPrefab;
    
    [Header("Ability Prefabs")]
    [SerializeField] private List<GameObject> abilityPrefabs = new List<GameObject>();
    
    [Header("Spawn Settings")]
    [SerializeField] private float spawnHeight = 10f;
    [SerializeField] private bool autoSpawn = false;
    [SerializeField] private float autoSpawnInterval = 10f;
    [SerializeField] private Vector2 spawnAreaMin = new Vector2(-20f, -20f);
    [SerializeField] private Vector2 spawnAreaMax = new Vector2(20f, 20f);
    
    [Header("Drop Limit")]
    [SerializeField] private int maxDrops = 8;
    
    [Header("Drop Points")]
    [SerializeField] private Transform dropPointsContainer;
    [SerializeField] private bool useDropPoints = false;
    
    private float autoSpawnTimer = 0f;
    private List<Transform> dropPoints = new List<Transform>(16);
    private List<AbilityDrop> activeDrops = new List<AbilityDrop>(16);

    private void Start()
    {
        // Create default drop prefab if none assigned
        if (dropPrefab == null)
        {
            dropPrefab = CreateDefaultDropPrefab();
        }
        
        // Cache drop points from container
        if (dropPointsContainer != null)
        {
            CacheDropPoints();
        }
    }

    private void Update()
    {
        // Clean up null references from collected/destroyed drops (no lambda allocation)
        CleanupNullDrops();
        
        if (autoSpawn)
        {
            autoSpawnTimer += Time.deltaTime;
            
            if (autoSpawnTimer >= autoSpawnInterval)
            {
                SpawnRandomDrop();
                autoSpawnTimer = 0f;
            }
        }
    }
    
    /// <summary>
    /// Removes null entries from activeDrops list without lambda allocation.
    /// Uses reverse iteration to avoid index shifting issues.
    /// </summary>
    private void CleanupNullDrops()
    {
        for (int i = activeDrops.Count - 1; i >= 0; i--)
        {
            if (activeDrops[i] == null)
            {
                activeDrops.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// Clears all active ability drops from the world.
    /// Call this when restarting a level or respawning.
    /// </summary>
    public void ClearAllDrops()
    {
        CleanupNullDrops();
        
        for (int i = activeDrops.Count - 1; i >= 0; i--)
        {
            AbilityDrop drop = activeDrops[i];
            if (drop != null && drop.gameObject != null)
            {
                // Use object pool if available
                if (ObjectPool.Instance != null)
                {
                    ObjectPool.Instance.Despawn(drop.gameObject);
                }
                else
                {
                    Destroy(drop.gameObject);
                }
            }
        }
        
        activeDrops.Clear();
        
        #if UNITY_EDITOR
        Debug.Log("[DropManager] Cleared all ability drops");
        #endif
    }

    /// <summary>
    /// Gets the current number of active drops in the world
    /// </summary>
    public int GetActiveDropCount()
    {
        // Clean up null references first (no lambda allocation)
        CleanupNullDrops();
        return activeDrops.Count;
    }
    
    /// <summary>
    /// Checks if more drops can be spawned
    /// </summary>
    public bool CanSpawnDrop()
    {
        return GetActiveDropCount() < maxDrops;
    }

    /// <summary>
    /// Spawns a drop at a specific position
    /// </summary>
    public void SpawnDrop(Vector3 position, GameObject abilityPrefab = null)
    {
        if (dropPrefab == null) return;
        
        // Check if we've reached the maximum number of drops
        if (!CanSpawnDrop())
        {
            #if UNITY_EDITOR
            Debug.Log($"[DropManager] Cannot spawn drop - maximum of {maxDrops} drops already active ({GetActiveDropCount()})");
            #endif
            return;
        }
        
        // Use object pool if available, otherwise instantiate
        GameObject drop;
        if (ObjectPool.Instance != null)
        {
            drop = ObjectPool.Instance.Spawn(dropPrefab, position, Quaternion.identity);
        }
        else
        {
            drop = Instantiate(dropPrefab, position, Quaternion.identity);
        }
        
        AbilityDrop dropScript = drop.GetComponent<AbilityDrop>();
        
        // Track the active drop
        if (dropScript != null)
        {
            activeDrops.Add(dropScript);
            #if UNITY_EDITOR
            Debug.Log($"[DropManager] Spawned drop. Active drops: {activeDrops.Count}/{maxDrops}");
            #endif
        }
    }

    /// <summary>
    /// Spawns a drop at a random position within the spawn area
    /// </summary>
    public void SpawnRandomDrop()
    {
        Vector3 spawnPos;
        
        if (useDropPoints && dropPoints.Count > 0)
        {
            // Use random drop point
            Transform randomPoint = dropPoints[Random.Range(0, dropPoints.Count)];
            spawnPos = randomPoint.position;
        }
        else
        {
            // Use random area position
            spawnPos = new Vector3(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                spawnHeight,
                Random.Range(spawnAreaMin.y, spawnAreaMax.y)
            );
        }
        
        SpawnDrop(spawnPos);
    }
    
    /// <summary>
    /// Spawns a drop at a specific drop point index
    /// </summary>
    public void SpawnAtDropPoint(int index, GameObject abilityPrefab = null)
    {
        if (!useDropPoints || dropPoints.Count == 0)
        {
            #if UNITY_EDITOR
            Debug.LogWarning("Drop points not configured!");
            #endif
            return;
        }
        
        if (index < 0 || index >= dropPoints.Count)
        {
            #if UNITY_EDITOR
            Debug.LogWarning($"Drop point index {index} out of range!");
            #endif
            return;
        }
        
        SpawnDrop(dropPoints[index].position, abilityPrefab);
    }

    /// <summary>
    /// Adds an ability prefab to the pool
    /// </summary>
    public void RegisterAbility(GameObject abilityPrefab)
    {
        if (abilityPrefab != null && !abilityPrefabs.Contains(abilityPrefab))
        {
            abilityPrefabs.Add(abilityPrefab);
        }
    }
    
    /// <summary>
    /// Caches all child transforms as drop points
    /// </summary>
    private void CacheDropPoints()
    {
        dropPoints.Clear();
        
        foreach (Transform child in dropPointsContainer)
        {
            dropPoints.Add(child);
        }
        
        #if UNITY_EDITOR
        Debug.Log($"Cached {dropPoints.Count} drop points from {dropPointsContainer.name}");
        #endif
    }
    
    /// <summary>
    /// Gets the total number of drop points
    /// </summary>
    public int GetDropPointCount()
    {
        return dropPoints.Count;
    }

    private GameObject GetRandomAbility()
    {
        if (abilityPrefabs.Count == 0)
        {
            #if UNITY_EDITOR
            Debug.LogWarning("No ability prefabs registered in DropManager! Add abilities to the list in inspector.");
            #endif
            return null;
        }
        
        return abilityPrefabs[Random.Range(0, abilityPrefabs.Count)];
    }

    private GameObject CreateDefaultDropPrefab()
    {
        GameObject drop = new GameObject("DefaultDrop");
        
        // Add visual representation (capsule)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "DropVisual";
        visual.transform.SetParent(drop.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(0.5f, 0.75f, 0.5f);
        
        // Set color
        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.8f, 0.6f, 1f, 1f); // Purple color
            renderer.material = mat;
        }
        
        // Add rigidbody for physics
        Rigidbody rb = drop.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.linearDamping = 0.5f;
        
        // Add collider
        CapsuleCollider collider = drop.AddComponent<CapsuleCollider>();
        collider.radius = 0.25f;
        collider.height = 0.75f;
        collider.isTrigger = true;
        
        // Add AbilityDrop script
        drop.AddComponent<AbilityDrop>();
        
        drop.SetActive(false);
        return drop;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw spawn area
        Gizmos.color = Color.green;
        Vector3 center = new Vector3(
            (spawnAreaMin.x + spawnAreaMax.x) / 2f,
            spawnHeight,
            (spawnAreaMin.y + spawnAreaMax.y) / 2f
        );
        Vector3 size = new Vector3(
            spawnAreaMax.x - spawnAreaMin.x,
            0.1f,
            spawnAreaMax.y - spawnAreaMin.y
        );
        Gizmos.DrawWireCube(center, size);
    }
}