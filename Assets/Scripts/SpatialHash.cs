using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spatial hashing system for efficient proximity queries.
/// Divides the world into a grid of cells for O(1) neighbor lookups.
/// Use this instead of FindObjectsOfType or distance checks against all objects.
/// </summary>
/// <typeparam name="T">The type of objects to store (must be a Component)</typeparam>
public class SpatialHash<T> where T : Component
{
    private Dictionary<int, List<T>> cells;
    private Dictionary<T, int> objectCells; // Track which cell each object is in
    private float cellSize;
    private float cellSizeInverse; // Cached for faster division
    private int gridWidth;
    
    // Reusable lists to avoid allocations
    private List<T> queryResults;
    private HashSet<int> queriedCells;
    
    /// <summary>
    /// Creates a new spatial hash grid
    /// </summary>
    /// <param name="cellSize">Size of each cell. Smaller = more precise but more memory. 
    /// Should be roughly the size of your query radius.</param>
    /// <param name="expectedObjects">Expected number of objects for initial capacity</param>
    public SpatialHash(float cellSize = 10f, int expectedObjects = 100)
    {
        this.cellSize = cellSize;
        this.cellSizeInverse = 1f / cellSize;
        this.gridWidth = 1000; // Arbitrary large number for hash calculation
        
        int expectedCells = Mathf.Max(16, expectedObjects / 4);
        cells = new Dictionary<int, List<T>>(expectedCells);
        objectCells = new Dictionary<T, int>(expectedObjects);
        queryResults = new List<T>(32);
        queriedCells = new HashSet<int>();
    }
    
    /// <summary>
    /// Gets the cell key for a world position
    /// </summary>
    private int GetCellKey(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x * cellSizeInverse);
        int z = Mathf.FloorToInt(position.z * cellSizeInverse);
        return x + z * gridWidth;
    }
    
    /// <summary>
    /// Gets the cell key for grid coordinates
    /// </summary>
    private int GetCellKey(int x, int z)
    {
        return x + z * gridWidth;
    }
    
    /// <summary>
    /// Inserts an object into the spatial hash
    /// </summary>
    public void Insert(T obj)
    {
        if (obj == null) return;
        
        int cellKey = GetCellKey(obj.transform.position);
        
        // Remove from old cell if it was already inserted
        if (objectCells.TryGetValue(obj, out int oldCell))
        {
            if (oldCell == cellKey) return; // Already in correct cell
            RemoveFromCell(obj, oldCell);
        }
        
        // Add to new cell
        if (!cells.TryGetValue(cellKey, out List<T> cell))
        {
            cell = new List<T>(8);
            cells[cellKey] = cell;
        }
        
        cell.Add(obj);
        objectCells[obj] = cellKey;
    }
    
    /// <summary>
    /// Removes an object from the spatial hash
    /// </summary>
    public void Remove(T obj)
    {
        if (obj == null) return;
        
        if (objectCells.TryGetValue(obj, out int cellKey))
        {
            RemoveFromCell(obj, cellKey);
            objectCells.Remove(obj);
        }
    }
    
    /// <summary>
    /// Removes an object from a specific cell
    /// </summary>
    private void RemoveFromCell(T obj, int cellKey)
    {
        if (cells.TryGetValue(cellKey, out List<T> cell))
        {
            cell.Remove(obj);
            if (cell.Count == 0)
            {
                cells.Remove(cellKey);
            }
        }
    }
    
    /// <summary>
    /// Updates an object's position in the spatial hash.
    /// Call this when an object moves.
    /// </summary>
    public void UpdatePosition(T obj)
    {
        Insert(obj); // Insert handles the update logic
    }
    
    /// <summary>
    /// Finds all objects within a radius of a position.
    /// Returns a reusable list - do not cache the reference!
    /// </summary>
    public List<T> QueryRadius(Vector3 center, float radius)
    {
        queryResults.Clear();
        queriedCells.Clear();
        
        float radiusSqr = radius * radius;
        
        // Calculate cell range to check
        int minX = Mathf.FloorToInt((center.x - radius) * cellSizeInverse);
        int maxX = Mathf.FloorToInt((center.x + radius) * cellSizeInverse);
        int minZ = Mathf.FloorToInt((center.z - radius) * cellSizeInverse);
        int maxZ = Mathf.FloorToInt((center.z + radius) * cellSizeInverse);
        
        // Check all cells in range
        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                int cellKey = GetCellKey(x, z);
                
                if (queriedCells.Contains(cellKey)) continue;
                queriedCells.Add(cellKey);
                
                if (cells.TryGetValue(cellKey, out List<T> cell))
                {
                    int count = cell.Count;
                    for (int i = 0; i < count; i++)
                    {
                        T obj = cell[i];
                        if (obj == null) continue;
                        
                        // Use sqrMagnitude for faster distance check
                        Vector3 diff = obj.transform.position - center;
                        if (diff.sqrMagnitude <= radiusSqr)
                        {
                            queryResults.Add(obj);
                        }
                    }
                }
            }
        }
        
        return queryResults;
    }
    
    /// <summary>
    /// Finds the nearest object to a position within a maximum radius.
    /// Returns null if no object found.
    /// </summary>
    public T QueryNearest(Vector3 center, float maxRadius)
    {
        List<T> nearby = QueryRadius(center, maxRadius);
        
        T nearest = null;
        float nearestDistSqr = float.MaxValue;
        
        int count = nearby.Count;
        for (int i = 0; i < count; i++)
        {
            T obj = nearby[i];
            if (obj == null) continue;
            
            float distSqr = (obj.transform.position - center).sqrMagnitude;
            if (distSqr < nearestDistSqr)
            {
                nearestDistSqr = distSqr;
                nearest = obj;
            }
        }
        
        return nearest;
    }
    
    /// <summary>
    /// Gets all objects in the spatial hash
    /// </summary>
    public IEnumerable<T> GetAll()
    {
        return objectCells.Keys;
    }
    
    /// <summary>
    /// Gets the total number of objects in the spatial hash
    /// </summary>
    public int Count => objectCells.Count;
    
    /// <summary>
    /// Clears all objects from the spatial hash
    /// </summary>
    public void Clear()
    {
        cells.Clear();
        objectCells.Clear();
    }
    
    /// <summary>
    /// Removes null/destroyed objects from the spatial hash.
    /// Call this periodically if objects can be destroyed without calling Remove().
    /// </summary>
    public void CleanupNulls()
    {
        // Find all null objects
        List<T> toRemove = null;
        
        foreach (var kvp in objectCells)
        {
            if (kvp.Key == null)
            {
                if (toRemove == null) toRemove = new List<T>();
                toRemove.Add(kvp.Key);
            }
        }
        
        // Remove them
        if (toRemove != null)
        {
            for (int i = 0; i < toRemove.Count; i++)
            {
                Remove(toRemove[i]);
            }
        }
        
        // Also clean up cells
        List<int> emptyCells = null;
        foreach (var kvp in cells)
        {
            List<T> cell = kvp.Value;
            for (int i = cell.Count - 1; i >= 0; i--)
            {
                if (cell[i] == null)
                {
                    cell.RemoveAt(i);
                }
            }
            
            if (cell.Count == 0)
            {
                if (emptyCells == null) emptyCells = new List<int>();
                emptyCells.Add(kvp.Key);
            }
        }
        
        if (emptyCells != null)
        {
            for (int i = 0; i < emptyCells.Count; i++)
            {
                cells.Remove(emptyCells[i]);
            }
        }
    }
}

