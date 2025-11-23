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
    
    private List<GameObject> activeEnemies = new List<GameObject>();
    private Dictionary<SpawnGroup, Coroutine> activeSpawnRoutines = new Dictionary<SpawnGroup, Coroutine>();
    
    private void Start()
    {
        // Auto-find snake references if not assigned
        if (snakeBody == null)
        {
            snakeBody = FindFirstObjectByType<SnakeBody>();
        }
        
        if (snakeHealth == null)
        {
            snakeHealth = FindFirstObjectByType<SnakeHealth>();
        }
    }
    
    public void SpawnWave(WaveData waveData)
    {
        // Sort groups by threshold (highest first, so initial spawns happen first)
        List<SpawnGroup> sortedGroups = new List<SpawnGroup>(waveData.spawnGroups);
        sortedGroups.Sort((a, b) => b.spawnThreshold.CompareTo(a.spawnThreshold));
        
        // Spawn groups that should appear immediately (threshold >= total enemies)
        int totalEnemies = waveData.GetTotalEnemies();
        foreach (var group in sortedGroups)
        {
            if (group.spawnThreshold >= totalEnemies)
            {
                SpawnGroup(group);
            }
        }
    }
    
    public void CheckSpawnThresholds(WaveData waveData, int remainingEnemies)
    {
        foreach (var group in waveData.spawnGroups)
        {
            // Check if this group should spawn and hasn't spawned yet
            if (remainingEnemies <= group.spawnThreshold && !activeSpawnRoutines.ContainsKey(group))
            {
                SpawnGroup(group);
            }
        }
    }
    
    public void SpawnGroup(SpawnGroup group)
    {
        if (activeSpawnRoutines.ContainsKey(group))
        {
            return; // Already spawning/spawned
        }
        
        Coroutine routine = StartCoroutine(SpawnGroupCoroutine(group));
        activeSpawnRoutines[group] = routine;
    }
    
    private IEnumerator SpawnGroupCoroutine(SpawnGroup group)
    {
        if (group.spawnZoneIndex < 0 || group.spawnZoneIndex >= spawnZones.Count)
        {
            Debug.LogError($"Invalid spawn zone index: {group.spawnZoneIndex}");
            yield break;
        }
        
        BoxCollider spawnZone = spawnZones[group.spawnZoneIndex].zoneCollider;
        
        if (spawnZone == null)
        {
            Debug.LogError($"Spawn zone at index {group.spawnZoneIndex} has no collider!");
            yield break;
        }
        
        for (int i = 0; i < group.count; i++)
        {
            Vector3 spawnPos = GetRandomPositionInBox(spawnZone);
            GameObject enemy = Instantiate(group.enemyPrefab, spawnPos, Quaternion.identity);
            activeEnemies.Add(enemy);
            
            // Initialize apple enemy with snake references
            AppleEnemy appleEnemy = enemy.GetComponent<AppleEnemy>();
            if (appleEnemy != null)
            {
                appleEnemy.Initialize(snakeBody, snakeHealth);
            }
            
            if (i < group.count - 1)
            {
                yield return new WaitForSeconds(group.spawnDelay);
            }
        }
        
        activeSpawnRoutines.Remove(group);
    }
    
    private Vector3 GetRandomPositionInBox(BoxCollider box)
    {
        Vector3 center = box.transform.position + box.center;
        Vector3 size = box.size;
        
        Vector3 randomPoint = new Vector3(
            Random.Range(-size.x / 2, size.x / 2),
            Random.Range(-size.y / 2, size.y / 2),
            Random.Range(-size.z / 2, size.z / 2)
        );
        
        // Transform local point to world space
        return box.transform.TransformPoint(randomPoint);
    }
    
    public void RemoveEnemy(GameObject enemy)
    {
        activeEnemies.Remove(enemy);
    }
    
    public int GetActiveEnemyCount()
    {
        // Clean up null references
        activeEnemies.RemoveAll(e => e == null);
        return activeEnemies.Count;
    }
    
    public void ClearAllEnemies()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        activeEnemies.Clear();
        
        // Stop all spawn routines
        foreach (var routine in activeSpawnRoutines.Values)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
        }
        activeSpawnRoutines.Clear();
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