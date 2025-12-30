using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Configuration for an enemy type that can be unlocked at a specific wave
/// </summary>
[System.Serializable]
public class EnemyUnlockConfig
{
    [Header("Enemy Settings")]
    [Tooltip("The enemy prefab to spawn")]
    public GameObject enemyPrefab;
    
    [Tooltip("The wave number when this enemy becomes available (0 = from the start)")]
    [Min(0)] public int unlockAtWave = 0;
    
    [Header("Spawn Settings")]
    [Tooltip("Base maximum number of this enemy on screen at wave 0")]
    [Min(1)] public int baseMaxOnScreen = 1;
    
    [Tooltip("Spawn cooldown in seconds")]
    [Min(0.1f)] public float spawnCooldown = 1f;
    
    [Tooltip("Allowed spawn zones (empty = all zones)")]
    public List<int> allowedSpawnZones = new List<int>();
    
    [Header("Scaling")]
    [Tooltip("How much the max on screen increases per wave (added after exponential scaling)")]
    [Min(0)] public float maxOnScreenScalePerWave = 0.2f;
    
    [Header("Health Scaling")]
    [Tooltip("Base health multiplier for this enemy type")]
    [Min(0.1f)] public float baseHealthMultiplier = 1f;
    
    [Tooltip("How much the health multiplier increases per wave")]
    [Min(0)] public float healthScalePerWave = 0.1f;
}

/// <summary>
/// ScriptableObject that configures the infinite wave system with exponential difficulty scaling
/// </summary>
[CreateAssetMenu(fileName = "InfiniteWaveConfig", menuName = "Wave System/Infinite Wave Config")]
public class InfiniteWaveConfig : ScriptableObject
{
    [Header("Wave Naming")]
    [Tooltip("Prefix for wave names (e.g., 'Wave' results in 'Wave 1', 'Wave 2', etc.)")]
    public string waveNamePrefix = "Wave";
    
    [Header("Difficulty Scaling")]
    [Tooltip("Base difficulty multiplier at wave 0")]
    [Min(0.1f)] public float baseDifficulty = 1f;
    
    [Tooltip("Exponential growth rate for difficulty (higher = faster scaling)")]
    [Range(0.01f, 0.5f)] public float difficultyGrowthRate = 0.1f;
    
    [Tooltip("Maximum difficulty multiplier cap")]
    [Min(1f)] public float maxDifficultyMultiplier = 10f;
    
    [Header("Health Scaling")]
    [Tooltip("Base health multiplier for all enemies at wave 0")]
    [Min(0.1f)] public float baseHealthMultiplier = 1f;
    
    [Tooltip("Wave number where health scaling starts (before this, health stays at base)")]
    public int healthScalingStartWave = 15;
    
    [Tooltip("How much the global health multiplier increases per wave (after scaling starts)")]
    [Min(0)] public float globalHealthScalePerWave = 0.1f;
    
    [Tooltip("Maximum health multiplier cap")]
    [Min(1f)] public float maxHealthMultiplier = 5f;
    
    [Header("Wave 20+ Difficulty Spike")]
    [Tooltip("Wave number where difficulty spikes")]
    public int difficultySpikeWave = 20;
    
    [Tooltip("Additional difficulty multiplier applied after spike wave")]
    [Min(1f)] public float difficultySpikeMultiplier = 1.5f;
    
    [Tooltip("Additional health multiplier applied after spike wave")]
    [Min(1f)] public float healthSpikeMultiplier = 1.3f;
    
    [Header("Enemy Configuration")]
    [Tooltip("List of all enemies that can spawn, with their unlock waves")]
    public List<EnemyUnlockConfig> enemyConfigs = new List<EnemyUnlockConfig>();
    
    [Header("Global Spawn Settings")]
    [Tooltip("Minimum spawn cooldown (prevents spawning too fast at high waves)")]
    [Min(0.1f)] public float minSpawnCooldown = 0.2f;
    
    [Tooltip("Cooldown reduction per wave (multiplied by difficulty)")]
    [Range(0f, 0.1f)] public float cooldownReductionPerWave = 0.02f;
    
    /// <summary>
    /// Calculates the difficulty multiplier for a given wave using exponential scaling
    /// Formula: baseDifficulty * e^(growthRate * waveNumber)
    /// </summary>
    public float GetDifficultyMultiplier(int waveNumber)
    {
        float multiplier = baseDifficulty * Mathf.Exp(difficultyGrowthRate * waveNumber);
        
        // Apply difficulty spike after wave 20
        if (waveNumber >= difficultySpikeWave)
        {
            multiplier *= difficultySpikeMultiplier;
        }
        
        return Mathf.Min(multiplier, maxDifficultyMultiplier);
    }
    