/// <summary>
/// MonoBehaviour wrapper for SpatialHash that auto-updates enemy positions.
/// Attach to a manager object in your scene.
/// </summary>
public class EnemySpatialHash : MonoBehaviour
{
    public static EnemySpatialHash Instance { get; private set; }
    
    [SerializeField] private float cellSize = 10f;
    [SerializeField] private float updateInterval = 0.1f; // How often to update positions
    
    private SpatialHash<AppleEnemy> spatialHash;
    private float updateTimer = 0f;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        spatialHash = new SpatialHash<AppleEnemy>(cellSize, 100);
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    private void Update()
    {
        updateTimer += Time.deltaTime;
        
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateAllPositions();
        }
    }
    
    /// <summary>
    /// Updates all enemy positions in the spatial hash
    /// </summary>
    private void UpdateAllPositions()
    {
        // Clean up destroyed enemies
        spatialHash.CleanupNulls();
        
        // Update positions of all tracked enemies
        foreach (var enemy in spatialHash.GetAll())
        {
            if (enemy != null)
            {
                spatialHash.UpdatePosition(enemy);
            }
        }
    }
    
    /// <summary>
    /// Registers an enemy with the spatial hash
    /// </summary>
    public void RegisterEnemy(AppleEnemy enemy)
    {
        if (enemy != null)
        {
            spatialHash.Insert(enemy);
        }
    }
    
    /// <summary>
    /// Unregisters an enemy from the spatial hash
    /// </summary>
    public void UnregisterEnemy(AppleEnemy enemy)
    {
        if (enemy != null)
        {
            spatialHash.Remove(enemy);
        }
    }
    
    /// <summary>
    /// Finds all enemies within a radius of a position
    /// </summary>
    public List<AppleEnemy> GetEnemiesInRadius(Vector3 center, float radius)
    {
        return spatialHash.QueryRadius(center, radius);
    }
    
    /// <summary>
    /// Finds the nearest enemy to a position
    /// </summary>
    public AppleEnemy GetNearestEnemy(Vector3 center, float maxRadius = 50f)
    {
        return spatialHash.QueryNearest(center, maxRadius);
    }
    
    /// <summary>
    /// Gets the total number of tracked enemies
    /// </summary>
    public int EnemyCount => spatialHash.Count;
}