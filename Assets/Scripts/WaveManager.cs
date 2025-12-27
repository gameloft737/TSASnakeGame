using UnityEngine;
using UnityEngine.Events;
using System.Collections;
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
    private bool waveActive = false;
    private bool inChoicePhase = false;
    private bool isRestartingFromDeath = false;

    private void OnEnable()
    {
        AppleEnemy.OnAppleDied += OnEnemyDied;
        XPManager.OnXPCollected += OnXPCollected;
    }

    private void OnDisable()
    {
        AppleEnemy.OnAppleDied -= OnEnemyDied;
        XPManager.OnXPCollected -= OnXPCollected;
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
        
        // Reset wave data
        currentWave.ResetConfigs();
        
        waveActive = true;
        inChoicePhase = false;
        
        // Enable player movement and unpause attacks
        SetPlayerMovement(true);
        SetAttacksPaused(false);
        
        if (waveUI != null)
        {
            waveUI.UpdateWaveNumber(currentWaveIndex + 1);
            waveUI.UpdateAppleCount(0, currentWave.xpToComplete); // Show XP progress
        }
        
        if (enemySpawner != null)
        {
            enemySpawner.StartWaveSpawning(currentWave);
        }
    }

    /// <summary>
    /// Called when XP is collected
    /// </summary>
    private void OnXPCollected(int amount)
    {
        if (!waveActive || currentWave == null) return;
        
        // Add XP to current wave tracking
        currentWave.AddXP(amount);
        
        // Update UI
        if (waveUI != null)
        {
            waveUI.UpdateAppleCount(currentWave.xpCollectedThisWave, currentWave.xpToComplete);
        }
        
        // Check if wave is complete
        if (currentWave.IsWaveComplete())
        {
            EndWave();
        }
    }

    private void OnEnemyDied(AppleEnemy enemy)
    {
        if (!waveActive) return;
        
        // Notify spawner about the death for tracking
        if (enemySpawner != null)
        {
            enemySpawner.OnEnemyDied(enemy.gameObject);
        }
    }

    private void EndWave()
    {
        waveActive = false;
        
        // Stop spawning
        if (enemySpawner != null)
        {
            enemySpawner.StopSpawning();
        }
        
        // Stop player movement and pause attacks immediately
        SetPlayerMovement(false);
        SetAttacksPaused(true);
        
        OnWaveComplete?.Invoke();
        
        inChoicePhase = true;
        
        // Show attack selection UI with animation
        if (attackSelectionUI != null && attackManager != null)
        {
            attackSelectionUI.ShowAttackSelection(attackManager);
        }
    }

    public void OnAttackSelected()
    {
        inChoicePhase = false;
        
        // Only increment wave index if NOT restarting from death
        if (!isRestartingFromDeath)
        {
            currentWaveIndex++;
        }
        
        // Reset the restart flag
        isRestartingFromDeath = false;
        
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
        
        // Mark that we're restarting from death (don't progress to next wave)
        isRestartingFromDeath = true;
        
        // Stop player and pause attacks
        SetPlayerMovement(false);
        SetAttacksPaused(true);
        
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
            
            Rigidbody rb = playerMovement.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (!enabled)
                {
                    // Stop all motion and make kinematic to prevent physics
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }
                else
                {
                    // Re-enable physics
                    rb.isKinematic = false;
                }
            }
        }
    }
    
    private void SetAttacksPaused(bool paused)
    {
        if (attackManager != null)
        {
            attackManager.SetPaused(paused);
        }
    }

    public int GetCurrentWaveIndex() => currentWaveIndex;
    public bool IsWaveActive() => waveActive;
    public bool IsInChoicePhase() => inChoicePhase;
    public int GetWaveCount() => waves.Count;
    
    /// <summary>
    /// Get current wave's XP progress
    /// </summary>
    public int GetCurrentXP() => currentWave != null ? currentWave.xpCollectedThisWave : 0;
    
    /// <summary>
    /// Get current wave's XP requirement
    /// </summary>
    public int GetXPRequired() => currentWave != null ? currentWave.xpToComplete : 0;

    public WaveData GetWaveData(int index)
    {
        if (index >= 0 && index < waves.Count)
        {
            return waves[index];
        }
        return null;
    }
}