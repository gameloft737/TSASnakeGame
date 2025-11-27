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

    private void Start()
    {
        // Get SnakeBody if not assigned
        if (snakeBody == null)
        {
            snakeBody = GetComponentInParent<SnakeBody>();
        }
    }

    private void PlaceBombs()
    {
        if (snakeBody == null)
        {
            Debug.LogWarning("BombPlacementAbility: SnakeBody not found!");
            return;
        }

        List<BodyPart> bodyParts = snakeBody.bodyParts;
        int totalParts = bodyParts.Count;

        // Need at least 7 parts (3 + 1 + 3) to place any bombs
        if (totalParts < 7)
        {
            Debug.Log("BombPlacementAbility: Not enough body parts to place bombs!");
            return;
        }

        // Last index is first part (neck), index 0 is tail
        // Exclude last 3 (neck area) and first 3 (tail area)
        // Start at index 1 (second from tail), place with gap of 1
        int startIndex = totalParts - 4; // Start from 4th from last (excluding last 3)
        int endIndex = 3; // Stop before first 3

        // Place bombs with gap of 1 between them
        for (int i = startIndex; i > endIndex; i -= 4)
        {
            BodyPart part = bodyParts[i];
            
            // Instantiate bomb at body part position
            GameObject bomb = Instantiate(bombPrefab, part.transform.position, Quaternion.identity);
            bomb.GetComponent<Bomb>().damage = damage;
            // Parent to body part so it follows
            bomb.transform.SetParent(part.transform);
            
            activeBombs.Add(bomb);
        }

        Debug.Log($"BombPlacementAbility: Placed {activeBombs.Count} bombs!");
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
                Destroy(bomb);
            }
        }
        activeBombs.Clear();
    }

    private void OnDestroy()
    {
        ClearBombs();
    }
}