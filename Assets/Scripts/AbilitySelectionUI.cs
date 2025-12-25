using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI for selecting one of three random abilities
/// </summary>
public class AbilitySelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private Button[] abilityButtons; // 3 buttons
    [SerializeField] private TextMeshProUGUI[] abilityNameTexts;
    [SerializeField] private TextMeshProUGUI[] abilityDescTexts;
    [SerializeField] private TextMeshProUGUI[] abilityLevelTexts;
    [SerializeField] private Image[] abilityIcons;

    [Header("Settings")]
    [SerializeField] private bool pauseGameOnOpen = true;

    private AbilityOption[] currentOptions;
    private System.Action<GameObject> onAbilitySelected;

    private void Awake()
    {
        // Setup button listeners
        for (int i = 0; i < abilityButtons.Length; i++)
        {
            int index = i; // Capture for closure
            abilityButtons[i].onClick.AddListener(() => SelectAbility(index));
        }
        
        Hide();
    }

    public void Show(AbilityOption[] options, System.Action<GameObject> callback)
    {
        currentOptions = options;
        onAbilitySelected = callback;
        
        // Display options
        for (int i = 0; i < abilityButtons.Length && i < options.Length; i++)
        {
            DisplayOption(i, options[i]);
            abilityButtons[i].gameObject.SetActive(true);
        }
        
        // Hide unused buttons
        for (int i = options.Length; i < abilityButtons.Length; i++)
        {
            abilityButtons[i].gameObject.SetActive(false);
        }
        
        selectionPanel.SetActive(true);
        
        if (pauseGameOnOpen)
        {
            Time.timeScale = 0f;
        }
    }

    private void DisplayOption(int index, AbilityOption option)
    {
        if (option.existingAbility != null)
        {
            // Existing ability - show level up info
            int currentLevel = option.existingAbility.GetCurrentLevel();
            bool isMaxLevel = option.existingAbility.IsMaxLevel();
            
            abilityNameTexts[index].text = option.abilityName;
            abilityLevelTexts[index].text = isMaxLevel ? 
                $"Level {currentLevel} (MAX) - Extend Duration" : 
                $"Level {currentLevel} → {currentLevel + 1}";
            abilityDescTexts[index].text = option.description;
        }
        else
        {
            // New ability
            abilityNameTexts[index].text = option.abilityName;
            abilityLevelTexts[index].text = "NEW - Level 1";
            abilityDescTexts[index].text = option.description;
        }
        
        if (abilityIcons[index] != null && option.icon != null)
        {
            abilityIcons[index].sprite = option.icon;
        }
    }

    private void SelectAbility(int index)
    {
        if (index < 0 || index >= currentOptions.Length) return;
        
        onAbilitySelected?.Invoke(currentOptions[index].abilityPrefab);
        Hide();
    }

    public void Hide()
    {
        selectionPanel.SetActive(false);
        
        if (pauseGameOnOpen)
        {
            Time.timeScale = 1f;
        }
    }
}

[System.Serializable]
public class AbilityOption
{
    public GameObject abilityPrefab;
    public string abilityName;
    public string description;
    public Sprite icon;
    public BaseAbility existingAbility; // If upgrading existing ability
}