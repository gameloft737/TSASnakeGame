using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CheckpointData
{
    public int levelMilestone;
    public int waveIndex;
    public int xp;
    public int xpLevel;
    public int xpToNextLevel;
    public string attackPrefabName;
    public int attackLevel;
    public List<AbilityCheckpointData> abilities = new List<AbilityCheckpointData>();
    public float damageMultiplierBonus;
    public float rangeMultiplierBonus;
    public float maxHealthBonus;
    public float maxHealthPercentBonus;
    public float healthRegenPerSecond;
    public float speedMultiplierBonus;
    public float cooldownReductionBonus;
    public float critChanceBonus;
    public float critMultiplierBonus;
    public float lifestealBonus;
    public float damageReductionBonus;
    public float xpMultiplierBonus;
    
    // Player position and rotation
    public Vector3 playerPosition;
    public Quaternion playerRotation;
    public Quaternion orientationRotation;
    
    // Body segment count
    public int bodySegmentCount;
    
    // Growth tracking
    public int totalApplesEaten;
    public int applesForCurrentSegment;
    public int currentApplesThreshold;
    public int segmentsAddedFromApples;
}

[System.Serializable]
public class AbilityCheckpointData
{
    public string abilityPrefabName;
    public int level;
    public string abilitySoName;
}

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private CheckpointData currentCheckpoint;
    private XPManager xpManager;
    private AttackManager attackManager;
    private AbilityManager abilityManager;
    private PlayerStats playerStats;
    private WaveManager waveManager;
    private LevelUIManager levelUIManager;
    private SnakeHealth snakeHealth;
    private EnemySpawner enemySpawner;
    private CameraManager cameraManager;
    private PlayerMovement playerMovement;
    private SnakeBody snakeBody;
    private XPUI xpUI;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
    }
    
    private void Start()
    {
        FindReferences();
        SaveCheckpoint(0);
    }
    
    private void FindReferences()
    {
        if (xpManager == null) xpManager = FindFirstObjectByType<XPManager>();
        if (attackManager == null) attackManager = FindFirstObjectByType<AttackManager>();
        if (abilityManager == null) abilityManager = FindFirstObjectByType<AbilityManager>();
        if (playerStats == null) playerStats = PlayerStats.Instance;
        if (waveManager == null) waveManager = FindFirstObjectByType<WaveManager>();
        if (levelUIManager == null) levelUIManager = LevelUIManager.Instance;
        if (snakeHealth == null) snakeHealth = FindFirstObjectByType<SnakeHealth>();
        if (enemySpawner == null) enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (cameraManager == null) cameraManager = FindFirstObjectByType<CameraManager>();
        if (playerMovement == null) playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (snakeBody == null) snakeBody = FindFirstObjectByType<SnakeBody>();
        if (xpUI == null) xpUI = FindFirstObjectByType<XPUI>();
    }
    
    public void SaveCheckpoint(int levelMilestone)
    {
        FindReferences();
        
        currentCheckpoint = new CheckpointData
        {
            levelMilestone = levelMilestone,
            waveIndex = waveManager != null ? waveManager.currentWaveIndex : 0
        };
        
        if (xpManager != null)
        {
            currentCheckpoint.xp = xpManager.GetCurrentXP();
            currentCheckpoint.xpLevel = xpManager.GetCurrentLevel();
            currentCheckpoint.xpToNextLevel = xpManager.GetXPToNextLevel();
        }
        
        if (attackManager != null)
        {
            Attack currentAttack = attackManager.GetCurrentAttack();
            if (currentAttack != null)
            {
                currentCheckpoint.attackPrefabName = currentAttack.gameObject.name;
                currentCheckpoint.attackLevel = currentAttack.GetCurrentLevel();
            }
        }
        
        if (abilityManager != null)
        {
            currentCheckpoint.abilities.Clear();
            List<BaseAbility> abilities = abilityManager.GetActiveAbilities();
            foreach (var ability in abilities)
            {
                if (ability != null)
                {
                    AbilityCheckpointData abilityData = new AbilityCheckpointData
                    {
                        abilityPrefabName = ability.gameObject.name,
                        level = ability.GetCurrentLevel()
                    };
                    AbilitySO abilitySO = abilityManager.GetAbilitySO(ability);
                    if (abilitySO != null) abilityData.abilitySoName = abilitySO.name;
                    currentCheckpoint.abilities.Add(abilityData);
                }
            }
        }
        
        if (playerStats != null) SavePlayerStatsBonuses();
        
        // Save player position and rotation
        if (playerMovement != null)
        {
            currentCheckpoint.playerPosition = playerMovement.transform.position;
            currentCheckpoint.playerRotation = playerMovement.transform.rotation;
            
            // Also save orientation rotation if it exists
            Transform orientation = GetOrientationTransform(playerMovement);
            if (orientation != null)
            {
                currentCheckpoint.orientationRotation = orientation.rotation;
            }
            
            // Always log checkpoint position for debugging
            Debug.Log($"[CheckpointManager] SAVED checkpoint position: {currentCheckpoint.playerPosition} at level {levelMilestone}");
        }
        
        // Save body segment count and growth tracking
        if (snakeBody != null)
        {
            currentCheckpoint.bodySegmentCount = snakeBody.bodyParts.Count;
            
            // Save growth tracking using reflection
            var type = typeof(SnakeBody);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            currentCheckpoint.totalApplesEaten = GetField<int>(type, snakeBody, "totalApplesEaten", flags);
            currentCheckpoint.applesForCurrentSegment = GetField<int>(type, snakeBody, "applesForCurrentSegment", flags);
            currentCheckpoint.currentApplesThreshold = GetField<int>(type, snakeBody, "currentApplesThreshold", flags);
            currentCheckpoint.segmentsAddedFromApples = GetField<int>(type, snakeBody, "segmentsAddedFromApples", flags);
        }
        
        if (debugMode)
            Debug.Log($"[CheckpointManager] Saved checkpoint at level {levelMilestone}. Wave: {currentCheckpoint.waveIndex}, XP Level: {currentCheckpoint.xpLevel}, Attack: {currentCheckpoint.attackPrefabName} (Lv{currentCheckpoint.attackLevel}), Abilities: {currentCheckpoint.abilities.Count}, Body Segments: {currentCheckpoint.bodySegmentCount}, Position: {currentCheckpoint.playerPosition}");
    }
    
    private void SavePlayerStatsBonuses()
    {
        var type = typeof(PlayerStats);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        
        currentCheckpoint.damageMultiplierBonus = GetField<float>(type, playerStats, "damageMultiplierBonus", flags);
        currentCheckpoint.rangeMultiplierBonus = GetField<float>(type, playerStats, "rangeMultiplierBonus", flags);
        currentCheckpoint.maxHealthBonus = GetField<float>(type, playerStats, "maxHealthBonus", flags);
        currentCheckpoint.maxHealthPercentBonus = GetField<float>(type, playerStats, "maxHealthPercentBonus", flags);
        currentCheckpoint.healthRegenPerSecond = GetField<float>(type, playerStats, "healthRegenPerSecond", flags);
        currentCheckpoint.speedMultiplierBonus = GetField<float>(type, playerStats, "speedMultiplierBonus", flags);
        currentCheckpoint.cooldownReductionBonus = GetField<float>(type, playerStats, "cooldownReductionBonus", flags);
        currentCheckpoint.critChanceBonus = GetField<float>(type, playerStats, "critChanceBonus", flags);
        currentCheckpoint.critMultiplierBonus = GetField<float>(type, playerStats, "critMultiplierBonus", flags);
        currentCheckpoint.lifestealBonus = GetField<float>(type, playerStats, "lifestealBonus", flags);
        currentCheckpoint.damageReductionBonus = GetField<float>(type, playerStats, "damageReductionBonus", flags);
        currentCheckpoint.xpMultiplierBonus = GetField<float>(type, playerStats, "xpMultiplierBonus", flags);
    }
    
    private T GetField<T>(System.Type type, object obj, string name, System.Reflection.BindingFlags flags)
    {
        var field = type.GetField(name, flags);
        return field != null ? (T)field.GetValue(obj) : default(T);
    }
    
    private void SetField<T>(System.Type type, object obj, string name, T value, System.Reflection.BindingFlags flags)
    {
        var field = type.GetField(name, flags);
        if (field != null) field.SetValue(obj, value);
    }
    
    public bool RestoreCheckpoint()
    {
        if (currentCheckpoint == null) { Debug.LogWarning("[CheckpointManager] No checkpoint!"); return false; }
        
        FindReferences();
        if (debugMode) Debug.Log($"[CheckpointManager] Restoring checkpoint at level {currentCheckpoint.levelMilestone}");
        
        if (enemySpawner != null) enemySpawner.ClearAllEnemies();
        if (xpManager != null) xpManager.SetXP(currentCheckpoint.xp, currentCheckpoint.xpLevel, currentCheckpoint.xpToNextLevel);
        if (waveManager != null) waveManager.currentWaveIndex = currentCheckpoint.waveIndex;
        
        RestorePlayerStatsBonuses();
        
        if (attackManager != null)
        {
            Attack currentAttack = attackManager.GetCurrentAttack();
            if (currentAttack != null && currentCheckpoint.attackLevel > 0)
                currentAttack.SetLevel(currentCheckpoint.attackLevel);
        }
        
        RestoreAbilities();
        
        // Restore player position and rotation with proper Rigidbody handling
        TeleportPlayer();
        
        // Restore body segments (remove extras)
        RestoreBodySegments();
        
        if (snakeHealth != null) snakeHealth.ResetHealth();
        
        // Force update XPUI immediately
        if (xpUI != null)
        {
            xpUI.UpdateXP(currentCheckpoint.xp, currentCheckpoint.xpToNextLevel);
            xpUI.UpdateLevel(currentCheckpoint.xpLevel);
        }
        
        if (levelUIManager != null)
        {
            var type = typeof(LevelUIManager);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            SetField(type, levelUIManager, "lastAnnouncedLevel", currentCheckpoint.levelMilestone, flags);
        }
        
        if (cameraManager != null) cameraManager.SwitchToNormalCamera();
        
        // Re-enable player movement (teleport already handled Rigidbody)
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
        
        if (attackManager != null) attackManager.SetFrozen(false);
        if (waveManager != null) waveManager.StartCurrentWave();
        
        if (debugMode) Debug.Log("[CheckpointManager] Checkpoint restored!");
        return true;
    }
    
    private void RestorePlayerStatsBonuses()
    {
        if (playerStats == null) return;
        playerStats.ResetAllBonuses();
        
        var type = typeof(PlayerStats);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        
        SetField(type, playerStats, "damageMultiplierBonus", currentCheckpoint.damageMultiplierBonus, flags);
        SetField(type, playerStats, "rangeMultiplierBonus", currentCheckpoint.rangeMultiplierBonus, flags);
        SetField(type, playerStats, "maxHealthBonus", currentCheckpoint.maxHealthBonus, flags);
        SetField(type, playerStats, "maxHealthPercentBonus", currentCheckpoint.maxHealthPercentBonus, flags);
        SetField(type, playerStats, "healthRegenPerSecond", currentCheckpoint.healthRegenPerSecond, flags);
        SetField(type, playerStats, "speedMultiplierBonus", currentCheckpoint.speedMultiplierBonus, flags);
        SetField(type, playerStats, "cooldownReductionBonus", currentCheckpoint.cooldownReductionBonus, flags);
        SetField(type, playerStats, "critChanceBonus", currentCheckpoint.critChanceBonus, flags);
        SetField(type, playerStats, "critMultiplierBonus", currentCheckpoint.critMultiplierBonus, flags);
        SetField(type, playerStats, "lifestealBonus", currentCheckpoint.lifestealBonus, flags);
        SetField(type, playerStats, "damageReductionBonus", currentCheckpoint.damageReductionBonus, flags);
        SetField(type, playerStats, "xpMultiplierBonus", currentCheckpoint.xpMultiplierBonus, flags);
        
        playerStats.onStatsChanged?.Invoke();
    }
    
    private void RestoreAbilities()
    {
        if (abilityManager == null) return;
        
        List<BaseAbility> currentAbilities = new List<BaseAbility>(abilityManager.GetActiveAbilities());
        Dictionary<string, AbilityCheckpointData> checkpointMap = new Dictionary<string, AbilityCheckpointData>();
        
        foreach (var data in currentCheckpoint.abilities)
        {
            string key = data.abilityPrefabName.Replace("(Clone)", "").Trim();
            checkpointMap[key] = data;
        }
        
        foreach (var ability in currentAbilities)
        {
            if (ability == null) continue;
            string name = ability.gameObject.name.Replace("(Clone)", "").Trim();
            
            if (checkpointMap.TryGetValue(name, out AbilityCheckpointData data))
            {
                ability.SetLevel(data.level);
                if (debugMode) Debug.Log($"[CheckpointManager] Restored ability {name} to level {data.level}");
            }
            else
            {
                if (debugMode) Debug.Log($"[CheckpointManager] Removing ability {name} (not in checkpoint)");
                abilityManager.RemoveAbility(ability);
            }
        }
    }
    
    private void RestoreBodySegments()
    {
        if (snakeBody == null) return;
        
        int currentCount = snakeBody.bodyParts.Count;
        int targetCount = currentCheckpoint.bodySegmentCount;
        
        if (currentCount > targetCount)
        {
            // Remove extra segments from the end
            int segmentsToRemove = currentCount - targetCount;
            if (debugMode) Debug.Log($"[CheckpointManager] Removing {segmentsToRemove} body segments");
            
            for (int i = 0; i < segmentsToRemove; i++)
            {
                if (snakeBody.bodyParts.Count > 0)
                {
                    int lastIndex = snakeBody.bodyParts.Count - 1;
                    BodyPart lastPart = snakeBody.bodyParts[lastIndex];
                    snakeBody.bodyParts.RemoveAt(lastIndex);
                    
                    if (lastPart != null && lastPart.gameObject != null)
                    {
                        Destroy(lastPart.gameObject);
                    }
                }
            }
            
            // Update bodyLength field using reflection
            var type = typeof(SnakeBody);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            SetField(type, snakeBody, "bodyLength", targetCount, flags);
        }
        
        // Restore growth tracking
        var snakeType = typeof(SnakeBody);
        var snakeFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        SetField(snakeType, snakeBody, "totalApplesEaten", currentCheckpoint.totalApplesEaten, snakeFlags);
        SetField(snakeType, snakeBody, "applesForCurrentSegment", currentCheckpoint.applesForCurrentSegment, snakeFlags);
        SetField(snakeType, snakeBody, "currentApplesThreshold", currentCheckpoint.currentApplesThreshold, snakeFlags);
        SetField(snakeType, snakeBody, "segmentsAddedFromApples", currentCheckpoint.segmentsAddedFromApples, snakeFlags);
    }
    
    private void TeleportPlayer()
    {
        if (playerMovement == null)
        {
            Debug.LogError("[CheckpointManager] Cannot teleport - playerMovement is null!");
            return;
        }
        
        Vector3 currentPos = playerMovement.transform.position;
        Vector3 targetPos = currentCheckpoint.playerPosition;
        
        Debug.Log($"[CheckpointManager] TELEPORTING: Current pos = {currentPos}, Target pos = {targetPos}");
        
        Rigidbody rb = playerMovement.GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            // Make kinematic to allow direct position setting
            rb.isKinematic = true;
            
            // Force physics sync before teleport
            Physics.SyncTransforms();
            
            // Set position and rotation using Transform.SetPositionAndRotation for atomic update
            playerMovement.transform.SetPositionAndRotation(targetPos, currentCheckpoint.playerRotation);
            
            // Also restore orientation rotation if it exists
            Transform orientation = GetOrientationTransform(playerMovement);
            if (orientation != null)
            {
                orientation.rotation = currentCheckpoint.orientationRotation;
            }
            
            // Reset velocity
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // Force physics sync after teleport
            Physics.SyncTransforms();
            
            // Restore kinematic state (should be false for gameplay)
            rb.isKinematic = false;
            
            Debug.Log($"[CheckpointManager] TELEPORTED player from {currentPos} to {playerMovement.transform.position}");
        }
        else
        {
            // No Rigidbody, just set transform directly
            playerMovement.transform.SetPositionAndRotation(targetPos, currentCheckpoint.playerRotation);
            
            Transform orientation = GetOrientationTransform(playerMovement);
            if (orientation != null)
            {
                orientation.rotation = currentCheckpoint.orientationRotation;
            }
            
            Debug.Log($"[CheckpointManager] TELEPORTED player (no RB) from {currentPos} to {playerMovement.transform.position}");
        }
    }
    
    private Transform GetOrientationTransform(PlayerMovement pm)
    {
        // Use reflection to get the orientation field from PlayerMovement
        var type = typeof(PlayerMovement);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
        var field = type.GetField("orientation", flags);
        if (field != null)
        {
            return field.GetValue(pm) as Transform;
        }
        return null;
    }
    
    public bool HasCheckpoint() => currentCheckpoint != null;
    public int GetCheckpointLevel() => currentCheckpoint?.levelMilestone ?? 0;
}