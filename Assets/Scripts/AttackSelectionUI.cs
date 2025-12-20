using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AttackSelectionUI : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator uiAnimator;
    [SerializeField] private string openBool = "isOpen";
    [SerializeField] private Animator deathAnimator;
    [SerializeField] private string deathTrigger = "ShowDeath";
    
    [Header("UI References")]
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private Transform attackButtonContainer;
    [SerializeField] private GameObject attackButtonPrefab;
    [SerializeField] private Button continueButton;

    [SerializeField] private Transform appleCountContainer;
    [SerializeField] private GameObject appleCountPrefab;
    [SerializeField] private GameObject nonUI;
    
    [Header("Death Screen")]
    [SerializeField] private GameObject deathScreenPanel;

    private DepthOfField dof;

    [SerializeField] private AttackManager attackManager;
    [SerializeField] private WaveManager waveManager;
    private List<GameObject> spawnedButtons = new List<GameObject>();

    [Header("DOF Settings")]
    [SerializeField] private float blurTime = 0.4f;
    [SerializeField] private float targetStart = 3f;
    [SerializeField] private float targetEnd = 10f;
    
    private int attackIdxSelected = 0;
    private Coroutine dofRoutine;
    private List<GameObject> spawnedAppleCounts = new List<GameObject>();
    
    private void Start()
    {
        attackIdxSelected = attackManager.GetCurrentAttackIndex();
        postProcessVolume.profile.TryGet(out dof);

        if (uiAnimator == null) uiAnimator = GetComponent<Animator>();
        if (waveManager == null) waveManager = FindFirstObjectByType<WaveManager>();
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);

        HideInstant();
        
        // Make sure death screen is hidden at start
        if (deathScreenPanel != null)
        {
            deathScreenPanel.SetActive(false);
        }
    }

    private IEnumerator LerpDOF(bool enable)
    {
        if (dof == null) yield break;

        dof.active = true;

        float startStart = dof.gaussianStart.value;
        float startEnd = dof.gaussianEnd.value;

        float endStart = enable ? targetStart : 0f;
        float endEnd = enable ? targetEnd : 0f;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / blurTime;
            float lerp = Mathf.Clamp01(t);

            dof.gaussianStart.value = Mathf.Lerp(startStart, endStart, lerp);
            dof.gaussianEnd.value = Mathf.Lerp(startEnd, endEnd, lerp);

            yield return null;
        }

        if (!enable) dof.active = false;
    }

    public void ShowAttackSelection(AttackManager manager)
    {
        attackManager = manager;
        cameraManager.SwitchToPauseCamera();
        nonUI.SetActive(false);
        // Everything happens at the same time on start
        if (selectionPanel != null) selectionPanel.SetActive(true);
        if (uiAnimator != null) uiAnimator.SetBool(openBool, true);
        
        SpawnButtons();
        SpawnAppleCounts(); 

        if (dofRoutine != null) StopCoroutine(dofRoutine);
        dofRoutine = StartCoroutine(LerpDOF(true));
    }
    
    public void ShowDeathScreen(bool show)
    {
        if (deathScreenPanel != null)
        {
            deathScreenPanel.SetActive(show);
        nonUI.SetActive(false);
            
            if (show && deathAnimator != null)
            {
                deathAnimator.SetTrigger(deathTrigger);
            }
        }
    }
    
    private void SpawnAppleCounts()
    {
        // Clear existing apple count displays
        foreach (var display in spawnedAppleCounts) Destroy(display);
        spawnedAppleCounts.Clear();
        
        if (waveManager == null || appleCountContainer == null || appleCountPrefab == null) return;
        
        // Get next wave index
        int nextWaveIndex = waveManager.GetCurrentWaveIndex();
        
        // Check if there's a next wave
        if (nextWaveIndex >= waveManager.GetWaveCount()) return;
        
        WaveData nextWave = waveManager.GetWaveData(nextWaveIndex + 1);
        if (nextWave == null) return;
        
        // Spawn apple count displays from WaveData
        foreach (var spriteCount in nextWave.spriteCounts)
        {
            if (spriteCount.sprite == null || spriteCount.count <= 0) continue;
            
            GameObject displayObj = Instantiate(appleCountPrefab, appleCountContainer);
            spawnedAppleCounts.Add(displayObj);
            
            AppleCountDisplay display = displayObj.GetComponent<AppleCountDisplay>();
            if (display != null)
            {
                display.Initialize(spriteCount.sprite, spriteCount.count);
            }
        }
    }
    
    private void SpawnButtons()
    {
        foreach (var button in spawnedButtons) Destroy(button);
        spawnedButtons.Clear();

        for (int i = 0; i < attackManager.GetAttackCount(); i++)
        {
            int attackIndex = i;
            Attack attack = attackManager.attacks[i];

            GameObject buttonObj = Instantiate(attackButtonPrefab, attackButtonContainer);
            spawnedButtons.Add(buttonObj);

            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            Image buttonImage = buttonObj.GetComponent<Image>();

            if (buttonText != null && attack != null) buttonText.text = attack.attackName;

            if (attackIndex == attackManager.GetCurrentAttackIndex())
            {
                if (buttonImage != null) buttonImage.color = Color.green;
            }

            if (button != null) button.onClick.AddListener(() => OnAttackButtonClicked(attackIndex));
        }
    }

    private void HideInstant()
    {
        if (selectionPanel != null) selectionPanel.SetActive(false);
        dof.active = false;
    }

    private void OnAttackButtonClicked(int attackIndex)
    {
        attackIdxSelected = attackIndex;
    }

    private void OnContinueClicked()
    {
        StartCoroutine(CloseSequence());
    }

    private IEnumerator CloseSequence()
    {
        // Start animation and DOF at the same time
        if (uiAnimator != null) uiAnimator.SetBool(openBool, false);
        
        if (dofRoutine != null) StopCoroutine(dofRoutine);
        dofRoutine = StartCoroutine(LerpDOF(false));
        
        // Wait for both to complete
        yield return dofRoutine;
        
        // Only lift pause after animation and DOF are done
        if (selectionPanel != null) selectionPanel.SetActive(false);
        cameraManager.SwitchToNormalCamera();
        
        nonUI.SetActive(true);
        attackManager.SetAttackIndex(attackIdxSelected);
        if (waveManager != null) waveManager.OnAttackSelected();
    }
}