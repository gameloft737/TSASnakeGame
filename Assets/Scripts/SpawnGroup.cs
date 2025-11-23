using UnityEngine;
using System.Collections;
using System.Collections.Generic;


[System.Serializable]
public class SpawnGroup
{
    [Header("What to Spawn")]
    public GameObject enemyPrefab;
    public int count;
    
    [Header("Where to Spawn")]
    [Tooltip("Index of the spawn zone in EnemySpawner's zone list")]
    public int spawnZoneIndex;
    
    [Header("When to Spawn")]
    [Tooltip("Spawn this group when total remaining enemies <= this number")]
    public int spawnThreshold;
    
    [Header("How to Spawn")]
    [Tooltip("Delay between spawning each enemy in this group")]
    public float spawnDelay = 0.1f;
}