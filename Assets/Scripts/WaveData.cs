using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WaveData", menuName = "Wave System/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Wave Info")]
    public string waveName;
    
    [Header("Spawn Configuration")]
    public List<SpawnGroup> spawnGroups = new List<SpawnGroup>();
    
    public int GetTotalEnemies()
    {
        int total = 0;
        foreach (var group in spawnGroups)
        {
            total += group.count;
        }
        return total;
    }
}