using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnZone
    {
        public string zoneName;
        public BoxCollider zoneCollider;
    }
    
    [Header("Spawn Zones")]
    [Tooltip("Indexed spawn zones - reference these by index in WaveData")]
    public List<SpawnZone> spawnZones = new List<SpawnZone>();
    
    [Header("Snake References")]
    [SerializeField] private SnakeBody snakeBody;
    [SerializeField] private SnakeHealth snakeHealth;
    
    [Header("Spawn Settings")]
    [Tooltip("How often to check if new enemies should be spawned")]
    [SerializeField] private float spawnCheckInterval = 0.25f;
    
    private List<GameObject> activeEnemies = new List<GameObject>();
    
    // Wave tracking
    private WaveData currentWaveData;
    private bool isSpawningActive = false;
    private Coroutine spawnLoopCoroutine;
    
    // Track which config spawned which enemy
    private Dictionary<GameObject, EnemySpawnConfig> enemyToConfigMap = new Dictionary<GameObject, EnemySpawnConfig>();
    
    private void Start()
    {
        // Subscribe to SnakeBody initialization to get references
        SnakeBody.OnBodyPartsInitialized += OnSnakeInitialized;
        
        // Try to find immediately if snake already exists
        if (snakeBody == null)
        {
            snakeBody = FindFirstObjectByType<SnakeBody>();
        }
        
        if (snakeHealth == null && snakeBody != null)
        {
            snakeHealth = snakeBody.GetComponent<SnakeHealth>();
        }
    }
    
    private void OnDestroy()
    {
        SnakeBody.OnBodyPartsInitialized -= OnSnakeInitialized;
    }
    
    private void OnSnakeInitialized()
    {
        // Get references when snake is initialized
        if (snakeBody == null)
        {
            snakeBody = FindFirstObjectByType<SnakeBody>();
        }
        
        if (snakeHealth == null && snakeBody != null)
        {
            snakeHealth = snakeBody.GetComponent<SnakeHealth>();
        }
    }
    
    /// <summary>
    /// Start spawning enemies for a wave
    /// </summary>
    public void StartWaveSpawning(WaveData waveData)
    {
        currentWaveData = waveData;
        
        // Reset all configs
        waveData.ResetConfigs();
        
        // Clear tracking
        enemyToConfigMap.Clear();
        
        // Start the spawn loop
        isSpawningActive = true;
        if (spawnLoopCoroutine != null)
        {
            StopCoroutine(spawnLoopCoroutine);
        }
        spawnLoopCoroutine = StartCoroutine(SpawnLoopCoroutine());
    }
    
    /// <summary>
    /// Stop spawning new enemies
    /// </summary>
    public void StopSpawning()
    {
        isSpawningActive = false;
        if (spawnLoopCoroutine != null)
        {
            StopCoroutine(spawnLoopCoroutine);
            spawnLoopCoroutine = null;
        }
    }
    
    /// <summary>
    /// Main spawn loop that continuously checks and spawns enemies
    /// </summary>
    private IEnumerator SpawnLoopCoroutine()
    {
        while (isSpawningActive && currentWaveData != null)
        {
            // Check each enemy config
            foreach (var config in currentWaveData.enemyConfigs)
            {
                if (config.CanSpawn())
                {
                    SpawnEnemyFromConfig(config);
                }
            }
            
            yield return new WaitForSeconds(spawnCheckInterval);
        }
    }
    
    /// <summary>
    /// Spawn a single enemy from the given config
    /// </summary>
    private void SpawnEnemyFromConfig(EnemySpawnConfig config)
    {
        if (config.enemyPrefab == null)
        {
            Debug.LogWarning("EnemySpawnConfig has no prefab assigned!");
            return;
        }
        
        // Get a random spawn zone
        int zoneIndex = GetRandomZoneIndex(config);
        if (zoneIndex < 0 || zoneIndex >= spawnZones.Count)
        {
            Debug.LogError($"Invalid spawn zone index: {zoneIndex}");
            return;
        }
        
        BoxCollider spawnZone = spawnZones[zoneIndex].zoneCollider;
        if (spawnZone == null)
        {
            Debug.LogError($"Spawn zone at index {zoneIndex} has no collider!");
            return;
        }
        
        // Spawn the enemy
        Vector3 spawnPos = GetRandomPositionInBox(spawnZone);
        GameObject enemy = Instantiate(config.enemyPrefab, spawnPos, Quaternion.identity);
        activeEnemies.Add(enemy);
        
        // Track which config this enemy belongs to
        enemyToConfigMap[enemy] = config;
        
        // Update config tracking
        config.currentOnScreen++;
        config.lastSpawnTime = Time.time;
        
        // Initialize apple enemy with snake references
        AppleEnemy appleEnemy = enemy.GetComponent<AppleEnemy>();
        if (appleEnemy != null)
        {
            appleEnemy.Initialize(snakeBody, snakeHealth);
        }
    }
    
    /// <summary>
    /// Get a random zone index from the config's allowed zones
    /// </summary>
    private int GetRandomZoneIndex(EnemySpawnConfig config)
    {
        if (config.allowedSpawnZones == null || config.allowedSpawnZones.Count == 0)
        {
            // Use any zone
            if (spawnZones.Count == 0) return -1;
            return Random.Range(0, spawnZones.Count);
        }
        
        // Pick from allowed zones
        return config.allowedSpawnZones[Random.Range(0, config.allowedSpawnZones.Count)];
    }
    
    /// <summary>
    /// Called when an enemy dies - updates tracking
    /// </summary>
    public void OnEnemyDied(GameObject enemy)
    {
        if (enemyToConfigMap.TryGetValue(enemy, out EnemySpawnConfig config))
        {
            config.currentOnScreen--;
            enemyToConfigMap.Remove(enemy);
        }
        
        activeEnemies.Remove(enemy);
    }
    
    /// <summary>
    /// Called when an enemy is removed without dying (e.g., wave reset)
    /// </summary>
    public void OnEnemyRemoved(GameObject enemy)
    {
        if (enemyToConfigMap.TryGetValue(enemy, out EnemySpawnConfig config))
        {
            config.currentOnScreen--;
            enemyToConfigMap.Remove(enemy);
        }
        
        activeEnemies.Remove(enemy);
    }
    
    private Vector3 GetRandomPositionInBox(BoxCollider box)
    {
        Vector3 size = box.size;
        
        Vector3 randomPoint = new Vector3(
            Random.Range(-size.x / 2, size.x / 2),
            Random.Range(-size.y / 2, size.y / 2),
            Random.Range(-size.z / 2, size.z / 2)
        );
        
        // Transform local point to world space
        return box.transform.TransformPoint(box.center + randomPoint);
    }
    
    public void RemoveEnemy(GameObject enemy)
    {
        OnEnemyRemoved(enemy);
    }
    
    public int GetActiveEnemyCount()
    {
        // Clean up null references
        activeEnemies.RemoveAll(e => e == null);
        return activeEnemies.Count;
    }
    
    public void ClearAllEnemies()
    {
        // Stop spawning
        StopSpawning();
        
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        activeEnemies.Clear();
        enemyToConfigMap.Clear();
    }
    
    private void OnDrawGizmos()
    {
        for (int i = 0; i < spawnZones.Count; i++)
        {
            if (spawnZones[i].zoneCollider != null)
            {
                BoxCollider zone = spawnZones[i].zoneCollider;
                Gizmos.color = Color.cyan;
                Gizmos.matrix = zone.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(zone.center, zone.size);
                
                // Draw index label
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(zone.transform.position, $"Zone {i}: {spawnZones[i].zoneName}");
                #endif
            }
        }
    }
}