using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;

[System.Serializable]
public class SpriteCount
{
    public Sprite sprite;
    public int count;
}

[CreateAssetMenu(fileName = "WaveData", menuName = "Wave System/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Wave Info")]
    public string waveName;
    
    [Header("Spawn Configuration")]
    public List<SpawnGroup> spawnGroups = new List<SpawnGroup>();
    
    [Header("Sprite Configuration")]
    public List<SpriteCount> spriteCounts = new List<SpriteCount>();
    
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

#if UNITY_EDITOR

[CustomEditor(typeof(WaveData))]
public class WaveDataEditor : Editor
{
    private bool showPrefabSummary = true;
    
    public override void OnInspectorGUI()
    {
        WaveData waveData = (WaveData)target;
        
        // Draw default inspector
        DrawDefaultInspector();
        
        // Add spacing
        EditorGUILayout.Space(10);
        
        // Prefab Summary Section
        showPrefabSummary = EditorGUILayout.Foldout(showPrefabSummary, "Prefab Summary", true);
        
        if (showPrefabSummary)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Calculate prefab counts
            Dictionary<GameObject, int> prefabCounts = new Dictionary<GameObject, int>();
            
            foreach (var group in waveData.spawnGroups)
            {
                if (group.enemyPrefab != null)
                {
                    if (prefabCounts.ContainsKey(group.enemyPrefab))
                    {
                        prefabCounts[group.enemyPrefab] += group.count;
                    }
                    else
                    {
                        prefabCounts[group.enemyPrefab] = group.count;
                    }
                }
            }
            
            // Display each prefab with icon and count
            if (prefabCounts.Count > 0)
            {
                foreach (var kvp in prefabCounts.OrderByDescending(x => x.Value))
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    // Get prefab preview texture
                    Texture2D preview = AssetPreview.GetAssetPreview(kvp.Key);
                    if (preview != null)
                    {
                        GUILayout.Label(preview, GUILayout.Width(50), GUILayout.Height(50));
                    }
                    else
                    {
                        GUILayout.Label("No Preview", GUILayout.Width(50), GUILayout.Height(50));
                    }
                    
                    // Display prefab name and count
                    EditorGUILayout.BeginVertical();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(kvp.Key.name, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Count: {kvp.Value}");
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(5);
                }
                
                // Total count
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Total Enemies: {waveData.GetTotalEnemies()}", EditorStyles.boldLabel);
            }
            else
            {
                EditorGUILayout.LabelField("No spawn groups configured", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
    }
}
#endif