using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    [Header("Camera References")]
    public CinemachineCamera normalCam;
    public CinemachineCamera pauseCam;
    
    private CinemachineCamera currentCam;
    private bool isPaused = false;
    private bool isFrozen = false; // Whether camera input is frozen
    
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
        
        currentCam = pauseCam;
        pauseCam.Priority = 2;
        if (normalCam != null) normalCam.Priority = 1;
        isPaused = true;
    }
    
    public void SwitchToNormalCamera()
    {
        if (normalCam == null) return;
        
        currentCam = normalCam;
        normalCam.Priority = 2;
        if (pauseCam != null) pauseCam.Priority = 1;
        isPaused = false;
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
}