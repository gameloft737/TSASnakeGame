using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLookAt : MonoBehaviour
{
    [Header("Look Settings")]
    [SerializeField] private float mouseSmoothing = 0.15f; // Lower = smoother, higher = more responsive
    
    private Vector2 lookInput; // Mouse input
    private Vector2 smoothedLookInput; // Smoothed mouse input
    private float targetXRotation; // Target rotation value
    private bool isFrozen = false; // Whether look input is frozen

    void Update()
    {
        if (isFrozen) return; // Skip updates when frozen
        
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
        // Always return to neutral position (aim camera removed)
        targetXRotation = Mathf.Lerp(targetXRotation, 0f, 5f * Time.deltaTime);
        
        // Apply the rotation (only affecting X axis)
        transform.localRotation = Quaternion.Euler(targetXRotation, 0f, 0f);
    }

    // Input callback - connect this to your Input Action for Look
    public void OnLook(InputAction.CallbackContext context)
    {
        if (isFrozen) return; // Block input when frozen
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

    /// <summary>
    /// Freezes or unfreezes the mouse look
    /// </summary>
    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;
        
        if (frozen)
        {
            // Clear input when freezing
            lookInput = Vector2.zero;
            smoothedLookInput = Vector2.zero;
        }
    }

    /// <summary>
    /// Returns whether the mouse look is currently frozen
    /// </summary>
    public bool IsFrozen() => isFrozen;
}