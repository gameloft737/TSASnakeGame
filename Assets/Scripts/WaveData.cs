using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class EnemySpawnConfig
{
    [Header("Enemy Type")]
    public GameObject enemyPrefab;
    
    [Header("Spawn Limits")]
    [Min(1)] public int maxOnScreen = 3;
    
    [Header("Spawn Settings")]
    public List<int> allowedSpawnZones = new List<int>();
    public float spawnCooldown = 0.5f;
    
    [System.NonSerialized] public int currentOnScreen = 0;
    [System.NonSerialized] public float lastSpawnTime = 0f;
    
    public bool CanSpawn()
    {
        return currentOnScreen < maxOnScreen && Time.time >= lastSpawnTime + spawnCooldown;
    }
    
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
    
    [Header("Enemy Configuration")]
    public List<EnemySpawnConfig> enemyConfigs = new List<EnemySpawnConfig>();
    
    public void ResetConfigs()
    {
        foreach (var config in enemyConfigs)
        {
            config.Reset();
        }
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
        
        DrawDefaultInspector();
        EditorGUILayout.Space(10);
        
        showEnemySummary = EditorGUILayout.Foldout(showEnemySummary, "Enemy Configuration Summary", true);
        
        if (showEnemySummary)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (waveData.enemyConfigs != null && waveData.enemyConfigs.Count > 0)
            {
                int totalMaxOnScreen = 0;
                
                foreach (var config in waveData.enemyConfigs)
                {
                    if (config.enemyPrefab)
                    {
                        EditorGUILayout.BeginHorizontal();
                        
                        Texture2D preview = AssetPreview.GetAssetPreview(config.enemyPrefab);
                        if (preview)
                        {
                            GUILayout.Label(preview, GUILayout.Width(50), GUILayout.Height(50));
                        }
                        else
                        {
                            GUILayout.Label("No Preview", GUILayout.Width(50), GUILayout.Height(50));
                        }
                        
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
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Wave Summary", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Max Enemies On Screen: {totalMaxOnScreen}");
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("Enemies spawn continuously until player levels up.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField("No enemy configurations", EditorStyles.miniLabel);
                EditorGUILayout.HelpBox("Add enemy configurations to define which enemies spawn in this wave.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
    }
}
#endif