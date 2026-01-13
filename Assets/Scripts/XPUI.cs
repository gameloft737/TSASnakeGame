using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class XPUI : MonoBehaviour
{
    [Header("XP Bar")]
    [SerializeField] private Slider xpSlider;
    [SerializeField] private Image xpFillImage;
    [SerializeField] private TextMeshProUGUI xpText;
    
    [Header("Level Display")]
    [SerializeField] private TextMeshProUGUI levelText;
    
    [Header("Visual Settings")]
    [SerializeField] private Color xpBarColor = new Color(0.5f, 0.8f, 1f);
    [SerializeField] private bool animateXPGain = true;
    [SerializeField] private float animationSpeed = 5f;
    
    private float targetXPValue = 0f;
    private float currentDisplayXP = 0f;
    private bool isAnimating = false;
    
    private void Start()
    {
        if (xpFillImage) xpFillImage.color = xpBarColor;
        
        if (XPManager.Instance)
        {
            XPManager.Instance.OnXPChanged.AddListener(UpdateXP);
            XPManager.Instance.OnLevelUp.AddListener(OnLevelUp);
            
            UpdateXP(XPManager.Instance.GetCurrentXP(), XPManager.Instance.GetXPToNextLevel());
            UpdateLevel(XPManager.Instance.GetCurrentLevel());
        }
    }
    
    private void OnDestroy()
    {
        if (XPManager.Instance)
        {
            XPManager.Instance.OnXPChanged.RemoveListener(UpdateXP);
            XPManager.Instance.OnLevelUp.RemoveListener(OnLevelUp);
        }
    }
    
    private void Update()
    {
        if (!isAnimating || !animateXPGain || !xpSlider) return;
        
        currentDisplayXP = Mathf.Lerp(currentDisplayXP, targetXPValue, animationSpeed * Time.deltaTime);
        xpSlider.value = currentDisplayXP;
        
        if (Mathf.Abs(currentDisplayXP - targetXPValue) < 0.001f)
        {
            currentDisplayXP = targetXPValue;
            xpSlider.value = currentDisplayXP;
            isAnimating = false;
        }
    }
    
    public void UpdateXP(int currentXP, int xpToNextLevel)
    {
        targetXPValue = (float)currentXP / xpToNextLevel;
        
        if (xpSlider)
        {
            xpSlider.maxValue = 1f;
            
            if (!animateXPGain)
            {
                xpSlider.value = targetXPValue;
            }
            else
            {
                isAnimating = true;
            }
        }
        
        if (xpText) xpText.text = $"{currentXP} / {xpToNextLevel} XP";
    }
    
    public void UpdateLevel(int level)
    {
        if (levelText) levelText.text = $"Rank {level}";
    }
    
    private void OnLevelUp(int newLevel)
    {
        UpdateLevel(newLevel);
        currentDisplayXP = 0f;
        isAnimating = true;
    }
}