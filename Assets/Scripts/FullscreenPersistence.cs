using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures fullscreen setting persists across scene changes in WebGL builds.
/// This script uses DontDestroyOnLoad to persist across scenes and applies
/// the saved fullscreen setting whenever a new scene is loaded.
/// </summary>
public class FullscreenPersistence : MonoBehaviour
{
    private static FullscreenPersistence instance;
    
    private const string FULLSCREEN_KEY = "Fullscreen";
    private const string RESOLUTION_KEY = "Resolution";
    
    private void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Apply settings immediately on first load
        ApplyFullscreenSetting();
    }
    
    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }
    
    /// <summary>
    /// Called whenever a new scene is loaded
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Apply fullscreen setting after a short delay to ensure Unity has finished scene setup
        // This is especially important for WebGL where screen state can be inconsistent during scene transitions
        StartCoroutine(ApplyFullscreenSettingDelayed());
    }
    
    /// <summary>
    /// Applies fullscreen setting with a small delay to ensure scene is fully loaded
    /// </summary>
    private System.Collections.IEnumerator ApplyFullscreenSettingDelayed()
    {
        // Wait for end of frame to ensure scene is fully loaded
        yield return new WaitForEndOfFrame();
        
        // Additional frame wait for WebGL stability
        yield return null;
        
        ApplyFullscreenSetting();
    }
    
    /// <summary>
    /// Applies the saved fullscreen setting from PlayerPrefs
    /// </summary>
    private void ApplyFullscreenSetting()
    {
        // Get saved fullscreen preference (default to current state if not set)
        bool savedFullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, Screen.fullScreen ? 1 : 0) == 1;
        
        // Only apply if different from current state to avoid unnecessary screen mode changes
        if (Screen.fullScreen != savedFullscreen)
        {
            Debug.Log($"[FullscreenPersistence] Applying fullscreen setting: {savedFullscreen} (was {Screen.fullScreen})");
            Screen.fullScreen = savedFullscreen;
        }
    }
    
    /// <summary>
    /// Static method to force apply fullscreen setting (can be called from anywhere)
    /// </summary>
    public static void ForceApplyFullscreen()
    {
        bool savedFullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, Screen.fullScreen ? 1 : 0) == 1;
        Screen.fullScreen = savedFullscreen;
        Debug.Log($"[FullscreenPersistence] Force applied fullscreen: {savedFullscreen}");
    }
    
    /// <summary>
    /// Static method to set and save fullscreen preference
    /// </summary>
    public static void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FULLSCREEN_KEY, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"[FullscreenPersistence] Set fullscreen to: {isFullscreen}");
    }
    
    /// <summary>
    /// Static method to get the saved fullscreen preference
    /// </summary>
    public static bool GetSavedFullscreen()
    {
        return PlayerPrefs.GetInt(FULLSCREEN_KEY, Screen.fullScreen ? 1 : 0) == 1;
    }
    
    /// <summary>
    /// Creates the FullscreenPersistence object if it doesn't exist.
    /// Call this from a startup scene or bootstrap script.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // Check if instance already exists
        if (instance != null) return;
        
        // Create new GameObject with FullscreenPersistence
        GameObject go = new GameObject("FullscreenPersistence");
        go.AddComponent<FullscreenPersistence>();
        
        Debug.Log("[FullscreenPersistence] Initialized via RuntimeInitializeOnLoadMethod");
    }
}