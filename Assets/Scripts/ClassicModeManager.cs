
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Manages switching between the normal 3D snake game and classic 2D top-down snake mode.
/// Toggle with right-click (or configured input).
/// 
/// SETUP INSTRUCTIONS:
/// 1. Create an empty GameObject in your scene and name it "ClassicModeManager"
/// 2. Add this script to that GameObject
/// 3. The script will automatically find references to PlayerMovement, SnakeBody, CameraManager, etc.
/// 4. Optionally assign custom prefabs for classic apples and body segments
/// 5. Adjust grid settings (size, width, height, origin) as needed
/// 6. Right-click to toggle between normal and classic mode during gameplay
/// </summary>
public class ClassicModeManager : MonoBehaviour
{
    [Header("Mode Settings")]
    [SerializeField] private bool startInClassicMode = false;
    
    [Header("Grid Settings")]
    [Tooltip("Size of each grid cell in world units")]
    [SerializeField] private float gridCellSize = 1f;
    [Tooltip("Number of cells wide")]
    [SerializeField] private int gridWidth = 20;
    [Tooltip("Number of cells tall")]
    [SerializeField] private int gridHeight = 20;
    [Tooltip("World position of the grid's bottom-left corner")]
    [SerializeField] private Vector3 gridOrigin = Vector3.zero;
    
    [Header("Classic Mode Settings")]
    [Tooltip("Time between snake movements (lower = faster)")]
    [SerializeField] private float classicMoveInterval = 0.15f;
    [Tooltip("Starting length of the snake in classic mode")]
    [SerializeField] private int initialSnakeLength = 3;
    [Tooltip("Number of apples on screen at once")]
    [SerializeField] private int applesInClassicMode = 3;
    
    [Header("Camera References")]
    [SerializeField] private CinemachineCamera topDownCamera;
    [SerializeField] private CameraManager cameraManager;
    
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private SnakeBody snakeBody;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private Transform snakeHead;
    
    [Header("Classic Mode Prefabs (Optional)")]
    [Tooltip("Custom prefab for classic mode apples. If null, a simple red sphere is created.")]
    [SerializeField] private GameObject classicApplePrefab;
    [Tooltip("Custom prefab for classic mode body segments. If null, a simple cube is created.")]
    [SerializeField] private GameObject classicBodySegmentPrefab;
    [Tooltip("Custom prefab for grid visualization. If null, line renderers are used.")]
    [SerializeField] private GameObject gridVisualizerPrefab;
    
    // State
    private bool isClassicMode = false;
    private bool isTransitioning = false;
    
    // Classic mode components
    private ClassicSnakeController classicController;
    private List<GameObject> classicApples = new List<GameObject>();
    private GameObject gridVisualizer;
    
    // Cached states for restoration
    private Vector3 savedSnakePosition;
    private Quaternion savedSnakeRotation;
    private bool wasSpawningActive;
    
    // Input
    private PlayerControls playerControls;
    
    public static ClassicModeManager Instance { get; private set; }
    
    // Events
    public System.Action<bool> OnModeChanged;
    
