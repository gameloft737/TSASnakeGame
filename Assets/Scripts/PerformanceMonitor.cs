using UnityEngine;
using System.Text;

/// <summary>
/// Real-time performance monitoring utility.
/// Displays FPS, memory usage, and other metrics.
/// 
/// Usage: Add this component to a GameObject in your scene.
/// Press F1 to toggle the display on/off.
/// </summary>
public class PerformanceMonitor : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private bool showOnStart = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;
    [SerializeField] private int fontSize = 14;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.7f);
    
    [Header("Update Settings")]
    [SerializeField] private float updateInterval = 0.5f;
    
    // FPS tracking
    private float fps;
    private float minFps = float.MaxValue;
    private float maxFps = 0f;
    private int frameCount;
    private float fpsTimer;
    private float deltaTime;
    
    // Memory tracking
    private long totalMemory;
    private long usedMemory;
    private long gcMemory;
    
    // Object counts
    private int enemyCount;
    private int xpDropCount;
    
    // Display
    private bool isVisible;
    private GUIStyle textStyle;
    private GUIStyle backgroundStyle;
    private Texture2D backgroundTexture;
    private StringBuilder stringBuilder;
    private string displayText = "";
    private Rect displayRect;
    
    // Cached references
    private ObjectPool objectPool;
    
    private void Start()
    {
        isVisible = showOnStart;
        stringBuilder = new StringBuilder(512);
        
        // Create background texture
        backgroundTexture = new Texture2D(1, 1);
        backgroundTexture.SetPixel(0, 0, backgroundColor);
        backgroundTexture.Apply();
        
        // Cache object pool reference
        objectPool = ObjectPool.Instance;
        
        // Initialize display rect
        displayRect = new Rect(10, 10, 300, 200);
    }
    
    private void Update()
    {
        // Toggle visibility
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
        }
        
        if (!isVisible) return;
        
        // Track frame time
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        frameCount++;
        fpsTimer += Time.unscaledDeltaTime;
        
        // Update metrics at interval
        if (fpsTimer >= updateInterval)
        {
            UpdateMetrics();
            fpsTimer = 0f;
            frameCount = 0;
        }
    }
    
    private void UpdateMetrics()
    {
        // Calculate FPS
        fps = frameCount / updateInterval;
        
        // Track min/max FPS (reset after 10 seconds)
        if (fps < minFps) minFps = fps;
        if (fps > maxFps) maxFps = fps;
        
        // Memory metrics
        totalMemory = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024 * 1024);
        usedMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
        gcMemory = System.GC.GetTotalMemory(false) / (1024 * 1024);
        
        // Object counts (using cached static list from AppleEnemy)
        // This avoids FindObjectsOfType which is slow
        enemyCount = GetEnemyCount();
        xpDropCount = GetXPDropCount();
        
        // Build display string
        BuildDisplayText();
    }
    
    private int GetEnemyCount()
    {
        // Use reflection or a public accessor if available
        // For now, use a simple approach
        var spawner = FindFirstObjectByType<EnemySpawner>();
        return spawner != null ? spawner.GetActiveEnemyCount() : 0;
    }
    
    private int GetXPDropCount()
    {
        // Count active XP drops
        if (objectPool != null && objectPool.HasPool("XPDrop_Pool"))
        {
            return objectPool.GetActiveCount("XPDrop_Pool");
        }
        return 0;
    }
    
    private void BuildDisplayText()
    {
        stringBuilder.Clear();
        
        // Header
        stringBuilder.AppendLine("=== PERFORMANCE MONITOR ===");
        stringBuilder.AppendLine();
        
        // FPS Section
        stringBuilder.Append("FPS: ");
        stringBuilder.Append(fps.ToString("F1"));
        stringBuilder.Append(" (");
        stringBuilder.Append((deltaTime * 1000f).ToString("F1"));
        stringBuilder.AppendLine(" ms)");
        
        stringBuilder.Append("Min/Max: ");
        stringBuilder.Append(minFps.ToString("F0"));
        stringBuilder.Append(" / ");
        stringBuilder.AppendLine(maxFps.ToString("F0"));
        stringBuilder.AppendLine();
        
        // Memory Section
        stringBuilder.AppendLine("--- Memory ---");
        stringBuilder.Append("Reserved: ");
        stringBuilder.Append(totalMemory);
        stringBuilder.AppendLine(" MB");
        
        stringBuilder.Append("Allocated: ");
        stringBuilder.Append(usedMemory);
        stringBuilder.AppendLine(" MB");
        
        stringBuilder.Append("GC Heap: ");
        stringBuilder.Append(gcMemory);
        stringBuilder.AppendLine(" MB");
        stringBuilder.AppendLine();
        
        // Objects Section
        stringBuilder.AppendLine("--- Objects ---");
        stringBuilder.Append("Enemies: ");
        stringBuilder.AppendLine(enemyCount.ToString());
        
        stringBuilder.Append("XP Drops: ");
        stringBuilder.AppendLine(xpDropCount.ToString());
        
        // Pool Stats (if available)
        if (objectPool != null)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("--- Object Pools ---");
            // Add pool stats here if you implement GetAllPoolTags()
        }
        
        stringBuilder.AppendLine();
        stringBuilder.Append("Press ");
        stringBuilder.Append(toggleKey.ToString());
        stringBuilder.Append(" to hide");
        
        displayText = stringBuilder.ToString();
    }
    
    private void OnGUI()
    {
        if (!isVisible) return;
        
        // Initialize styles if needed
        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                normal = { textColor = textColor }
            };
        }
        
        if (backgroundStyle == null)
        {
            backgroundStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = backgroundTexture }
            };
        }
        
        // Calculate content size
        GUIContent content = new GUIContent(displayText);
        Vector2 size = textStyle.CalcSize(content);
        displayRect.width = size.x + 20;
        displayRect.height = size.y + 20;
        
        // Draw background
        GUI.Box(displayRect, GUIContent.none, backgroundStyle);
        
        // Draw text
        Rect textRect = new Rect(displayRect.x + 10, displayRect.y + 10, size.x, size.y);
        GUI.Label(textRect, displayText, textStyle);
    }
    
    private void OnDestroy()
    {
        if (backgroundTexture != null)
        {
            Destroy(backgroundTexture);
        }
    }
    
    /// <summary>
    /// Resets min/max FPS tracking
    /// </summary>
    public void ResetMinMaxFPS()
    {
        minFps = float.MaxValue;
        maxFps = 0f;
    }
    
    /// <summary>
    /// Gets the current FPS
    /// </summary>
    public float GetCurrentFPS() => fps;
    
    /// <summary>
    /// Gets the minimum recorded FPS
    /// </summary>
    public float GetMinFPS() => minFps;
    
    /// <summary>
    /// Gets the maximum recorded FPS
    /// </summary>
    public float GetMaxFPS() => maxFps;
}