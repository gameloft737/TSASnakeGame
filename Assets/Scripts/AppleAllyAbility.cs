using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Active ability that spawns apple allies that fight for the player.
/// At higher levels, spawns multiple allies at once.
/// Allies are white-tinted apples that attack enemy apples.
/// Uses AbilityUpgradeData for level-based stats.
/// </summary>
public class AppleAllyAbility : BaseAbility
{
    [Header("Ally Spawn Settings (Fallback if no UpgradeData)")]
    [SerializeField] private int baseAllyCount = 1; // Number of allies to spawn
    [SerializeField] private float baseSpawnRadius = 3f; // Radius around player to spawn allies
    [SerializeField] private float baseSpawnCooldown = 10f; // Cooldown between spawns
    
    [Header("Ally Prefab")]
    [Tooltip("The apple enemy prefab to spawn as an ally. Will be tinted white.")]
    [SerializeField] private GameObject allyPrefab; // Prefab to spawn as ally
    
    [Header("Visual Settings - Attachment to Unhide")]
    [Tooltip("Direct reference to a GameObject to show (if already in scene)")]
    [SerializeField] private GameObject attachmentToShow; // Model to unhide on snake when ability is active
    
    [Tooltip("Name of the GameObject to find and show (searches in scene at runtime)")]
    [SerializeField] private string attachmentObjectName = ""; // Find by name if direct reference not set
    
    [Tooltip("Tag of the GameObject to find and show (searches in scene at runtime)")]
    [SerializeField] private string attachmentObjectTag = ""; // Find by tag if name not set
    
    [Tooltip("If true, searches for the attachment as a child of the player/snake")]
    [SerializeField] private bool searchInPlayerChildren = true;
    
    [Header("Ally Visual Settings")]
    [SerializeField] private Material allyMaterial; // Optional material to apply to spawned allies
    [SerializeField] private Color allyTintColor = Color.white; // White tint for allies (default)
    
    [Header("Ally Stats")]
    [SerializeField] private float allyDamageMultiplier = 1f; // Damage multiplier for ally attacks
    [SerializeField] private float allyHealthMultiplier = 1f; // Health multiplier for allies
    
    [Header("Audio")]
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Effects")]
    [SerializeField] private GameObject spawnEffectPrefab; // VFX when spawning an ally
    
    // Custom stat names for upgrade data
    private const string STAT_ALLY_COUNT = "allyCount";
    private const string STAT_SPAWN_RADIUS = "spawnRadius";
    private const string STAT_MAX_ALLIES = "maxAllies";
    
    private Transform playerTransform;
    private float cooldownTimer = 0f;
    private List<AppleEnemy> currentAllies = new List<AppleEnemy>();
    private SnakeBody snakeBody;
    private SnakeHealth snakeHealth;
    private GameObject foundAttachment; // Cached reference to found attachment
    private int baseMaxAlliesAlive = 5; // Base maximum allies that can be alive at once
    
