using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    [Header("Camera References")]
    public CinemachineCamera normalCam;
    public CinemachineCamera pauseCam;
    
    [Header("Blend Settings")]
    [Tooltip("Use instant cut when returning from pause camera to avoid spinning")]
    [SerializeField] private bool useCutOnReturn = true;
    [Tooltip("Blend time when transitioning TO pause camera")]
    [SerializeField] private float blendToPauseTime = 0.5f;
    [Tooltip("Blend time when returning FROM pause camera (if not using cut)")]
    [SerializeField] private float blendFromPauseTime = 0.3f;
    
    private CinemachineCamera currentCam;
    private bool isPaused = false;
    private bool isFrozen = false; // Whether camera input is frozen
    private CinemachineBrain cinemachineBrain;
    private CinemachineBlendDefinition originalDefaultBlend;
    private bool hasStoredOriginalBlend = false;
    
    private void Awake()
    {
        // Find the Cinemachine Brain on the main camera
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();
        }
        
        if (cinemachineBrain == null)
        {
            cinemachineBrain = FindFirstObjectByType<CinemachineBrain>();
        }
        
        // Store the original blend settings
        if (cinemachineBrain != null && !hasStoredOriginalBlend)
        {
            originalDefaultBlend = cinemachineBrain.DefaultBlend;
            hasStoredOriginalBlend = true;
        }
    }
    
    private void Start()
    {
        // Set initial camera priorities
        currentCam = normalCam;
        if (normalCam != null) normalCam.Priority = 2;
        if (pauseCam != null) pauseCam.Priority = 1;
    }
    
    public void SwitchToPauseCamera()
    {
        if (pauseCam == null)
        {
            Debug.LogWarning("[CameraManager] pauseCam is not assigned!");
            return;
        }
        
        // Set a smooth blend when going TO the pause camera
        SetBlendStyle(CinemachineBlendDefinition.Styles.EaseInOut, blendToPauseTime);
        
        currentCam = pauseCam;
        pauseCam.Priority = 2;
        if (normalCam != null) normalCam.Priority = 1;
        isPaused = true;
    }
    
    public void SwitchToNormalCamera()
    {
        if (normalCam == null) return;
        
        // Use cut or fast blend when returning FROM pause camera to avoid spinning
        if (useCutOnReturn)
        {
            SetBlendStyle(CinemachineBlendDefinition.Styles.Cut, 0f);
        }
        else
        {
            // Use a fast ease-out blend with position-only interpolation
            SetBlendStyle(CinemachineBlendDefinition.Styles.EaseOut, blendFromPauseTime);
        }
        
        currentCam = normalCam;
        normalCam.Priority = 2;
        if (pauseCam != null) pauseCam.Priority = 1;
        isPaused = false;
        
        // Restore original blend settings after a short delay
        if (cinemachineBrain != null)
        {
            StartCoroutine(RestoreBlendAfterDelay(useCutOnReturn ? 0.1f : blendFromPauseTime + 0.1f));
        }
    }
    
    /// <summary>
    /// Sets the Cinemachine Brain's default blend style
    /// </summary>
    private void SetBlendStyle(CinemachineBlendDefinition.Styles style, float time)
    {
        if (cinemachineBrain != null)
        {
            cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(style, time);
        }
    }
    
    /// <summary>
    /// Restores the original blend settings after a delay
    /// </summary>
    private System.Collections.IEnumerator RestoreBlendAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        
        if (cinemachineBrain != null && hasStoredOriginalBlend)
        {
            cinemachineBrain.DefaultBlend = originalDefaultBlend;
        }
    }

    /// <summary>
    /// Freezes or unfreezes camera input
    /// </summary>
    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;
    }

    /// <summary>
    /// Returns whether camera input is frozen
    /// </summary>
    public bool IsFrozen() => isFrozen;
    
    /// <summary>
    /// Returns whether the pause camera is active
    /// </summary>
    public bool IsPaused() => isPaused;
    
    /// <summary>
    /// Gets the Cinemachine Brain reference
    /// </summary>
    public CinemachineBrain GetCinemachineBrain() => cinemachineBrain;
}