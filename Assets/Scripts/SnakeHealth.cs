using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class SnakeHealth : MonoBehaviour
{
    public static SnakeHealth Instance { get; private set; }
    
    [Header("Health Settings")]
    [SerializeField] private float baseMaxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    // Cached max health (base + bonuses)
    private float maxHealth;
    
    // Invincibility flag - when true, player takes no damage
    private bool isInvincible = false;
    
    [Header("References")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private DeathScreenManager deathScreenManager;
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
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[SnakeHealth] Multiple instances detected. Using first instance.");
        }
        
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
        
        if (deathScreenManager == null)
        {
            deathScreenManager = FindFirstObjectByType<DeathScreenManager>();
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
        
        // Don't take damage if invincible
        if (isInvincible) return;
        
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
        
        // Play death sound
        SoundManager.Play("Death", gameObject);
        
        // Immediately stop player movement and pause attacks
        StopPlayerAndAttacks();
        
        onDeath?.Invoke();
        
        // Show death screen with restart/quit options
        if (deathScreenManager != null)
        {
            deathScreenManager.ShowDeathScreen();
        }
        else
        {
            // Try to find or create DeathScreenManager
            deathScreenManager = FindFirstObjectByType<DeathScreenManager>();
            
            if (deathScreenManager == null)
            {
                // Create a new DeathScreenManager
                GameObject deathManagerObj = new GameObject("DeathScreenManager");
                deathScreenManager = deathManagerObj.AddComponent<DeathScreenManager>();
                Debug.Log("[SnakeHealth] Created DeathScreenManager automatically");
            }
            
            deathScreenManager.ShowDeathScreen();
        }
    }
    
    /// <summary>
    /// Resets the snake's health to full. Called by DeathScreenManager when restarting.
    /// </summary>
    public void ResetHealth()
    {
        UpdateMaxHealth();
        currentHealth = maxHealth;
        isDead = false;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log("[SnakeHealth] Health reset to full");
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
    
    /// <summary>
    /// Sets the invincibility state. When invincible, the player takes no damage.
    /// </summary>
    public void SetInvincible(bool invincible)
    {
        isInvincible = invincible;
        Debug.Log($"[SnakeHealth] Invincibility set to: {invincible}");
    }
    
    /// <summary>
    /// Returns whether the player is currently invincible.
    /// </summary>
    public bool IsInvincible()
    {
        return isInvincible;
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