using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Zones")]
    [Tooltip("Add box colliders as children - these define spawn areas")]
    public List<BoxCollider> spawnZones = new List<BoxCollider>();
    
    private List<GameObject> activeEnemies = new List<GameObject>();
    
    private void Start()
    {
        // Auto-find box colliders if not assigned
        if (spawnZones.Count == 0)
        {
            spawnZones.AddRange(GetComponentsInChildren<BoxCollider>());
        }
    }
    
    public void SpawnGroup(SpawnGroup group)
    {
        StartCoroutine(SpawnGroupCoroutine(group));
    }
    
    private IEnumerator SpawnGroupCoroutine(SpawnGroup group)
    {
        if (group.spawnZone == null)
        {
            Debug.LogError("SpawnGroup has no spawn zone assigned!");
            yield break;
        }
        
        for (int i = 0; i < group.count; i++)
        {
            Vector3 spawnPos = GetRandomPositionInBox(group.spawnZone);
            GameObject enemy = Instantiate(group.enemyPrefab, spawnPos, Quaternion.identity);
            activeEnemies.Add(enemy);
            
            // Subscribe to enemy death
            AppleEnemy appleEnemy = enemy.GetComponent<AppleEnemy>();
            if (appleEnemy != null)
            {
                // You'll need to add an event to AppleEnemy for death tracking
            }
            
            if (i < group.count - 1)
            {
                yield return new WaitForSeconds(group.spawnDelay);
            }
        }
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
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        foreach (var zone in spawnZones)
        {
            if (zone != null)
            {
                Gizmos.matrix = zone.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(zone.center, zone.size);
            }
        }
    }
}