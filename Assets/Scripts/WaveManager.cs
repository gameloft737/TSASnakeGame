using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Mode")]
    [SerializeField] private bool useInfiniteWaves = true;
    
    [Header("Infinite Wave Configuration")]
    [SerializeField] private InfiniteWaveConfig infiniteWaveConfig;
    
    [Header("Legacy Wave Configuration (used when useInfiniteWaves is false)")]
    [SerializeField] private List<WaveData> waves = new List<WaveData>();
    
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private AttackSelectionUI attackSelectionUI;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private AttackManager attackManager;
    
    [Header("Events")]
    public UnityEvent OnWaveComplete;
    public UnityEvent OnAllWavesComplete;
    public UnityEvent<int> OnWaveStarted;
    
    public int currentWaveIndex = 0;
    private WaveData currentWave;
    private List<EnemySpawnConfig> currentInfiniteWaveConfigs;
    private bool waveActive = false;
    private bool inChoicePhase = false;

    private void OnEnable()
    {
        XPManager.OnLeveledUp += OnLevelUp;
        AppleEnemy.OnAppleDied += OnEnemyDied;
    }

    private void OnDisable()
    {
        XPManager.OnLeveledUp -= OnLevelUp;
        AppleEnemy.OnAppleDied -= OnEnemyDied;
    }

    private void Start()
    {
        FindReferences();
        StartWave();
    }

    private void FindReferences()
    {
        if (!enemySpawner) enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (!attackSelectionUI) attackSelectionUI = FindFirstObjectByType<AttackSelectionUI>();
        if (!playerMovement) playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (!attackManager) attackManager = FindFirstObjectByType<AttackManager>();
    }

    public void StartWave()
    {
        if (useInfiniteWaves)
        {
            StartInfiniteWave();
        }
        else
        {
            StartLegacyWave();
        }
    }
    
    private void StartInfiniteWave()
    {
        if (!infiniteWaveConfig)
        {
            Debug.LogError("InfiniteWaveConfig is not assigned! Please assign it in the inspector.");
            return;
        }
        
        // Generate wave configuration dynamically
        currentInfiniteWaveConfigs = infiniteWaveConfig.GenerateWaveConfig(currentWaveIndex);
        
        // Reset all configs
        foreach (var config in currentInfiniteWaveConfigs)
        {
            config.Reset();
        }
        
        waveActive = true;
        inChoicePhase = false;
        
        SetPlayerMovement(true);
        SetAttacksPaused(false);
        
        if (enemySpawner)
        {
            enemySpawner.StartInfiniteWaveSpawning(currentInfiniteWaveConfigs);
        }
        
        OnWaveStarted?.Invoke(currentWaveIndex);
        
        Debug.Log($"Started {infiniteWaveConfig.GetWaveName(currentWaveIndex)} - Difficulty: {infiniteWaveConfig.GetDifficultyMultiplier(currentWaveIndex):F2}x");
    }
    
    private void StartLegacyWave()
    {
        if (currentWaveIndex >= waves.Count)
        {
            OnAllWavesComplete?.Invoke();
            return;
        }

        currentWave = waves[currentWaveIndex];
        currentWave.ResetConfigs();
        
        waveActive = true;
        inChoicePhase = false;
        
        SetPlayerMovement(true);
        SetAttacksPaused(false);
        
        if (enemySpawner)
        {
            enemySpawner.StartWaveSpawning(currentWave);
        }
        
        OnWaveStarted?.Invoke(currentWaveIndex);
    }

    private void OnLevelUp(int newLevel)
    {
        if (!waveActive) return;
        
        ShowAttackSelection();
    }

    private void OnEnemyDied(AppleEnemy enemy)
    {
        if (!waveActive || !enemySpawner) return;
        enemySpawner.OnEnemyDied(enemy.gameObject);
    }

    private void ShowAttackSelection()
    {
        waveActive = false;
        
        if (enemySpawner) enemySpawner.StopSpawning();
        
        SetPlayerMovement(false);
        SetAttacksPaused(true);
        
        OnWaveComplete?.Invoke();
        
        inChoicePhase = true;
        
        if (attackSelectionUI && attackManager)
        {
            attackSelectionUI.ShowAttackSelection(attackManager);
        }
    }

    public void OnAttackSelected()
    {
        inChoicePhase = false;
        currentWaveIndex++;
        StartWave();
    }

    public void ResetCurrentWave()
    {
        if (enemySpawner) enemySpawner.ClearAllEnemies();
        
        waveActive = false;
        inChoicePhase = true;
        
        SetPlayerMovement(false);
        SetAttacksPaused(true);
        
        if (attackSelectionUI && attackManager)
        {
            attackSelectionUI.ShowAttackSelection(attackManager);
        }
    }

    private void SetPlayerMovement(bool enabled)
    {
        if (!playerMovement) return;
        
        playerMovement.enabled = enabled;
        
        Rigidbody rb = playerMovement.GetComponent<Rigidbody>();
        if (rb)
        {
            if (!enabled)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            else
            {
                rb.isKinematic = false;
            }
        }
    }
    
    private void SetAttacksPaused(bool paused)
    {
        if (attackManager) attackManager.SetPaused(paused);
    }

    public int GetCurrentWaveIndex() => currentWaveIndex;
    public bool IsWaveActive() => waveActive;
    public bool IsInChoicePhase() => inChoicePhase;
    public bool IsInfiniteMode() => useInfiniteWaves;
    public int GetWaveCount() => useInfiniteWaves ? int.MaxValue : waves.Count;
    public WaveData GetWaveData(int index) => index >= 0 && index < waves.Count ? waves[index] : null;
    public InfiniteWaveConfig GetInfiniteWaveConfig() => infiniteWaveConfig;
    
    public float GetCurrentDifficultyMultiplier()
    {
        if (useInfiniteWaves && infiniteWaveConfig)
        {
            return infiniteWaveConfig.GetDifficultyMultiplier(currentWaveIndex);
        }
        return 1f;
    }
    
    public string GetCurrentWaveName()
    {
        if (useInfiniteWaves && infiniteWaveConfig)
        {
            return infiniteWaveConfig.GetWaveName(currentWaveIndex);
        }
        return currentWave != null ? currentWave.waveName : $"Wave {currentWaveIndex + 1}";
    }
}