    /// <summary>
    /// Calculates the health multiplier for enemies at a given wave
    /// Health stays at base until healthScalingStartWave, then scales by globalHealthScalePerWave
    /// </summary>
    public float GetHealthMultiplier(int waveNumber)
    {
        float multiplier = baseHealthMultiplier;
        
        // Only start scaling health after the scaling start wave
        if (waveNumber >= healthScalingStartWave)
        {
            int wavesSinceScalingStart = waveNumber - healthScalingStartWave;
            multiplier += wavesSinceScalingStart * globalHealthScalePerWave;
        }
        
        // Apply health spike after wave 20
        if (waveNumber >= difficultySpikeWave)
        {
            multiplier *= healthSpikeMultiplier;
        }
        
        return Mathf.Min(multiplier, maxHealthMultiplier);
    }
    
    /// <summary>
    /// Calculates the health multiplier for a specific enemy type at a given wave
    /// </summary>
    public float GetEnemyHealthMultiplier(EnemyUnlockConfig config, int waveNumber)
    {
        float globalMult = GetHealthMultiplier(waveNumber);
        int wavesSinceUnlock = Mathf.Max(0, waveNumber - config.unlockAtWave);
        float enemyMult = config.baseHealthMultiplier + (wavesSinceUnlock * config.healthScalePerWave);
        
        return globalMult * enemyMult;
    }
    
    /// <summary>
    /// Gets all enemies that are unlocked at or before the specified wave
    /// </summary>
    public List<EnemyUnlockConfig> GetUnlockedEnemies(int waveNumber)
    {
        List<EnemyUnlockConfig> unlocked = new List<EnemyUnlockConfig>();
        
        foreach (var config in enemyConfigs)
        {
            if (config.enemyPrefab != null && config.unlockAtWave <= waveNumber)
            {
                unlocked.Add(config);
            }
        }
        
        return unlocked;
    }
    
    /// <summary>
    /// Calculates the max on screen for an enemy at a specific wave
    /// </summary>
    public int GetMaxOnScreen(EnemyUnlockConfig config, int waveNumber)
    {
        float difficultyMult = GetDifficultyMultiplier(waveNumber);
        int wavesSinceUnlock = Mathf.Max(0, waveNumber - config.unlockAtWave);
        
        // Base + (waves since unlock * scale per wave * difficulty multiplier)
        float scaledMax = config.baseMaxOnScreen + (wavesSinceUnlock * config.maxOnScreenScalePerWave * difficultyMult);
        
        return Mathf.Max(1, Mathf.RoundToInt(scaledMax));
    }
    
    /// <summary>
    /// Calculates the spawn cooldown for an enemy at a specific wave
    /// </summary>
    public float GetSpawnCooldown(EnemyUnlockConfig config, int waveNumber)
    {
        float reduction = waveNumber * cooldownReductionPerWave;
        float cooldown = config.spawnCooldown - reduction;
        
        return Mathf.Max(minSpawnCooldown, cooldown);
    }
    
    /// <summary>
    /// Generates a WaveData-compatible configuration for the specified wave
    /// </summary>
    public List<EnemySpawnConfig> GenerateWaveConfig(int waveNumber)
    {
        List<EnemySpawnConfig> configs = new List<EnemySpawnConfig>();
        List<EnemyUnlockConfig> unlockedEnemies = GetUnlockedEnemies(waveNumber);
        
        foreach (var unlockConfig in unlockedEnemies)
        {
            EnemySpawnConfig spawnConfig = new EnemySpawnConfig
            {
                enemyPrefab = unlockConfig.enemyPrefab,
                maxOnScreen = GetMaxOnScreen(unlockConfig, waveNumber),
                spawnCooldown = GetSpawnCooldown(unlockConfig, waveNumber),
                allowedSpawnZones = new List<int>(unlockConfig.allowedSpawnZones)
            };
            
            configs.Add(spawnConfig);
        }
        
        return configs;
    }
    
    /// <summary>
    /// Gets the wave name for a specific wave number
    /// </summary>
    public string GetWaveName(int waveNumber)
    {
        return $"{waveNamePrefix} {waveNumber + 1}";
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(InfiniteWaveConfig))]
public class InfiniteWaveConfigEditor : UnityEditor.Editor
{
    private int previewWaveNumber = 0;
    private bool showPreview = true;
    
