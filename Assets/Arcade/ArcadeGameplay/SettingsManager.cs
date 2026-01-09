using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages game settings including audio, graphics, and controls.
/// Persists settings using PlayerPrefs.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }
    
    [Header("Audio Settings")]
    [Tooltip("Optional: Audio Mixer for volume control")]
    public AudioMixer audioMixer;
    
    [Tooltip("Master volume slider (0-1)")]
    public Slider masterVolumeSlider;
    
    [Tooltip("Music volume slider (0-1)")]
    public Slider musicVolumeSlider;
    
    [Tooltip("SFX volume slider (0-1)")]
    public Slider sfxVolumeSlider;
    
    [Header("Graphics Settings")]
    [Tooltip("Dropdown for quality level selection")]
    public TMP_Dropdown qualityDropdown;
    
    [Tooltip("Dropdown for resolution selection")]
    public TMP_Dropdown resolutionDropdown;
    
    [Tooltip("Toggle for fullscreen mode")]
    public Toggle fullscreenToggle;
    
    [Tooltip("Toggle for VSync")]
    public Toggle vsyncToggle;
    
    [Header("Controls Settings")]
    [Tooltip("Slider for mouse sensitivity")]
    public Slider sensitivitySlider;
    
    [Tooltip("Text to display current sensitivity value")]
    public TextMeshProUGUI sensitivityValueText;
    
    [Tooltip("Toggle for inverted Y-axis")]
    public Toggle invertYToggle;
    
    [Header("FPS Controller Reference")]
    [Tooltip("Reference to update sensitivity in real-time")]
    public EasyPeasyFirstPersonController.FirstPersonController fpsController;
    
    // PlayerPrefs keys
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const string QUALITY_KEY = "QualityLevel";
    private const string RESOLUTION_KEY = "Resolution";
    private const string FULLSCREEN_KEY = "Fullscreen";
    private const string VSYNC_KEY = "VSync";
    private const string SENSITIVITY_KEY = "MouseSensitivity";
    private const string INVERT_Y_KEY = "InvertY";
    
    private Resolution[] resolutions;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        InitializeResolutions();
        LoadSettings();
        SetupListeners();
    }
    
    /// <summary>
    /// Initializes the resolution dropdown with available resolutions
    /// </summary>
    private void InitializeResolutions()
    {
        if (resolutionDropdown == null) return;
        
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        
        List<string> options = new List<string>();
        int currentResolutionIndex = 0;
        
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = $"{resolutions[i].width} x {resolutions[i].height} @ {resolutions[i].refreshRateRatio}Hz";
            options.Add(option);
            
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }
        
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = PlayerPrefs.GetInt(RESOLUTION_KEY, currentResolutionIndex);
        resolutionDropdown.RefreshShownValue();
    }
    
    /// <summary>
    /// Sets up all UI element listeners
    /// </summary>
    private void SetupListeners()
    {
        // Audio listeners
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        }
        
        // Graphics listeners
        if (qualityDropdown != null)
        {
            qualityDropdown.onValueChanged.AddListener(SetQuality);
        }
        
        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }
        
        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
        
        if (vsyncToggle != null)
        {
            vsyncToggle.onValueChanged.AddListener(SetVSync);
        }
        
        // Controls listeners
        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        }
        
        if (invertYToggle != null)
        {
            invertYToggle.onValueChanged.AddListener(SetInvertY);
        }
    }
    
    /// <summary>
    /// Loads all saved settings from PlayerPrefs
    /// </summary>
    public void LoadSettings()
    {
        // Load Audio settings
        float masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        float musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
        float sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
        
        if (masterVolumeSlider != null) masterVolumeSlider.value = masterVolume;
        if (musicVolumeSlider != null) musicVolumeSlider.value = musicVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVolume;
        
        // Apply audio settings
        SetMasterVolume(masterVolume);
        SetMusicVolume(musicVolume);
        SetSFXVolume(sfxVolume);
        
        // Load Graphics settings
        int qualityLevel = PlayerPrefs.GetInt(QUALITY_KEY, QualitySettings.GetQualityLevel());
        bool isFullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, Screen.fullScreen ? 1 : 0) == 1;
        bool isVSync = PlayerPrefs.GetInt(VSYNC_KEY, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;
        
        if (qualityDropdown != null) qualityDropdown.value = qualityLevel;
        if (fullscreenToggle != null) fullscreenToggle.isOn = isFullscreen;
        if (vsyncToggle != null) vsyncToggle.isOn = isVSync;
        
        // Apply graphics settings
        SetQuality(qualityLevel);
        SetFullscreen(isFullscreen);
        SetVSync(isVSync);
        
        // Load Controls settings
        float sensitivity = PlayerPrefs.GetFloat(SENSITIVITY_KEY, 50f);
        bool invertY = PlayerPrefs.GetInt(INVERT_Y_KEY, 0) == 1;
        
        if (sensitivitySlider != null) sensitivitySlider.value = sensitivity;
        if (invertYToggle != null) invertYToggle.isOn = invertY;
        
        // Apply controls settings
        SetSensitivity(sensitivity);
        SetInvertY(invertY);
    }
    
    /// <summary>
    /// Saves all current settings to PlayerPrefs
    /// </summary>
    public void SaveSettings()
    {
        PlayerPrefs.Save();
    }
    
    #region Audio Settings
    
    public void SetMasterVolume(float volume)
    {
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, volume);
        
        if (audioMixer != null)
        {
            // Convert linear to logarithmic for audio mixer (-80dB to 0dB)
            float dB = volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f;
            audioMixer.SetFloat("MasterVolume", dB);
        }
        else
        {
            // Fallback: set AudioListener volume
            AudioListener.volume = volume;
        }
    }
    
    public void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, volume);
        
        if (audioMixer != null)
        {
            float dB = volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f;
            audioMixer.SetFloat("MusicVolume", dB);
        }
    }
    
    public void SetSFXVolume(float volume)
    {
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, volume);
        
        if (audioMixer != null)
        {
            float dB = volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f;
            audioMixer.SetFloat("SFXVolume", dB);
        }
    }
    
    #endregion
    
    #region Graphics Settings
    
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt(QUALITY_KEY, qualityIndex);
    }
    
    public void SetResolution(int resolutionIndex)
    {
        if (resolutions == null || resolutionIndex >= resolutions.Length) return;
        
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt(RESOLUTION_KEY, resolutionIndex);
    }
    
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FULLSCREEN_KEY, isFullscreen ? 1 : 0);
    }
    
    public void SetVSync(bool enabled)
    {
        QualitySettings.vSyncCount = enabled ? 1 : 0;
        PlayerPrefs.SetInt(VSYNC_KEY, enabled ? 1 : 0);
    }
    
    #endregion
    
    #region Controls Settings
    
    public void SetSensitivity(float sensitivity)
    {
        PlayerPrefs.SetFloat(SENSITIVITY_KEY, sensitivity);
        
        // Update sensitivity text display
        if (sensitivityValueText != null)
        {
            sensitivityValueText.text = sensitivity.ToString("F1");
        }
        
        // Apply to FPS controller if available
        if (fpsController != null)
        {
            fpsController.mouseSensitivity = sensitivity;
        }
    }
    
    public void SetInvertY(bool inverted)
    {
        PlayerPrefs.SetInt(INVERT_Y_KEY, inverted ? 1 : 0);
        
        // Note: You would need to modify the FirstPersonController to support inverted Y
        // This is a placeholder for that functionality
    }
    
    #endregion
    
    /// <summary>
    /// Resets all settings to default values
    /// </summary>
    public void ResetToDefaults()
    {
        // Audio defaults
        if (masterVolumeSlider != null) masterVolumeSlider.value = 1f;
        if (musicVolumeSlider != null) musicVolumeSlider.value = 1f;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = 1f;
        
        // Graphics defaults
        if (qualityDropdown != null) qualityDropdown.value = QualitySettings.names.Length - 1; // Highest quality
        if (fullscreenToggle != null) fullscreenToggle.isOn = true;
        if (vsyncToggle != null) vsyncToggle.isOn = true;
        
        // Controls defaults
        if (sensitivitySlider != null) sensitivitySlider.value = 50f;
        if (invertYToggle != null) invertYToggle.isOn = false;
        
        SaveSettings();
    }
    
    private void OnDestroy()
    {
        SaveSettings();
    }
}