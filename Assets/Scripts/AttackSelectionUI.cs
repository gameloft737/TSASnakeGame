using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AttackSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private Transform attackButtonContainer;
    [SerializeField] private GameObject attackButtonPrefab;
    [SerializeField] private Button continueButton;
    
    [SerializeField]private AttackManager attackManager;
    [SerializeField]private WaveManager waveManager;
    private List<GameObject> spawnedButtons = new List<GameObject>();

    private void Start()
    {
        if (waveManager == null)
        {
            waveManager = FindFirstObjectByType<WaveManager>();
        }
        
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        
        HideAttackSelection();
    }

    public void ShowAttackSelection(AttackManager manager)
    {
        attackManager = manager;
        
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(true);
        }
        
        // Clear old buttons
        foreach (var button in spawnedButtons)
        {
            Destroy(button);
        }
        spawnedButtons.Clear();
        
        // Create button for each attack
        for (int i = 0; i < attackManager.GetAttackCount(); i++)
        {
            int attackIndex = i;
            Attack attack = attackManager.attacks[i];
            
            GameObject buttonObj = Instantiate(attackButtonPrefab, attackButtonContainer);
            spawnedButtons.Add(buttonObj);
            
            // Setup button
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            Image buttonImage = buttonObj.GetComponent<Image>();
            
            if (buttonText != null && attack != null)
            {
                buttonText.text = attack.attackName;
            }
            
            // Highlight current attack
            if (attackIndex == attackManager.GetCurrentAttackIndex())
            {
                if (buttonImage != null)
                {
                    buttonImage.color = Color.green;
                }
            }
            
            if (button != null)
            {
                button.onClick.AddListener(() => OnAttackButtonClicked(attackIndex));
            }
        }
        
        // Pause game
        Time.timeScale = 0f;
    }

    public void HideAttackSelection()
    {
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }
        
        // Resume game
        Time.timeScale = 1f;
    }

    private void OnAttackButtonClicked(int attackIndex)
    {
        if (attackManager != null)
        {
            attackManager.SetAttackIndex(attackIndex);
            Debug.Log($"Selected attack: {attackManager.GetCurrentAttack().attackName}");
        }
        
        // Refresh UI to show new selection
        if (attackManager != null)
        {
            ShowAttackSelection(attackManager);
        }
    }

    private void OnContinueClicked()
    {
        HideAttackSelection();
        
        if (waveManager != null)
        {
            waveManager.OnAttackSelected();
        }
    }
}