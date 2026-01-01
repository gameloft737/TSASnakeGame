using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Places bombs on every other body part (excluding first 3)
/// Bombs respawn after a cooldown period when all bombs have exploded
/// </summary>
public class BombPlacementAbility : BaseAbility
{
    [Header("Bomb Settings")]
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private float defaultRespawnCooldown = 10f; // Default cooldown if no upgrade data
    [SerializeField] private float bombArmDelay = 5f; // Bombs can't explode for this many seconds after placement
    
    [Header("References")]
    [SerializeField] private SnakeBody snakeBody;
    public float damage;
    
    private List<GameObject> activeBombs = new List<GameObject>();
    private bool hasPlacedBombs = false;
    private int bombsExploded = 0;
    private float respawnTimer = 0f;
    private bool isWaitingToRespawn = false;

    private void OnEnable()
    {
        // Subscribe to the event
        SnakeBody.OnBodyPartsInitialized += PlaceBombs;
    }

    private void OnDisable()
    {
        // Unsubscribe from the event
        SnakeBody.OnBodyPartsInitialized -= PlaceBombs;
    }

    protected override void Awake()
    {
        // Don't call base.Awake() - we want infinite duration until bombs explode
        isActive = true;
    }

    protected override void Update()
    {
        // Skip updates when frozen
        if (isFrozen) return;
        
        // Handle respawn timer
        if (isWaitingToRespawn)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
            {
                isWaitingToRespawn = false;
                hasPlacedBombs = false;
                PlaceBombs();
            }
        }
    }

    private void Start()
    {
        // Get SnakeBody if not assigned
        if (snakeBody == null)
        {
            snakeBody = GetComponentInParent<SnakeBody>();
        }
        
        // Check if body parts already exist and place bombs immediately
        if (snakeBody != null && snakeBody.bodyParts != null && snakeBody.bodyParts.Count > 0)
        {
            PlaceBombs();
        }
    }

    private void PlaceBombs()
    {
        // Prevent placing bombs multiple times (unless leveling up)
        if (hasPlacedBombs) return;
        
        if (snakeBody == null)
        {
            snakeBody = GetComponentInParent<SnakeBody>();
            if (snakeBody == null)
            {
                Debug.LogWarning("BombPlacementAbility: SnakeBody not found!");
                return;
            }
        }

        List<BodyPart> bodyParts = snakeBody.bodyParts;
        
        if (bodyParts == null || bodyParts.Count == 0)
        {
            Debug.LogWarning("BombPlacementAbility: Body parts list is empty!");
            return;
        }
        
        int totalParts = bodyParts.Count;

        // Need at least 4 parts (3 safe + 1 for bomb) to place any bombs
        if (totalParts < 4)
        {
            Debug.Log("BombPlacementAbility: Not enough body parts to place bombs!");
            return;
        }

        // Only safe zone is the first 3 segments (tail area, indices 0, 1, 2)
        int startIndex = 3; // Start right after the safe zone
        int endIndex = totalParts - 1; // Go all the way to the last segment

        // Calculate spacing between bombs based on level
        // Level 1: Every 3rd segment (gap of 2)
        // Level 2: Every 2nd segment (gap of 1)
        // Level 3+: Every segment (no gap)
        int spacing = Mathf.Max(1, 4 - currentLevel); // 3, 2, 1, 1, 1...

        // Place bombs with calculated spacing
        for (int i = startIndex; i <= endIndex; i += spacing)
        {
            BodyPart part = bodyParts[i];
            
            // Instantiate bomb at body part position with matching rotation
            GameObject bomb = Instantiate(bombPrefab, part.transform.position, part.transform.rotation);
            Bomb bombComponent = bomb.GetComponent<Bomb>();
            if (bombComponent != null)
            {
                bombComponent.damage = damage;
                // Arm the bomb after delay
                bombComponent.ArmBomb(bombArmDelay);
            }
            
            // Subscribe to bomb explosion event
            BombExplosionNotifier notifier = bomb.AddComponent<BombExplosionNotifier>();
            notifier.OnBombExploded += HandleBombExploded;
            
            // Parent to body part so it follows
            bomb.transform.SetParent(part.transform);
            
            // Reset local position and rotation to ensure proper alignment
            bomb.transform.localPosition = Vector3.zero;
            bomb.transform.localRotation = Quaternion.identity;
            
            activeBombs.Add(bomb);
        }

        hasPlacedBombs = true;
        bombsExploded = 0; // Reset counter
        Debug.Log($"BombPlacementAbility: Placed {activeBombs.Count} bombs at level {currentLevel} with spacing {spacing}!");
    }

    /// <summary>
    /// Called when a bomb explodes
    /// </summary>
    private void HandleBombExploded()
    {
        bombsExploded++;
        Debug.Log($"Bomb exploded! Total exploded: {bombsExploded}/{activeBombs.Count}");
        
        // If all bombs have exploded, start respawn timer
        if (bombsExploded >= activeBombs.Count)
        {
            StartRespawnTimer();
        }
    }
    
    /// <summary>
    /// Starts the respawn timer using cooldown from upgrade data
    /// </summary>
    private void StartRespawnTimer()
    {
        // Get cooldown from upgrade data, or use default
        float cooldown = GetRespawnCooldown();
        
        Debug.Log($"All bombs exploded! Respawning in {cooldown} seconds.");
        
        // Clear the exploded bombs list
        activeBombs.Clear();
        bombsExploded = 0;
        
        // Start the respawn timer
        respawnTimer = cooldown;
        isWaitingToRespawn = true;
    }
    
    /// <summary>
    /// Gets the respawn cooldown from upgrade data or returns default
    /// </summary>
    public float GetRespawnCooldown()
    {
        if (upgradeData != null)
        {
            return upgradeData.GetCooldown(currentLevel);
        }
        return defaultRespawnCooldown;
    }
    
    /// <summary>
    /// Gets the current respawn timer value (for UI display)
    /// </summary>
    public float GetRespawnTimer() => respawnTimer;
    
    /// <summary>
    /// Returns true if bombs are waiting to respawn
    /// </summary>
    public bool IsWaitingToRespawn() => isWaitingToRespawn;

    /// <summary>
    /// Levels up the ability and places bombs more densely
    /// </summary>
    public override bool LevelUp()
    {
        if (currentLevel >= maxLevel)
        {
            Debug.Log($"{GetType().Name} already at max level!");
            return false;
        }
        
        currentLevel++;
        Debug.Log($"{GetType().Name} leveled up to {currentLevel}!");
        
        // Clear existing bombs and reposition with new spacing
        ClearBombs();
        hasPlacedBombs = false;
        PlaceBombs();
        
        return true;
    }

    /// <summary>
    /// Clears all active bombs
    /// </summary>
    public void ClearBombs()
    {
        foreach (GameObject bomb in activeBombs)
        {
            if (bomb != null)
            {
                // Unsubscribe before destroying
                BombExplosionNotifier notifier = bomb.GetComponent<BombExplosionNotifier>();
                if (notifier != null)
                {
                    notifier.OnBombExploded -= HandleBombExploded;
                }
                Destroy(bomb);
            }
        }
        activeBombs.Clear();
    }

    private void OnDestroy()
    {
        ClearBombs();
    }
    
    /// <summary>
    /// Override SetFrozen to also pause the respawn timer
    /// </summary>
    public override void SetFrozen(bool frozen)
    {
        base.SetFrozen(frozen);
        // Timer is handled in Update which already checks isFrozen
    }
}

/// <summary>
/// Helper component to notify when a bomb explodes
/// </summary>
public class BombExplosionNotifier : MonoBehaviour
{
    public System.Action OnBombExploded;
    
    private void OnDestroy()
    {
        // When the bomb is destroyed (exploded), notify listeners
        OnBombExploded?.Invoke();
    }
}