using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI Button component for displaying attack upgrade information
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
    [SerializeField] private Color maxLevelColor = Color.yellow;
    [SerializeField] private Color newColor = new Color(0.5f, 0.8f, 1f); // Light blue for new attacks
    
    private Attack attack;
    private int attackIndex;
    private AttackSelectionUI selectionUI;
    private Button button;
    
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
    }
    
    /// <summary>
    /// Initialize the button with attack data
    /// </summary>
    public void Initialize(Attack attack, int attackIndex, AttackSelectionUI selectionUI, bool isSelected)
    {
        this.attack = attack;
        this.attackIndex = attackIndex;
        this.selectionUI = selectionUI;
        this.playerOwnsAttack = false; // Will be set by InitializeWithOwnership
        
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
        
        UpdateDisplay(isSelected);
        
        // Set up button click
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }
    }
    
    /// <summary>
    /// Updates the display with current attack information
    /// </summary>
    public void UpdateDisplay(bool isSelected)
    {
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
        
        // Set background color based on selection, new status, and upgrade status
        if (backgroundImage != null)
        {
            if (isSelected)
            {
                backgroundImage.color = selectedColor;
            }
            else if (isNew)
            {
                backgroundImage.color = newColor;
            }
            else if (!canUpgrade)
            {
                backgroundImage.color = maxLevelColor;
            }
            else
            {
                backgroundImage.color = normalColor;
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