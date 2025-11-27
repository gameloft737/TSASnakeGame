using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class EditorScreenShots : MonoBehaviour 
{
    [HideInInspector] public Camera targetCamera;
    [HideInInspector] public Transform startPosition;
    [HideInInspector] public Transform endPosition;
    [HideInInspector] public bool isScanning = false;
    [HideInInspector] public bool usePositionMarkers = false;
    [HideInInspector] public int gridWidth = 3;
    [HideInInspector] public int gridHeight = 3;
    [HideInInspector] public bool useTopDownView = true;
    
    void OnDrawGizmos()
    {
        if (targetCamera == null) return;
        
        Gizmos.color = new Color(0, 1, 0, 0.3f); // Green with transparency
        Vector3 scanCenter;
        float totalWidth;
        float totalHeight;
        
        if (usePositionMarkers && startPosition != null && endPosition != null)
        {
            // Draw scan area from position markers
            Vector3 start = startPosition.position;
            Vector3 end = endPosition.position;
            
            if (useTopDownView)
            {
                // For top-down: use X and Z axes
                totalWidth = Mathf.Abs(end.x - start.x);
                totalHeight = Mathf.Abs(end.z - start.z);
                
                scanCenter = new Vector3(
                    (start.x + end.x) / 2f,
                    targetCamera.transform.position.y,
                    (start.z + end.z) / 2f
                );
            }
            else
            {
                // For other views: use X and Y axes
                totalWidth = Mathf.Abs(end.x - start.x);
                totalHeight = Mathf.Abs(end.y - start.y);
                
                scanCenter = new Vector3(
                    (start.x + end.x) / 2f,
                    (start.y + end.y) / 2f,
                    targetCamera.transform.position.z
                );
            }
            
            // Draw corner markers
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(start, 0.5f);
            Gizmos.DrawWireSphere(end, 0.5f);
        }
        else
        {
            // Draw scan area based on camera view
            float tileHeight;
            float tileWidth;
            
            if (targetCamera.orthographic)
            {
                tileHeight = targetCamera.orthographicSize * 2f;
                tileWidth = tileHeight * targetCamera.aspect;
            }
            else
            {
                float distance = 10f; // Default distance for perspective
                float fovRad = targetCamera.fieldOfView * Mathf.Deg2Rad;
                tileHeight = 2f * distance * Mathf.Tan(fovRad / 2f);
                tileWidth = tileHeight * targetCamera.aspect;
            }
            
            totalWidth = tileWidth * gridWidth;
            totalHeight = tileHeight * gridHeight;
            scanCenter = targetCamera.transform.position;
        }
        
        // Draw outer boundary and grid based on view mode
        if (useTopDownView)
        {
            // Draw on XZ plane (horizontal ground plane)
            Gizmos.color = Color.green;
            Vector3 size = new Vector3(totalWidth, 0.1f, totalHeight);
            Gizmos.DrawWireCube(scanCenter, size);
            
            // Draw grid lines
            float tileW = totalWidth / gridWidth;
            float tileH = totalHeight / gridHeight;
            
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            
            // Lines parallel to Z axis
            for (int x = 1; x < gridWidth; x++)
            {
                float xPos = scanCenter.x - totalWidth / 2f + (x * tileW);
                Vector3 start = new Vector3(xPos, scanCenter.y, scanCenter.z - totalHeight / 2f);
                Vector3 end = new Vector3(xPos, scanCenter.y, scanCenter.z + totalHeight / 2f);
                Gizmos.DrawLine(start, end);
            }
            
            // Lines parallel to X axis
            for (int z = 1; z < gridHeight; z++)
            {
                float zPos = scanCenter.z - totalHeight / 2f + (z * tileH);
                Vector3 start = new Vector3(scanCenter.x - totalWidth / 2f, scanCenter.y, zPos);
                Vector3 end = new Vector3(scanCenter.x + totalWidth / 2f, scanCenter.y, zPos);
                Gizmos.DrawLine(start, end);
            }
            
            // Draw tile numbers
            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    float xPos = scanCenter.x - totalWidth / 2f + (x + 0.5f) * tileW;
                    float zPos = scanCenter.z - totalHeight / 2f + (y + 0.5f) * tileH;
                    Vector3 labelPos = new Vector3(xPos, scanCenter.y, zPos);
                    
                    int tileNum = y * gridWidth + x + 1;
                    UnityEditor.Handles.Label(labelPos, $"Tile {tileNum}", new GUIStyle()
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = new GUIStyleState() { textColor = Color.white }
                    });
                }
            }
            #endif
        }
        else
        {
            // Draw on XY plane (side/front view)
            Gizmos.color = Color.green;
            Vector3 size = new Vector3(totalWidth, totalHeight, 0.1f);
            Gizmos.DrawWireCube(scanCenter, size);
            
            // Draw grid lines
            float tileW = totalWidth / gridWidth;
            float tileH = totalHeight / gridHeight;
            
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            
            // Vertical lines
            for (int x = 1; x < gridWidth; x++)
            {
                float xPos = scanCenter.x - totalWidth / 2f + (x * tileW);
                Vector3 start = new Vector3(xPos, scanCenter.y - totalHeight / 2f, scanCenter.z);
                Vector3 end = new Vector3(xPos, scanCenter.y + totalHeight / 2f, scanCenter.z);
                Gizmos.DrawLine(start, end);
            }
            
            // Horizontal lines
            for (int y = 1; y < gridHeight; y++)
            {
                float yPos = scanCenter.y - totalHeight / 2f + (y * tileH);
                Vector3 start = new Vector3(scanCenter.x - totalWidth / 2f, yPos, scanCenter.z);
                Vector3 end = new Vector3(scanCenter.x + totalWidth / 2f, yPos, scanCenter.z);
                Gizmos.DrawLine(start, end);
            }
            
            // Draw tile numbers
            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    float xPos = scanCenter.x - totalWidth / 2f + (x + 0.5f) * tileW;
                    float yPos = scanCenter.y - totalHeight / 2f + (y + 0.5f) * tileH;
                    Vector3 labelPos = new Vector3(xPos, yPos, scanCenter.z);
                    
                    int tileNum = y * gridWidth + x + 1;
                    UnityEditor.Handles.Label(labelPos, $"Tile {tileNum}", new GUIStyle()
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = new GUIStyleState() { textColor = Color.white }
                    });
                }
            }
            #endif
        }
    }
}

