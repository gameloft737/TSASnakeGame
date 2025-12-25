using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DropShower : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private GameObject upgradeArrow;
    [SerializeField] private GameObject durationIcon;

    public void Initialize(AbilitySO ability, AbilityDrop.DropType dropType, int level, Camera worldSpaceCamera)
    {
        // Set camera for world space canvas
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && worldSpaceCamera != null)
        {
            canvas.worldCamera = worldSpaceCamera;
        }
        
        // Set icon
        if (iconImage != null && ability.icon != null)
        {
            iconImage.sprite = ability.icon;
        }
        
        // Set level text
        if (levelText != null)
        {
            levelText.text = $"Lv.{level}";
        }
        
        // Show appropriate indicators
        if (upgradeArrow != null)
        {
            upgradeArrow.SetActive(dropType == AbilityDrop.DropType.Upgrade);
        }
        
        if (durationIcon != null)
        {
            durationIcon.SetActive(dropType == AbilityDrop.DropType.Duration);
        }
     
    }
}