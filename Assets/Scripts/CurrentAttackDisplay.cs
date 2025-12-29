using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple display component for showing the current attack in the side panel.
/// Formatted to match the DraggableAttackSlot active attack styling.
/// </summary>
public class CurrentAttackDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image activeIndicator; // Visual indicator that this is the active attack
    [SerializeField] private Slider levelProgressBar; // Optional: shows progress to max level
    
    [Header("Colors")]
    [SerializeField] private Color activeSlotColor = new Color(0.3f, 1f, 0.3f, 1f); // Green for active attack (matches DraggableAttackSlot)
    [SerializeField] private Color normalColor = new Color(0.8f, 0.8f, 0.8f, 1f); // Normal slot color (matches DraggableAttackSlot)
    [SerializeField] private Color maxLevelColor = new Color(0.8f, 0.6f, 0.1f, 0.8f); // Gold for max level
    
    [Header("Display Mode")]
    [SerializeField] private bool showAsActive = true; // Whether to show this as the active attack (with green highlight)
    
    private void Awake()
    {
        // Auto-find components if not assigned
        if (iconImage == null)
            iconImage = transform.Find("Icon")?.GetComponent<Image>();
        if (nameText == null)
            nameText = transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (levelText == null)
            levelText = transform.Find("Level")?.GetComponent<TextMeshProUGUI>();
        if (statsText == null)
            statsText = transform.Find("Stats")?.GetComponent<TextMeshProUGUI>();
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
        if (activeIndicator == null)
            activeIndicator = transform.Find("ActiveIndicator")?.GetComponent<Image>();
        if (levelProgressBar == null)
            levelProgressBar = GetComponentInChildren<Slider>();
    }
    
    /// <summary>
    /// Initialize the display with attack data
    /// </summary>
    public void Initialize(Attack attack)
    {
        Initialize(attack, true); // Default to showing as active
    }
    
    /// <summary>
    /// Initialize the display with attack data and specify if it should show as active
    /// </summary>
    public void Initialize(Attack attack, bool isActive)
    {
        if (attack == null) return;
        
        showAsActive = isActive;
        
        int currentLevel = attack.GetCurrentLevel();
        int maxLevel = attack.GetMaxLevel();
        bool isMaxLevel = !attack.CanUpgrade();
        
        // Set icon
        if (iconImage != null)
        {
            if (attack.attackIcon != null)
            {
                iconImage.sprite = attack.attackIcon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }
        
        // Set name
        if (nameText != null)
        {
            nameText.text = attack.attackName;
        }
        
        // Set level text - match DraggableAttackSlot format "Lvl X"
        if (levelText != null)
        {
            if (isMaxLevel)
            {
                levelText.text = $"Lvl {currentLevel} (MAX)";
            }
            else
            {
                levelText.text = $"Lvl {currentLevel}";
            }
        }
        
        // Set stats text
        if (statsText != null)
        {
            float damage = attack.GetDamage();
            float range = attack.GetRange();
            statsText.text = $"DMG: {damage:F1}  RNG: {range:F1}";
        }
        
        // Set background color - use active color if showing as active attack (matches DraggableAttackSlot)
        if (backgroundImage != null)
        {
            if (isMaxLevel)
            {
                backgroundImage.color = maxLevelColor;
            }
            else if (showAsActive)
            {
                backgroundImage.color = activeSlotColor;
            }
            else
            {
                backgroundImage.color = normalColor;
            }
        }
        
        // Show/hide active indicator (matches DraggableAttackSlot behavior)
        if (activeIndicator != null)
        {
            activeIndicator.gameObject.SetActive(showAsActive);
        }
        
        // Set progress bar
        if (levelProgressBar != null)
        {
            levelProgressBar.minValue = 0;
            levelProgressBar.maxValue = maxLevel;
            levelProgressBar.value = currentLevel;
        }
    }
    
    /// <summary>
    /// Set whether this display should show as the active attack
    /// </summary>
    public void SetShowAsActive(bool isActive)
    {
        showAsActive = isActive;
    }
}