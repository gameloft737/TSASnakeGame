using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Configuration")]
    [SerializeField] private List<WaveData> waves = new List<WaveData>();
    [SerializeField] private int currentWaveIndex = 0;
    
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private WaveUI waveUI;
    [SerializeField] private AttackSelectionUI attackSelectionUI;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private AttackManager attackManager;
    
    [Header("Events")]
    public UnityEvent OnWaveComplete;
    public UnityEvent OnAllWavesComplete;
    
    private WaveData currentWave;
    private int remainingEnemies;
    private int totalEnemies;
    private bool waveActive = false;
    private bool inChoicePhase = false;
    private HashSet<SpawnGroup> spawnedGroups = new HashSet<SpawnGroup>();

    private void OnEnable()
    {
        AppleEnemy.OnAppleDied += OnEnemyDied;
    }

    private void OnDisable()
    {
        AppleEnemy.OnAppleDied -= OnEnemyDied;
    }

    private void Start()
    {
        if (enemySpawner == null)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }
        
        if (waveUI == null)
        {
            waveUI = FindFirstObjectByType<WaveUI>();
        }
        
        if (attackSelectionUI == null)
        {
            attackSelectionUI = FindFirstObjectByType<AttackSelectionUI>();
        }
        
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }
        
        if (attackManager == null)
        {
            attackManager = FindFirstObjectByType<AttackManager>();
        }
        
        StartWave();
    }

    public void StartWave()
    {
        if (currentWaveIndex >= waves.Count)
        {
            OnAllWavesComplete?.Invoke();
            return;
        }

        currentWave = waves[currentWaveIndex];
        totalEnemies = currentWave.GetTotalEnemies();
        remainingEnemies = totalEnemies;
        waveActive = true;
        inChoicePhase = false;
        spawnedGroups.Clear();
        
        // Enable player movement
        SetPlayerMovement(true);
        
        if (waveUI != null)
        {
            waveUI.UpdateWaveNumber(currentWaveIndex + 1);
            waveUI.UpdateAppleCount(remainingEnemies, totalEnemies);
        }
        
        // Spawn initial groups
        if (enemySpawner != null)
        {
            foreach (var group in currentWave.spawnGroups)
            {
                if (group.spawnThreshold >= totalEnemies)
                {
                    enemySpawner.SpawnGroup(group);
                    spawnedGroups.Add(group);
                }
            }
        }
    }

    private void OnEnemyDied(AppleEnemy enemy)
    {
        if (!waveActive) return;
        
        remainingEnemies--;
        
        if (waveUI != null)
        {
            waveUI.UpdateAppleCount(remainingEnemies, totalEnemies);
        }
        
        // Check if any spawn groups should trigger
        CheckSpawnThresholds();
        
        // Check if wave is complete
        if (remainingEnemies <= 0)
        {
            EndWave();
        }
    }

    private void CheckSpawnThresholds()
    {
        if (enemySpawner == null || currentWave == null) return;
        
        foreach (var group in currentWave.spawnGroups)
        {
            if (!spawnedGroups.Contains(group) && remainingEnemies <= group.spawnThreshold)
            {
                enemySpawner.SpawnGroup(group);
                spawnedGroups.Add(group);
            }
        }
    }

    private void EndWave()
    {
        waveActive = false;
        inChoicePhase = true;
        
        // Stop player movement
        SetPlayerMovement(false);
        
        // Show attack selection UI
        if (attackSelectionUI != null && attackManager != null)
        {
            attackSelectionUI.ShowAttackSelection(attackManager);
        }
        
        OnWaveComplete?.Invoke();
    }

    public void OnAttackSelected()
    {
        inChoicePhase = false;
        currentWaveIndex++;
        StartWave();
    }

    public void ResetCurrentWave()
    {
        if (enemySpawner != null)
        {
            enemySpawner.ClearAllEnemies();
        }
        
        waveActive = false;
        inChoicePhase = true;
        spawnedGroups.Clear();
        
        // Stop player and show choices
        SetPlayerMovement(false);
        
        if (attackSelectionUI != null && attackManager != null)
        {
            attackSelectionUI.ShowAttackSelection(attackManager);
        }
    }

    private void SetPlayerMovement(bool enabled)
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = enabled;
            
            // Stop rigidbody velocity when disabled
            if (!enabled)
            {
                Rigidbody rb = playerMovement.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
    }

    public int GetCurrentWaveIndex() => currentWaveIndex;
    public int GetRemainingEnemies() => remainingEnemies;
    public int GetTotalEnemies() => totalEnemies;
    public bool IsWaveActive() => waveActive;
    public bool IsInChoicePhase() => inChoicePhase;
}