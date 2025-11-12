using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider healthSlider;
    
    [Header("Visual Settings (Optional)")]
    [SerializeField] private Image fillImage; // Optional: The fill image to change color
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float lowHealthThreshold = 0.3f;
    [SerializeField] private bool smoothTransition = true;
    [SerializeField] private float transitionSpeed = 5f;
    
    private float targetValue;

    private void Start()
    {
        // Initialize slider settings
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f; // Start at full health
        }
        else
        {
            Debug.LogError("HealthBar: Slider is not assigned!");
        }
        
        targetValue = 1f;
    }

    private void Update()
    {
        if (smoothTransition && healthSlider != null)
        {
            // Smoothly lerp to target value
            healthSlider.value = Mathf.Lerp(healthSlider.value, targetValue, Time.deltaTime * transitionSpeed);
        }
    }

    public void UpdateHealthBar(float healthPercentage)
    {
        if (healthSlider == null)
        {
            Debug.LogError("HealthBar: Slider is not assigned!");
            return;
        }
        
        targetValue = healthPercentage;
        
        if (!smoothTransition)
        {
            healthSlider.value = targetValue;
        }
        
        // Optional: Update fill color if fill image is assigned
        if (fillImage != null)
        {
            if (healthPercentage <= lowHealthThreshold)
            {
                fillImage.color = lowHealthColor;
            }
            else
            {
                // Lerp between low and full health colors
                float colorT = (healthPercentage - lowHealthThreshold) / (1f - lowHealthThreshold);
                fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, colorT);
            }
        }
    }
}