using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class SnakeHealth : MonoBehaviour
{
    public static SnakeHealth Instance { get; private set; }
    
    [Header("Health Settings")]
    [SerializeField] private float baseMaxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    private float maxHealth;
    
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
    public UnityEvent<float, float> onHealthChanged;
    public UnityEvent onDeath;
    
    [Header("Death Sound")]
    [Tooltip("Drag the death audio clip here directly (plays reliably without depending on SoundManager name lookup)")]
    [SerializeField] private AudioClip deathSound;
    [Tooltip("Volume for the death sound (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float deathSoundVolume = 1f;
    
    private bool isDead = false;
    private Coroutine autoHealCoroutine;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[SnakeHealth] Multiple instances detected. Using first instance.");
        }
        
        UpdateMaxHealth();
        currentHealth = maxHealth;
    }
    
    private void Start()
    {
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
        
        StartCoroutine(InvokeInitialHealthNextFrame());
    }
    
    private IEnumerator InvokeInitialHealthNextFrame()
    {
        yield return null;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        if (isInvincible) return;
        
        if (waveManager != null && waveManager.IsInChoicePhase()) return;
        
        if (waveManager != null && !waveManager.IsWaveActive()) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        #if UNITY_EDITOR
        #endif
        
        lastDamageTime = Time.time;
        
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
        
        UpdateMaxHealth();
        
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        
        #if UNITY_EDITOR
        #endif
        
        lastDamageTime = Time.time;
        
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    private void UpdateMaxHealth()
    {
        float flatBonus = PlayerStats.Instance != null ? PlayerStats.Instance.GetMaxHealthBonus() : 0f;
        float percentBonus = PlayerStats.Instance != null ? PlayerStats.Instance.GetMaxHealthPercentBonus() : 0f;
        
        maxHealth = (baseMaxHealth + flatBonus) * (1f + percentBonus);
    }
    
    private void OnStatsChanged()
    {
        float oldMaxHealth = maxHealth;
        UpdateMaxHealth();
        
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
        
        // Reset level timer
        if (LevelUIManager.Instance != null)
        {
            LevelUIManager.Instance.ResetTimerForCurrentLevel();
        }
        
        // Play death sound. Prefer the directly-assigned AudioClip (drag-in)
        // so it plays reliably even if the SoundManager registration is missing
        // and even though ShowDeathScreen deactivates the snake moments later.
        // AudioSource.PlayClipAtPoint creates its own temporary, top-level
        // GameObject that survives the snake being disabled.
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position, deathSoundVolume);
        }
        else
        {
            // Fallback: try the named sound from SoundManager
            SoundManager.PlayAtPoint("Death", transform.position);
        }
        
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