using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Controls the snake movement in classic grid-based mode.
/// Handles WASD/Arrow key input for direction changes and timed grid movement.
/// </summary>
public class ClassicSnakeController : MonoBehaviour
{
    // References
    private ClassicModeManager modeManager;
    private Transform snakeHead;
    private PlayerControls playerControls;
    
    // Grid settings
    private float gridCellSize = 1f;
    private float moveInterval = 0.15f;
    
    // Movement state
    private Vector2Int currentDirection = Vector2Int.up; // Start moving up (positive Z)
    private Vector2Int nextDirection = Vector2Int.up;
    private Vector2Int gridPosition;
    private float moveTimer = 0f;
    private bool isInitialized = false;
    
    // Body segments
    private List<GameObject> bodySegments = new List<GameObject>();
    private List<Vector2Int> bodyPositions = new List<Vector2Int>();
    private int pendingGrowth = 0;
    
    // Visual settings
    private Color headColor = new Color(0.2f, 0.8f, 0.2f); // Green
    private Color bodyColor = new Color(0.3f, 0.6f, 0.3f); // Darker green
    
    public void Initialize(ClassicModeManager manager, Transform head, float cellSize, float interval, int initialLength = 3)
    {
        modeManager = manager;
        snakeHead = head;
        gridCellSize = cellSize;
        moveInterval = interval;
        
        // Convert head position to grid position
        if (modeManager != null)
        {
            gridPosition = modeManager.WorldToGridPosition(snakeHead.position);
        }
        else
        {
            gridPosition = new Vector2Int(10, 10); // Default center
        }
        
        // Reset direction
        currentDirection = Vector2Int.up;
        nextDirection = Vector2Int.up;
        moveTimer = 0f;
        
        // Setup input
        if (playerControls == null)
        {
            playerControls = new PlayerControls();
        }
        playerControls.Enable();
        
        // Create initial body segments
        CreateInitialBody(initialLength);
        
        // Update head visual
        UpdateHeadVisual();
        
        isInitialized = true;
        
        Debug.Log($"[ClassicSnakeController] Initialized at grid position {gridPosition}");
    }
    
    private void CreateInitialBody(int length)
    {
        // Clear any existing segments
        Cleanup();
        
        // Add positions for body (behind the head)
        for (int i = 1; i <= length; i++)
        {
            Vector2Int bodyPos = gridPosition - currentDirection * i;
            bodyPositions.Add(bodyPos);
            
            // Create visual segment
            GameObject segment = CreateBodySegment(bodyPos);
            bodySegments.Add(segment);
        }
    }
    
    private GameObject CreateBodySegment(Vector2Int gridPos)
    {
        GameObject segment;
        
        if (modeManager != null && modeManager.ClassicBodySegmentPrefab != null)
        {
            Vector3 worldPos = modeManager.GetGridWorldPosition(gridPos.x, gridPos.y);
            segment = Instantiate(modeManager.ClassicBodySegmentPrefab, worldPos, Quaternion.identity);
        }
        else
        {
            // Create a simple cube
            segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = "ClassicBodySegment";
            
            if (modeManager != null)
            {
                Vector3 worldPos = modeManager.GetGridWorldPosition(gridPos.x, gridPos.y);
                segment.transform.position = worldPos;
            }
            
            segment.transform.localScale = Vector3.one * gridCellSize * 0.9f;
            
            // Set color
            Renderer renderer = segment.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = bodyColor;
            }
        }
        
