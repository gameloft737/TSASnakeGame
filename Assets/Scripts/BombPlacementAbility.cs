using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Places bombs on every other body part (excluding first and last 3)
/// Bombs respawn after a cooldown period when all bombs have exploded
/// </summary>
public class BombPlacementAbility : BaseAbility
{
    [Header("Bomb Settings")]
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private float defaultRespawnCooldown = 10f; // Default cooldown if no upgrade data
    
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

        // Need at least 7 parts (3 + 1 + 3) to place any bombs
        if (totalParts < 7)
        {
            Debug.Log("BombPlacementAbility: Not enough body parts to place bombs!");
            return;
        }

        // Calculate starting position based on level (higher level = closer to head)
        // Level 1: Start from 4th from last
        // Level 2: Start from 5th from last (closer to head)
        // Level 3: Start from 6th from last (even closer to head)
        int startIndex = totalParts - (3 + currentLevel); // Dynamically adjust based on level
        int endIndex = 3; // Stop before first 3 (tail area)

        // Place bombs with gap of 3 between them (every 4th segment)
        for (int i = startIndex; i > endIndex; i -= 4)
        {
            BodyPart part = bodyParts[i];
            
            // Instantiate bomb at body part position with matching rotation
            GameObject bomb = Instantiate(bombPrefab, part.transform.position, part.transform.rotation);
            Bomb bombComponent = bomb.GetComponent<Bomb>();
            bombComponent.damage = damage;
            
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
        Debug.Log($"BombPlacementAbility: Placed {activeBombs.Count} bombs at level {currentLevel}!");
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
    /// Levels up the ability and repositions bombs closer to head
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
        
        // Clear existing bombs and reposition
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