    // Properties
    public bool IsClassicMode => isClassicMode;
    public float GridCellSize => gridCellSize;
    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public Vector3 GridOrigin => gridOrigin;
    public GameObject ClassicBodySegmentPrefab => classicBodySegmentPrefab;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        playerControls = new PlayerControls();
    }
    
    private void OnEnable()
    {
        playerControls.Enable();
        playerControls.Snake.Aim.performed += OnToggleClassicMode;
    }
    
    private void OnDisable()
    {
        playerControls.Snake.Aim.performed -= OnToggleClassicMode;
        playerControls.Disable();
    }
    
    private void Start()
    {
        FindReferences();
        
        classicController = GetComponent<ClassicSnakeController>();
        if (classicController == null)
        {
            classicController = gameObject.AddComponent<ClassicSnakeController>();
        }
        classicController.enabled = false;
        
        if (topDownCamera == null)
        {
            CreateTopDownCamera();
        }
        
        if (startInClassicMode)
        {
            EnterClassicMode();
        }
    }
    
    private void FindReferences()
    {
        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<CameraManager>();
        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (snakeBody == null)
            snakeBody = FindFirstObjectByType<SnakeBody>();
        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (snakeHead == null && snakeBody != null)
            snakeHead = snakeBody.transform;
    }
    
    private void CreateTopDownCamera()
    {
        GameObject camObj = new GameObject("ClassicMode_TopDownCamera");
        topDownCamera = camObj.AddComponent<CinemachineCamera>();
        
        Vector3 gridCenter = gridOrigin + new Vector3(gridWidth * gridCellSize / 2f, 0, gridHeight * gridCellSize / 2f);
        float cameraHeight = Mathf.Max(gridWidth, gridHeight) * gridCellSize * 0.8f;
        
        camObj.transform.position = gridCenter + Vector3.up * cameraHeight;
        camObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        
        topDownCamera.Priority = 0;
        
        Debug.Log($"[ClassicModeManager] Created top-down camera at height {cameraHeight}");
    }
    
    private void OnToggleClassicMode(InputAction.CallbackContext context)
    {
        if (isTransitioning) return;
        ToggleClassicMode();
    }
    
    public void ToggleClassicMode()
    {
        if (isTransitioning) return;
        
        if (isClassicMode)
            ExitClassicMode();
        else
            EnterClassicMode();
    }
    
    public void EnterClassicMode()
    {
        if (isClassicMode || isTransitioning) return;
        
        isTransitioning = true;
        Debug.Log("[ClassicModeManager] Entering Classic Mode");
        
        SaveCurrentState();
        DisableNormalMode();
        SetupClassicMode();
        SwitchToTopDownCamera();
        
        isClassicMode = true;
        isTransitioning = false;
        
        OnModeChanged?.Invoke(true);
    }
    
    public void ExitClassicMode()
    {
        if (!isClassicMode || isTransitioning) return;
        
        isTransitioning = true;
        Debug.Log("[ClassicModeManager] Exiting Classic Mode");
        
        CleanupClassicMode();
        RestoreNormalMode();
        SwitchToNormalCamera();
        
        isClassicMode = false;
        isTransitioning = false;
        
        OnModeChanged?.Invoke(false);
    }
    
    private void SaveCurrentState()
    {
        if (snakeHead != null)
        {
            savedSnakePosition = snakeHead.position;
            savedSnakeRotation = snakeHead.rotation;
        }
        
        wasSpawningActive = enemySpawner != null && enemySpawner.IsSpawning();
    }
    
    private void DisableNormalMode()
    {
        if (playerMovement != null)
            playerMovement.SetFrozen(true);
        
        if (enemySpawner != null)
            enemySpawner.StopSpawning();
        
        AppleEnemy[] enemies = FindObjectsByType<AppleEnemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
            enemy.SetFrozen(true);
        
        if (snakeBody != null)
        {
            foreach (var part in snakeBody.bodyParts)
            {
                if (part != null)
                    part.gameObject.SetActive(false);
            }
        }
    }
    
    private void SetupClassicMode()
    {
        Vector3 startPos = GetGridWorldPosition(gridWidth / 2, gridHeight / 2);
        if (snakeHead != null)
        {
            snakeHead.position = startPos;
            snakeHead.rotation = Quaternion.identity;
        }
        
        if (classicController != null)
        {
            classicController.Initialize(this, snakeHead, gridCellSize, classicMoveInterval, initialSnakeLength);
            classicController.enabled = true;
        }
        
        CreateGridVisualizer();
        SpawnClassicApples();
    }
    
    private void CleanupClassicMode()
    {
        if (classicController != null)
        {
            classicController.enabled = false;
            classicController.Cleanup();
        }
        
        foreach (var apple in classicApples)
        {
            if (apple != null)
                Destroy(apple);
        }
        classicApples.Clear();
        
        if (gridVisualizer != null)
        {
            Destroy(gridVisualizer);
            gridVisualizer = null;
        }
    }
    
    private void RestoreNormalMode()
    {
        if (snakeHead != null)
        {
            snakeHead.position = savedSnakePosition;
            snakeHead.rotation = savedSnakeRotation;
        }
        
        if (snakeBody != null)
        {
            foreach (var part in snakeBody.bodyParts)
            {
                if (part != null)
                    part.gameObject.SetActive(true);
            }
        }
        
        if (playerMovement != null)
            playerMovement.SetFrozen(false);
        
        AppleEnemy[] enemies = FindObjectsByType<AppleEnemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
            enemy.SetFrozen(false);
        
        if (wasSpawningActive && enemySpawner != null)
            enemySpawner.ResumeSpawning();
    }
    
    private void SwitchToTopDownCamera()
    {
        if (topDownCamera != null)
        {
            Vector3 gridCenter = gridOrigin + new Vector3(gridWidth * gridCellSize / 2f, 0, gridHeight * gridCellSize / 2f);
            float cameraHeight = Mathf.Max(gridWidth, gridHeight) * gridCellSize * 0.8f;
            
            topDownCamera.transform.position = gridCenter + Vector3.up * cameraHeight;
            topDownCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            topDownCamera.Priority = 100;
            
            if (cameraManager != null)
            {
                if (cameraManager.normalCam != null)
                    cameraManager.normalCam.Priority = 0;
                if (cameraManager.aimCam != null)
                    cameraManager.aimCam.Priority = 0;
                if (cameraManager.pauseCam != null)
                    cameraManager.pauseCam.Priority = 0;
            }
        }
    }
    
    private void SwitchToNormalCamera()
    {
        if (topDownCamera != null)
            topDownCamera.Priority = 0;
        
        if (cameraManager != null)
            cameraManager.SwitchToNormalCamera();
    }
    
    private void CreateGridVisualizer()
    {
        if (gridVisualizerPrefab != null)
        {
            gridVisualizer = Instantiate(gridVisualizerPrefab, gridOrigin, Quaternion.identity);
        }
        else
        {
            gridVisualizer = new GameObject("GridVisualizer");
            gridVisualizer.transform.position = gridOrigin;
            
            for (int x = 0; x <= gridWidth; x++)
            {
                CreateGridLine(
                    new Vector3(x * gridCellSize, 0.01f, 0),
                    new Vector3(x * gridCellSize, 0.01f, gridHeight * gridCellSize),
                    gridVisualizer.transform
                );
            }
            
            for (int z = 0; z <= gridHeight; z++)
            {
                CreateGridLine(
                    new Vector3(0, 0.01f, z * gridCellSize),
                    new Vector3(gridWidth * gridCellSize, 0.01f, z * gridCellSize),
                    gridVisualizer.transform
                );
            }
        }
    }
    
    private void CreateGridLine(Vector3 start, Vector3 end, Transform parent)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.SetParent(parent);
        lineObj.transform.localPosition = Vector3.zero;
        
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, gridOrigin + start);
        lr.SetPosition(1, gridOrigin + end);
        lr.startWidth = 0.02f;
        lr.endWidth = 0.02f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        lr.endColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    }
    
    private void SpawnClassicApples()
    {
        for (int i = 0; i < applesInClassicMode; i++)
        {
            SpawnClassicApple();
        }
    }
    
    public void SpawnClassicApple()
    {
        int attempts = 0;
        int maxAttempts = 100;
        
        while (attempts < maxAttempts)
        {
            int x = Random.Range(0, gridWidth);
            int z = Random.Range(0, gridHeight);
            Vector3 pos = GetGridWorldPosition(x, z);
            
            if (!IsPositionOccupied(pos))
            {
                GameObject apple;
                if (classicApplePrefab != null)
                {
                    apple = Instantiate(classicApplePrefab, pos, Quaternion.identity);
                }
                else
                {
                    apple = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    apple.name = "ClassicApple";
                    apple.transform.position = pos;
                    apple.transform.localScale = Vector3.one * gridCellSize * 0.8f;
                    
                    Renderer renderer = apple.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = new Material(Shader.Find("Standard"));
                        renderer.material.color = Color.red;
                    }
                    
                    // Add classic apple component
                    ClassicApple classicAppleComp = apple.AddComponent<ClassicApple>();
                    classicAppleComp.Initialize(this);
                }
                
                classicApples.Add(apple);
                return;
            }
            
            attempts++;
        }
        
        Debug.LogWarning("[ClassicModeManager] Could not find empty position for apple");
    }
    
    public void OnAppleCollected(GameObject apple)
    {
        classicApples.Remove(apple);
        Destroy(apple);
        
        // Grow the snake
        if (classicController != null)
        {
            classicController.Grow();
        }
        
        // Spawn a new apple
        SpawnClassicApple();
    }
    
    public Vector3 GetGridWorldPosition(int gridX, int gridZ)
    {
        return gridOrigin + new Vector3(
            gridX * gridCellSize + gridCellSize / 2f,
            0.5f,
            gridZ * gridCellSize + gridCellSize / 2f
        );
    }
    
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - gridOrigin;
        int x = Mathf.FloorToInt(localPos.x / gridCellSize);
        int z = Mathf.FloorToInt(localPos.z / gridCellSize);
        return new Vector2Int(
            Mathf.Clamp(x, 0, gridWidth - 1),
            Mathf.Clamp(z, 0, gridHeight - 1)
        );
    }
    
    public bool IsPositionInGrid(int gridX, int gridZ)
    {
        return gridX >= 0 && gridX < gridWidth && gridZ >= 0 && gridZ < gridHeight;
    }
    
    public bool IsPositionOccupied(Vector3 worldPos)
    {
        // Check if snake head is at this position
        if (snakeHead != null)
        {
            float dist = Vector3.Distance(worldPos, snakeHead.position);
            if (dist < gridCellSize * 0.5f)
                return true;
        }
        
        // Check classic snake body segments
        if (classicController != null)
        {
            foreach (var segment in classicController.GetBodySegments())
            {
                if (segment != null)
                {
                    float dist = Vector3.Distance(worldPos, segment.transform.position);
                    if (dist < gridCellSize * 0.5f)
                        return true;
                }
            }
        }
        
        // Check apples
        foreach (var apple in classicApples)
        {
            if (apple != null)
            {
                float dist = Vector3.Distance(worldPos, apple.transform.position);
                if (dist < gridCellSize * 0.5f)
                    return true;
            }
        }
        
        return false;
    }
    
    public void OnSnakeCollision()
    {
        Debug.Log("[ClassicModeManager] Snake collision! Game Over in classic mode.");
        // You can add game over logic here
        // For now, just exit classic mode
        ExitClassicMode();
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw grid bounds
        Gizmos.color = Color.cyan;
        Vector3 size = new Vector3(gridWidth * gridCellSize, 0.1f, gridHeight * gridCellSize);
        Vector3 center = gridOrigin + size / 2f;
        center.y = 0.05f;
        Gizmos.DrawWireCube(center, size);
        
        // Draw grid origin
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(gridOrigin, 0.2f);
    }
}