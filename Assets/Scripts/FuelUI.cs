using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FuelUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider fuelSlider;
    [SerializeField] private Image fillImage; // Optional: reference to slider's fill image
    [SerializeField] private TextMeshProUGUI fuelText; // Optional: text display
    
    [Header("Visual Settings")]
    [SerializeField] private Gradient fuelGradient; // Color changes based on fuel level
    [SerializeField] private bool showPercentage = true;
    [SerializeField] private bool showNumericValue = false;
    
    [Header("References")]
    [SerializeField] private AttackManager attackManager;

    private void Start()
    {
        // Setup slider
        if (fuelSlider != null)
        {
            fuelSlider.minValue = 0f;
            fuelSlider.maxValue = 100f;
            fuelSlider.value = 100f;
        }

        // Setup gradient if not configured
        if (fuelGradient == null || fuelGradient.colorKeys.Length == 0)
        {
            SetupDefaultGradient();
        }

        // Auto-find fill image if not assigned
        if (fillImage == null && fuelSlider != null)
        {
            fillImage = fuelSlider.fillRect?.GetComponent<Image>();
        }
    }

    private void Update()
    {
        UpdateFuelDisplay();
    }

    private void UpdateFuelDisplay()
    {
        Attack currentAttack = attackManager?.GetCurrentAttack();
        
        if (currentAttack == null) return;

        float currentFuel = currentAttack.GetCurrentFuel();
        float fuelPercentage = currentAttack.GetFuelPercentage();

        // Update slider
        if (fuelSlider != null)
        {
            fuelSlider.value = currentFuel;
        }

        // Update fill color based on fuel level
        if (fillImage != null && fuelGradient != null)
        {
            fillImage.color = fuelGradient.Evaluate(fuelPercentage);
        }

        // Update text display
        if (fuelText != null)
        {
            if (showPercentage && showNumericValue)
            {
                fuelText.text = $"{currentFuel:F0} ({fuelPercentage * 100f:F0}%)";
            }
            else if (showPercentage)
            {
                fuelText.text = $"{fuelPercentage * 100f:F0}%";
            }
            else if (showNumericValue)
            {
                fuelText.text = $"{currentFuel:F0}";
            }
        }
    }

    private void SetupDefaultGradient()
    {
        fuelGradient = new Gradient();
        
        // Red -> Yellow -> Green gradient
        GradientColorKey[] colorKeys = new GradientColorKey[3];
        colorKeys[0] = new GradientColorKey(Color.red, 0f);      // Empty = Red
        colorKeys[1] = new GradientColorKey(Color.yellow, 0.5f);  // Half = Yellow
        colorKeys[2] = new GradientColorKey(Color.green, 1f);     // Full = Green
        
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);
        
        fuelGradient.SetKeys(colorKeys, alphaKeys);
    }

    // Optional: Public method to flash/shake UI when fuel depletes
    public void OnFuelDepleted()
    {
        // You can add animation/effects here
        Debug.Log("Fuel depleted!");
    }

    // Optional: Show activation threshold line
    private void OnValidate()
    {
        if (fuelSlider != null && Application.isPlaying)
        {
            fuelSlider.maxValue = 100f;
        }
    }
}