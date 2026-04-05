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
    
    [Header("Auto Heal Settings")]
    [Tooltip("Time in seconds after taking damage before auto heal starts")]
    [SerializeField] private float autoHealDelay = 5f;
    [Tooltip("Health restored per second during auto heal")]
    [SerializeField] private float autoHealRate = 10f;
    private float lastDamageTime = -100f;
    
    [Header("References")]
    [SerializeField] private SnakeBody snakeBody;
    
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
    private Coroutine autoHealCoroutine;
    
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
        
        if (snakeBody == null)
        {
            snakeBody = FindFirstObjectByType<SnakeBody>();
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
        
        // Record damage time for auto-heal
        lastDamageTime = Time.time;
        
        // Start auto-heal if not already running
        if (autoHealCoroutine == null && autoHealDelay > 0 && autoHealRate > 0)
        {
            autoHealCoroutine = StartCoroutine(AutoHealRoutine());
        }
        
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
        
        // Reset damage timer so auto-heal waits 5 seconds after healing
        lastDamageTime = Time.time;
        
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
        
        if (autoHealCoroutine != null)
        {
            StopCoroutine(autoHealCoroutine);
            autoHealCoroutine = null;
        }
    }
    
    private IEnumerator AutoHealRoutine()
    {
        while (currentHealth < maxHealth && !isDead)
        {
            // Wait for the delay after last damage
            float timeSinceDamage = Time.time - lastDamageTime;
            if (timeSinceDamage < autoHealDelay)
            {
                yield return new WaitForSeconds(autoHealDelay - timeSinceDamage);
            }
            
            // Heal while not taking damage and not at full health
            while (Time.time - lastDamageTime >= autoHealDelay && currentHealth < maxHealth && !isDead)
            {
                currentHealth += autoHealRate * Time.deltaTime;
                currentHealth = Mathf.Min(currentHealth, maxHealth);
                onHealthChanged?.Invoke(currentHealth, maxHealth);
                yield return null;
            }
            
            // If we exited the inner loop because we took damage, wait a bit before checking again
            if (currentHealth < maxHealth && !isDead)
            {
                yield return null;
            }
        }
        
        autoHealCoroutine = null;
    }
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log("Snake died!");
        
        // Reset level timer
        if (LevelUIManager.Instance != null)
        {
            LevelUIManager.Instance.ResetTimerForCurrentLevel();
        }
        
        // Play death sound
        SoundManager.Play("Death", gameObject);
        
        // Immediately stop player movement and pause attacks
        StopPlayerAndAttacks();
        
        // Reset snake skin if it was changed due to evolution
        ResetSnakeSkin();
        
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
    
    private void ResetSnakeSkin()
    {
        if (snakeBody != null)
        {
            snakeBody.ClearEvolutionMaterials();
        }
    }
}