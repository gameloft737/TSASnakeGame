using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Places bombs on every other body part (excluding first and last 3)
/// </summary>
public class BombPlacementAbility : BaseAbility
{
    [Header("Bomb Settings")]
    [SerializeField] private GameObject bombPrefab;
    
    [Header("References")]
    [SerializeField] private SnakeBody snakeBody;
    public float damage;
    
    private List<GameObject> activeBombs = new List<GameObject>();
    private bool hasPlacedBombs = false;
    private int bombsExploded = 0;

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
        // Override to prevent duration countdown - bombs control lifetime
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
            
            // Instantiate bomb at body part position
            GameObject bomb = Instantiate(bombPrefab, part.transform.position, Quaternion.identity);
            Bomb bombComponent = bomb.GetComponent<Bomb>();
            bombComponent.damage = damage;
            
            // Subscribe to bomb explosion event
            BombExplosionNotifier notifier = bomb.AddComponent<BombExplosionNotifier>();
            notifier.OnBombExploded += HandleBombExploded;
            
            // Parent to body part so it follows
            bomb.transform.SetParent(part.transform);
            
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
        
        // If all bombs have exploded, remove the ability
        if (bombsExploded >= activeBombs.Count)
        {
            Debug.Log("All bombs exploded! Removing ability.");
            RemoveAbility();
        }
    }

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

    /// <summary>
    /// Removes the ability from the game
    /// </summary>
    private void RemoveAbility()
    {
        ClearBombs();
        Destroy(this.gameObject);
    }

    private void OnDestroy()
    {
        ClearBombs();
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