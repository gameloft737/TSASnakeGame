using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Active ability that converts enemy apples into allies that fight for the player.
/// At higher levels, converts multiple apples at once.
/// Uses AbilityUpgradeData for level-based stats.
/// </summary>
public class AppleAllyAbility : BaseAbility
{
    [Header("Ally Conversion Settings (Fallback if no UpgradeData)")]
    [SerializeField] private int baseAllyCount = 1; // Number of apples to convert
    [SerializeField] private float baseConversionRange = 15f; // Range to find apples
    [SerializeField] private float baseConversionCooldown = 10f; // Cooldown between conversions
    
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
    [SerializeField] private Material allyMaterial; // Optional material to apply to converted allies
    [SerializeField] private Color allyTintColor = new Color(0.5f, 1f, 0.5f, 1f); // Green tint for allies
    
    [Header("Ally Stats")]
    [SerializeField] private float allyDamageMultiplier = 1f; // Damage multiplier for ally attacks
    [SerializeField] private float allyHealthMultiplier = 1f; // Health multiplier for allies
    
    [Header("Audio")]
    [SerializeField] private AudioClip conversionSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Effects")]
    [SerializeField] private GameObject conversionEffectPrefab; // VFX when converting an apple
    
    // Custom stat names for upgrade data
    private const string STAT_ALLY_COUNT = "allyCount";
    private const string STAT_CONVERSION_RANGE = "conversionRange";
    
    private Transform playerTransform;
    private float cooldownTimer = 0f;
    private List<AppleEnemy> currentAllies = new List<AppleEnemy>();
    private SnakeBody snakeBody;
    private GameObject foundAttachment; // Cached reference to found attachment
    
    protected override void Awake()
    {
        // Find player transform
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        // Find snake body for attachment
        snakeBody = FindFirstObjectByType<SnakeBody>();
        
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
        
        // Perform initial conversion
        ConvertApplesToAllies();
        
        Debug.Log($"AppleAllyAbility: Activated at level {currentLevel} - can convert {GetAllyCount()} apples");
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
            // Try to convert more apples when cooldown is ready
            ConvertApplesToAllies();
            cooldownTimer = GetConversionCooldown();
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
    /// Converts nearby enemy apples to allies
    /// </summary>
    private void ConvertApplesToAllies()
    {
        if (playerTransform == null) return;
        
        int allyCount = GetAllyCount();
        float range = GetConversionRange();
        float rangeSqr = range * range;
        
        // Find all enemy apples in range
        AppleEnemy[] allApples = FindObjectsByType<AppleEnemy>(FindObjectsSortMode.None);
        
        // Filter to only non-ally apples within range, sorted by health (highest first)
        List<AppleEnemy> eligibleApples = allApples
            .Where(apple => apple != null && 
                           !apple.IsAlly() && 
                           !apple.IsFrozen() &&
                           (apple.transform.position - playerTransform.position).sqrMagnitude <= rangeSqr)
            .OrderByDescending(apple => apple.GetHealthPercentage())
            .ToList();
        
        // Convert up to allyCount apples
        int converted = 0;
        foreach (AppleEnemy apple in eligibleApples)
        {
            if (converted >= allyCount) break;
            
            // Skip if we already have this as an ally
            if (currentAllies.Contains(apple)) continue;
            
            // Convert to ally
            ConvertToAlly(apple);
            converted++;
        }
        
        if (converted > 0)
        {
            Debug.Log($"AppleAllyAbility: Converted {converted} apples to allies");
        }
    }
    
    /// <summary>
    /// Converts a single apple enemy to an ally
    /// </summary>
    private void ConvertToAlly(AppleEnemy apple)
    {
        if (apple == null) return;
        
        // Set as ally
        apple.SetAsAlly(true, allyDamageMultiplier, allyHealthMultiplier);
        
        // Apply visual changes
        if (allyMaterial != null)
        {
            apple.SetAllyMaterial(allyMaterial);
        }
        else
        {
            apple.SetAllyTint(allyTintColor);
        }
        
        // Add to our list
        currentAllies.Add(apple);
        
        // Subscribe to death event to remove from list
        AppleEnemy.OnAppleDied += OnAllyDied;
        
        // Play conversion effect
        if (conversionEffectPrefab != null)
        {
            Instantiate(conversionEffectPrefab, apple.transform.position, Quaternion.identity);
        }
        
        // Play sound
        if (conversionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(conversionSound);
        }
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
    /// Gets the number of apples to convert based on level
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
    /// Gets the conversion range based on level
    /// </summary>
    private float GetConversionRange()
    {
        if (upgradeData != null)
        {
            float range = GetCustomStat(STAT_CONVERSION_RANGE, baseConversionRange);
            return range > 0 ? range : baseConversionRange;
        }
        // Default scaling: +2 range per level
        return baseConversionRange + (currentLevel - 1) * 2f;
    }
    
    /// <summary>
    /// Gets the cooldown between conversions
    /// </summary>
    private float GetConversionCooldown()
    {
        if (upgradeData != null)
        {
            float cooldown = GetCooldown();
            return cooldown > 0 ? cooldown : baseConversionCooldown;
        }
        // Default scaling: -1 second per level (min 3 seconds)
        return Mathf.Max(3f, baseConversionCooldown - (currentLevel - 1) * 1f);
    }
    
    protected override void OnLevelUp()
    {
        base.OnLevelUp();
        
        // Immediately try to convert more apples on level up
        ConvertApplesToAllies();
        
        Debug.Log($"AppleAllyAbility: Level {currentLevel} - Allies: {GetAllyCount()}, Range: {GetConversionRange():F1}, Cooldown: {GetConversionCooldown():F1}s");
    }
    
    protected override void DeactivateAbility()
    {
        base.DeactivateAbility();
        
        // Hide the attachment
        ShowAttachment(false);
        
        // Revert all allies back to enemies
        foreach (AppleEnemy ally in currentAllies)
        {
            if (ally != null)
            {
                ally.SetAsAlly(false, 1f, 1f);
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
            float range = Application.isPlaying ? GetConversionRange() : baseConversionRange;
            
            // Draw conversion range
            Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.3f);
            Gizmos.DrawWireSphere(drawTransform.position, range);
            
            // Draw lines to current allies
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
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