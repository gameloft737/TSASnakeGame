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
    public List<SpawnZone> spawnZones = new List<SpawnZone>();
    
    [Header("Snake References")]
    [SerializeField] private SnakeBody snakeBody;
    [SerializeField] private SnakeHealth snakeHealth;
    
    [Header("Spawn Settings")]
    [SerializeField] private float spawnCheckInterval = 0.25f;
    [SerializeField] private float countSyncInterval = 1.0f; // How often to sync enemy counts
    
    private List<GameObject> activeEnemies = new List<GameObject>();
    private Dictionary<GameObject, EnemySpawnConfig> enemyToConfigMap = new Dictionary<GameObject, EnemySpawnConfig>();
    
    private WaveData currentWaveData;
    private List<EnemySpawnConfig> currentInfiniteConfigs;
    private bool isSpawningActive = false;
    private bool isInfiniteMode = false;
    private Coroutine spawnLoopCoroutine;
    
    private void Start()
    {
        SnakeBody.OnBodyPartsInitialized += OnSnakeInitialized;
        FindSnakeReferences();
    }
    
    private void OnDestroy()
    {
        SnakeBody.OnBodyPartsInitialized -= OnSnakeInitialized;
    }
    
    private void OnSnakeInitialized()
    {
        FindSnakeReferences();
    }
    
    private void FindSnakeReferences()
    {
        if (!snakeBody) snakeBody = FindFirstObjectByType<SnakeBody>();
        if (!snakeHealth && snakeBody) snakeHealth = snakeBody.GetComponent<SnakeHealth>();
    }
    
    public void StartWaveSpawning(WaveData waveData)
    {
        currentWaveData = waveData;
        currentInfiniteConfigs = null;
        isInfiniteMode = false;
        waveData.ResetConfigs();
        
        enemyToConfigMap.Clear();
        
        isSpawningActive = true;
        if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);
        spawnLoopCoroutine = StartCoroutine(SpawnLoopCoroutine());
    }
    
    public void StartInfiniteWaveSpawning(List<EnemySpawnConfig> configs)
    {
        currentWaveData = null;
        currentInfiniteConfigs = configs;
        isInfiniteMode = true;
        
        // Reset all configs
        foreach (var config in configs)
        {
            config.Reset();
        }
        
        // Clear the mapping - enemies from previous waves will be orphaned but that's OK
        // They will still be tracked in activeEnemies list
        enemyToConfigMap.Clear();
        
        isSpawningActive = true;
        if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);
        spawnLoopCoroutine = StartCoroutine(SpawnLoopCoroutine());
        
        Debug.Log($"[EnemySpawner] Started infinite wave spawning with {configs.Count} enemy types, isInfiniteMode={isInfiniteMode}");
    }
    
    public void StopSpawning()
    {
        Debug.Log("[EnemySpawner] StopSpawning called");
        isSpawningActive = false;
        if (spawnLoopCoroutine != null)
        {
            StopCoroutine(spawnLoopCoroutine);
            spawnLoopCoroutine = null;
        }
    }
    
    /// <summary>
    /// Resume spawning with the current wave configuration (infinite or legacy)
    /// Call this when returning from a menu that paused spawning
    /// </summary>
    public void ResumeSpawning()
    {
        Debug.Log($"[EnemySpawner] ResumeSpawning called. isInfiniteMode={isInfiniteMode}, hasConfigs={(isInfiniteMode ? currentInfiniteConfigs != null : currentWaveData != null)}");
        
        // Don't resume if already spawning
        if (isSpawningActive && spawnLoopCoroutine != null)
        {
            Debug.Log("[EnemySpawner] Already spawning, no need to resume");
            return;
        }
        
        // Check if we have valid configs to resume with
        if (isInfiniteMode && currentInfiniteConfigs != null && currentInfiniteConfigs.Count > 0)
        {
            isSpawningActive = true;
            if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);
            spawnLoopCoroutine = StartCoroutine(SpawnLoopCoroutine());
            Debug.Log("[EnemySpawner] Resumed infinite wave spawning");
        }
        else if (!isInfiniteMode && currentWaveData != null)
        {
            isSpawningActive = true;
            if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);
            spawnLoopCoroutine = StartCoroutine(SpawnLoopCoroutine());
            Debug.Log("[EnemySpawner] Resumed legacy wave spawning");
        }
        else
        {
            Debug.LogWarning("[EnemySpawner] Cannot resume spawning - no valid configuration");
        }
    }
    
    /// <summary>
    /// Check if spawning is currently active
    /// </summary>
    public bool IsSpawning()
    {
        return isSpawningActive && spawnLoopCoroutine != null;
    }
    
    private IEnumerator SpawnLoopCoroutine()
    {
        Debug.Log($"[EnemySpawner] SpawnLoopCoroutine started. isInfiniteMode={isInfiniteMode}, isSpawningActive={isSpawningActive}");
        
        float lastSyncTime = Time.time;
        
        while (isSpawningActive)
        {
            List<EnemySpawnConfig> configsToUse = null;
            
            if (isInfiniteMode && currentInfiniteConfigs != null)
            {
                configsToUse = currentInfiniteConfigs;
            }
            else if (!isInfiniteMode && currentWaveData != null)
            {
                configsToUse = currentWaveData.enemyConfigs;
            }
            
            if (configsToUse == null || configsToUse.Count == 0)
            {
                Debug.LogWarning($"[EnemySpawner] No configs to use! isInfiniteMode={isInfiniteMode}, currentInfiniteConfigs null={currentInfiniteConfigs == null}");
                yield return new WaitForSeconds(spawnCheckInterval);
                continue;
            }
            
            // Periodically sync enemy counts to fix any desync issues
            if (Time.time - lastSyncTime >= countSyncInterval)
            {
                SyncEnemyCounts(configsToUse);
                lastSyncTime = Time.time;
            }
            
            foreach (var config in configsToUse)
            {
                if (config.CanSpawn())
                {
                    SpawnEnemyFromConfig(config);
                }
            }
            
            yield return new WaitForSeconds(spawnCheckInterval);
        }
        
        Debug.Log($"[EnemySpawner] SpawnLoopCoroutine ended. isSpawningActive={isSpawningActive}");
    }
    
    /// <summary>
    /// Syncs the currentOnScreen counts with actual living enemies.
    /// This fixes any desync that might occur if enemies are destroyed without proper notification.
    /// </summary>
    private void SyncEnemyCounts(List<EnemySpawnConfig> configs)
    {
        // Clean up null entries from activeEnemies list
        activeEnemies.RemoveAll(e => e == null);
        
        // Clean up null entries from enemyToConfigMap
        List<GameObject> deadEnemies = new List<GameObject>();
        foreach (var kvp in enemyToConfigMap)
        {
            if (kvp.Key == null)
            {
                deadEnemies.Add(kvp.Key);
            }
        }
        foreach (var deadEnemy in deadEnemies)
        {
            enemyToConfigMap.Remove(deadEnemy);
        }
        
        // Reset all config counts
        foreach (var config in configs)
        {
            config.currentOnScreen = 0;
        }
        
        // Recount based on actual living enemies
        foreach (var kvp in enemyToConfigMap)
        {
            if (kvp.Key != null && kvp.Value != null)
            {
                kvp.Value.currentOnScreen++;
            }
        }
    }
    
    private void SpawnEnemyFromConfig(EnemySpawnConfig config)
    {
        if (!config.enemyPrefab)
        {
            Debug.LogWarning("EnemySpawnConfig has no prefab assigned!");
            return;
        }
        
        int zoneIndex = GetRandomZoneIndex(config);
        if (zoneIndex < 0 || zoneIndex >= spawnZones.Count)
        {
            Debug.LogError($"Invalid spawn zone index: {zoneIndex}");
            return;
        }
        
        BoxCollider spawnZone = spawnZones[zoneIndex].zoneCollider;
        if (!spawnZone)
        {
            Debug.LogError($"Spawn zone at index {zoneIndex} has no collider!");
            return;
        }
        
        Vector3 spawnPos = GetRandomPositionInBox(spawnZone);
        GameObject enemy = Instantiate(config.enemyPrefab, spawnPos, Quaternion.identity);
        activeEnemies.Add(enemy);
        
        enemyToConfigMap[enemy] = config;
        
        config.currentOnScreen++;
        config.lastSpawnTime = Time.time;
        
        AppleEnemy appleEnemy = enemy.GetComponent<AppleEnemy>();
        if (appleEnemy)
        {
            appleEnemy.Initialize(snakeBody, snakeHealth);
        }
    }
    
    private int GetRandomZoneIndex(EnemySpawnConfig config)
    {
        if (config.allowedSpawnZones == null || config.allowedSpawnZones.Count == 0)
        {
            return spawnZones.Count == 0 ? -1 : Random.Range(0, spawnZones.Count);
        }
        
        return config.allowedSpawnZones[Random.Range(0, config.allowedSpawnZones.Count)];
    }
    
    public void OnEnemyDied(GameObject enemy)
    {
        if (enemyToConfigMap.TryGetValue(enemy, out EnemySpawnConfig config))
        {
            config.currentOnScreen--;
            enemyToConfigMap.Remove(enemy);
        }
        
        activeEnemies.Remove(enemy);
    }
    
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
        
        return box.transform.TransformPoint(box.center + randomPoint);
    }
    
    public void RemoveEnemy(GameObject enemy) => OnEnemyRemoved(enemy);
    
    public int GetActiveEnemyCount()
    {
        activeEnemies.RemoveAll(e => e == null);
        return activeEnemies.Count;
    }
    
    public void ClearAllEnemies()
    {
        StopSpawning();
        
        foreach (var enemy in activeEnemies)
        {
            if (enemy) Destroy(enemy);
        }
        
        activeEnemies.Clear();
        enemyToConfigMap.Clear();
    }
    
    private void OnDrawGizmos()
    {
        for (int i = 0; i < spawnZones.Count; i++)
        {
            if (spawnZones[i].zoneCollider)
            {
                BoxCollider zone = spawnZones[i].zoneCollider;
                Gizmos.color = Color.cyan;
                Gizmos.matrix = zone.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(zone.center, zone.size);
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(zone.transform.position, $"Zone {i}: {spawnZones[i].zoneName}");
                #endif
            }
        }
    }
}