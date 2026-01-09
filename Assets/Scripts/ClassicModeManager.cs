
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Manages switching between the normal 3D snake game and classic 2D top-down snake mode.
/// Toggle with Tab key (configurable).
///
/// SETUP INSTRUCTIONS:
/// 1. Create an empty GameObject in your scene and name it "ClassicModeManager"
/// 2. Add this script to that GameObject
/// 3. Create a Cinemachine Camera for top-down view (see README for camera setup)
/// 4. Assign the top-down camera to the "Top Down Camera" field
/// 5. Press Tab to toggle between normal and classic mode during gameplay
///
/// CAMERA SETUP (do this in Unity Editor):
/// 1. GameObject > Cinemachine > Cinemachine Camera
/// 2. Name it "TopDownCamera"
/// 3. Set Position to (0, 50, 0) and Rotation to (90, 0, 0)
/// 4. Add CinemachineFollow component
/// 5. Set Tracking Target to your snake head
/// 6. Set Follow Offset to (0, 50, 0)
/// 7. In Tracker Settings, set Binding Mode to "World Space"
/// 8. Assign this camera to ClassicModeManager's "Top Down Camera" field
/// </summary>
public class ClassicModeManager : MonoBehaviour
{
    [Header("Mode Settings")]
    [SerializeField] private bool startInClassicMode = false;
    
    [Header("Grid Settings")]
    [Tooltip("Size of each grid cell in world units")]
    [SerializeField] private float gridCellSize = 1f;
    [Tooltip("Time between snake movements (lower = faster)")]
    [SerializeField] private float classicMoveInterval = 0.15f;
    [Tooltip("Time between enemy movements (lower = faster)")]
    [SerializeField] private float enemyMoveInterval = 0.3f;
    
    [Header("Camera References (REQUIRED - Create manually in Editor)")]
    [Tooltip("Assign your top-down Cinemachine camera here")]
    [SerializeField] private CinemachineCamera topDownCamera;
    [SerializeField] private CameraManager cameraManager;
    
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private Transform snakeHead;
    
    [Header("Visual Settings")]
    [Tooltip("Show grid lines in classic mode")]
    [SerializeField] private bool showGridLines = true;
    [SerializeField] private Color gridLineColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private float gridLineWidth = 0.02f;
    [SerializeField] private int gridVisibleRadius = 15; // How many cells around the snake to show
    
    [Header("Toggle Key")]
    [Tooltip("Key to toggle classic mode (default: Tab)")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
    
    // State
    private bool isClassicMode = false;
    private bool isTransitioning = false;
    private bool isMenuOpen = false; // Track if a menu is currently open
    
    // Grid visualization
    private GameObject gridVisualizer;
    private List<LineRenderer> gridLines = new List<LineRenderer>();
    
    // Cached states for restoration
    private Vector3 savedSnakePosition;
    private Quaternion savedSnakeRotation;
    private bool wasSpawningActive;
    
    // Input
    private PlayerControls playerControls;
    
    // Cached enemies for mode switching
    private List<AppleEnemy> cachedEnemies = new List<AppleEnemy>();
    
    public static ClassicModeManager Instance { get; private set; }
    
    // Events
    public System.Action<bool> OnModeChanged;
    
    // Properties
    public bool IsClassicMode => isClassicMode;
    public float GridCellSize => gridCellSize;
    public float ClassicMoveInterval => classicMoveInterval;
    
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
        // Don't use Aim input - it conflicts with camera toggle
        // We'll use keyboard input in Update instead
    }
    
    private void OnDisable()
    {
        playerControls.Disable();
    }
    
    private void Start()
    {
        FindReferences();
        
        if (topDownCamera == null)
        {
            Debug.LogError("[ClassicModeManager] Top Down Camera is not assigned! Please create a Cinemachine camera and assign it.");
            Debug.LogError("[ClassicModeManager] See the script header comments for camera setup instructions.");
        }
        else
        {
            // Make sure camera starts inactive
            topDownCamera.Priority = 0;
        }
        
        if (startInClassicMode)
        {
            EnterClassicMode();
        }
    }
    
