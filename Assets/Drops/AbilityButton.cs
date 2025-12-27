using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilityButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button button;  // The button component on this prefab
    [SerializeField] private Image abilityIcon;  // The icon image for the ability
    [SerializeField] private TextMeshProUGUI abilityNameText;  // Text to display ability name
    [SerializeField] private TextMeshProUGUI levelText;  // Text to display level info
    [SerializeField] private TextMeshProUGUI descriptionText;  // Text to display ability description
    [SerializeField] private GameObject newIndicator;  // Shows when ability is new (not yet acquired)
    [SerializeField] private GameObject upgradeIndicator;  // Shows when ability can be upgraded
    [SerializeField] private Image backgroundImage;  // Background image for color changes
    
    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color newColor = new Color(0.5f, 0.8f, 1f);  // Light blue for new abilities
    [SerializeField] private Color maxLevelColor = Color.yellow;

    private AbilitySO abilitySO;  // The Ability ScriptableObject to associate with this button
    private AbilityCollector abilityCollector;  // The ability collector to add the ability to the player
    private AbilityManager abilityManager;  // Reference to check current ability levels

    private void Awake()
    {
        // Get button component if not assigned
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        // Get TextMeshProUGUI component if not assigned
        if (abilityNameText == null)
        {
            abilityNameText = transform.Find("AbilityName")?.GetComponent<TextMeshProUGUI>();
            if (abilityNameText == null)
            {
                abilityNameText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }
        
        // Try to find level text
        if (levelText == null)
        {
            levelText = transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
        }
        
        // Try to find description text
        if (descriptionText == null)
        {
            descriptionText = transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
        }
        
        // Try to find new indicator
        if (newIndicator == null)
        {
            newIndicator = transform.Find("NewIndicator")?.gameObject;
            if (newIndicator == null)
            {
                newIndicator = transform.Find("New")?.gameObject;
            }
        }
        
        // Try to find upgrade indicator
        if (upgradeIndicator == null)
        {
            upgradeIndicator = transform.Find("UpgradeIndicator")?.gameObject;
            if (upgradeIndicator == null)
            {
                upgradeIndicator = transform.Find("Upgrade")?.gameObject;
            }
        }
        
        // Try to find background image
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        // Get Image component for icon if not assigned (look for child named "Icon" or first child Image)
        if (abilityIcon == null)
        {
            // Try to find an Image component in children (not the button's own image)
            Image[] images = GetComponentsInChildren<Image>();
            foreach (Image img in images)
            {
                if (img.gameObject != this.gameObject && img.gameObject.name.ToLower().Contains("icon"))
                {
                    abilityIcon = img;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Initialize the button with an ability and collector reference
    /// </summary>
    /// <param name="ability">The AbilitySO to associate with this button</param>
    /// <param name="collector">The AbilityCollector that will handle adding the ability</param>
    public void Initialize(AbilitySO ability, AbilityCollector collector)
    {
        abilitySO = ability;
        abilityCollector = collector;
        
        // Find the ability manager to check current levels
        abilityManager = FindFirstObjectByType<AbilityManager>();

        // Set up the button click listener
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }

        UpdateDisplay();
    }
    
    /// <summary>
    /// Updates the display with current ability information
    /// </summary>
    private void UpdateDisplay()
    {
        if (abilitySO == null) return;
        
        // Get current level info
        int currentLevel = 0;
        int maxLevel = 3; // Default max level from BaseAbility
        bool isNew = true;
        bool isMaxLevel = false;
        
        if (abilityManager != null && abilitySO.abilityPrefab != null)
        {
            BaseAbility existingAbility = abilityManager.GetAbility(abilitySO.abilityPrefab);
            if (existingAbility != null)
            {
                currentLevel = existingAbility.GetCurrentLevel();
                maxLevel = existingAbility.GetMaxLevel();
                isNew = false;
                isMaxLevel = existingAbility.IsMaxLevel();
            }
        }

        // Set the ability icon if available
        if (abilityIcon != null && abilitySO.icon != null)
        {
            abilityIcon.sprite = abilitySO.icon;
            abilityIcon.enabled = true;
        }

        // Set the ability name text
        if (abilityNameText != null)
        {
            abilityNameText.text = abilitySO.abilityName;
        }
        
        // Set level text - show what level it will become
        if (levelText != null)
        {
            if (isNew)
            {
                levelText.text = "Lv. 1";
            }
            else if (isMaxLevel)
            {
                levelText.text = $"Lv. {currentLevel} (MAX)";
            }
            else
            {
                int nextLevel = currentLevel + 1;
                levelText.text = $"Lv. {nextLevel}";
            }
        }
        
        // Set description text based on ability type
        if (descriptionText != null)
        {
            descriptionText.text = GetAbilityDescription(isNew ? 1 : currentLevel + 1);
        }
        
        // Set background color
        if (backgroundImage != null)
        {
            if (isNew)
            {
                backgroundImage.color = newColor;
            }
            else if (isMaxLevel)
            {
                backgroundImage.color = maxLevelColor;
            }
            else
            {
                backgroundImage.color = normalColor;
            }
        }
        
        // Show/hide new indicator (only for new abilities)
        if (newIndicator != null)
        {
            newIndicator.SetActive(isNew);
        }
        
        // Show/hide upgrade indicator (only for existing abilities that can still upgrade)
        if (upgradeIndicator != null)
        {
            upgradeIndicator.SetActive(!isNew && !isMaxLevel);
        }
    }
    
    /// <summary>
    /// Gets a description for the ability at the specified level
    /// </summary>
    private string GetAbilityDescription(int level)
    {
        if (abilitySO == null || abilitySO.abilityPrefab == null) return "";
        
        // Check the type of ability and generate appropriate description
        string prefabName = abilitySO.abilityPrefab.name.ToLower();
        
        if (prefabName.Contains("damage"))
        {
            float bonus = 15 * level;
            return $"+{bonus}% damage to all attacks";
        }
        else if (prefabName.Contains("range"))
        {
            float bonus = 20 * level;
            return $"+{bonus}% range to all attacks";
        }
        else if (prefabName.Contains("health") && prefabName.Contains("regen"))
        {
            float bonus = 2 * level;
            return $"+{bonus} HP/sec regeneration";
        }
        else if (prefabName.Contains("health"))
        {
            float bonus = 25 * level;
            return $"+{bonus} max health";
        }
        else if (prefabName.Contains("speed"))
        {
            float bonus = 10 * level;
            return $"+{bonus}% movement speed";
        }
        
        return abilitySO.abilityName;
    }

    // This method will be called when the player clicks the button
    private void OnButtonClicked()
    {
        if (abilitySO != null && abilityCollector != null)
        {
            abilityCollector.SelectAbility(abilitySO);  // Select this ability and close the UI
        }
    }
}
