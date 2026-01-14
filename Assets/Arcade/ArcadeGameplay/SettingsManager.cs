using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }
    
    [Header("Audio Settings")]
    public AudioMixer audioMixer;
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    
    [Header("Graphics Settings")]
    public TMP_Dropdown qualityDropdown;
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;
    public Toggle vsyncToggle;
    
    [Header("Controls Settings")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityValueText;
    public Toggle invertYToggle;
    
    [Header("FPS Controller Reference")]
    public EasyPeasyFirstPersonController.FirstPersonController fpsController;
    
    [Header("Snake Camera Reference")]
    [Tooltip("Reference to the Cinemachine sensitivity controller for the snake game")]
    public CinemachineSensitivityController snakeCameraSensitivity;
    
    [Header("Sensitivity Scaling")]
    public float sensitivityMultiplier = 5f;
    
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const string QUALITY_KEY = "QualityLevel";
    private const string RESOLUTION_KEY = "Resolution";
    private const string FULLSCREEN_KEY = "Fullscreen";
    private const string VSYNC_KEY = "VSync";
    private const string SENSITIVITY_KEY = "MouseSensitivity";
    private const string INVERT_Y_KEY = "InvertY";
    
    private List<Resolution> filteredResolutions;
    private bool listenersSetup = false;
    private bool initialized = false;
    
    private void Awake() { if (Instance == null) Instance = this; }
    private void Start() { Initialize(); }
    
    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        InitializeQualityDropdown();
        InitializeResolutions();
        FindFPSController();
        SetupListeners();
        LoadSettings();
    }
    
    private void OnEnable()
    {
        if (!initialized) Initialize();
        RefreshUIFromPlayerPrefs();
    }
    
    public void RefreshUIFromPlayerPrefs()
    {
        float masterVol = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        float musicVol = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
        float sfxVol = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
        
        if (masterVolumeSlider) masterVolumeSlider.SetValueWithoutNotify(masterVol);
        if (musicVolumeSlider) musicVolumeSlider.SetValueWithoutNotify(musicVol);
        if (sfxVolumeSlider) sfxVolumeSlider.SetValueWithoutNotify(sfxVol);
        
        int quality = PlayerPrefs.GetInt(QUALITY_KEY, QualitySettings.GetQualityLevel());
        int resIdx = PlayerPrefs.GetInt(RESOLUTION_KEY, GetCurrentResolutionIndex());
        bool fullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, Screen.fullScreen ? 1 : 0) == 1;
        bool vsync = PlayerPrefs.GetInt(VSYNC_KEY, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;
        
        if (qualityDropdown) { qualityDropdown.SetValueWithoutNotify(quality); qualityDropdown.RefreshShownValue(); }
        if (resolutionDropdown) { resolutionDropdown.SetValueWithoutNotify(resIdx); resolutionDropdown.RefreshShownValue(); }
        if (fullscreenToggle) fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
        if (vsyncToggle) vsyncToggle.SetIsOnWithoutNotify(vsync);
        
        float sens = PlayerPrefs.GetFloat(SENSITIVITY_KEY, 50f);
        bool invertY = PlayerPrefs.GetInt(INVERT_Y_KEY, 0) == 1;
        
        if (sensitivitySlider) sensitivitySlider.SetValueWithoutNotify(sens);
        if (invertYToggle) invertYToggle.SetIsOnWithoutNotify(invertY);
        if (sensitivityValueText) sensitivityValueText.text = sens.ToString("F1");
        
        FindFPSController();
    }
    
    private int GetCurrentResolutionIndex()
    {
        if (filteredResolutions == null || filteredResolutions.Count == 0) return 0;
        for (int i = 0; i < filteredResolutions.Count; i++)
            if (filteredResolutions[i].width == Screen.currentResolution.width && filteredResolutions[i].height == Screen.currentResolution.height)
                return i;
        return 0;
    }
    
    private void FindFPSController()
    {
        if (fpsController == null)
            fpsController = FindFirstObjectByType<EasyPeasyFirstPersonController.FirstPersonController>();
    }
    
    private void FindSnakeCameraSensitivity()
    {
        if (snakeCameraSensitivity == null)
            snakeCameraSensitivity = FindFirstObjectByType<CinemachineSensitivityController>();
    }
    
    private void InitializeQualityDropdown()
    {
        if (qualityDropdown == null) return;
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        int saved = PlayerPrefs.GetInt(QUALITY_KEY, QualitySettings.GetQualityLevel());
        qualityDropdown.value = saved;
        qualityDropdown.RefreshShownValue();
    }
    
    private void InitializeResolutions()
    {
        if (resolutionDropdown == null) return;
        Resolution[] resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        filteredResolutions = new List<Resolution>();
        HashSet<string> seen = new HashSet<string>();
        
        for (int i = resolutions.Length - 1; i >= 0; i--)
        {
            string key = $"{resolutions[i].width}x{resolutions[i].height}";
            if (!seen.Contains(key)) { seen.Add(key); filteredResolutions.Add(resolutions[i]); }
        }
        filteredResolutions.Reverse();
        
        List<string> options = new List<string>();
        int currentIdx = 0;
        for (int i = 0; i < filteredResolutions.Count; i++)
        {
            options.Add($"{filteredResolutions[i].width} x {filteredResolutions[i].height}");
            if (filteredResolutions[i].width == Screen.currentResolution.width && filteredResolutions[i].height == Screen.currentResolution.height)
                currentIdx = i;
        }
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = PlayerPrefs.GetInt(RESOLUTION_KEY, currentIdx);
        resolutionDropdown.RefreshShownValue();
    }
    
    private void SetupListeners()
    {
        if (listenersSetup) return;
        listenersSetup = true;
        
        if (masterVolumeSlider) { masterVolumeSlider.onValueChanged.RemoveAllListeners(); masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume); }
        if (musicVolumeSlider) { musicVolumeSlider.onValueChanged.RemoveAllListeners(); musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume); }
        if (sfxVolumeSlider) { sfxVolumeSlider.onValueChanged.RemoveAllListeners(); sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume); }
        if (qualityDropdown) { qualityDropdown.onValueChanged.RemoveAllListeners(); qualityDropdown.onValueChanged.AddListener(SetQuality); }
        if (resolutionDropdown) { resolutionDropdown.onValueChanged.RemoveAllListeners(); resolutionDropdown.onValueChanged.AddListener(SetResolution); }
        if (fullscreenToggle) { fullscreenToggle.onValueChanged.RemoveAllListeners(); fullscreenToggle.onValueChanged.AddListener(SetFullscreen); }
        if (vsyncToggle) { vsyncToggle.onValueChanged.RemoveAllListeners(); vsyncToggle.onValueChanged.AddListener(SetVSync); }
        if (sensitivitySlider) { sensitivitySlider.onValueChanged.RemoveAllListeners(); sensitivitySlider.onValueChanged.AddListener(SetSensitivity); }
        if (invertYToggle) { invertYToggle.onValueChanged.RemoveAllListeners(); invertYToggle.onValueChanged.AddListener(SetInvertY); }
    }
    
    public void LoadSettings()
    {
        float masterVol = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        float musicVol = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
        float sfxVol = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
        if (masterVolumeSlider) masterVolumeSlider.value = masterVol;
        if (musicVolumeSlider) musicVolumeSlider.value = musicVol;
        if (sfxVolumeSlider) sfxVolumeSlider.value = sfxVol;
        SetMasterVolume(masterVol); SetMusicVolume(musicVol); SetSFXVolume(sfxVol);
        
        int quality = PlayerPrefs.GetInt(QUALITY_KEY, QualitySettings.GetQualityLevel());
        bool fullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, Screen.fullScreen ? 1 : 0) == 1;
        bool vsync = PlayerPrefs.GetInt(VSYNC_KEY, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;
        if (qualityDropdown) qualityDropdown.value = quality;
        if (fullscreenToggle) fullscreenToggle.isOn = fullscreen;
        if (vsyncToggle) vsyncToggle.isOn = vsync;
        SetQuality(quality); SetFullscreen(fullscreen); SetVSync(vsync);
        
        float sens = PlayerPrefs.GetFloat(SENSITIVITY_KEY, 50f);
        bool invertY = PlayerPrefs.GetInt(INVERT_Y_KEY, 0) == 1;
        if (sensitivitySlider) sensitivitySlider.value = sens;
        if (invertYToggle) invertYToggle.isOn = invertY;
        SetSensitivity(sens);
        SetInvertY(invertY);
    }
    
    public void SaveSettings() { PlayerPrefs.Save(); }
    
    public void SetMasterVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, volume);
        PlayerPrefs.Save();
        if (audioMixer != null) audioMixer.SetFloat("MasterVolume", volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f);
        AudioListener.volume = volume;
    }
    
    public void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, volume);
        PlayerPrefs.Save();
        if (audioMixer != null) audioMixer.SetFloat("MusicVolume", volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f);
    }
    
    public void SetSFXVolume(float volume)
    {
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, volume);
        PlayerPrefs.Save();
        if (audioMixer != null) audioMixer.SetFloat("SFXVolume", volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f);
    }
    
    public void SetQuality(int qualityIndex)
    {
        Debug.Log($"[SettingsManager] Setting quality to level {qualityIndex} ({(qualityIndex < QualitySettings.names.Length ? QualitySettings.names[qualityIndex] : "Unknown")})");
        QualitySettings.SetQualityLevel(qualityIndex, true);
        PlayerPrefs.SetInt(QUALITY_KEY, qualityIndex);
        PlayerPrefs.Save();
    }
    
    public void SetResolution(int resolutionIndex)
    {
        if (filteredResolutions == null || resolutionIndex >= filteredResolutions.Count) return;
        Resolution res = filteredResolutions[resolutionIndex];
        Debug.Log($"[SettingsManager] Setting resolution to {res.width}x{res.height} (fullscreen: {Screen.fullScreen})");
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        PlayerPrefs.SetInt(RESOLUTION_KEY, resolutionIndex);
        PlayerPrefs.Save();
    }
    
    public void SetFullscreen(bool isFullscreen)
    {
        // Use FullscreenPersistence for consistent fullscreen handling across scenes
        FullscreenPersistence.SetFullscreen(isFullscreen);
    }
    
    public void SetVSync(bool enabled)
    {
        QualitySettings.vSyncCount = enabled ? 1 : 0;
        PlayerPrefs.SetInt(VSYNC_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    public void SetSensitivity(float sensitivity)
    {
        PlayerPrefs.SetFloat(SENSITIVITY_KEY, sensitivity);
        PlayerPrefs.Save();
        if (sensitivityValueText) sensitivityValueText.text = sensitivity.ToString("F1");
        
        // Apply to FPS controller if available
        if (fpsController == null) FindFPSController();
        if (fpsController != null) fpsController.mouseSensitivity = sensitivity * sensitivityMultiplier;
        
        // Apply to Snake Cinemachine camera if available
        if (snakeCameraSensitivity == null) FindSnakeCameraSensitivity();
        if (snakeCameraSensitivity != null) snakeCameraSensitivity.SetSensitivity(sensitivity);
    }
    
    public void SetInvertY(bool inverted)
    {
        PlayerPrefs.SetInt(INVERT_Y_KEY, inverted ? 1 : 0);
        PlayerPrefs.Save();
        if (fpsController == null) FindFPSController();
        if (fpsController != null) fpsController.invertY = inverted;
    }
    
    public void ReapplySensitivity()
    {
        float sens = PlayerPrefs.GetFloat(SENSITIVITY_KEY, 50f);
        bool invertY = PlayerPrefs.GetInt(INVERT_Y_KEY, 0) == 1;
        
        // Apply to FPS controller if available
        if (fpsController == null) FindFPSController();
        if (fpsController != null)
        {
            fpsController.mouseSensitivity = sens * sensitivityMultiplier;
            fpsController.invertY = invertY;
        }
        
        // Apply to Snake Cinemachine camera if available
        if (snakeCameraSensitivity == null) FindSnakeCameraSensitivity();
        if (snakeCameraSensitivity != null)
        {
            snakeCameraSensitivity.SetSensitivity(sens);
        }
    }
    
    public void ResetToDefaults()
    {
        if (masterVolumeSlider) masterVolumeSlider.value = 1f;
        if (musicVolumeSlider) musicVolumeSlider.value = 1f;
        if (sfxVolumeSlider) sfxVolumeSlider.value = 1f;
        if (qualityDropdown) qualityDropdown.value = QualitySettings.names.Length - 1;
        if (fullscreenToggle) fullscreenToggle.isOn = true;
        if (vsyncToggle) vsyncToggle.isOn = true;
        if (sensitivitySlider) sensitivitySlider.value = 50f;
        if (invertYToggle) invertYToggle.isOn = false;
        SaveSettings();
    }
    
    private void OnDestroy() { SaveSettings(); }
}