    private void Update()
    {
        // Check for toggle key press (only when not in menu and not transitioning)
        if (Input.GetKeyDown(toggleKey) && !isMenuOpen && !isTransitioning)
        {
            ToggleClassicMode();
        }
        
        if (isClassicMode && showGridLines && gridVisualizer != null)
        {
            UpdateGridVisualization();
        }
    }
    
    private void FindReferences()
    {
        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<CameraManager>();
        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (snakeHead == null && playerMovement != null)
            snakeHead = playerMovement.transform;
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
        EnableClassicModeOnComponents();
        SwitchToTopDownCamera();
        
        if (showGridLines)
        {
            CreateGridVisualizer();
        }
        
        isClassicMode = true;
        isTransitioning = false;
        
        OnModeChanged?.Invoke(true);
    }
    
    public void ExitClassicMode()
    {
        if (!isClassicMode || isTransitioning) return;
        
        isTransitioning = true;
        Debug.Log("[ClassicModeManager] Exiting Classic Mode");
        
        DisableClassicModeOnComponents();
        SwitchToNormalCamera();
        CleanupGridVisualizer();
        
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
    
    private void EnableClassicModeOnComponents()
    {
        // Enable classic mode on player movement
        // The body segments will naturally follow the head since they already do that
        if (playerMovement != null)
        {
            playerMovement.SetClassicMode(true, gridCellSize, classicMoveInterval);
        }
        
        // Enable classic mode on all existing apple enemies
        AppleEnemy[] enemies = FindObjectsByType<AppleEnemy>(FindObjectsSortMode.None);
        cachedEnemies.Clear();
        foreach (var enemy in enemies)
        {
            if (enemy != null && !enemy.IsFrozen())
            {
                enemy.SetClassicMode(true, gridCellSize, enemyMoveInterval);
                cachedEnemies.Add(enemy);
            }
        }
        
        Debug.Log($"[ClassicModeManager] Enabled classic mode on {cachedEnemies.Count} enemies");
        
        // Freeze camera manager input
        if (cameraManager != null)
        {
            cameraManager.SetFrozen(true);
        }
    }
    
    private void DisableClassicModeOnComponents()
    {
        // Disable classic mode on player movement
        if (playerMovement != null)
        {
            playerMovement.SetClassicMode(false);
        }
        
        // Disable classic mode on all apple enemies
        AppleEnemy[] enemies = FindObjectsByType<AppleEnemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.SetClassicMode(false);
            }
        }
        
