using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple display component for showing a current ability in the side panel
/// </summary>
public class CurrentAbilityDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
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
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
        if (levelProgressBar == null)
            levelProgressBar = GetComponentInChildren<Slider>();
    }
    
    /// <summary>
    /// Initialize the display with ability data
    /// </summary>
    public void Initialize(BaseAbility ability, AbilitySO abilitySO)
    {
        if (ability == null) return;
        
        int currentLevel = ability.GetCurrentLevel();
        int maxLevel = ability.GetMaxLevel();
        bool isMaxLevel = ability.IsMaxLevel();
        
        // Set icon
        if (iconImage != null)
        {
            if (abilitySO != null && abilitySO.icon != null)
            {
                iconImage.sprite = abilitySO.icon;
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
            string abilityName = abilitySO != null ? abilitySO.abilityName : ability.gameObject.name.Replace("(Clone)", "").Trim();
            nameText.text = abilityName;
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