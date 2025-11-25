using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLookAt : MonoBehaviour
{
    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 0.5f; // How much mouse movement affects rotation
    [SerializeField] private float mouseSmoothing = 0.15f; // Lower = smoother, higher = more responsive
    [SerializeField] private float minXRotation = -5f; // Minimum X rotation
    [SerializeField] private float maxXRotation = 5f; // Maximum X rotation
    
    [Header("Camera Reference")]
    [SerializeField] private CameraManager cameraManager;
    
    private Vector2 lookInput; // Mouse input
    private Vector2 smoothedLookInput; // Smoothed mouse input
    private float targetXRotation; // Target rotation value

    void Update()
    {
        SmoothMouseInput();
        ApplyRotation();
    }

    private void SmoothMouseInput()
    {
        // Smooth the mouse input using lerp
        smoothedLookInput = Vector2.Lerp(smoothedLookInput, lookInput, 1f - mouseSmoothing);
    }

    private void ApplyRotation()
    {
        // Only rotate when aiming
        if (cameraManager != null && cameraManager.IsAiming())
        {
            // Use vertical mouse movement (Y axis) to affect X rotation
            targetXRotation += smoothedLookInput.y * mouseSensitivity;
            
            // Clamp the rotation between min and max values
            targetXRotation = Mathf.Clamp(targetXRotation, minXRotation, maxXRotation);
        }
        else
        {
            // When not aiming, return to neutral position
            targetXRotation = Mathf.Lerp(targetXRotation, 0f, 5f * Time.deltaTime);
        }
        
        // Apply the rotation (only affecting X axis)
        transform.localRotation = Quaternion.Euler(targetXRotation, 0f, 0f);
    }

    // Input callback - connect this to your Input Action for Look
    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    // Optional: Reset rotation to neutral position
    public void ResetRotation()
    {
        targetXRotation = 0f;
        smoothedLookInput = Vector2.zero;
        lookInput = Vector2.zero;
    }

    // Public method to get smoothed mouse input for other scripts
    public Vector2 GetSmoothedLookInput()
    {
        return smoothedLookInput;
    }
}