        // Unfreeze camera manager input
        if (cameraManager != null)
        {
            cameraManager.SetFrozen(false);
        }
    }
    
    private void SwitchToTopDownCamera()
    {
        if (topDownCamera != null)
        {
            topDownCamera.Priority = 100;
            
            if (cameraManager != null)
            {
                if (cameraManager.normalCam != null)
                    cameraManager.normalCam.Priority = 0;
                if (cameraManager.pauseCam != null)
                    cameraManager.pauseCam.Priority = 0;
            }
        }
        else
        {
            Debug.LogError("[ClassicModeManager] Cannot switch to top-down camera - it's not assigned!");
        }
    }
    
    private void SwitchToNormalCamera()
    {
        if (topDownCamera != null)
            topDownCamera.Priority = 0;
        
        if (cameraManager != null)
            cameraManager.SwitchToNormalCamera();
    }
    
    /// <summary>
    /// Called when a menu opens (drop collection, attack selection, etc.)
    /// Temporarily pauses classic mode updates but keeps the mode active
    /// </summary>
    public void OnMenuOpened()
    {
        isMenuOpen = true;
        
        // Lower top-down camera priority so pause camera can take over
        if (topDownCamera != null)
        {
            topDownCamera.Priority = 0;
        }
        
        Debug.Log("[ClassicModeManager] Menu opened - lowered top-down camera priority");
    }
    
    /// <summary>
    /// Called when a menu closes
    /// Restores the appropriate camera based on whether we're in classic mode
    /// </summary>
    public void OnMenuClosed()
    {
        isMenuOpen = false;
        
        // Restore the correct camera
        if (isClassicMode)
        {
            // Switch back to top-down camera
            SwitchToTopDownCamera();
            Debug.Log("[ClassicModeManager] Menu closed - restoring top-down camera");
        }
        else
        {
            // Switch back to normal camera when not in classic mode
            SwitchToNormalCamera();
            Debug.Log("[ClassicModeManager] Menu closed - restoring normal camera");
        }
    }
    
    /// <summary>
    /// Returns whether a menu is currently open
    /// </summary>
    public bool IsMenuOpen() => isMenuOpen;
    
    private void CreateGridVisualizer()
    {
        if (gridVisualizer != null) return;
        
        gridVisualizer = new GameObject("GridVisualizer");
        gridVisualizer.transform.position = Vector3.zero;
        
        // Create a pool of line renderers for the grid
        int totalLines = (gridVisibleRadius * 2 + 1) * 2; // Horizontal + Vertical lines
        
        for (int i = 0; i < totalLines; i++)
        {
            GameObject lineObj = new GameObject($"GridLine_{i}");
            lineObj.transform.SetParent(gridVisualizer.transform);
            
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = gridLineWidth;
            lr.endWidth = gridLineWidth;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = gridLineColor;
            lr.endColor = gridLineColor;
            lr.useWorldSpace = true;
            
            gridLines.Add(lr);
        }
    }
    
    private void UpdateGridVisualization()
    {
        if (snakeHead == null || gridLines.Count == 0) return;
        
        // Get snake's grid position
        int centerX = Mathf.RoundToInt(snakeHead.position.x / gridCellSize);
        int centerZ = Mathf.RoundToInt(snakeHead.position.z / gridCellSize);
        
        int lineIndex = 0;
        float gridExtent = gridVisibleRadius * gridCellSize;
        float y = 0.01f; // Slightly above ground
        
        // Vertical lines (along Z axis)
        for (int x = -gridVisibleRadius; x <= gridVisibleRadius && lineIndex < gridLines.Count; x++)
        {
            float worldX = (centerX + x) * gridCellSize;
            float startZ = (centerZ - gridVisibleRadius) * gridCellSize;
            float endZ = (centerZ + gridVisibleRadius) * gridCellSize;
            
            gridLines[lineIndex].SetPosition(0, new Vector3(worldX, y, startZ));
            gridLines[lineIndex].SetPosition(1, new Vector3(worldX, y, endZ));
            lineIndex++;
        }
        
        // Horizontal lines (along X axis)
        for (int z = -gridVisibleRadius; z <= gridVisibleRadius && lineIndex < gridLines.Count; z++)
        {
            float worldZ = (centerZ + z) * gridCellSize;
            float startX = (centerX - gridVisibleRadius) * gridCellSize;
            float endX = (centerX + gridVisibleRadius) * gridCellSize;
            
            gridLines[lineIndex].SetPosition(0, new Vector3(startX, y, worldZ));
            gridLines[lineIndex].SetPosition(1, new Vector3(endX, y, worldZ));
            lineIndex++;
        }
    }
    
    private void CleanupGridVisualizer()
    {
        if (gridVisualizer != null)
        {
            Destroy(gridVisualizer);
            gridVisualizer = null;
        }
        gridLines.Clear();
    }
    
    /// <summary>
    /// Registers a newly spawned enemy with classic mode if active
    /// </summary>
    public void RegisterEnemy(AppleEnemy enemy)
    {
        if (isClassicMode && enemy != null)
        {
            enemy.SetClassicMode(true, gridCellSize, enemyMoveInterval);
            cachedEnemies.Add(enemy);
        }
    }
    
    /// <summary>
    /// Unregisters an enemy (called when enemy dies)
    /// </summary>
    public void UnregisterEnemy(AppleEnemy enemy)
    {
        cachedEnemies.Remove(enemy);
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw grid cell size indicator
        Gizmos.color = Color.cyan;
        Vector3 center = snakeHead != null ? snakeHead.position : transform.position;
        center.y = 0.1f;
        
        // Draw a single cell
        Vector3 cellCenter = new Vector3(
            Mathf.Round(center.x / gridCellSize) * gridCellSize,
            0.1f,
            Mathf.Round(center.z / gridCellSize) * gridCellSize
        );
        
        Gizmos.DrawWireCube(cellCenter, new Vector3(gridCellSize, 0.1f, gridCellSize));
        
        // Draw visible radius
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        float radius = gridVisibleRadius * gridCellSize;
        Gizmos.DrawWireCube(cellCenter, new Vector3(radius * 2, 0.1f, radius * 2));
    }
}