using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class SpriteCount
{
    public Sprite sprite;
    public int count;
}

/// <summary>
/// Configuration for a single enemy type in a wave.
/// Defines how many can be on screen at once.
/// </summary>
[System.Serializable]
public class EnemySpawnConfig
{
    [Header("Enemy Type")]
    [Tooltip("The enemy prefab to spawn")]
    public GameObject enemyPrefab;
    
    [Header("Spawn Limits")]
    [Tooltip("Maximum number of this enemy type that can be on screen at once")]
    [Min(1)]
    public int maxOnScreen = 3;
    
    [Header("Spawn Settings")]
    [Tooltip("Which spawn zones this enemy can spawn from (indices into EnemySpawner's zone list). Leave empty to use all zones.")]
    public List<int> allowedSpawnZones = new List<int>();
    
    [Tooltip("Minimum delay between spawning enemies of this type")]
    public float spawnCooldown = 0.5f;
    
    // Runtime tracking (not serialized)
    [System.NonSerialized] public int currentOnScreen = 0;
    [System.NonSerialized] public float lastSpawnTime = 0f;
    
    /// <summary>
    /// Returns true if more enemies of this type can be spawned
    /// </summary>
    public bool CanSpawn()
    {
        return currentOnScreen < maxOnScreen && 
               Time.time >= lastSpawnTime + spawnCooldown;
    }
    
    /// <summary>
    /// Reset runtime tracking for a new wave
    /// </summary>
    public void Reset()
    {
        currentOnScreen = 0;
        lastSpawnTime = 0f;
    }
}

[CreateAssetMenu(fileName = "WaveData", menuName = "Wave System/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Wave Info")]
    public string waveName;
    
    [Header("Wave Completion")]
    [Tooltip("XP required to complete this wave and move to the next")]
    public int xpToComplete = 100;
    
    [Header("Enemy Configuration")]
    [Tooltip("Configure each enemy type for this wave")]
    public List<EnemySpawnConfig> enemyConfigs = new List<EnemySpawnConfig>();
    
    [Header("Sprite Configuration")]
    public List<SpriteCount> spriteCounts = new List<SpriteCount>();
    
    // Runtime tracking for XP collected this wave
    [System.NonSerialized] public int xpCollectedThisWave = 0;
    
    /// <summary>
    /// Check if wave is complete (XP threshold reached)
    /// </summary>
    public bool IsWaveComplete()
    {
        return xpCollectedThisWave >= xpToComplete;
    }
    
    /// <summary>
    /// Get XP progress as percentage (0-1)
    /// </summary>
    public float GetXPProgress()
    {
        if (xpToComplete <= 0) return 1f;
        return Mathf.Clamp01((float)xpCollectedThisWave / xpToComplete);
    }
    
    /// <summary>
    /// Reset all enemy configs for a new wave attempt
    /// </summary>
    public void ResetConfigs()
    {
        xpCollectedThisWave = 0;
        foreach (var config in enemyConfigs)
        {
            config.Reset();
        }
    }
    
    /// <summary>
    /// Add XP collected during this wave
    /// </summary>
    public void AddXP(int amount)
    {
        xpCollectedThisWave += amount;
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(WaveData))]
public class WaveDataEditor : Editor
{
    private bool showEnemySummary = true;
    
    public override void OnInspectorGUI()
    {
        WaveData waveData = (WaveData)target;
        
        // Draw default inspector
        DrawDefaultInspector();
        
        // Add spacing
        EditorGUILayout.Space(10);
        
        // Enemy Config Summary Section
        showEnemySummary = EditorGUILayout.Foldout(showEnemySummary, "Enemy Configuration Summary", true);
        
        if (showEnemySummary)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (waveData.enemyConfigs != null && waveData.enemyConfigs.Count > 0)
            {
                int totalMaxOnScreen = 0;
                
                foreach (var config in waveData.enemyConfigs)
                {
                    if (config.enemyPrefab != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        
                        // Get prefab preview texture
                        Texture2D preview = AssetPreview.GetAssetPreview(config.enemyPrefab);
                        if (preview != null)
                        {
                            GUILayout.Label(preview, GUILayout.Width(50), GUILayout.Height(50));
                        }
                        else
                        {
                            GUILayout.Label("No Preview", GUILayout.Width(50), GUILayout.Height(50));
                        }
                        
                        // Display config info
                        EditorGUILayout.BeginVertical();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField(config.enemyPrefab.name, EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"Max On Screen: {config.maxOnScreen}");
                        if (config.allowedSpawnZones.Count > 0)
                        {
                            EditorGUILayout.LabelField($"Zones: {string.Join(", ", config.allowedSpawnZones)}");
                        }
                        else
                        {
                            EditorGUILayout.LabelField("Zones: All", EditorStyles.miniLabel);
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndVertical();
                        
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.Space(5);
                        
                        totalMaxOnScreen += config.maxOnScreen;
                    }
                }
                
                // Totals and XP info
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Wave Summary", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"XP Required to Complete: {waveData.xpToComplete}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Max Enemies On Screen: {totalMaxOnScreen}");
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox($"Enemies will continuously spawn until the player collects {waveData.xpToComplete} XP from killing them.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField("No enemy configurations set up", EditorStyles.miniLabel);
                EditorGUILayout.HelpBox("Add enemy configurations above to define which enemies spawn in this wave.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
    }
}
#endif