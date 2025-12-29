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
    
    [Header("Current Abilities Display (Attack Menu - Passive)")]
    [SerializeField] private Transform passiveAbilitiesContainer;
    [SerializeField] private GameObject passiveAbilityDisplayPrefab;
    
    [Header("Current Abilities Display (Attack Menu - Active)")]
    [SerializeField] private Transform activeAbilitiesContainer;
    [SerializeField] private GameObject activeAbilityDisplayPrefab;
    
    [Header("Current Attack Display")]
    [SerializeField] private Transform currentAttackContainer;
    [SerializeField] private GameObject currentAttackDisplayPrefab;
    
    [Header("Attack Reorder Panel")]
    [SerializeField] private AttackReorderPanel attackReorderPanel;
    
    [Header("References")]
    [SerializeField] private AttackManager attackManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private AbilityManager abilityManager;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLookAt mouseLookAt;
    [SerializeField] private SnakeHealth snakeHealth;
    
    [Header("Selection Pool")]
    [SerializeField] private List<Attack> possibleAttacks = new List<Attack>();
    [SerializeField] private List<AbilitySO> possibleAbilities = new List<AbilitySO>();
    [SerializeField] private int optionsToShow = 3;
    
    [Header("Fallback Options")]
    [SerializeField] private float healAmount = 50f;
    [SerializeField] private float speedBoostMultiplier = 1.5f;
    [SerializeField] private float speedBoostDuration = 10f;
    
    [Header("Post-Selection")]
    [SerializeField] private float enemyFreezeDelayAfterClose = 0.5f;

    [Header("DOF Settings")]
    [SerializeField] private float blurTime = 0.4f;
    [SerializeField] private float targetStart = 3f;
    [SerializeField] private float targetEnd = 10f;
    
    private DepthOfField dof;
    private Attack selectedAttack = null;
    private AbilitySO selectedAbility = null;
    private FallbackOption selectedFallback = FallbackOption.None;
    private Coroutine dofRoutine;
    
    private enum FallbackOption { None, Heal, SpeedBoost }
    
    private List<GameObject> spawnedButtons = new List<GameObject>();
    private List<GameObject> spawnedPassiveAbilityDisplays = new List<GameObject>();
    private List<GameObject> spawnedActiveAbilityDisplays = new List<GameObject>();
    private GameObject spawnedCurrentAttackDisplay;
    private List<AppleEnemy> frozenEnemies = new List<AppleEnemy>();
    private List<BaseAbility> frozenAbilities = new List<BaseAbility>();
    
    private Dictionary<GameObject, Attack> buttonToAttack = new Dictionary<GameObject, Attack>();
    private Dictionary<GameObject, AbilitySO> buttonToAbility = new Dictionary<GameObject, AbilitySO>();
    private Dictionary<GameObject, FallbackOption> buttonToFallback = new Dictionary<GameObject, FallbackOption>();
    
    private void Start()
    {
        FindReferences();
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
        if (!snakeHealth) snakeHealth = FindFirstObjectByType<SnakeHealth>();
    }

    public void ShowAttackSelection(AttackManager manager)
    {
        attackManager = manager;
        if (cameraManager) cameraManager.SwitchToPauseCamera();
        if (nonUI) nonUI.SetActive(false);
        if (selectionPanel) selectionPanel.SetActive(true);
        if (uiAnimator) uiAnimator.SetBool(openBool, true);
        SpawnButtons();
        PopulateCurrentAbilities();
        PopulateCurrentAttack();
        RefreshAttackReorderPanel();
        if (dofRoutine != null) StopCoroutine(dofRoutine);
        dofRoutine = StartCoroutine(LerpDOF(true));
        FreezeAllEntities();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    /// <summary>
    /// Refresh the attack reorder panel to show current attacks
    /// </summary>
    private void RefreshAttackReorderPanel()
    {
        if (attackReorderPanel != null)
        {
            attackReorderPanel.gameObject.SetActive(true);
            attackReorderPanel.RefreshSlots();
        }
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
            dof.gaussianStart.value = Mathf.Lerp(startStart, endStart, Mathf.Clamp01(t));
            dof.gaussianEnd.value = Mathf.Lerp(startEnd, endEnd, Mathf.Clamp01(t));
            yield return null;
        }
        if (!enable) dof.active = false;
    }
    
    private void SpawnButtons()
    {
        foreach (var button in spawnedButtons) Destroy(button);
        spawnedButtons.Clear();
        buttonToAttack.Clear();
        buttonToAbility.Clear();
        buttonToFallback.Clear();
        selectedAttack = null;
        selectedAbility = null;
        selectedFallback = FallbackOption.None;

        List<object> upgradeableOptions = GetUpgradeableOptions();
        if (upgradeableOptions.Count == 0) { SpawnFallbackButtons(); return; }
        
        List<object> selectedOptions = GetRandomOptions(upgradeableOptions, optionsToShow);
        foreach (object option in selectedOptions)
        {
            if (option is Attack attack) SpawnAttackButton(attack);
            else if (option is AbilitySO abilitySO) SpawnAbilityButton(abilitySO);
        }
    }
    
    private void SpawnFallbackButtons()
    {
        SpawnFallbackButton(FallbackOption.Heal, "Heal", $"Restore {healAmount} health", new Color(0.5f, 1f, 0.5f));
        SpawnFallbackButton(FallbackOption.SpeedBoost, "Speed Boost", $"+{(speedBoostMultiplier - 1f) * 100f:F0}% speed for {speedBoostDuration}s", new Color(1f, 1f, 0.5f));
    }
    
    private void SpawnFallbackButton(FallbackOption fallbackType, string name, string description, Color bgColor)
    {
        GameObject buttonObj = Instantiate(attackButtonPrefab, attackButtonContainer);
        spawnedButtons.Add(buttonObj);
        buttonToFallback[buttonObj] = fallbackType;
        
        AttackButton attackButton = buttonObj.GetComponent<AttackButton>();
        if (attackButton != null) attackButton.enabled = false;
        
        TextMeshProUGUI nameText = buttonObj.transform.Find("AttackName")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI levelText = buttonObj.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descriptionText = buttonObj.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
        Image backgroundImage = buttonObj.GetComponent<Image>();
        
        if (nameText != null) nameText.text = $"[Bonus] {name}";
        if (levelText != null) levelText.text = "";
        if (descriptionText != null) descriptionText.text = description;
        if (backgroundImage != null) backgroundImage.color = bgColor;
        
        Button button = buttonObj.GetComponent<Button>();
        if (button != null) { button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => OnFallbackSelected(fallbackType)); }
    }
    
    private List<object> GetUpgradeableOptions()
    {
        List<object> options = new List<object>();
        
        // Use possibleAttacks if populated, otherwise fall back to attackManager.attacks
        List<Attack> attacksToCheck = new List<Attack>();
        if (possibleAttacks != null && possibleAttacks.Count > 0)
        {
            attacksToCheck = possibleAttacks;
        }
        else if (attackManager != null && attackManager.attacks != null && attackManager.attacks.Count > 0)
        {
            attacksToCheck = attackManager.attacks;
        }
        
        bool canAddMoreAttacks = attackManager != null && attackManager.CanAddAttack();
        int ownedAttackCount = attackManager != null ? attackManager.GetAttackCount() : 0;
        
        Debug.Log($"[AttackSelectionUI] Checking {attacksToCheck.Count} attacks, owned={ownedAttackCount}, canAddMore={canAddMoreAttacks}");
        
        foreach (Attack attack in attacksToCheck)
        {
            if (attack == null) continue;
            
            bool ownsThisAttack = attackManager != null && attackManager.HasAttack(attack);
            
            // If player owns this attack, check if it can be upgraded
            if (ownsThisAttack)
            {
                if (attack.CanUpgrade())
                {
                    Debug.Log($"[AttackSelectionUI] Adding attack (can upgrade): {attack.attackName}");
                    options.Add(attack);
                }
            }
            // If player doesn't own this attack and has room for more, add it as an option
            else if (canAddMoreAttacks)
            {
                Debug.Log($"[AttackSelectionUI] Adding attack (new, slots available): {attack.attackName}");
                options.Add(attack);
            }
        }
        
        bool passiveSlotsFull = abilityManager != null && !abilityManager.CanAddPassiveAbility();
        bool activeSlotsFull = abilityManager != null && !abilityManager.CanAddActiveAbility();
        
        foreach (AbilitySO abilitySO in possibleAbilities)
        {
            if (abilitySO == null) continue;
            int currentLevel = abilityManager != null ? abilityManager.GetAbilityLevel(abilitySO.abilityPrefab) : 0;
            bool hasAbility = currentLevel > 0;
            
            if (hasAbility) { if (currentLevel < abilitySO.maxLevel) options.Add(abilitySO); }
            else
            {
                if (abilitySO.abilityType == AbilityType.Passive && !passiveSlotsFull) options.Add(abilitySO);
                else if (abilitySO.abilityType == AbilityType.Active && !activeSlotsFull) options.Add(abilitySO);
            }
        }
        return options;
    }
    
    private List<object> GetRandomOptions(List<object> available, int count)
    {
        List<object> result = new List<object>();
        List<object> pool = new List<object>(available);
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            result.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex);
        }
        return result;
    }
    
    private void SpawnAttackButton(Attack attack)
    {
        GameObject buttonObj = Instantiate(attackButtonPrefab, attackButtonContainer);
        spawnedButtons.Add(buttonObj);
        buttonToAttack[buttonObj] = attack;
        
        bool ownsAttack = attackManager != null && attackManager.HasAttack(attack);
        
        AttackButton attackButton = buttonObj.GetComponent<AttackButton>();
        if (attackButton)
        {
            attackButton.InitializeWithOwnership(attack, possibleAttacks.IndexOf(attack), this, false, ownsAttack);
        }
        else
        {
            SetupBasicAttackButton(buttonObj, attack);
        }
    }
    
    private void SetupBasicAttackButton(GameObject buttonObj, Attack attack)
    {
        TextMeshProUGUI nameText = buttonObj.transform.Find("AttackName")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI levelText = buttonObj.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descriptionText = buttonObj.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
        Image iconImage = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
        Image backgroundImage = buttonObj.GetComponent<Image>();
        
        bool isNew = attackManager == null || !attackManager.HasAttack(attack);
        int currentLevel = isNew ? 0 : attack.GetCurrentLevel();
        
        if (nameText != null) nameText.text = attack.attackName;
        if (levelText != null) levelText.text = isNew ? "(NEW) " : $"(Lvl {currentLevel + 1}) ";
        if (descriptionText != null)
        {
            AttackUpgradeData upgradeData = attack.GetUpgradeData();
            if (upgradeData != null)
            {
                AttackLevelStats stats = upgradeData.GetStatsForLevel(isNew ? 1 : currentLevel + 1);
                descriptionText.text = stats.description;
            }
        }
        if (iconImage != null && attack.attackIcon != null) { iconImage.sprite = attack.attackIcon; iconImage.enabled = true; }
        if (backgroundImage != null) backgroundImage.color = isNew ? new Color(1f, 0.8f, 0.5f) : Color.white;
        
        Button button = buttonObj.GetComponent<Button>();
        if (button != null) { button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => OnAttackSelected(attack)); }
    }
    
    private void SpawnAbilityButton(AbilitySO abilitySO)
    {
        GameObject buttonObj = Instantiate(attackButtonPrefab, attackButtonContainer);
        spawnedButtons.Add(buttonObj);
        buttonToAbility[buttonObj] = abilitySO;
        
        int currentLevel = abilityManager != null ? abilityManager.GetAbilityLevel(abilitySO.abilityPrefab) : 0;
        
        AttackButton attackButton = buttonObj.GetComponent<AttackButton>();
        if (attackButton != null)
        {
            // Use the new ability initialization method
            attackButton.InitializeWithAbility(abilitySO, currentLevel, this, false);
        }
        else
        {
            // Fallback to manual setup if no AttackButton component
            SetupAbilityButtonManual(buttonObj, abilitySO, currentLevel);
        }
    }
    
    private void SetupAbilityButtonManual(GameObject buttonObj, AbilitySO abilitySO, int currentLevel)
    {
        TextMeshProUGUI nameText = buttonObj.transform.Find("AttackName")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI levelText = buttonObj.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descriptionText = buttonObj.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
        Image iconImage = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
        Image backgroundImage = buttonObj.GetComponent<Image>();
        
        bool isNew = currentLevel == 0;
        int nextLevel = isNew ? 1 : currentLevel + 1;
        
        if (nameText != null) nameText.text = abilitySO.abilityName;
        if (levelText != null) levelText.text = isNew ? "(NEW) " : $"(Lvl {nextLevel}) ";
        
        // Get description from upgrade data if available
        if (descriptionText != null)
        {
            string levelDescription = abilitySO.GetDescriptionForLevel(nextLevel);
            if (!string.IsNullOrEmpty(levelDescription))
            {
                descriptionText.text = levelDescription;
            }
            else if (!string.IsNullOrEmpty(abilitySO.description))
            {
                descriptionText.text = abilitySO.description;
            }
            else
            {
                descriptionText.text = isNew ? "Gain this ability" : $"Level up to Lvl {nextLevel}";
            }
        }
        
        // Set icon from AbilitySO or its upgrade data
        if (iconImage != null)
        {
            if (abilitySO.icon != null)
            {
                iconImage.sprite = abilitySO.icon;
                iconImage.enabled = true;
            }
            else if (abilitySO.upgradeData != null && abilitySO.upgradeData.abilityIcon != null)
            {
                iconImage.sprite = abilitySO.upgradeData.abilityIcon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.color = abilitySO.abilityType == AbilityType.Passive
                ? (isNew ? new Color(0.5f, 0.8f, 1f) : new Color(0.7f, 0.9f, 1f))
                : (isNew ? new Color(1f, 0.6f, 0.6f) : new Color(1f, 0.8f, 0.8f));
        }
        
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnAbilitySelected(abilitySO));
        }
    }

    private void HideUI() { if (selectionPanel) selectionPanel.SetActive(false); if (dof) dof.active = false; }

    public void OnAttackButtonClicked(int attackIndex) { if (attackIndex >= 0 && attackIndex < possibleAttacks.Count) OnAttackSelected(possibleAttacks[attackIndex]); }
    
    public void OnAttackSelected(Attack attack) { selectedAttack = attack; selectedAbility = null; selectedFallback = FallbackOption.None; UpdateButtonSelections(); }
    
    public void OnAbilitySelected(AbilitySO abilitySO) { selectedAbility = abilitySO; selectedAttack = null; selectedFallback = FallbackOption.None; UpdateButtonSelections(); }
    
    private void OnFallbackSelected(FallbackOption fallback) { selectedFallback = fallback; selectedAttack = null; selectedAbility = null; UpdateButtonSelections(); }
    
    private void UpdateButtonSelections()
    {
        foreach (var buttonObj in spawnedButtons)
        {
            Image bgImage = buttonObj.GetComponent<Image>();
            if (bgImage == null) continue;
            
            if (buttonToAttack.ContainsKey(buttonObj))
            {
                Attack attack = buttonToAttack[buttonObj];
                if (attack == selectedAttack) bgImage.color = Color.green;
                else { bool isNew = attackManager == null || !attackManager.HasAttack(attack); bgImage.color = isNew ? new Color(1f, 0.8f, 0.5f) : Color.white; }
            }
            else if (buttonToAbility.ContainsKey(buttonObj))
            {
                AbilitySO abilitySO = buttonToAbility[buttonObj];
                if (abilitySO == selectedAbility) bgImage.color = Color.green;
                else { int lvl = abilityManager != null ? abilityManager.GetAbilityLevel(abilitySO.abilityPrefab) : 0; bool isNew = lvl == 0; bgImage.color = abilitySO.abilityType == AbilityType.Passive ? (isNew ? new Color(0.5f, 0.8f, 1f) : new Color(0.7f, 0.9f, 1f)) : (isNew ? new Color(1f, 0.6f, 0.6f) : new Color(1f, 0.8f, 0.8f)); }
            }
            else if (buttonToFallback.ContainsKey(buttonObj))
            {
                FallbackOption fallback = buttonToFallback[buttonObj];
                if (fallback == selectedFallback) bgImage.color = Color.green;
                else bgImage.color = fallback == FallbackOption.Heal ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 1f, 0.5f);
            }
        }
    }

    private void OnContinueClicked() { StartCoroutine(CloseSequence()); }

    private IEnumerator CloseSequence()
    {
        // Apply selection first
        ApplySelection();
        
        // Close UI animation
        if (uiAnimator) uiAnimator.SetBool(openBool, false);
        if (dofRoutine != null) StopCoroutine(dofRoutine);
        dofRoutine = StartCoroutine(LerpDOF(false));
        yield return dofRoutine;
        
        if (selectionPanel) selectionPanel.SetActive(false);
        if (cameraManager) cameraManager.SwitchToNormalCamera();
        ClearCurrentAbilityDisplays();
        ClearCurrentAttackDisplay();
        if (nonUI) nonUI.SetActive(true);
        
        // Unfreeze player controls but keep enemies frozen for a moment
        if (playerMovement) playerMovement.enabled = true;
        if (mouseLookAt) mouseLookAt.enabled = true;
        if (attackManager) attackManager.SetFrozen(false);
        
        // Unfreeze abilities
        foreach (BaseAbility ability in frozenAbilities)
        {
            if (ability) ability.enabled = true;
        }
        frozenAbilities.Clear();
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // Notify wave manager
        if (waveManager) waveManager.OnAttackSelected();
        
        // Keep enemies frozen for a short delay to give player time to react
        if (enemyFreezeDelayAfterClose > 0)
        {
            yield return new WaitForSeconds(enemyFreezeDelayAfterClose);
        }
        
        // Now unfreeze enemies
        foreach (AppleEnemy enemy in frozenEnemies)
        {
            if (enemy) enemy.SetFrozen(false);
        }
        frozenEnemies.Clear();
    }
    
    private void ApplySelection()
    {
        if (selectedFallback != FallbackOption.None) { ApplyFallbackOption(); return; }
        if (selectedAbility != null && abilityManager != null) { abilityManager.AddAbility(selectedAbility.abilityPrefab, selectedAbility); return; }
        if (selectedAttack != null && attackManager != null)
        {
            bool ownsAttack = attackManager.HasAttack(selectedAttack);
            
            if (ownsAttack)
            {
                // Player owns this attack - try to upgrade it
                if (selectedAttack.CanUpgrade())
                {
                    selectedAttack.TryUpgrade();
                    Debug.Log($"Upgraded attack: {selectedAttack.attackName}");
                }
                
                // Don't automatically switch to this attack - let player keep their current active attack
            }
            else
            {
                // Player doesn't own this attack - add it to their collection
                // The attack will be added to the end of the list, not made active
                if (attackManager.AddAttack(selectedAttack))
                {
                    Debug.Log($"Added new attack: {selectedAttack.attackName}");
                    // Don't switch to the new attack - keep the current active attack
                }
            }
        }
    }
    
    private void ApplyFallbackOption()
    {
        if (selectedFallback == FallbackOption.Heal && snakeHealth != null) snakeHealth.Heal(healAmount);
        else if (selectedFallback == FallbackOption.SpeedBoost && playerMovement != null) StartCoroutine(ApplyTemporarySpeedBoost());
    }
    
    private IEnumerator ApplyTemporarySpeedBoost()
    {
        if (playerMovement == null) yield break;
        float origMax = playerMovement.maxSpeed, origDef = playerMovement.defaultSpeed;
        playerMovement.maxSpeed *= speedBoostMultiplier;
        playerMovement.defaultSpeed *= speedBoostMultiplier;
        yield return new WaitForSeconds(speedBoostDuration);
        playerMovement.maxSpeed = origMax;
        playerMovement.defaultSpeed = origDef;
    }
    
    private void PopulateCurrentAbilities()
    {
        ClearCurrentAbilityDisplays();
        if (!abilityManager) return;
        
        foreach (BaseAbility ability in abilityManager.GetActiveAbilities())
        {
            if (!ability) continue;
            
            // First try to get the AbilitySO from the AbilityManager's stored mapping
            AbilitySO matchingSO = abilityManager.GetAbilitySO(ability);
            
            // Fallback to searching possible abilities if not found in mapping
            if (matchingSO == null)
            {
                matchingSO = FindAbilitySOForAbility(ability);
            }
            
            // Determine if this is a passive or active ability
            // Default to passive if no SO found (safer default)
            bool isPassive = matchingSO == null || matchingSO.abilityType == AbilityType.Passive;
            
            Debug.Log($"[AttackSelectionUI] Ability: {ability.gameObject.name}, SO: {(matchingSO != null ? matchingSO.abilityName : "null")}, Type: {(matchingSO != null ? matchingSO.abilityType.ToString() : "unknown")}, isPassive: {isPassive}");
            
            // Choose the appropriate container and prefab based on ability type
            Transform targetContainer = isPassive ? passiveAbilitiesContainer : activeAbilitiesContainer;
            GameObject prefab = isPassive ? passiveAbilityDisplayPrefab : activeAbilityDisplayPrefab;
            List<GameObject> displayList = isPassive ? spawnedPassiveAbilityDisplays : spawnedActiveAbilityDisplays;
            
            if (targetContainer == null || prefab == null) continue;
            
            GameObject displayObj = Instantiate(prefab, targetContainer);
            displayList.Add(displayObj);
            CurrentAbilityDisplay display = displayObj.GetComponent<CurrentAbilityDisplay>();
            if (display) display.Initialize(ability, matchingSO);
        }
    }
    
    private AbilitySO FindAbilitySOForAbility(BaseAbility ability)
    {
        if (!ability) return null;
        string abilityName = ability.gameObject.name;
        
        // First search in possibleAbilities
        foreach (AbilitySO so in possibleAbilities)
        {
            if (so && so.abilityPrefab && abilityName.Contains(so.abilityPrefab.name))
                return so;
        }
        
        // Fallback: search in AbilityCollector's available abilities
        AbilityCollector abilityCollector = FindFirstObjectByType<AbilityCollector>();
        if (abilityCollector != null)
        {
            List<AbilitySO> availableAbilities = abilityCollector.GetAvailableAbilities();
            if (availableAbilities != null)
            {
                foreach (AbilitySO so in availableAbilities)
                {
                    if (so && so.abilityPrefab && abilityName.Contains(so.abilityPrefab.name))
                        return so;
                }
            }
        }
        
        return null;
    }
    
    private void ClearCurrentAbilityDisplays()
    {
        // Clear passive ability displays
        foreach (var d in spawnedPassiveAbilityDisplays) if (d) Destroy(d);
        spawnedPassiveAbilityDisplays.Clear();
        
        // Clear active ability displays
        foreach (var d in spawnedActiveAbilityDisplays) if (d) Destroy(d);
        spawnedActiveAbilityDisplays.Clear();
    }
    
    private void PopulateCurrentAttack()
    {
        ClearCurrentAttackDisplay();
        if (!currentAttackContainer || !attackManager) return;
        Attack currentAttack = attackManager.GetCurrentAttack();
        if (currentAttack == null) return;
        if (currentAttackDisplayPrefab)
        {
            spawnedCurrentAttackDisplay = Instantiate(currentAttackDisplayPrefab, currentAttackContainer);
            CurrentAttackDisplay display = spawnedCurrentAttackDisplay.GetComponent<CurrentAttackDisplay>();
            if (display) display.Initialize(currentAttack);
        }
    }
    
    private void ClearCurrentAttackDisplay() { if (spawnedCurrentAttackDisplay) { Destroy(spawnedCurrentAttackDisplay); spawnedCurrentAttackDisplay = null; } }
    
    private void FreezeAllEntities()
    {
        frozenEnemies.Clear();
        frozenAbilities.Clear();
        
        // Freeze all enemies using their SetFrozen method
        foreach (AppleEnemy enemy in FindObjectsByType<AppleEnemy>(FindObjectsSortMode.None))
        {
            if (enemy)
            {
                enemy.SetFrozen(true);
                frozenEnemies.Add(enemy);
            }
        }
        
        // Freeze player movement and look
        if (playerMovement) playerMovement.enabled = false;
        if (mouseLookAt) mouseLookAt.enabled = false;
        
        // Freeze attack manager
        if (attackManager) attackManager.SetFrozen(true);
        
        // Freeze abilities
        if (abilityManager)
        {
            foreach (BaseAbility ability in abilityManager.GetActiveAbilities())
            {
                if (ability)
                {
                    ability.enabled = false;
                    frozenAbilities.Add(ability);
                }
            }
        }
    }
    
    private void UnfreezeAllEntities()
    {
        // This method is now only used for immediate unfreezing (e.g., on death)
        // The CloseSequence handles the delayed unfreeze for normal selection
        
        // Unfreeze all enemies
        foreach (AppleEnemy enemy in frozenEnemies)
        {
            if (enemy) enemy.SetFrozen(false);
        }
        frozenEnemies.Clear();
        
        // Unfreeze player movement and look
        if (playerMovement) playerMovement.enabled = true;
        if (mouseLookAt) mouseLookAt.enabled = true;
        
        // Unfreeze attack manager
        if (attackManager) attackManager.SetFrozen(false);
        
        // Unfreeze abilities
        foreach (BaseAbility ability in frozenAbilities)
        {
            if (ability) ability.enabled = true;
        }
        frozenAbilities.Clear();
    }
}