    protected override void Awake()
    {
        // Find player transform
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        // Find snake body and health for attachment and ally initialization
        snakeBody = FindFirstObjectByType<SnakeBody>();
        snakeHealth = FindFirstObjectByType<SnakeHealth>();
        
        // Find the attachment object if not directly assigned
        FindAttachmentObject();
        
        // Get or add audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound
            }
        }
        
        base.Awake();
    }
    
    /// <summary>
    /// Finds the attachment object to show/hide based on configuration
    /// </summary>
    private void FindAttachmentObject()
    {
        // If direct reference is set, use it
        if (attachmentToShow != null)
        {
            foundAttachment = attachmentToShow;
            Debug.Log($"AppleAllyAbility: Using direct attachment reference: {foundAttachment.name}");
            return;
        }
        
        // Try to find by name
        if (!string.IsNullOrEmpty(attachmentObjectName))
        {
            if (searchInPlayerChildren && playerTransform != null)
            {
                // Search in player's children
                foundAttachment = FindChildByName(playerTransform, attachmentObjectName);
            }
            else
            {
                // Search in entire scene
                GameObject found = GameObject.Find(attachmentObjectName);
                if (found != null)
                {
                    foundAttachment = found;
                }
            }
            
            if (foundAttachment != null)
            {
                Debug.Log($"AppleAllyAbility: Found attachment by name: {foundAttachment.name}");
                return;
            }
        }
        
        // Try to find by tag
        if (!string.IsNullOrEmpty(attachmentObjectTag))
        {
            if (searchInPlayerChildren && playerTransform != null)
            {
                // Search in player's children for tagged object
                foundAttachment = FindChildByTag(playerTransform, attachmentObjectTag);
            }
            else
            {
                // Search in entire scene
                GameObject found = GameObject.FindWithTag(attachmentObjectTag);
                if (found != null)
                {
                    foundAttachment = found;
                }
            }
            
            if (foundAttachment != null)
            {
                Debug.Log($"AppleAllyAbility: Found attachment by tag: {foundAttachment.name}");
                return;
            }
        }
        
        if (foundAttachment == null)
        {
            Debug.LogWarning("AppleAllyAbility: No attachment object found. Set attachmentToShow, attachmentObjectName, or attachmentObjectTag.");
        }
    }
    
    /// <summary>
    /// Recursively searches for a child GameObject by name
    /// </summary>
    private GameObject FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child.gameObject;
            }
            
            GameObject found = FindChildByName(child, name);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Recursively searches for a child GameObject by tag
    /// </summary>
    private GameObject FindChildByTag(Transform parent, string tag)
    {
        foreach (Transform child in parent)
        {
            if (child.CompareTag(tag))
            {
                return child.gameObject;
            }
            
            GameObject found = FindChildByTag(child, tag);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }
    
    protected override void ActivateAbility()
    {
        base.ActivateAbility();
        
        // Show the attachment model on the snake
        ShowAttachment(true);
        
        // Spawn initial allies
        SpawnAllies();
        
        Debug.Log($"AppleAllyAbility: Activated at level {currentLevel} - can spawn {GetAllyCount()} allies");
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (!isActive || isFrozen) return;
        
        // Update cooldown
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }
        else
        {
            // Try to spawn more allies when cooldown is ready (if below max)
            int maxAllies = GetMaxAllies();
            if (currentAllies.Count < maxAllies)
            {
                SpawnAllies();
                cooldownTimer = GetSpawnCooldown();
            }
        }
        
        // Clean up dead allies from the list
        CleanupDeadAllies();
    }
    
    /// <summary>
    /// Shows or hides the attachment model on the snake
    /// </summary>
    private void ShowAttachment(bool show)
    {
        // Use found attachment (which could be direct reference, found by name, or found by tag)
        if (foundAttachment != null)
        {
            foundAttachment.SetActive(show);
            Debug.Log($"AppleAllyAbility: Attachment '{foundAttachment.name}' {(show ? "shown" : "hidden")}");
        }
        else
        {
            Debug.LogWarning($"AppleAllyAbility: Cannot {(show ? "show" : "hide")} attachment - no attachment found!");
        }
    }
    
    /// <summary>
    /// Spawns new apple allies near the player
    /// </summary>
    private void SpawnAllies()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("AppleAllyAbility: Cannot spawn allies - missing player transform!");
            return;
        }
        
        if (allyPrefab == null)
        {
            Debug.LogWarning("AppleAllyAbility: Cannot spawn allies - ally prefab not assigned! Please assign an AppleEnemy prefab.");
            return;
        }
        
        int allyCount = GetAllyCount();
        float spawnRadius = GetSpawnRadius();
        int maxAllies = GetMaxAllies();
        
        // Calculate how many we can spawn (respecting max limit)
        int canSpawn = Mathf.Min(allyCount, maxAllies - currentAllies.Count);
        
        if (canSpawn <= 0) return;
        
        int spawned = 0;
        for (int i = 0; i < canSpawn; i++)
        {
            // Calculate spawn position in a circle around the player
            float angle = (float)i / canSpawn * Mathf.PI * 2f;
            // Add some randomness to the angle
            angle += Random.Range(-0.3f, 0.3f);
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * spawnRadius;
            Vector3 spawnPos = playerTransform.position + offset;
            
            // Spawn the ally
            AppleEnemy ally = SpawnAlly(spawnPos);
            if (ally != null)
            {
                spawned++;
            }
        }
        
        if (spawned > 0)
        {
            Debug.Log($"AppleAllyAbility: Spawned {spawned} white apple allies");
        }
    }
    
    /// <summary>
    /// Spawns a single apple ally at the specified position
    /// </summary>
    private AppleEnemy SpawnAlly(Vector3 position)
    {
        if (allyPrefab == null) return null;
        
        // Instantiate the ally
        GameObject allyObj = Instantiate(allyPrefab, position, Quaternion.identity);
        AppleEnemy ally = allyObj.GetComponent<AppleEnemy>();
        
        if (ally == null)
        {
            Debug.LogError("AppleAllyAbility: Ally prefab does not have AppleEnemy component!");
            Destroy(allyObj);
            return null;
        }
        
        // Initialize the ally with snake references
        ally.Initialize(snakeBody, snakeHealth);
        
        // Set as ally immediately
        ally.SetAsAlly(true, allyDamageMultiplier, allyHealthMultiplier);
        
        // Apply white tint visual
        if (allyMaterial != null)
        {
            ally.SetAllyMaterial(allyMaterial);
        }
        else
        {
            // Default to white tint
            ally.SetAllyTint(allyTintColor);
        }
        
        // Add to our list
        currentAllies.Add(ally);
        
        // Subscribe to death event to remove from list
        AppleEnemy.OnAppleDied += OnAllyDied;
        
        // Play spawn effect
        if (spawnEffectPrefab != null)
        {
            Instantiate(spawnEffectPrefab, position, Quaternion.identity);
        }
        
        // Play spawn sound using SoundManager
        SoundManager.Play("AppleSpawn", gameObject);
        
        return ally;
    }
    
    /// <summary>
    /// Called when an ally dies
    /// </summary>
    private void OnAllyDied(AppleEnemy apple)
    {
        if (currentAllies.Contains(apple))
        {
            currentAllies.Remove(apple);
        }
    }
    
    /// <summary>
    /// Removes dead allies from the tracking list
    /// </summary>
    private void CleanupDeadAllies()
    {
        currentAllies.RemoveAll(ally => ally == null);
    }
    
    /// <summary>
    /// Gets the number of allies to spawn based on level
    /// </summary>
    private int GetAllyCount()
    {
        if (upgradeData != null)
        {
            int count = Mathf.RoundToInt(GetCustomStat(STAT_ALLY_COUNT, baseAllyCount));
            return Mathf.Max(1, count);
        }
        // Default scaling: +1 ally per level
        return baseAllyCount + (currentLevel - 1);
    }
    
    /// <summary>
    /// Gets the spawn radius based on level
    /// </summary>
    private float GetSpawnRadius()
    {
        if (upgradeData != null)
        {
            float radius = GetCustomStat(STAT_SPAWN_RADIUS, baseSpawnRadius);
            return radius > 0 ? radius : baseSpawnRadius;
        }
        // Default scaling: +0.5 radius per level
        return baseSpawnRadius + (currentLevel - 1) * 0.5f;
    }
    
    /// <summary>
    /// Gets the cooldown between spawns
    /// </summary>
    private float GetSpawnCooldown()
    {
        if (upgradeData != null)
        {
            float cooldown = GetCooldown();
            return cooldown > 0 ? cooldown : baseSpawnCooldown;
        }
        // Default scaling: -1 second per level (min 3 seconds)
        return Mathf.Max(3f, baseSpawnCooldown - (currentLevel - 1) * 1f);
    }
    
    /// <summary>
    /// Gets the maximum number of allies that can be alive at once
    /// </summary>
    private int GetMaxAllies()
    {
        if (upgradeData != null)
        {
            int max = Mathf.RoundToInt(GetCustomStat(STAT_MAX_ALLIES, baseMaxAlliesAlive));
            return Mathf.Max(1, max);
        }
        // Default scaling: +2 max allies per level
        return baseMaxAlliesAlive + (currentLevel - 1) * 2;
    }
    
    protected override void OnLevelUp()
    {
        base.OnLevelUp();
        
        // Immediately try to spawn more allies on level up
        SpawnAllies();
        
        Debug.Log($"AppleAllyAbility: Level {currentLevel} - Allies: {GetAllyCount()}, Radius: {GetSpawnRadius():F1}, Cooldown: {GetSpawnCooldown():F1}s, Max: {GetMaxAllies()}");
    }
    
    protected override void DeactivateAbility()
    {
        base.DeactivateAbility();
        
        // Hide the attachment
        ShowAttachment(false);
        
        // Destroy all spawned allies when ability is deactivated
        foreach (AppleEnemy ally in currentAllies)
        {
            if (ally != null)
            {
                Destroy(ally.gameObject);
            }
        }
        currentAllies.Clear();
        
        // Unsubscribe from events
        AppleEnemy.OnAppleDied -= OnAllyDied;
    }
    
    private void OnDestroy()
    {
        AppleEnemy.OnAppleDied -= OnAllyDied;
    }
    
    /// <summary>
    /// Debug visualization
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Transform drawTransform = playerTransform;
        if (drawTransform == null)
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
            {
                drawTransform = player.transform;
            }
        }
        
        if (drawTransform != null)
        {
            float radius = Application.isPlaying ? GetSpawnRadius() : baseSpawnRadius;
            
            // Draw spawn radius
            Gizmos.color = new Color(1f, 1f, 1f, 0.3f); // White for ally spawn area
            Gizmos.DrawWireSphere(drawTransform.position, radius);
            
            // Draw lines to current allies
            if (Application.isPlaying)
            {
                Gizmos.color = Color.white;
                foreach (AppleEnemy ally in currentAllies)
                {
                    if (ally != null)
                    {
                        Gizmos.DrawLine(drawTransform.position, ally.transform.position);
                    }
                }
            }
        }
    }
}