using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AttackSelectionUI : MonoBehaviour
{
    [Header("Animators")]
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
    [SerializeField] private GameObject nonUI;
    
    [Header("Death Screen")]
    [SerializeField] private GameObject deathScreenPanel;
    
    [Header("Current Display")]
    [SerializeField] private Transform currentAbilitiesContainer;
    [SerializeField] private GameObject currentAbilityDisplayPrefab;
    [SerializeField] private Transform currentAttackContainer;
    [SerializeField] private GameObject currentAttackDisplayPrefab;
    
    [Header("References")]
    [SerializeField] private AttackManager attackManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private AbilityManager abilityManager;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLookAt mouseLookAt;
    [SerializeField] private List<AbilitySO> availableAbilities = new List<AbilitySO>();

    [Header("DOF Settings")]
    [SerializeField] private float blurTime = 0.4f;
    [SerializeField] private float targetStart = 3f;
    [SerializeField] private float targetEnd = 10f;
    
    private DepthOfField dof;
    private int attackIdxSelected = 0;
    private Coroutine dofRoutine;
    
    private List<GameObject> spawnedButtons = new List<GameObject>();
    private List<GameObject> spawnedCurrentAbilityDisplays = new List<GameObject>();
    private GameObject spawnedCurrentAttackDisplay;
    private List<AppleEnemy> frozenEnemies = new List<AppleEnemy>();
    private List<BaseAbility> frozenAbilities = new List<BaseAbility>();
    
    private void Start()
    {
        FindReferences();
        
        if (attackManager) attackIdxSelected = attackManager.GetCurrentAttackIndex();
        if (postProcessVolume) postProcessVolume.profile.TryGet(out dof);
        if (continueButton) continueButton.onClick.AddListener(OnContinueClicked);

        HideUI();
        if (deathScreenPanel) deathScreenPanel.SetActive(false);
    }

    private void FindReferences()
    {
        if (!uiAnimator) uiAnimator = GetComponent<Animator>();
        if (!waveManager) waveManager = FindFirstObjectByType<WaveManager>();
        if (!abilityManager) abilityManager = FindFirstObjectByType<AbilityManager>();
        if (!playerMovement) playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (!mouseLookAt) mouseLookAt = FindFirstObjectByType<MouseLookAt>();
    }

    public void ShowAttackSelection(AttackManager manager)
    {
        Debug.Log("ShowAttackSelection called!");
        
        attackManager = manager;
        
        if (cameraManager) cameraManager.SwitchToPauseCamera();
        if (nonUI) nonUI.SetActive(false);
        if (selectionPanel) selectionPanel.SetActive(true);
        if (uiAnimator) uiAnimator.SetBool(openBool, true);
        
        SpawnButtons();
        PopulateCurrentAbilities();
        PopulateCurrentAttack();

        if (dofRoutine != null) StopCoroutine(dofRoutine);
        dofRoutine = StartCoroutine(LerpDOF(true));
        
        FreezeAllEntities();
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    public void ShowDeathScreen(bool show)
    {
        if (!deathScreenPanel) return;
        
        deathScreenPanel.SetActive(show);
        if (nonUI) nonUI.SetActive(!show);
        
        if (show && deathAnimator) deathAnimator.SetTrigger(deathTrigger);
    }

    private IEnumerator LerpDOF(bool enable)
    {
        if (!dof) yield break;

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

            AttackButton attackButton = buttonObj.GetComponent<AttackButton>();
            if (attackButton)
            {
                bool isSelected = attackIndex == attackManager.GetCurrentAttackIndex();
                attackButton.Initialize(attack, attackIndex, this, isSelected);
            }
            else
            {
                Button button = buttonObj.GetComponent<Button>();
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                Image buttonImage = buttonObj.GetComponent<Image>();

                if (buttonText && attack) buttonText.text = attack.attackName;
                if (attackIndex == attackManager.GetCurrentAttackIndex() && buttonImage)
                    buttonImage.color = Color.green;
                if (button) button.onClick.AddListener(() => OnAttackButtonClicked(attackIndex));
            }
        }
    }

    private void HideUI()
    {
        if (selectionPanel) selectionPanel.SetActive(false);
        if (dof) dof.active = false;
    }

    public void OnAttackButtonClicked(int attackIndex)
    {
        attackIdxSelected = attackIndex;
        UpdateButtonSelections();
    }
    
    private void UpdateButtonSelections()
    {
        foreach (var buttonObj in spawnedButtons)
        {
            AttackButton attackButton = buttonObj.GetComponent<AttackButton>();
            if (attackButton)
            {
                bool isSelected = attackButton.GetAttackIndex() == attackIdxSelected;
                attackButton.SetSelected(isSelected);
            }
        }
    }

    private void OnContinueClicked()
    {
        StartCoroutine(CloseSequence());
    }

    private IEnumerator CloseSequence()
    {
        UnfreezeAllEntities();
        UpgradeSelectedAttack();
        
        if (uiAnimator) uiAnimator.SetBool(openBool, false);
        if (dofRoutine != null) StopCoroutine(dofRoutine);
        dofRoutine = StartCoroutine(LerpDOF(false));
        
        yield return dofRoutine;
        
        if (selectionPanel) selectionPanel.SetActive(false);
        if (cameraManager) cameraManager.SwitchToNormalCamera();
        
        ClearCurrentAbilityDisplays();
        ClearCurrentAttackDisplay();
        
        if (nonUI) nonUI.SetActive(true);
        if (attackManager) attackManager.SetAttackIndex(attackIdxSelected);
        if (waveManager) waveManager.OnAttackSelected();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    private void UpgradeSelectedAttack()
    {
        if (!attackManager) return;
        if (attackIdxSelected < 0 || attackIdxSelected >= attackManager.attacks.Count) return;
        
        Attack selectedAttack = attackManager.attacks[attackIdxSelected];
        if (selectedAttack && selectedAttack.CanUpgrade())
        {
            selectedAttack.TryUpgrade();
        }
    }
    
    private void PopulateCurrentAbilities()
    {
        ClearCurrentAbilityDisplays();
        if (!currentAbilitiesContainer || !abilityManager) return;
        
        List<BaseAbility> activeAbilities = abilityManager.GetActiveAbilities();
        
        foreach (BaseAbility ability in activeAbilities)
        {
            if (!ability) continue;
            
            AbilitySO matchingSO = FindAbilitySOForAbility(ability);
            
            if (currentAbilityDisplayPrefab)
            {
                GameObject displayObj = Instantiate(currentAbilityDisplayPrefab, currentAbilitiesContainer);
                spawnedCurrentAbilityDisplays.Add(displayObj);
                
                CurrentAbilityDisplay display = displayObj.GetComponent<CurrentAbilityDisplay>();
                if (display)
                {
                    display.Initialize(ability, matchingSO);
                }
                else
                {
                    SetupBasicAbilityDisplay(displayObj, ability, matchingSO);
                }
            }
        }
    }
    
    private AbilitySO FindAbilitySOForAbility(BaseAbility ability)
    {
        if (!ability) return null;
        
        string abilityName = ability.gameObject.name;
        
        foreach (AbilitySO so in availableAbilities)
        {
            if (so && so.abilityPrefab && abilityName.Contains(so.abilityPrefab.name))
            {
                return so;
            }
        }
        
        return null;
    }
    
    private void SetupBasicAbilityDisplay(GameObject displayObj, BaseAbility ability, AbilitySO abilitySO)
    {
        TextMeshProUGUI nameText = displayObj.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText)
        {
            string abilityName = abilitySO ? abilitySO.abilityName : ability.gameObject.name;
            nameText.text = $"{abilityName} Lv.{ability.GetCurrentLevel()}";
        }
        
        if (abilitySO && abilitySO.icon)
        {
            Image iconImage = displayObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage) iconImage.sprite = abilitySO.icon;
        }
    }
    
    private void ClearCurrentAbilityDisplays()
    {
        foreach (GameObject display in spawnedCurrentAbilityDisplays)
        {
            if (display) Destroy(display);
        }
        spawnedCurrentAbilityDisplays.Clear();
    }
    
    private void PopulateCurrentAttack()
    {
        ClearCurrentAttackDisplay();
        if (!currentAttackContainer || !attackManager) return;
        
        Attack currentAttack = attackManager.GetCurrentAttack();
        if (!currentAttack) return;
        
        if (currentAttackDisplayPrefab)
        {
            spawnedCurrentAttackDisplay = Instantiate(currentAttackDisplayPrefab, currentAttackContainer);
            
            CurrentAttackDisplay display = spawnedCurrentAttackDisplay.GetComponent<CurrentAttackDisplay>();
            if (display)
            {
                display.Initialize(currentAttack);
            }
            else
            {
                SetupBasicAttackDisplay(spawnedCurrentAttackDisplay, currentAttack);
            }
        }
    }
    
    private void SetupBasicAttackDisplay(GameObject displayObj, Attack attack)
    {
        TextMeshProUGUI nameText = displayObj.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText) nameText.text = $"{attack.attackName} Lv.{attack.GetCurrentLevel()}";
        
        if (attack.attackIcon)
        {
            Image iconImage = displayObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage) iconImage.sprite = attack.attackIcon;
        }
    }
    
    private void ClearCurrentAttackDisplay()
    {
        if (spawnedCurrentAttackDisplay)
        {
            Destroy(spawnedCurrentAttackDisplay);
            spawnedCurrentAttackDisplay = null;
        }
    }
    
    private void FreezeAllEntities()
    {
        if (playerMovement) playerMovement.SetFrozen(true);
        if (mouseLookAt) mouseLookAt.SetFrozen(true);
        if (cameraManager) cameraManager.SetFrozen(true);
        if (attackManager) attackManager.SetFrozen(true);

        frozenEnemies.Clear();
        AppleEnemy[] enemies = FindObjectsByType<AppleEnemy>(FindObjectsSortMode.None);
        foreach (AppleEnemy enemy in enemies)
        {
            if (enemy)
            {
                enemy.SetFrozen(true);
                frozenEnemies.Add(enemy);
            }
        }

        frozenAbilities.Clear();
        if (abilityManager)
        {
            List<BaseAbility> activeAbilities = abilityManager.GetActiveAbilities();
            foreach (BaseAbility ability in activeAbilities)
            {
                if (ability)
                {
                    ability.SetFrozen(true);
                    frozenAbilities.Add(ability);
                }
            }
        }
    }

    private void UnfreezeAllEntities()
    {
        if (playerMovement) playerMovement.SetFrozen(false);
        if (mouseLookAt) mouseLookAt.SetFrozen(false);
        if (cameraManager) cameraManager.SetFrozen(false);
        if (attackManager) attackManager.SetFrozen(false);

        foreach (AppleEnemy enemy in frozenEnemies)
        {
            if (enemy) enemy.SetFrozen(false);
        }
        frozenEnemies.Clear();

        foreach (BaseAbility ability in frozenAbilities)
        {
            if (ability) ability.SetFrozen(false);
        }
        frozenAbilities.Clear();
    }
}