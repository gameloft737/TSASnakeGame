using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaveUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider appleCountSlider;
    [SerializeField] private TextMeshProUGUI appleCountText;
    [SerializeField] private TextMeshProUGUI waveNumberText;

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
}