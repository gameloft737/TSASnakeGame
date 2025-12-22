using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class SnakeHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
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
    public UnityEvent<float> onHealthChanged;
    public UnityEvent onDeath;
    
    private bool isDead = false;
    
    private void Start()
    {
        currentHealth = maxHealth;
        
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
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        // Don't take damage if we're in choice phase or death screen is showing
        if (waveManager != null && waveManager.IsInChoicePhase()) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        Debug.Log($"Snake took {damage:F1} damage! Health: {currentHealth:F1}/{maxHealth}");
        
        onHealthChanged?.Invoke(GetHealthPercentage());
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        
        Debug.Log($"Snake healed {amount:F1}! Health: {currentHealth:F1}/{maxHealth}");
        
        onHealthChanged?.Invoke(GetHealthPercentage());
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
        currentHealth = maxHealth;
        isDead = false;
        onHealthChanged?.Invoke(GetHealthPercentage());
        
        // Trigger wave reset (which shows attack selection)
        if (waveManager != null)
        {
            waveManager.ResetCurrentWave();
        }
        
        Debug.Log("Level reset - wave will restart after attack selection!");
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