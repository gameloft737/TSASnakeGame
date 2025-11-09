using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
    [Header("Camera References")]
    public CinemachineCamera normalCam;
    public CinemachineCamera aimCam;
    
    private CinemachineCamera currentCam;
    
    private void Start()
    {
        // Set initial camera priorities
        currentCam = normalCam;
        normalCam.Priority = 2;
        aimCam.Priority = 1;
    }
    
    // Called by Unity Events when aim button is pressed
    public void OnAim(InputAction.CallbackContext context)
    {
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
    private void SwitchToAimCamera()
    {
        currentCam = aimCam;
        aimCam.Priority = 2;
        normalCam.Priority = 1;
    }
    
    private void SwitchToNormalCamera()
    {
        currentCam = normalCam;
        normalCam.Priority = 2;
        aimCam.Priority = 1;
    }
}