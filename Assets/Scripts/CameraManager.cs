using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
    [Header("Camera References")]
    public CinemachineCamera normalCam;
    public CinemachineCamera aimCam;

    
    public CinemachineCamera pauseCam;
    
    private CinemachineCamera currentCam;
    private bool isAiming = false;
    private bool isPaused = false;
    private void Start()
    {
        // Set initial camera priorities
        currentCam = normalCam;
        normalCam.Priority = 2;
        aimCam.Priority = 1;
        
        pauseCam.Priority = 1;
    }
    
    // Called by Unity Events when aim button is pressed
    public void OnAim(InputAction.CallbackContext context)
    {
        if (isPaused) return;
        if (context.performed)
        {
            // Button pressed
            SwitchToAimCamera();
        }
        else if (context.canceled)
        {
            // Button released
            SwitchToNormalCamera();
        }
    }
    
    public void SwitchToAimCamera()
    {
        currentCam = aimCam;
        aimCam.Priority = 2;
        normalCam.Priority = 1;
        pauseCam.Priority = 1;
        isAiming = true;
        isPaused = false;
        
    }
    public void SwitchToPauseCamera()
    {
        currentCam = pauseCam;
        pauseCam.Priority = 2;
        normalCam.Priority = 1;
        aimCam.Priority = 1;
        isAiming = false;
        isPaused = true;
    }
    
    public void SwitchToNormalCamera()
    {
        currentCam = normalCam;
        normalCam.Priority = 2;
        aimCam.Priority = 1;
        pauseCam.Priority = 1;
        isAiming = false;
        isPaused = false;
        
    }
    
    public bool IsAiming() => isAiming;
}