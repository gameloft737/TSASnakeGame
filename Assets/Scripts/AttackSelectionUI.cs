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
    
    [Header("UI References")]
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private Transform attackButtonContainer;
    [SerializeField] private GameObject attackButtonPrefab;
    [SerializeField] private Button continueButton;

    private DepthOfField dof;

    [SerializeField] private AttackManager attackManager;
    [SerializeField] private WaveManager waveManager;
    private List<GameObject> spawnedButtons = new List<GameObject>();

    [Header("DOF Settings")]
    [SerializeField] private float blurTime = 0.4f;
    [SerializeField] private float targetStart = 3f;
    [SerializeField] private float targetEnd = 10f;
    [Header("Fade Overlay")]
    [SerializeField] private RawImage fadeOverlay;
    [SerializeField] private float overlayMaxOpacity = 0.35f;
    private int attackIdxSelected = 0;
    private Coroutine dofRoutine;

    private void Start()
    {
        attackIdxSelected = attackManager.GetCurrentAttackIndex();
        postProcessVolume.profile.TryGet(out dof);

        if (uiAnimator == null) uiAnimator = GetComponent<Animator>();
        if (waveManager == null) waveManager = FindFirstObjectByType<WaveManager>();
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);

        HideInstant();
    }

   private IEnumerator LerpDOF(bool enable)
    {
        if (dof == null) yield break;

        dof.active = true;

        float startStart = dof.gaussianStart.value;
        float startEnd = dof.gaussianEnd.value;

        float endStart = enable ? targetStart : 0f;
        float endEnd = enable ? targetEnd : 0f;

        float startAlpha = fadeOverlay != null ? fadeOverlay.color.a : 0f;
        float endAlpha = enable ? overlayMaxOpacity : 0f;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / blurTime;

            float lerp = Mathf.Clamp01(t);

            dof.gaussianStart.value = Mathf.Lerp(startStart, endStart, lerp);
            dof.gaussianEnd.value = Mathf.Lerp(startEnd, endEnd, lerp);

            if (fadeOverlay != null)
            {
                Color c = fadeOverlay.color;
                c.a = Mathf.Lerp(startAlpha, endAlpha, lerp);
                fadeOverlay.color = c;
            }

            yield return null;
        }

        if (!enable) dof.active = false;
    }


    private Coroutine BlurThenOpenRoutine;

    public void ShowAttackSelection(AttackManager manager)
    {
        attackManager = manager;
        cameraManager.SwitchToPauseCamera();
        if (BlurThenOpenRoutine != null) StopCoroutine(BlurThenOpenRoutine);
        BlurThenOpenRoutine = StartCoroutine(BlurThenOpen());
    }

    private IEnumerator BlurThenOpen()
    {
       

        if (dofRoutine != null) StopCoroutine(dofRoutine);
        dofRoutine = StartCoroutine(LerpDOF(true));

         if (selectionPanel != null) selectionPanel.SetActive(true);
         
        if (uiAnimator != null) uiAnimator.SetBool(openBool, true);
        yield return dofRoutine;
        SpawnButtons();

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

    public void HideAttackSelection()
    {
        
        cameraManager.SwitchToNormalCamera();
        if (dofRoutine != null) StopCoroutine(dofRoutine);
        dofRoutine = StartCoroutine(LerpDOF(false));
    }

    public void OnCloseAnimationComplete()
    {
        if (selectionPanel != null) selectionPanel.SetActive(false);

        if (dofRoutine != null) StopCoroutine(dofRoutine);
        dofRoutine = StartCoroutine(LerpDOF(false));

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
        
        if (uiAnimator != null) uiAnimator.SetBool(openBool, false);

        HideAttackSelection();
        
        attackManager.SetAttackIndex(attackIdxSelected);

        if (waveManager != null) waveManager.OnAttackSelected();
        
    }
}