    public override void OnInspectorGUI()
    {
        InfiniteWaveConfig config = (InfiniteWaveConfig)target;
        
        DrawDefaultInspector();
        
        UnityEditor.EditorGUILayout.Space(15);
        UnityEditor.EditorGUILayout.LabelField("Wave Preview", UnityEditor.EditorStyles.boldLabel);
        
        showPreview = UnityEditor.EditorGUILayout.Foldout(showPreview, "Preview Wave Configuration", true);
        
        if (showPreview)
        {
            UnityEditor.EditorGUILayout.BeginVertical(UnityEditor.EditorStyles.helpBox);
            
            previewWaveNumber = UnityEditor.EditorGUILayout.IntSlider("Preview Wave", previewWaveNumber, 0, 50);
            
            UnityEditor.EditorGUILayout.Space(5);
            UnityEditor.EditorGUILayout.LabelField($"Wave Name: {config.GetWaveName(previewWaveNumber)}");
            UnityEditor.EditorGUILayout.LabelField($"Difficulty Multiplier: {config.GetDifficultyMultiplier(previewWaveNumber):F2}x");
            
            UnityEditor.EditorGUILayout.Space(10);
            UnityEditor.EditorGUILayout.LabelField("Enemies at this wave:", UnityEditor.EditorStyles.boldLabel);
            
            var unlockedEnemies = config.GetUnlockedEnemies(previewWaveNumber);
            
            if (unlockedEnemies.Count == 0)
            {
                UnityEditor.EditorGUILayout.LabelField("No enemies unlocked yet", UnityEditor.EditorStyles.miniLabel);
            }
            else
            {
                foreach (var enemy in unlockedEnemies)
                {
                    UnityEditor.EditorGUILayout.BeginHorizontal();
                    
                    Texture2D preview = UnityEditor.AssetPreview.GetAssetPreview(enemy.enemyPrefab);
                    if (preview)
                    {
                        GUILayout.Label(preview, GUILayout.Width(40), GUILayout.Height(40));
                    }
                    
                    UnityEditor.EditorGUILayout.BeginVertical();
                    UnityEditor.EditorGUILayout.LabelField(enemy.enemyPrefab.name, UnityEditor.EditorStyles.boldLabel);
                    UnityEditor.EditorGUILayout.LabelField($"Max On Screen: {config.GetMaxOnScreen(enemy, previewWaveNumber)}");
                    UnityEditor.EditorGUILayout.LabelField($"Spawn Cooldown: {config.GetSpawnCooldown(enemy, previewWaveNumber):F2}s");
                    
                    if (enemy.unlockAtWave > 0)
                    {
                        UnityEditor.EditorGUILayout.LabelField($"(Unlocked at wave {enemy.unlockAtWave + 1})", UnityEditor.EditorStyles.miniLabel);
                    }
                    
                    UnityEditor.EditorGUILayout.EndVertical();
                    UnityEditor.EditorGUILayout.EndHorizontal();
                    UnityEditor.EditorGUILayout.Space(5);
                }
            }
            
            UnityEditor.EditorGUILayout.Space(5);
            
            // Show difficulty curve preview
            UnityEditor.EditorGUILayout.LabelField("Difficulty Curve (first 25 waves):", UnityEditor.EditorStyles.boldLabel);
            
            UnityEditor.EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < 25; i += 5)
            {
                UnityEditor.EditorGUILayout.LabelField($"W{i + 1}: {config.GetDifficultyMultiplier(i):F1}x", GUILayout.Width(70));
            }
            UnityEditor.EditorGUILayout.EndHorizontal();
            
            UnityEditor.EditorGUILayout.Space(5);
            UnityEditor.EditorGUILayout.LabelField("Health Curve (first 25 waves):", UnityEditor.EditorStyles.boldLabel);
            
            UnityEditor.EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < 25; i += 5)
            {
                UnityEditor.EditorGUILayout.LabelField($"W{i + 1}: {config.GetHealthMultiplier(i):F1}x", GUILayout.Width(70));
            }
            UnityEditor.EditorGUILayout.EndHorizontal();
            
            if (previewWaveNumber >= config.difficultySpikeWave)
            {
                UnityEditor.EditorGUILayout.Space(5);
                UnityEditor.EditorGUILayout.HelpBox($"Wave {previewWaveNumber + 1} is past the difficulty spike (Wave {config.difficultySpikeWave + 1})! Difficulty x{config.difficultySpikeMultiplier}, Health x{config.healthSpikeMultiplier}", UnityEditor.MessageType.Warning);
            }
            
            UnityEditor.EditorGUILayout.EndVertical();
        }
    }
}
#endif