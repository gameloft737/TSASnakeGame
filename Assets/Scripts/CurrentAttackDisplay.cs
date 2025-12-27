using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple display component for showing the current attack in the side panel
/// </summary>
public class CurrentAttackDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Slider levelProgressBar; // Optional: shows progress to max level
    
    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color maxLevelColor = new Color(0.8f, 0.6f, 0.1f, 0.8f); // Gold for max level
    
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
        if (levelProgressBar == null)
            levelProgressBar = GetComponentInChildren<Slider>();
    }
    
    /// <summary>
    /// Initialize the display with attack data
    /// </summary>
    public void Initialize(Attack attack)
    {
        if (attack == null) return;
        
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
        
        // Set level text
        if (levelText != null)
        {
            if (isMaxLevel)
            {
                levelText.text = $"Lv. {currentLevel} (MAX)";
            }
            else
            {
                levelText.text = $"Lv. {currentLevel}/{maxLevel}";
            }
        }
        
        // Set stats text
        if (statsText != null)
        {
            float damage = attack.GetDamage();
            float range = attack.GetRange();
            statsText.text = $"DMG: {damage:F1}  RNG: {range:F1}";
        }
        
        // Set background color
        if (backgroundImage != null)
        {
            backgroundImage.color = isMaxLevel ? maxLevelColor : normalColor;
        }
        
        // Set progress bar
        if (levelProgressBar != null)
        {
            levelProgressBar.minValue = 0;
            levelProgressBar.maxValue = maxLevel;
            levelProgressBar.value = currentLevel;
        }
    }
}