using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FuelUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider fuelSlider;
    [SerializeField] private Image fillImage; // Optional: reference to slider's fill image
    [SerializeField] private TextMeshProUGUI fuelText; // Optional: text display
    [SerializeField] private TextMeshProUGUI additionalText; // Optional: additional text that shows/hides with fuel UI
    
    [Header("Visual Settings")]
    [SerializeField] private Gradient fuelGradient; // Color changes based on fuel level
    [SerializeField] private bool showPercentage = true;
    [SerializeField] private bool showNumericValue = false;
    
    [Header("References")]
    [SerializeField] private AttackManager attackManager;

    // Cached last-rendered values so we can avoid rebuilding strings / touching the canvas every frame.
    // On WebGL this dramatically reduces GC pressure and canvas rebuilds.
    private int _lastFuelInt = int.MinValue;
    private int _lastPercentInt = int.MinValue;
    private bool _lastAdditionalActive;
    private bool _additionalActiveInitialized;
    private bool _lastAttackWasNull = true;

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
        
        // Hide additional text initially (will be shown when attack is active)
        if (additionalText != null)
        {
            additionalText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        UpdateFuelDisplay();
    }

    private void UpdateFuelDisplay()
    {
        Attack currentAttack = attackManager?.GetCurrentAttack();
        bool attackIsNull = currentAttack == null;

        // Show/hide additional text based on whether there's an active attack — only toggle when state changes
        if (additionalText != null)
        {
            bool shouldBeActive = !attackIsNull;
            if (!_additionalActiveInitialized || shouldBeActive != _lastAdditionalActive)
            {
                additionalText.gameObject.SetActive(shouldBeActive);
                _lastAdditionalActive = shouldBeActive;
                _additionalActiveInitialized = true;
            }
        }
        
        if (attackIsNull)
        {
            _lastAttackWasNull = true;
            return;
        }

        // If we just transitioned from null → valid, force a refresh
        if (_lastAttackWasNull)
        {
            _lastFuelInt = int.MinValue;
            _lastPercentInt = int.MinValue;
            _lastAttackWasNull = false;
        }

        float currentFuel = currentAttack.GetCurrentFuel();
        float fuelPercentage = currentAttack.GetFuelPercentage();
        int fuelInt = Mathf.RoundToInt(currentFuel);
        int percentInt = Mathf.RoundToInt(fuelPercentage * 100f);

        // Only update slider if value actually changed (avoids canvas rebuild)
        if (fuelSlider != null && fuelInt != _lastFuelInt)
        {
            fuelSlider.value = currentFuel;
        }

        // Only update fill color when percent bucket changes
        if (fillImage != null && fuelGradient != null && percentInt != _lastPercentInt)
        {
            fillImage.color = fuelGradient.Evaluate(fuelPercentage);
        }

        // Update text display — only re-string when display values change
        if (fuelText != null && (fuelInt != _lastFuelInt || percentInt != _lastPercentInt))
        {
            if (showPercentage && showNumericValue)
            {
                fuelText.text = $"{fuelInt} ({percentInt}%)";
            }
            else if (showPercentage)
            {
                fuelText.text = $"{percentInt}%";
            }
            else if (showNumericValue)
            {
                fuelText.text = fuelInt.ToString();
            }
        }

        _lastFuelInt = fuelInt;
        _lastPercentInt = percentInt;
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
    }
    
    /// <summary>
    /// Sets the additional text content that appears with the fuel UI
    /// </summary>
    /// <param name="text">The text to display</param>
    public void SetAdditionalText(string text)
    {
        if (additionalText != null)
        {
            additionalText.text = text;
        }
    }
    
    /// <summary>
    /// Gets the current additional text content
    /// </summary>
    public string GetAdditionalText()
    {
        return additionalText != null ? additionalText.text : string.Empty;
    }
    
    /// <summary>
    /// Shows or hides the additional text
    /// </summary>
    /// <param name="visible">Whether the text should be visible</param>
    public void SetAdditionalTextVisible(bool visible)
    {
        if (additionalText != null)
        {
            additionalText.gameObject.SetActive(visible);
        }
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