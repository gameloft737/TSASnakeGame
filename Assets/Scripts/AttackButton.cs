using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI Button component for displaying attack or ability upgrade information
/// </summary>
public class AttackButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI attackNameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject upgradeIndicator; // Optional: shows when upgrade is available
    [SerializeField] private GameObject newIndicator; // Shows when attack is new (level 1)
    
    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;
    
    private Attack attack;
    private AbilitySO abilitySO;
    private int attackIndex;
    private int currentAbilityLevel;
    private AttackSelectionUI selectionUI;
    private Button button;
    private bool isAbilityMode = false;
    private Color customNormalColor;
    private Color customSelectedColor;
    private bool useCustomColors = false;
    
    private void Awake()
    {
        button = GetComponent<Button>();
        
        // Auto-find UI components if not assigned
        if (attackNameText == null)
            attackNameText = transform.Find("AttackName")?.GetComponent<TextMeshProUGUI>();
        if (levelText == null)
            levelText = transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
        if (descriptionText == null)
            descriptionText = transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
        if (iconImage == null)
            iconImage = transform.Find("Icon")?.GetComponent<Image>();
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
        if (newIndicator == null)
            newIndicator = transform.Find("NewIndicator")?.gameObject;
            
        // Set button color block to use only normal and pressed
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.highlightedColor = colors.normalColor;
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
        }
    }
    
    /// <summary>
    /// Initialize the button with attack data
    /// </summary>
    public void Initialize(Attack attack, int attackIndex, AttackSelectionUI selectionUI, bool isSelected)
    {
        this.attack = attack;
        this.attackIndex = attackIndex;
        this.selectionUI = selectionUI;
        this.playerOwnsAttack = false;
        this.useCustomColors = false;
        
        UpdateDisplay(isSelected);
        
        // Set up button click
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }
    }
    
    private bool playerOwnsAttack = false;
    
    /// <summary>
    /// Initialize the button with attack data and ownership info
    /// </summary>
    public void InitializeWithOwnership(Attack attack, int attackIndex, AttackSelectionUI selectionUI, bool isSelected, bool ownsAttack)
    {
        this.attack = attack;
        this.attackIndex = attackIndex;
        this.selectionUI = selectionUI;
        this.playerOwnsAttack = ownsAttack;
        this.useCustomColors = false;
        
        UpdateDisplay(isSelected);
        
        // Set up button click
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }
    }
    
    /// <summary>
    /// Initialize the button with ability data
    /// </summary>
    public void InitializeWithAbility(AbilitySO abilitySO, int currentLevel, AttackSelectionUI selectionUI, bool isSelected)
    {
        this.abilitySO = abilitySO;
        this.attack = null;
        this.currentAbilityLevel = currentLevel;
        this.selectionUI = selectionUI;
        this.isAbilityMode = true;
        this.useCustomColors = false;
        
        UpdateAbilityDisplay(isSelected);
        
        // Set up button click
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnAbilityButtonClicked);
        }
    }
    
    /// <summary>
    /// Set custom colors for this button (used for swaps and new attacks)
    /// </summary>
    public void SetCustomColors(Color normal, Color selected)
    {
        this.customNormalColor = normal;
        this.customSelectedColor = selected;
        this.useCustomColors = true;
    }
    
    /// <summary>
    /// Updates the display with current ability information
    /// </summary>
    public void UpdateAbilityDisplay(bool isSelected)
    {
        if (abilitySO == null) return;
        
        bool isNew = currentAbilityLevel == 0;
        int nextLevel = isNew ? 1 : currentAbilityLevel + 1;
        bool canUpgrade = abilitySO.CanUpgrade(currentAbilityLevel);
        int maxLevel = abilitySO.maxLevel;
        
        // Set ability name
        if (attackNameText != null)
        {
            attackNameText.text = abilitySO.abilityName;
        }
        
        // Set level text
        if (levelText != null)
        {
            if (isNew)
            {
                levelText.text = "(NEW) ";
            }
            else if (canUpgrade)
            {
                levelText.text = $"(Lvl {nextLevel}) ";
            }
            else
            {
                levelText.text = $"(Lvl {currentAbilityLevel})(MAX) ";
            }
        }
        
        // Set description from upgrade data
        if (descriptionText != null)
        {
            string description = abilitySO.GetDescriptionForLevel(nextLevel);
            if (!string.IsNullOrEmpty(description))
            {
                if (!canUpgrade && !isNew)
                {
                    descriptionText.text = description + " (Maxed)";
                }
                else
                {
                    descriptionText.text = description;
                }
            }
            else if (!string.IsNullOrEmpty(abilitySO.description))
            {
                descriptionText.text = abilitySO.description;
            }
            else
            {
                descriptionText.text = isNew ? "Gain this ability" : $"Level up to Lvl {nextLevel}";
            }
        }
        
        // Set icon
        if (iconImage != null)
        {
            if (abilitySO.icon != null)
            {
                iconImage.sprite = abilitySO.icon;
                iconImage.enabled = true;
            }
            else if (abilitySO.upgradeData != null && abilitySO.upgradeData.abilityIcon != null)
            {
                iconImage.sprite = abilitySO.upgradeData.abilityIcon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }
        
        // Set background color - only normal or selected
        if (backgroundImage != null)
        {
            if (isSelected)
            {
                backgroundImage.color = useCustomColors ? customSelectedColor : selectedColor;
            }
            else
            {
                backgroundImage.color = useCustomColors ? customNormalColor : normalColor;
            }
        }
        
        // Show/hide indicators
        if (newIndicator != null)
        {
            newIndicator.SetActive(isNew);
        }
        
        if (upgradeIndicator != null)
        {
            upgradeIndicator.SetActive(!isNew && canUpgrade);
        }
    }
    
    private void OnAbilityButtonClicked()
    {
        if (selectionUI != null && abilitySO != null)
        {
            selectionUI.OnAbilitySelected(abilitySO);
        }
    }
    
    /// <summary>
    /// Updates the display with current attack information
    /// </summary>
    public void UpdateDisplay(bool isSelected)
    {
        // If in ability mode, use ability display instead
        if (isAbilityMode)
        {
            UpdateAbilityDisplay(isSelected);
            return;
        }
        
        if (attack == null) return;
        
        AttackUpgradeData upgradeData = attack.GetUpgradeData();
        int currentLevel = attack.GetCurrentLevel();
        int maxLevel = attack.GetMaxLevel();
        bool canUpgrade = attack.CanUpgrade();
        
        // Attack is "new" if the player doesn't own it yet
        bool isNew = !playerOwnsAttack;
        
        // Set attack name
        if (attackNameText != null)
        {
            if (upgradeData != null)
                attackNameText.text = upgradeData.attackName;
            else
                attackNameText.text = attack.attackName;
        }
        
        // Set level text - show what level the attack will be after selection
        if (levelText != null)
        {
            if (upgradeData != null)
            {
                if (isNew)
                {
                    // New attacks will become level 1
                    levelText.text = "(Lvl 1) ";
                }
                else if (canUpgrade)
                {
                    // Show the level it will become after upgrade
                    int nextLevel = currentLevel + 1;
                    levelText.text = $"(Lvl {nextLevel}) ";
                }
                else
                {
                    // Already at max level
                    levelText.text = $"(Lvl {currentLevel})(MAX) ";
                }
            }
            else
            {
                levelText.text = ""; // No upgrade data, hide level
            }
        }
        
        // Set description - show what the NEXT level offers (or current level for new attacks)
        if (descriptionText != null)
        {
            if (isNew && upgradeData != null)
            {
                // For new attacks, show the level 1 description
                AttackLevelStats currentStats = upgradeData.GetStatsForLevel(currentLevel);
                descriptionText.text = currentStats.description;
            }
            else if (upgradeData != null && canUpgrade)
            {
                int nextLevel = currentLevel + 1;
                AttackLevelStats nextStats = upgradeData.GetStatsForLevel(nextLevel);
                descriptionText.text = nextStats.description;
            }
            else if (upgradeData != null)
            {
                // At max level, show current level description
                AttackLevelStats currentStats = upgradeData.GetStatsForLevel(currentLevel);
                descriptionText.text = currentStats.description + " (Maxed)";
            }
            else
            {
                descriptionText.text = "";
            }
        }
        
        // Set icon
        if (iconImage != null)
        {
            if (upgradeData != null && upgradeData.attackIcon != null)
            {
                iconImage.sprite = upgradeData.attackIcon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }
        
        // Set background color - only normal or selected
        if (backgroundImage != null)
        {
            if (isSelected)
            {
                backgroundImage.color = useCustomColors ? customSelectedColor : selectedColor;
            }
            else
            {
                backgroundImage.color = useCustomColors ? customNormalColor : normalColor;
            }
        }
        
        // Show/hide new indicator (only for attacks the player doesn't own)
        if (newIndicator != null)
        {
            newIndicator.SetActive(isNew);
        }
        
        // Show/hide upgrade indicator (only for owned attacks that can upgrade)
        if (upgradeIndicator != null)
        {
            upgradeIndicator.SetActive(!isNew && canUpgrade);
        }
    }
    
    /// <summary>
    /// Set whether this button is selected
    /// </summary>
    public void SetSelected(bool selected)
    {
        UpdateDisplay(selected);
    }
    
    private void OnButtonClicked()
    {
        if (selectionUI != null)
        {
            selectionUI.OnAttackButtonClicked(attackIndex);
        }
    }
    
    /// <summary>
    /// Gets the attack index this button represents
    /// </summary>
    public int GetAttackIndex() => attackIndex;
    
    /// <summary>
    /// Gets the attack this button represents
    /// </summary>
    public Attack GetAttack() => attack;
}