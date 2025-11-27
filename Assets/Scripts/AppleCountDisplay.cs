using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AppleCountDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image spriteImage;
    [SerializeField] private TextMeshProUGUI countText;
    
    public void Initialize(Sprite sprite, int count)
    {
        if (spriteImage != null)
        {
            spriteImage.sprite = sprite;
        }
        
        if (countText != null)
        {
            countText.text = "x" + count.ToString();
        }
    }
}