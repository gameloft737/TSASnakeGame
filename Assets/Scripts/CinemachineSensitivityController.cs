using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Controls the sensitivity of Cinemachine cameras that use CinemachineInputAxisController.
/// Loads sensitivity from PlayerPrefs (shared with SettingsManager) and applies it to the camera.
/// </summary>
public class CinemachineSensitivityController : MonoBehaviour
{
    [Header("Sensitivity Settings")]
    [Tooltip("Base sensitivity multiplier for the Cinemachine camera")]
    [SerializeField] private float sensitivityMultiplier = 0.1f;
    
    [Tooltip("If true, loads sensitivity from PlayerPrefs on Start")]
    [SerializeField] private bool loadFromPlayerPrefs = true;
    
    [Header("References")]
    [Tooltip("The Cinemachine camera to control (auto-finds if not set)")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
    
    [Tooltip("The Input Axis Controller to modify (auto-finds if not set)")]
    [SerializeField] private CinemachineInputAxisController inputAxisController;
    
    // PlayerPrefs key (same as SettingsManager)
    private const string SENSITIVITY_KEY = "MouseSensitivity";
    private const float DEFAULT_SENSITIVITY = 50f;
    
    private float currentSensitivity;
    private float baseGainX = 1f;
    private float baseGainY = 1f;
    private bool initialized = false;
    
    private void Start()
    {
        Initialize();
        
        // Load and apply sensitivity
        if (loadFromPlayerPrefs)
        {
            LoadSensitivityFromSettings();
        }
    }
    
    private void Initialize()
    {
        if (initialized) return;
        
        // Find components if not assigned
        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponent<CinemachineCamera>();
        }
        
        if (inputAxisController == null && cinemachineCamera != null)
        {
            inputAxisController = cinemachineCamera.GetComponent<CinemachineInputAxisController>();
        }
        
        if (inputAxisController == null)
        {
            // Try to find it on this GameObject
            inputAxisController = GetComponent<CinemachineInputAxisController>();
        }
        
        if (inputAxisController == null)
        {
            Debug.LogWarning("[CinemachineSensitivityController] No CinemachineInputAxisController found! Please assign one.");
            return;
        }
        
        // Store the base gain values from the controllers
        var controllers = inputAxisController.Controllers;
        if (controllers != null && controllers.Count >= 2)
        {
            baseGainX = controllers[0].Input.Gain;
            baseGainY = controllers[1].Input.Gain;
            
            // If gains are 0, use default of 1
            if (baseGainX == 0) baseGainX = 1f;
            if (baseGainY == 0) baseGainY = 1f;
        }
        
        initialized = true;
    }
    
    /// <summary>
    /// Loads sensitivity from PlayerPrefs and applies it
    /// </summary>
    public void LoadSensitivityFromSettings()
    {
        float savedSensitivity = PlayerPrefs.GetFloat(SENSITIVITY_KEY, DEFAULT_SENSITIVITY);
        SetSensitivity(savedSensitivity);
    }
    
    /// <summary>
    /// Sets the sensitivity of the Cinemachine camera by modifying the Input Axis Controller's gain
    /// </summary>
    /// <param name="sensitivity">The sensitivity value (0-100 range from settings slider)</param>
    public void SetSensitivity(float sensitivity)
    {
        if (!initialized) Initialize();
        
        currentSensitivity = sensitivity;
        float scaledSensitivity = sensitivity * sensitivityMultiplier;
        
        if (inputAxisController != null)
        {
            // Access the controllers and modify their gain values
            // The Controllers property is read-only, but we can modify the individual controller settings
            // through the InputAxisController's serialized properties
            var controllers = inputAxisController.Controllers;
            if (controllers != null && controllers.Count > 0)
            {
                // We need to use reflection or access the underlying data differently
                // For Cinemachine 3.x, we modify the gain through the controller's Input settings
                for (int i = 0; i < controllers.Count; i++)
                {
                    // Get the base gain for this axis
                    float baseGain = (i == 0) ? baseGainX : baseGainY;
                    
                    // Modify the controller's input gain
                    // Note: In Cinemachine 3.x, we need to access the controller differently
                    var controller = controllers[i];
                    
                    // The gain is applied as a multiplier to the input
                    // We store the scaled value and apply it through the controller's settings
                    controller.Input.Gain = baseGain * scaledSensitivity;
                }
            }
            
            Debug.Log($"[CinemachineSensitivityController] Set sensitivity to {sensitivity} (scaled: {scaledSensitivity})");
        }
    }
    
    /// <summary>
    /// Gets the current sensitivity value
    /// </summary>
    public float GetSensitivity() => currentSensitivity;
    
    /// <summary>
    /// Called when settings are changed - reloads sensitivity from PlayerPrefs
    /// </summary>
    public void RefreshSensitivity()
    {
        LoadSensitivityFromSettings();
    }
}