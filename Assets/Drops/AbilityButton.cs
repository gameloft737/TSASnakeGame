using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilityButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button button;  // The button component on this prefab
    [SerializeField] private Image abilityIcon;  // The icon image for the ability
    [SerializeField] private TextMeshProUGUI abilityNameText;  // Optional text to display ability name

    private AbilitySO abilitySO;  // The Ability ScriptableObject to associate with this button
    private AbilityCollector abilityCollector;  // The ability collector to add the ability to the player

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
            abilityNameText = GetComponentInChildren<TextMeshProUGUI>();
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

        // Set up the button click listener
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }

        // Set the ability icon if available
        if (abilitySO != null && abilityIcon != null && abilitySO.icon != null)
        {
            abilityIcon.sprite = abilitySO.icon;
            abilityIcon.enabled = true;
        }

        // Set the ability name text if available
        if (abilitySO != null && abilityNameText != null)
        {
            abilityNameText.text = abilitySO.abilityName;
            Debug.Log($"AbilityButton: Set text to '{abilitySO.abilityName}'");
        }
        else if (abilitySO != null && abilityNameText == null)
        {
            Debug.LogWarning($"AbilityButton: No TextMeshProUGUI found for ability '{abilitySO.abilityName}'. Make sure the prefab has a TextMeshProUGUI component.");
        }
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
