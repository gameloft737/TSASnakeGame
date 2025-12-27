using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for displaying player XP and level
/// </summary>
public class XPUI : MonoBehaviour
{
    [Header("XP Bar")]
    [SerializeField] private Slider xpSlider;
    [SerializeField] private Image xpFillImage;
    [SerializeField] private TextMeshProUGUI xpText;
    
    [Header("Level Display")]
    [SerializeField] private TextMeshProUGUI levelText;
    
    [Header("Wave XP Progress")]
    [SerializeField] private Slider waveXPSlider;
    [SerializeField] private TextMeshProUGUI waveXPText;
    
    [Header("Visual Settings")]
    [SerializeField] private Color xpBarColor = new Color(0.5f, 0.8f, 1f);
    [SerializeField] private Color waveXPBarColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private bool animateXPGain = true;
    [SerializeField] private float animationSpeed = 5f;
    
    private float targetXPValue = 0f;
    private float currentDisplayXP = 0f;
    
    private void Start()
    {
        // Set bar colors
        if (xpFillImage != null)
        {
            xpFillImage.color = xpBarColor;
        }
        
        // Subscribe to XP events
        if (XPManager.Instance != null)
        {
            XPManager.Instance.OnXPChanged.AddListener(UpdateXPDisplay);
            XPManager.Instance.OnLevelUp.AddListener(OnLevelUp);
            
            // Initialize display
            UpdateXPDisplay(XPManager.Instance.GetCurrentXP(), XPManager.Instance.GetXPToNextLevel());
            UpdateLevelDisplay(XPManager.Instance.GetCurrentLevel());
        }
    }
    
    private void OnDestroy()
    {
        if (XPManager.Instance != null)
        {
            XPManager.Instance.OnXPChanged.RemoveListener(UpdateXPDisplay);
            XPManager.Instance.OnLevelUp.RemoveListener(OnLevelUp);
        }
    }
    
    private void Update()
    {
        // Animate XP bar
        if (animateXPGain && xpSlider != null)
        {
            currentDisplayXP = Mathf.Lerp(currentDisplayXP, targetXPValue, animationSpeed * Time.deltaTime);
            xpSlider.value = currentDisplayXP;
        }
    }
    
    /// <summary>
    /// Update the XP display
    /// </summary>
    public void UpdateXPDisplay(int currentXP, int xpToNextLevel)
    {
        targetXPValue = (float)currentXP / xpToNextLevel;
        
        if (xpSlider != null)
        {
            xpSlider.maxValue = 1f;
            
            if (!animateXPGain)
            {
                xpSlider.value = targetXPValue;
            }
        }
        
        if (xpText != null)
        {
            xpText.text = $"{currentXP} / {xpToNextLevel} XP";
        }
    }
    
    /// <summary>
    /// Update the level display
    /// </summary>
    public void UpdateLevelDisplay(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"Lv. {level}";
        }
    }
    
    /// <summary>
    /// Called when player levels up
    /// </summary>
    private void OnLevelUp(int newLevel)
    {
        UpdateLevelDisplay(newLevel);
        
        // Reset animation for smooth transition
        currentDisplayXP = 0f;
        
        // TODO: Play level up effect
    }
    
    /// <summary>
    /// Update wave XP progress display
    /// </summary>
    public void UpdateWaveXPProgress(int currentXP, int requiredXP)
    {
        if (waveXPSlider != null)
        {
            waveXPSlider.maxValue = requiredXP;
            waveXPSlider.value = currentXP;
        }
        
        if (waveXPText != null)
        {
            waveXPText.text = $"{currentXP} / {requiredXP} XP";
        }
    }
}