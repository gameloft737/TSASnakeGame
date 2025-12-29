#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CreateDefaultInfiniteWaveConfig : MonoBehaviour
{
    [MenuItem("Tools/Wave System/Create Default Infinite Wave Config")]
    public static void CreateConfig()
    {
        // Create the ScriptableObject
        InfiniteWaveConfig config = ScriptableObject.CreateInstance<InfiniteWaveConfig>();
        
        // Set wave naming
        config.waveNamePrefix = "Wave";
        
        // Set difficulty scaling for gradual increase, hard by wave 40
        // Using e^(0.05 * wave) means:
        // Wave 0: 1.0x, Wave 10: 1.65x, Wave 20: 2.72x, Wave 30: 4.48x, Wave 40: 7.39x
        config.baseDifficulty = 1f;
        config.difficultyGrowthRate = 0.05f;
        config.maxDifficultyMultiplier = 15f;
        
        // Global spawn settings
        config.minSpawnCooldown = 0.3f;
        config.cooldownReductionPerWave = 0.015f;
        
        // Load enemy prefabs
        GameObject normalRed = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AppleEnemy/NormalRed.prefab");
        GameObject fatRed = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AppleEnemy/FatRed.prefab");
        GameObject tallRed = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AppleEnemy/TallRed.prefab");
        
        GameObject normalGreen = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AppleEnemy/NormalGreen.prefab");
        GameObject fatGreen = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AppleEnemy/FatGreen.prefab");
        GameObject tallGreen = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AppleEnemy/TallGreen.prefab");
        
        GameObject metal = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AppleEnemy/Metal.prefab");
        
        config.enemyConfigs = new List<EnemyUnlockConfig>();
        
        // === RED APPLE ENEMIES (Easy - Available from start) ===
        
        // Normal Red - Available from wave 0 (easiest, most common)
        config.enemyConfigs.Add(new EnemyUnlockConfig
        {
            enemyPrefab = normalRed,
            unlockAtWave = 0,
            baseMaxOnScreen = 3,
            spawnCooldown = 2.0f,
            maxOnScreenScalePerWave = 0.15f,
            allowedSpawnZones = new List<int>()
        });
        
        // Fat Red - Available from wave 3
        config.enemyConfigs.Add(new EnemyUnlockConfig
        {
            enemyPrefab = fatRed,
            unlockAtWave = 3,
            baseMaxOnScreen = 2,
            spawnCooldown = 2.5f,
            maxOnScreenScalePerWave = 0.12f,
            allowedSpawnZones = new List<int>()
        });
        
        // Tall Red - Available from wave 6
        config.enemyConfigs.Add(new EnemyUnlockConfig
        {
            enemyPrefab = tallRed,
            unlockAtWave = 6,
            baseMaxOnScreen = 2,
            spawnCooldown = 2.5f,
            maxOnScreenScalePerWave = 0.12f,
            allowedSpawnZones = new List<int>()
        });
        
        // === GREEN APPLE ENEMIES (Medium - Unlocked mid-game) ===
        
        // Normal Green - Available from wave 10
        config.enemyConfigs.Add(new EnemyUnlockConfig
        {
            enemyPrefab = normalGreen,
            unlockAtWave = 10,
            baseMaxOnScreen = 2,
            spawnCooldown = 2.0f,
            maxOnScreenScalePerWave = 0.15f,
            allowedSpawnZones = new List<int>()
        });
        
        // Fat Green - Available from wave 15
        config.enemyConfigs.Add(new EnemyUnlockConfig
        {
            enemyPrefab = fatGreen,
            unlockAtWave = 15,
            baseMaxOnScreen = 2,
            spawnCooldown = 2.5f,
            maxOnScreenScalePerWave = 0.1f,
            allowedSpawnZones = new List<int>()
        });
        
        // Tall Green - Available from wave 20
        config.enemyConfigs.Add(new EnemyUnlockConfig
        {
            enemyPrefab = tallGreen,
            unlockAtWave = 20,
            baseMaxOnScreen = 2,
            spawnCooldown = 2.5f,
            maxOnScreenScalePerWave = 0.1f,
            allowedSpawnZones = new List<int>()
        });
        
        // === METAL APPLE ENEMY (Hard - Late game boss-type) ===
        
        // Metal - Available from wave 25 (tough enemy, spawns rarely)
        config.enemyConfigs.Add(new EnemyUnlockConfig
        {
            enemyPrefab = metal,
            unlockAtWave = 25,
            baseMaxOnScreen = 1,
            spawnCooldown = 5.0f,
            maxOnScreenScalePerWave = 0.08f,
            allowedSpawnZones = new List<int>()
        });
        
        // Create the asset
        string path = "Assets/InfiniteWaveConfig.asset";
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // Select the created asset
        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
        
        Debug.Log($"Created InfiniteWaveConfig at {path}");
        Debug.Log("Wave Difficulty Progression:");
        Debug.Log($"  Wave 1: {config.GetDifficultyMultiplier(0):F2}x");
        Debug.Log($"  Wave 10: {config.GetDifficultyMultiplier(9):F2}x");
        Debug.Log($"  Wave 20: {config.GetDifficultyMultiplier(19):F2}x");
        Debug.Log($"  Wave 30: {config.GetDifficultyMultiplier(29):F2}x");
        Debug.Log($"  Wave 40: {config.GetDifficultyMultiplier(39):F2}x");
    }
}
#endif