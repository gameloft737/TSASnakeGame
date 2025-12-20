using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaveUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider appleCountSlider;
    [SerializeField] private TextMeshProUGUI appleCountText;
    [SerializeField] private TextMeshProUGUI waveNumberText;
    
    [Header("Death Screen")]
    [SerializeField] private GameObject deathScreenPanel;
    [SerializeField] private TextMeshProUGUI deathText;

    private void Start()
    {
        // Make sure death screen is hidden at start
        if (deathScreenPanel != null)
        {
            deathScreenPanel.SetActive(false);
        }
    }

    public void UpdateAppleCount(int current, int total)
    {
        if (appleCountSlider != null)
        {
            appleCountSlider.maxValue = total;
            appleCountSlider.value = current;
        }
        
        if (appleCountText != null)
        {
            appleCountText.text = $"{current} / {total}";
        }
    }

    public void UpdateWaveNumber(int waveNumber)
    {
        if (waveNumberText != null)
        {
            waveNumberText.text = $"Wave {waveNumber}";
        }
    }
    
    public void ShowDeathScreen(bool show)
    {
        if (deathScreenPanel != null)
        {
            deathScreenPanel.SetActive(show);
        }
        
        if (show && deathText != null)
        {
            deathText.text = "YOU'RE DEAD!";
        }
    }
}