[CustomEditor(typeof(EditorScreenShots))]
[CanEditMultipleObjects]
public class EditorScreenShotsEditor : Editor 
{
    public string textureName = "Minimap_";
    public string path = "Assets/Textures/Minimap/";
    public int superSize = 2;
    public int gridWidth = 3;  // Number of tiles horizontally
    public int gridHeight = 3; // Number of tiles vertically
    public bool usePositionMarkers = false;
    public bool useTopDownView = true;
    static int counter;
    
    private EditorScreenShots script;

    void OnEnable()
    {
        script = (EditorScreenShots)target;
        if (script.targetCamera == null)
        {
            script.targetCamera = Camera.main;
        }
        
        // Load saved values
        script.usePositionMarkers = usePositionMarkers;
        script.gridWidth = gridWidth;
        script.gridHeight = gridHeight;
        script.useTopDownView = useTopDownView;
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("High-Def Screenshot Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        textureName = EditorGUILayout.TextField("File Name:", textureName);
        path = EditorGUILayout.TextField("Save Path:", path);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Single Screenshot Settings", EditorStyles.boldLabel);
        superSize = EditorGUILayout.IntSlider("Resolution Multiplier:", superSize, 1, 8);
        
        EditorGUILayout.HelpBox(
            $"Single shot resolution: {Screen.width * superSize}x{Screen.height * superSize}",
            MessageType.Info);
        
        if(GUILayout.Button("Capture Single Screenshot", GUILayout.Height(30)))
        {
            Screenshot();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scan & Compile Settings", EditorStyles.boldLabel);
        
        script.targetCamera = (Camera)EditorGUILayout.ObjectField("Camera:", script.targetCamera, typeof(Camera), true);
        
        EditorGUILayout.Space();
        useTopDownView = EditorGUILayout.Toggle("Top-Down View", useTopDownView);
        script.useTopDownView = useTopDownView;
        
        if (useTopDownView)
        {
            EditorGUILayout.HelpBox(
                "Camera will face straight down (rotation: 90, 0, 0). Movement is along X and Z axes.",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Camera will maintain its current rotation. Movement is along Right and Up vectors.",
                MessageType.Info);
        }
        
        EditorGUILayout.Space();
        usePositionMarkers = EditorGUILayout.Toggle("Use Position Markers", usePositionMarkers);
        script.usePositionMarkers = usePositionMarkers;
        
        if (usePositionMarkers)
        {
            EditorGUILayout.HelpBox(
                "Position markers define the scan area. Place two empty GameObjects at opposite corners of the area you want to capture.",
                MessageType.Info);
            
            script.startPosition = (Transform)EditorGUILayout.ObjectField("Start Position:", script.startPosition, typeof(Transform), true);
            script.endPosition = (Transform)EditorGUILayout.ObjectField("End Position:", script.endPosition, typeof(Transform), true);
            
            if (script.startPosition != null && script.endPosition != null)
            {
                float areaWidth = Mathf.Abs(script.endPosition.position.x - script.startPosition.position.x);
                float areaHeight = Mathf.Abs(script.endPosition.position.y - script.startPosition.position.y);
                EditorGUILayout.LabelField($"Scan Area: {areaWidth:F2} x {areaHeight:F2} units");
            }
            
            if (GUILayout.Button("Create Position Markers"))
            {
                CreatePositionMarkers();
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Grid mode: Camera will scan in a grid pattern around its current position.",
                MessageType.Info);
        }
        
        EditorGUILayout.Space();
        gridWidth = EditorGUILayout.IntSlider("Grid Width:", gridWidth, 1, 10);
        gridHeight = EditorGUILayout.IntSlider("Grid Height:", gridHeight, 1, 10);
        script.gridWidth = gridWidth;
        script.gridHeight = gridHeight;
        
        // Force scene view to repaint when values change
        SceneView.RepaintAll();
        
        int totalWidth = Screen.width * superSize * gridWidth;
        int totalHeight = Screen.height * superSize * gridHeight;
        
        string cameraType = script.targetCamera != null 
            ? (script.targetCamera.orthographic ? "Orthographic" : "Perspective") 
            : "None";
        
        EditorGUILayout.HelpBox(
            $"Camera Type: {cameraType}\n" +
            $"Grid: {gridWidth}x{gridHeight} tiles\n" +
            $"Total captures: {gridWidth * gridHeight}\n" +
            $"Final resolution: {totalWidth}x{totalHeight}\n" +
            $"File size estimate: ~{(totalWidth * totalHeight * 4) / (1024 * 1024)}MB",
            MessageType.Info);
        
        bool canScan = script.targetCamera != null && !script.isScanning;
        if (usePositionMarkers)
        {
            canScan = canScan && script.startPosition != null && script.endPosition != null;
        }
        
        EditorGUI.BeginDisabledGroup(!canScan);
        if(GUILayout.Button("Scan & Compile Into One Image", GUILayout.Height(40)))
        {
            ScanAndCompile();
        }
        EditorGUI.EndDisabledGroup();
        
        if (script.targetCamera == null)
        {
            EditorGUILayout.HelpBox("Please assign a camera to scan!", MessageType.Warning);
        }
        
        if (usePositionMarkers && (script.startPosition == null || script.endPosition == null))
        {
            EditorGUILayout.HelpBox("Please assign both start and end positions!", MessageType.Warning);
        }
        
        if (script.isScanning)
        {
            EditorGUILayout.HelpBox("Scanning in progress...", MessageType.Info);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Next filename: {textureName}{counter}.png", EditorStyles.miniLabel);
        
        if(GUILayout.Button("Reset Counter"))
        {
            counter = 0;
        }
        
        if(GUILayout.Button("Open Screenshot Folder"))
        {
            OpenFolder();
        }
    }

    void Screenshot()
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
        
        string fullPath = path + textureName + counter + ".png";
        ScreenCapture.CaptureScreenshot(fullPath, superSize);
        counter++;
        
        Debug.Log($"Screenshot captured: {fullPath}");
    }
    
    void ScanAndCompile()
    {
        if (script.targetCamera == null)
        {
            Debug.LogError("No camera assigned!");
            return;
        }
        
        if (usePositionMarkers && (script.startPosition == null || script.endPosition == null))
        {
            Debug.LogError("Please assign both start and end positions!");
            return;
        }
        
        script.StartCoroutine(ScanCoroutine());
    }
    
    void CreatePositionMarkers()
    {
        GameObject startObj = new GameObject("Scan_StartPosition");
        GameObject endObj = new GameObject("Scan_EndPosition");
        
        if (script.targetCamera != null)
        {
            Vector3 camPos = script.targetCamera.transform.position;
            startObj.transform.position = camPos + new Vector3(-10, 10, 0);
            endObj.transform.position = camPos + new Vector3(10, -10, 0);
        }
        
        script.startPosition = startObj.transform;
        script.endPosition = endObj.transform;
        
        Selection.activeGameObject = startObj;
        Debug.Log("Position markers created! Move them to define your scan area.");
    }
    
    IEnumerator ScanCoroutine()
    {
        script.isScanning = true;
        Camera cam = script.targetCamera;
        
        // Store original camera settings
        Vector3 originalPos = cam.transform.position;
        Quaternion originalRot = cam.transform.rotation;
        float originalOrthoSize = cam.orthographicSize;
        float originalFOV = cam.fieldOfView;
        
        // Set top-down rotation if enabled
        if (useTopDownView)
        {
            cam.transform.rotation = Quaternion.Euler(90, 0, 0);
        }
        
        // Calculate capture dimensions
        int tileWidth = Screen.width * superSize;
        int tileHeight = Screen.height * superSize;
        int finalWidth = tileWidth * gridWidth;
        int finalHeight = tileHeight * gridHeight;
        
        // Create final texture
        Texture2D finalTexture = new Texture2D(finalWidth, finalHeight, TextureFormat.RGB24, false);
        
        // Determine scan area
        Vector3 scanStartPos;
        float totalWorldWidth;
        float totalWorldHeight;
        
        if (usePositionMarkers)
        {
            // Use position markers to define scan area
            Vector3 start = script.startPosition.position;
            Vector3 end = script.endPosition.position;
            
            if (useTopDownView)
            {
                // For top-down, use X and Z
                totalWorldWidth = Mathf.Abs(end.x - start.x);
                totalWorldHeight = Mathf.Abs(end.z - start.z);
                
                scanStartPos = new Vector3(
                    (start.x + end.x) / 2f,
                    cam.transform.position.y,
                    (start.z + end.z) / 2f
                );
            }
            else
            {
                // For other views, use X and Y
                totalWorldWidth = Mathf.Abs(end.x - start.x);
                totalWorldHeight = Mathf.Abs(end.y - start.y);
                
                scanStartPos = new Vector3(
                    (start.x + end.x) / 2f,
                    (start.y + end.y) / 2f,
                    cam.transform.position.z
                );
            }
        }
        else
        {
            // Use camera's current view to calculate tile size
            float tileWorldHeight;
            float tileWorldWidth;
            
            if (cam.orthographic)
            {
                tileWorldHeight = cam.orthographicSize * 2f;
                tileWorldWidth = tileWorldHeight * cam.aspect;
            }
            else
            {
                float distance = Vector3.Distance(cam.transform.position, cam.transform.position + cam.transform.forward * 10f);
                float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
                tileWorldHeight = 2f * distance * Mathf.Tan(fovRad / 2f);
                tileWorldWidth = tileWorldHeight * cam.aspect;
            }
            
            totalWorldWidth = tileWorldWidth * gridWidth;
            totalWorldHeight = tileWorldHeight * gridHeight;
            scanStartPos = cam.transform.position;
        }
        
        // Calculate tile dimensions
        float tileWorldWidth_calc = totalWorldWidth / gridWidth;
        float tileWorldHeight_calc = totalWorldHeight / gridHeight;
        
        Debug.Log($"Starting scan: {gridWidth}x{gridHeight} grid, final size: {finalWidth}x{finalHeight}");
        Debug.Log($"Camera type: {(cam.orthographic ? "Orthographic" : "Perspective")}");
        Debug.Log($"View mode: {(useTopDownView ? "Top-Down" : "Custom")}");
        Debug.Log($"Total world area: {totalWorldWidth:F2}x{totalWorldHeight:F2}");
        Debug.Log($"Tile world size: {tileWorldWidth_calc:F2}x{tileWorldHeight_calc:F2}");
        
        // Capture each tile
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                // Calculate tile position
                float offsetX = (x - (gridWidth - 1) / 2f) * tileWorldWidth_calc;
                float offsetY = (y - (gridHeight - 1) / 2f) * tileWorldHeight_calc;
                
                if (useTopDownView)
                {
                    // Move along X and Z axes for top-down
                    cam.transform.position = scanStartPos + new Vector3(offsetX, 0, -offsetY);
                }
                else
                {
                    // Move along camera's right and up vectors
                    cam.transform.position = scanStartPos + cam.transform.right * offsetX + cam.transform.up * offsetY;
                }
                
                // Wait for rendering
                yield return new WaitForEndOfFrame();
                
                // Capture screenshot
                string tempPath = path + "temp_tile_" + x + "_" + y + ".png";
                ScreenCapture.CaptureScreenshot(tempPath, superSize);
                
                // Wait for file to be written
                yield return new WaitForSeconds(0.5f);
                
                // Load and copy tile to final texture
                byte[] fileData = File.ReadAllBytes(tempPath);
                Texture2D tile = new Texture2D(2, 2);
                tile.LoadImage(fileData);
                
                int startX = x * tileWidth;
                int startY = (gridHeight - 1 - y) * tileHeight; // Flip Y
                
                finalTexture.SetPixels(startX, startY, tileWidth, tileHeight, tile.GetPixels());
                
                // Clean up temp file
                File.Delete(tempPath);
                
                Debug.Log($"Captured tile {x},{y} ({((y * gridWidth + x + 1) * 100) / (gridWidth * gridHeight)}%)");
            }
        }
        
        // Apply and save final texture
        finalTexture.Apply();
        
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        
        string finalPath = path + textureName + counter + "_compiled.png";
        File.WriteAllBytes(finalPath, finalTexture.EncodeToPNG());
        counter++;
        
        // Restore camera
        cam.transform.position = originalPos;
        cam.transform.rotation = originalRot;
        cam.orthographicSize = originalOrthoSize;
        
        script.isScanning = false;
        
        Debug.Log($"Scan complete! Saved to: {finalPath}");
        AssetDatabase.Refresh();
        
        // Clean up
        DestroyImmediate(finalTexture);
    }
    
    void OpenFolder()
    {
        if (Directory.Exists(path))
        {
            EditorUtility.RevealInFinder(path);
        }
        else
        {
            Debug.LogWarning($"Directory does not exist: {path}");
        }
    }
}