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
    [SerializeField] private TextMeshProUGUI evolutionPairingText;  // Shows which attack this passive pairs with for evolution
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
        
        // Try to find evolution pairing text
        if (evolutionPairingText == null)
        {
            evolutionPairingText = transform.Find("EvolutionPairingText")?.GetComponent<TextMeshProUGUI>();
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
        
        // Set description text from AbilitySO
        if (descriptionText != null)
        {
            // Use the description from AbilitySO
            if (!string.IsNullOrEmpty(abilitySO.description))
            {
                descriptionText.text = abilitySO.description;
            }
            else
            {
                // Fallback to level-specific description from upgrade data if available
                string levelDescription = abilitySO.GetDescriptionForLevel(isNew ? 1 : currentLevel + 1);
                if (!string.IsNullOrEmpty(levelDescription))
                {
                    descriptionText.text = levelDescription;
                }
                else
                {
                    descriptionText.text = abilitySO.abilityName;
                }
            }
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
        
        // Show evolution pairing text for passive abilities that pair with attacks
        if (evolutionPairingText != null)
        {
            string pairingInfo = GetEvolutionPairingInfo();
            if (!string.IsNullOrEmpty(pairingInfo))
            {
                evolutionPairingText.text = pairingInfo;
                evolutionPairingText.gameObject.SetActive(true);
            }
            else
            {
                evolutionPairingText.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Finds which attack this passive ability pairs with for evolution.
    /// Returns a string like "Evolves: Fire Breath" or empty if no pairing exists.
    /// </summary>
    private string GetEvolutionPairingInfo()
    {
        if (abilitySO == null || abilitySO.abilityPrefab == null) return null;
        
        // Only check for passive abilities
        if (abilitySO.abilityType != AbilityType.Passive) return null;
        
        // Find all attacks in the scene and check their evolution data
        Attack[] allAttacks = Object.FindObjectsByType<Attack>(FindObjectsSortMode.None);
        
        foreach (Attack attack in allAttacks)
        {
            AttackUpgradeData upgradeData = attack.GetUpgradeData();
            if (upgradeData == null || upgradeData.evolutionData == null) continue;
            
            // Check each evolution requirement
            foreach (EvolutionRequirement evolution in upgradeData.evolutionData.evolutions)
            {
                if (evolution.requiredPassivePrefab == abilitySO.abilityPrefab)
                {
                    // This passive pairs with this attack for evolution
                    return $"Evolves: {upgradeData.attackName}";
                }
            }
        }
        
        return null;
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