        return segment;
    }
    
    private void UpdateHeadVisual()
    {
        if (snakeHead == null) return;
        
        // Make head slightly larger and different color
        snakeHead.localScale = Vector3.one * gridCellSize * 0.95f;
        
        Renderer renderer = snakeHead.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = snakeHead.GetComponentInChildren<Renderer>();
        }
        
        if (renderer != null)
        {
            if (renderer.material == null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
            }
            renderer.material.color = headColor;
        }
    }
    
    private void OnEnable()
    {
        if (playerControls != null)
        {
            playerControls.Enable();
        }
    }
    
    private void OnDisable()
    {
        if (playerControls != null)
        {
            playerControls.Disable();
        }
    }
    
    private void Update()
    {
        if (!isInitialized || modeManager == null || !modeManager.IsClassicMode) return;
        
        HandleInput();
        
        moveTimer += Time.deltaTime;
        if (moveTimer >= moveInterval)
        {
            moveTimer = 0f;
            Move();
        }
    }
    
    private void HandleInput()
    {
        // Read movement input
        Vector2 input = playerControls.Snake.Movement.ReadValue<Vector2>();
        
        // Also check for WASD via keyboard
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                input.y = 1;
            else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                input.y = -1;
            
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                input.x = -1;
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                input.x = 1;
        }
        
        // Determine new direction (can't reverse)
        Vector2Int newDir = currentDirection;
        
        // Prioritize the axis with larger input
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            if (input.x > 0.1f) newDir = Vector2Int.right;
            else if (input.x < -0.1f) newDir = Vector2Int.left;
        }
        else if (Mathf.Abs(input.y) > 0.1f)
        {
            if (input.y > 0.1f) newDir = Vector2Int.up;
            else if (input.y < -0.1f) newDir = Vector2Int.down;
        }
        
        // Prevent reversing direction (can't go back on yourself)
        if (newDir + currentDirection != Vector2Int.zero)
        {
            nextDirection = newDir;
        }
    }
    
    private void Move()
    {
        // Apply the queued direction
        currentDirection = nextDirection;
        
        // Calculate new head position
        Vector2Int newHeadPos = gridPosition + currentDirection;
        
        // Check for wall collision (wrap around or game over)
        if (!modeManager.IsPositionInGrid(newHeadPos.x, newHeadPos.y))
        {
            // Wrap around
            if (newHeadPos.x < 0) newHeadPos.x = modeManager.GridWidth - 1;
            else if (newHeadPos.x >= modeManager.GridWidth) newHeadPos.x = 0;
            
            if (newHeadPos.y < 0) newHeadPos.y = modeManager.GridHeight - 1;
            else if (newHeadPos.y >= modeManager.GridHeight) newHeadPos.y = 0;
        }
        
        // Check for self collision
        if (IsPositionOnBody(newHeadPos))
        {
            modeManager.OnSnakeCollision();
            return;
        }
        
        // Store old head position for body to follow
        Vector2Int oldHeadPos = gridPosition;
        
        // Move head
        gridPosition = newHeadPos;
        Vector3 newWorldPos = modeManager.GetGridWorldPosition(gridPosition.x, gridPosition.y);
        snakeHead.position = newWorldPos;
        
        // Rotate head to face direction
        if (currentDirection != Vector2Int.zero)
        {
            Vector3 dir3D = new Vector3(currentDirection.x, 0, currentDirection.y);
            snakeHead.rotation = Quaternion.LookRotation(dir3D);
        }
        
        // Move body segments
        MoveBody(oldHeadPos);
        
        // Check for apple collision
        CheckAppleCollision();
    }
    
    private void MoveBody(Vector2Int newFirstPosition)
    {
        if (bodyPositions.Count == 0) return;
        
        // If we're growing, add a new segment instead of moving the tail
        if (pendingGrowth > 0)
        {
            pendingGrowth--;
            
            // Insert new position at the front
            bodyPositions.Insert(0, newFirstPosition);
            
            // Create new segment
            GameObject newSegment = CreateBodySegment(newFirstPosition);
            bodySegments.Insert(0, newSegment);
        }
        else
        {
            // Move each position forward (tail takes position of segment in front)
            for (int i = bodyPositions.Count - 1; i > 0; i--)
            {
                bodyPositions[i] = bodyPositions[i - 1];
            }
            bodyPositions[0] = newFirstPosition;
        }
        
        // Update visual positions
        for (int i = 0; i < bodySegments.Count && i < bodyPositions.Count; i++)
        {
            if (bodySegments[i] != null)
            {
                Vector3 worldPos = modeManager.GetGridWorldPosition(bodyPositions[i].x, bodyPositions[i].y);
                bodySegments[i].transform.position = worldPos;
            }
        }
    }
    
    private bool IsPositionOnBody(Vector2Int pos)
    {
        foreach (var bodyPos in bodyPositions)
        {
            if (bodyPos == pos)
                return true;
        }
        return false;
    }
    
    private void CheckAppleCollision()
    {
        Vector3 headWorldPos = modeManager.GetGridWorldPosition(gridPosition.x, gridPosition.y);
        
        // Find nearby apples
        ClassicApple[] apples = FindObjectsByType<ClassicApple>(FindObjectsSortMode.None);
        foreach (var apple in apples)
        {
            if (apple != null)
            {
                float dist = Vector3.Distance(headWorldPos, apple.transform.position);
                if (dist < gridCellSize * 0.8f)
                {
                    apple.Collect();
                    break;
                }
            }
        }
    }
    
    public void Grow(int amount = 1)
    {
        pendingGrowth += amount;
        Debug.Log($"[ClassicSnakeController] Snake will grow by {amount}. Pending growth: {pendingGrowth}");
    }
    
    public List<GameObject> GetBodySegments()
    {
        return bodySegments;
    }
    
    public int GetLength()
    {
        return 1 + bodySegments.Count; // Head + body
    }
    
    public void Cleanup()
    {
        // Destroy all body segments
        foreach (var segment in bodySegments)
        {
            if (segment != null)
            {
                Destroy(segment);
            }
        }
        bodySegments.Clear();
        bodyPositions.Clear();
        pendingGrowth = 0;
        isInitialized = false;
    }
    
    private void OnDestroy()
    {
        Cleanup();
        if (playerControls != null)
        {
            playerControls.Disable();
            playerControls.Dispose();
        }
    }
}