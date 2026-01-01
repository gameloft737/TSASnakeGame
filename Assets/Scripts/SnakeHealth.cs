using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class SnakeHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float baseMaxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    // Cached max health (base + bonuses)
    private float maxHealth;
    
    [Header("Death Settings")]
    [SerializeField] private float deathScreenDuration = 5f;
    
    [Header("References")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private AttackSelectionUI attackSelectionUI;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private AttackManager attackManager;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private CameraManager cameraManager;
    
    [Header("Events")]
    public UnityEvent<float, float> onHealthChanged; // currentHealth, maxHealth
    public UnityEvent onDeath;
    
    private bool isDead = false;
    
    private void Awake()
    {
        // Initialize health early so it's ready when listeners subscribe
        UpdateMaxHealth();
        currentHealth = maxHealth;
    }
    
    private void Start()
    {
        // Subscribe to stat changes
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.onStatsChanged.AddListener(OnStatsChanged);
        }
        
        if (waveManager == null)
        {
            waveManager = FindFirstObjectByType<WaveManager>();
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
        
        if (enemySpawner == null)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }
        
        if (cameraManager == null)
        {
            cameraManager = FindFirstObjectByType<CameraManager>();
        }
        
        // Invoke initial health state after all listeners have had a chance to subscribe
        StartCoroutine(InvokeInitialHealthNextFrame());
    }
    
    private IEnumerator InvokeInitialHealthNextFrame()
    {
        // Wait one frame to ensure all Start() methods have run and listeners are subscribed
        yield return null;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        // Don't take damage if we're in choice phase
        if (waveManager != null && waveManager.IsInChoicePhase()) return;
        
        // Don't take damage if wave is not active (during transitions)
        if (waveManager != null && !waveManager.IsWaveActive()) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        Debug.Log($"Snake took {damage:F1} damage! Health: {currentHealth:F1}/{maxHealth}");
        
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        // Update max health in case bonuses changed
        UpdateMaxHealth();
        
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        
        Debug.Log($"Snake healed {amount:F1}! Health: {currentHealth:F1}/{maxHealth}");
        
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    private void UpdateMaxHealth()
    {
        float flatBonus = PlayerStats.Instance != null ? PlayerStats.Instance.GetMaxHealthBonus() : 0f;
        float percentBonus = PlayerStats.Instance != null ? PlayerStats.Instance.GetMaxHealthPercentBonus() : 0f;
        
        // Apply flat bonus first, then percentage bonus
        maxHealth = (baseMaxHealth + flatBonus) * (1f + percentBonus);
    }
    
    private void OnStatsChanged()
    {
        float oldMaxHealth = maxHealth;
        UpdateMaxHealth();
        
        // If max health increased, heal by the difference
        if (maxHealth > oldMaxHealth)
        {
            currentHealth += (maxHealth - oldMaxHealth);
            onHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
    
    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.onStatsChanged.RemoveListener(OnStatsChanged);
        }
    }
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log("Snake died!");
        
        // Immediately stop player movement and pause attacks
        StopPlayerAndAttacks();
        
        onDeath?.Invoke();
        
        StartCoroutine(HandleDeath());
    }
    
    private IEnumerator HandleDeath()
    {
        // Show "YOU'RE DEAD" message
        if (attackSelectionUI != null)
        {
            attackSelectionUI.ShowDeathScreen(true);
        }
        
        // Wait for specified duration
        yield return new WaitForSeconds(deathScreenDuration);
        
        // Hide death screen
        if (attackSelectionUI != null)
        {
            attackSelectionUI.ShowDeathScreen(false);
        }
        
        // Reset health first
        UpdateMaxHealth();
        currentHealth = maxHealth;
        isDead = false;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        
        // Switch back to gameplay camera
        if (cameraManager != null)
        {
            cameraManager.SwitchToNormalCamera();
        }
        
        // Reset the current wave (clears enemies, stops movement/attacks)
        if (waveManager != null)
        {
            waveManager.ResetCurrentWave();
        }
        
        // Now start the wave again (enables movement/attacks, starts spawning)
        if (waveManager != null)
        {
            waveManager.StartCurrentWave();
        }
        
        Debug.Log("Level reset - wave restarting immediately!");
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
    
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public float GetMaxHealth()
    {
        return maxHealth;
    }
    
    public bool IsAlive()
    {
        return currentHealth > 0 && !isDead;
    }
    
    private void StopPlayerAndAttacks()
    {
        // Switch to pause camera
        if (cameraManager != null)
        {
            cameraManager.SwitchToPauseCamera();
        }
        
        // Stop player movement completely
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
            
            Rigidbody rb = playerMovement.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }
        
        // Pause all attacks
        if (attackManager != null)
        {
            attackManager.SetPaused(true);
        }
        
        // Clear all enemies/apples
        if (enemySpawner != null)
        {
            enemySpawner.ClearAllEnemies();
        }
    }
}