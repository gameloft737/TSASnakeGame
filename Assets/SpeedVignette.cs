using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SpeedVignette : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Volume postProcessVolume;
    
    [Header("Settings")]
    [SerializeField] private float normalSpeedIntensity = 0.2f;  // Intensity at defaultSpeed
    [SerializeField] private float boostSpeedIntensity = 0.45f;  // Intensity at maxSpeed
    [SerializeField] private float smoothSpeed = 5f;
    
    private Vignette vignette;
    private float targetIntensity;
    private float currentIntensity;

    void Start()
    {
        // Get the Vignette effect from the volume
        if (postProcessVolume != null && postProcessVolume.profile.TryGet(out vignette))
        {
            vignette.active = true;
        }
        else
        {
            Debug.LogError("Vignette not found in Volume profile!");
        }
    }

    void Update()
    {
        if (vignette == null || playerMovement == null) return;

        // Get player's actual speed
        float currentSpeed = playerMovement.GetComponent<Rigidbody>().linearVelocity.magnitude;
        
        // Map current speed between default and max speed
        float speedRatio = Mathf.InverseLerp(playerMovement.defaultSpeed, playerMovement.maxSpeed, currentSpeed);
        
        // Calculate target intensity based on speed ratio
        targetIntensity = Mathf.Lerp(normalSpeedIntensity, boostSpeedIntensity, speedRatio);
        
        // Smoothly lerp to target
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, smoothSpeed * Time.deltaTime);
        
        // Apply to vignette
        vignette.intensity.value = currentIntensity;
    }
}