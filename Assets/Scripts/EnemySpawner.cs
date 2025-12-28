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
    
    private List<GameObject> activeEnemies = new List<GameObject>();
    private Dictionary<GameObject, EnemySpawnConfig> enemyToConfigMap = new Dictionary<GameObject, EnemySpawnConfig>();
    
    private WaveData currentWaveData;
    private bool isSpawningActive = false;
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
        waveData.ResetConfigs();
        
        enemyToConfigMap.Clear();
        
        isSpawningActive = true;
        if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);
        spawnLoopCoroutine = StartCoroutine(SpawnLoopCoroutine());
    }
    
    public void StopSpawning()
    {
        isSpawningActive = false;
        if (spawnLoopCoroutine != null)
        {
            StopCoroutine(spawnLoopCoroutine);
            spawnLoopCoroutine = null;
        }
    }
    
    private IEnumerator SpawnLoopCoroutine()
    {
        while (isSpawningActive && currentWaveData != null)
        {
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