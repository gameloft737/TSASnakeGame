using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private Animator animator; // Optional: Animator component
    [SerializeField] private TextMeshProUGUI healthText; // Health text display
    
    [Header("Visual Settings")]
    [SerializeField] private Gradient healthGradient = new Gradient();
    [SerializeField] private string animatorParameterName = "HealthPercentage";
    
    [Header("Text Settings")]
    [SerializeField] private bool showHealthText = true;
    [SerializeField] private bool showDecimals = false; // If true, shows "80.5/100.0"
    
    private float currentHealthPercentage = 1f;
    private float storedCurrentHealth = 100f;
    private float storedMaxHealth = 100f;

    private void Start()
    {
        // Initialize slider settings
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
        }
        else
        {
            Debug.LogError("HealthBar: Slider is not assigned!");
        }
        
        // Set initial color from gradient
        if (fillImage != null)
        {
            fillImage.color = healthGradient.Evaluate(1f);
        }
        
        // Set initial animator value
        if (animator != null)
        {
            animator.SetFloat(animatorParameterName, 1f);
        }
        
        currentHealthPercentage = 1f;
        UpdateHealthText();
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (maxHealth <= 0)
        {
            Debug.LogWarning("HealthBar: maxHealth must be greater than 0!");
            return;
        }
        
        // Store the actual health values for text display
        storedCurrentHealth = currentHealth;
        storedMaxHealth = maxHealth;
        
        float healthPercentage = Mathf.Clamp01(currentHealth / maxHealth);
        UpdateHealthBarDirect(healthPercentage);
    }
    
    private void UpdateHealthBarDirect(float healthPercentage)
    {
        healthPercentage = Mathf.Clamp01(healthPercentage);
        currentHealthPercentage = healthPercentage;
        
        // Update slider
        if (healthSlider != null)
        {
            healthSlider.value = healthPercentage;
        }
        
        // Update fill color using gradient
        if (fillImage != null)
        {
            fillImage.color = healthGradient.Evaluate(healthPercentage);
        }
        
        // Update animator parameter (write-only)
        if (animator != null)
        {
            animator.SetFloat(animatorParameterName, healthPercentage);
        }
        
        // Update health text
        UpdateHealthText();
    }
    
    private void UpdateHealthText()
    {
        if (!showHealthText || healthText == null) return;
        
        if (showDecimals)
        {
            healthText.text = $"{storedCurrentHealth:F1}/{storedMaxHealth:F1}";
        }
        else
        {
            healthText.text = $"{Mathf.CeilToInt(storedCurrentHealth)}/{Mathf.CeilToInt(storedMaxHealth)}";
        }
    }
}