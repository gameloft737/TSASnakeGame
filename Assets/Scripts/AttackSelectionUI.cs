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
    
    [Header("References")]
    [SerializeField] private AttackManager attackManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private AbilityManager abilityManager;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLookAt mouseLookAt;
    [SerializeField] private SnakeHealth snakeHealth;
    [SerializeField] private AbilityCollector abilityCollector;
    [SerializeField] private XPManager xpManager;
    
    [Header("Selection Pool")]
    [SerializeField] private List<Attack> possibleAttacks = new List<Attack>();
    [SerializeField] private List<AbilitySO> possibleAbilities = new List<AbilitySO>();
    [SerializeField] private int optionsToShow = 3;
    [SerializeField] private GameObject swapAttackButtonPrefab;
    
    [Header("Attack Swap Settings")]
    [SerializeField] private int minLevelForSwap = 5;
    [SerializeField] [Range(0f, 1f)] private float swapChance = 0.33f;
    [SerializeField] private Color swapNormalColor = new Color(0.8f, 0.5f, 1f);
    [SerializeField] private Color swapSelectedColor = Color.green;
    
    [Header("New Attack Colors")]
    [SerializeField] private Color newAttackNormalColor = new Color(1f, 0.8f, 0.5f);
    [SerializeField] private Color newAttackSelectedColor = Color.green;
    
    [Header("Fallback Options")]
    [SerializeField] private float healAmount = 50f;
    [SerializeField] private float speedBoostMultiplier = 1.5f;
    [SerializeField] private float speedBoostDuration = 10f;

    [Header("DOF Settings")]
    [SerializeField] private float targetStart = 3f;
    [SerializeField] private float targetEnd = 10f;
    
    private DepthOfField dof;
    private Attack selectedAttack = null;
    private AbilitySO selectedAbility = null;
    private FallbackOption selectedFallback = FallbackOption.None;
    private Attack selectedSwapAttack = null;
    private bool isUIOpen = false;
    private bool isTransitioning = false;
    private Coroutine currentTransition = null;
    private const float ANIM_TIME = 0.66f;
    
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
    private Dictionary<GameObject, Attack> buttonToSwapAttack = new Dictionary<GameObject, Attack>();
    
    private void Start()
    {
        FindReferences();
        if (postProcessVolume) postProcessVolume.profile.TryGet(out dof);
        if (continueButton) continueButton.onClick.AddListener(OnContinueClicked);
        HideUI();
        if (deathScreenPanel) deathScreenPanel.SetActive(false);
        if (dof) dof.active = false;
        isUIOpen = false;
        isTransitioning = false;
    }

    private void FindReferences()
    {
        if (!uiAnimator) uiAnimator = GetComponent<Animator>();
        if (!waveManager) waveManager = FindFirstObjectByType<WaveManager>();
        if (!abilityManager) abilityManager = FindFirstObjectByType<AbilityManager>();
        if (!playerMovement) playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (!mouseLookAt) mouseLookAt = FindFirstObjectByType<MouseLookAt>();
        if (!snakeHealth) snakeHealth = FindFirstObjectByType<SnakeHealth>();
        if (!abilityCollector) abilityCollector = FindFirstObjectByType<AbilityCollector>();
        if (!xpManager) xpManager = FindFirstObjectByType<XPManager>();
    }

    public void ShowAttackSelection(AttackManager manager)
    {
        if (isUIOpen || isTransitioning) return;
        if (abilityCollector != null && abilityCollector.IsUIOpen()) return;
        
        attackManager = manager;
        
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
            currentTransition = null;
        }
        
        currentTransition = StartCoroutine(OpenUI());
    }
    
    private IEnumerator OpenUI()
    {
        isTransitioning = true;
        isUIOpen = true;
        
        // Notify ClassicModeManager that menu is opening
        if (ClassicModeManager.Instance != null)
        {
            ClassicModeManager.Instance.OnMenuOpened();
        }
        
        if (uiAnimator)
        {
            uiAnimator.SetBool(openBool, true);
            uiAnimator.Update(0f);
        }
        
        FreezeAllEntities();
        if (cameraManager) cameraManager.SwitchToPauseCamera();
        if (nonUI) nonUI.SetActive(false);
        
        SpawnButtons();
        PopulateCurrentAbilities();
        PopulateCurrentAttack();
        
        StartCoroutine(DOFLerp(true));
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        yield return new WaitForSecondsRealtime(ANIM_TIME);
        
        isTransitioning = false;
        currentTransition = null;
        
        if (uiAnimator && isUIOpen)
        {
            uiAnimator.SetBool(openBool, true);
        }
    }
    
    public void ShowDeathScreen(bool show)
    {
        if (!deathScreenPanel) return;
        deathScreenPanel.SetActive(show);
        if (nonUI) nonUI.SetActive(!show);
        if (show && deathAnimator) deathAnimator.SetTrigger(deathTrigger);
    }
    
    public bool IsUIOpen() => isUIOpen;

    private IEnumerator DOFLerp(bool enable)
    {
        if (!dof) yield break;
        
        dof.active = true;
        float start = enable ? 0f : targetStart;
        float end = enable ? targetStart : 0f;
        float endVal = enable ? targetEnd : 0f;
        
        float t = 0f;
        while (t < ANIM_TIME)
        {
            t += Time.unscaledDeltaTime;
            float p = t / ANIM_TIME;
            dof.gaussianStart.value = Mathf.Lerp(start, end, p);
            dof.gaussianEnd.value = Mathf.Lerp(enable ? 0f : targetEnd, endVal, p);
            yield return null;
        }
        
        dof.gaussianStart.value = end;
        dof.gaussianEnd.value = endVal;
        
        if (!enable) dof.active = false;
    }
    
    private void SpawnButtons()
    {
        foreach (var button in spawnedButtons) Destroy(button);
        spawnedButtons.Clear();
        buttonToAttack.Clear();
        buttonToAbility.Clear();
        buttonToFallback.Clear();
        buttonToSwapAttack.Clear();
        selectedAttack = null;
        selectedAbility = null;
        selectedFallback = FallbackOption.None;
        selectedSwapAttack = null;

        bool isFirstSelection = attackManager == null || attackManager.GetAttackCount() == 0;
        
        if (isFirstSelection) SpawnFirstSelectionButtons();
        else SpawnSubsequentSelectionButtons();
    }
    
    private void SpawnFirstSelectionButtons()
    {
        List<Attack> availableAttacks = new List<Attack>();
        
        if (possibleAttacks != null && possibleAttacks.Count > 0)
        {
            foreach (Attack attack in possibleAttacks)
            {
                if (attack != null) availableAttacks.Add(attack);
            }
        }
        
        if (availableAttacks.Count == 0)
        {
            SpawnFallbackButtons();
            return;
        }
        
        List<Attack> selectedAttacks = new List<Attack>();
        List<Attack> pool = new List<Attack>(availableAttacks);
        
        for (int i = 0; i < 3 && pool.Count > 0; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, pool.Count);
            selectedAttacks.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex);
        }
        
        foreach (Attack attack in selectedAttacks)
        {
            SpawnAttackButton(attack);
        }
    }
    
    private void SpawnSubsequentSelectionButtons()
    {
        List<object> options = new List<object>();
        
        Attack currentAttack = attackManager?.GetCurrentAttack();
        
        // Check if we should offer an attack swap
        bool shouldOfferSwap = ShouldOfferAttackSwap(currentAttack);
        
        if (currentAttack != null && currentAttack.CanUpgrade() && !shouldOfferSwap)
        {
            options.Add(currentAttack);
        }
        
        // Add attack swap option if applicable
        if (shouldOfferSwap)
        {
            Attack swapOption = GetRandomSwapAttack(currentAttack);
            if (swapOption != null)
            {
                options.Add(new AttackSwapOption { swapToAttack = swapOption });
            }
        }
        
        bool activeSlotsFull = abilityManager != null && !abilityManager.CanAddActiveAbility();
        
        foreach (AbilitySO abilitySO in possibleAbilities)
        {
            if (abilitySO == null) continue;
            
            int currentLevel = abilityManager != null ? abilityManager.GetAbilityLevel(abilitySO.abilityPrefab) : 0;
            bool hasAbility = currentLevel > 0;
            
            if (hasAbility)
            {
                // Player has this ability - only offer if can upgrade
                if (currentLevel < abilitySO.maxLevel)
                {
                    options.Add(abilitySO);
                }
            }
            else
            {
                // Player doesn't have this ability - check slot availability based on type
                if (abilitySO.abilityType == AbilityType.Active)
                {
                    if (!activeSlotsFull)
                    {
                        options.Add(abilitySO);
                    }
                }
                else if (abilitySO.abilityType == AbilityType.Passive)
                {
                    // Check passive slot availability
                    bool passiveSlotsFull = abilityManager != null && !abilityManager.CanAddPassiveAbility();
                    if (!passiveSlotsFull)
                    {
                        options.Add(abilitySO);
                    }
                }
            }
        }
        
        if (options.Count == 0)
        {
            SpawnFallbackButtons();
            return;
        }
        
        List<object> selectedOptions = GetRandomOptions(options, optionsToShow);
        
        foreach (object option in selectedOptions)
        {
            if (option is Attack attack) SpawnAttackButton(attack);
            else if (option is AbilitySO abilitySO) SpawnAbilityButton(abilitySO);
            else if (option is AttackSwapOption swapOption) SpawnSwapAttackButton(swapOption.swapToAttack);
        }
    }
    
    private bool ShouldOfferAttackSwap(Attack currentAttack)
    {
        if (currentAttack == null) return false;
        if (xpManager == null) return false;
        if (possibleAttacks == null || possibleAttacks.Count <= 1) return false;
        
        int playerLevel = xpManager.GetCurrentLevel();
        if (playerLevel < minLevelForSwap) return false;
        
        return Random.value < swapChance;
    }
    
    private Attack GetRandomSwapAttack(Attack currentAttack)
    {
        List<Attack> availableSwaps = new List<Attack>();
        
        foreach (Attack attack in possibleAttacks)
        {
            if (attack != null && attack != currentAttack)
            {
                availableSwaps.Add(attack);
            }
        }
        
        if (availableSwaps.Count == 0) return null;
        
        return availableSwaps[Random.Range(0, availableSwaps.Count)];
    }
    
    private void SpawnSwapAttackButton(Attack swapToAttack)
    {
        // Use swap-specific prefab if available, otherwise fall back to regular
        GameObject prefabToUse = swapAttackButtonPrefab != null ? swapAttackButtonPrefab : attackButtonPrefab;
        
        GameObject buttonObj = Instantiate(prefabToUse, attackButtonContainer);
        spawnedButtons.Add(buttonObj);
        buttonToSwapAttack[buttonObj] = swapToAttack;
        
        Attack currentAttack = attackManager.GetCurrentAttack();
        int currentLevel = currentAttack != null ? currentAttack.GetCurrentLevel() : 1;
        int targetLevel = Mathf.Min(currentLevel, swapToAttack.GetMaxLevel());
        
        // Get the AttackButton component and initialize it properly
        AttackButton attackButton = buttonObj.GetComponent<AttackButton>();
        if (attackButton)
        {
            // Initialize as if the player doesn't own this attack (ownsAttack = false)
            // This will show it as "new" with the attack's normal name, level, and description
            attackButton.InitializeWithOwnership(swapToAttack, possibleAttacks.IndexOf(swapToAttack), this, false, false);
            
            // Set custom swap colors
            attackButton.SetCustomColors(swapNormalColor, swapSelectedColor);
        }
        
        // Override the button click to use swap selection
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnSwapAttackSelected(swapToAttack));
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
            
            // Set custom colors for new attacks
            if (!ownsAttack)
            {
                attackButton.SetCustomColors(newAttackNormalColor, newAttackSelectedColor);
            }
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
            attackButton.InitializeWithAbility(abilitySO, currentLevel, this, false);
        }
        else
        {
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

    private void HideUI() { }

    public void OnAttackButtonClicked(int attackIndex) { if (attackIndex >= 0 && attackIndex < possibleAttacks.Count) OnAttackSelected(possibleAttacks[attackIndex]); }
    
    public void OnAttackSelected(Attack attack) { selectedAttack = attack; selectedAbility = null; selectedFallback = FallbackOption.None; selectedSwapAttack = null; UpdateButtonSelections(); }
    
    public void OnAbilitySelected(AbilitySO abilitySO) { selectedAbility = abilitySO; selectedAttack = null; selectedFallback = FallbackOption.None; selectedSwapAttack = null; UpdateButtonSelections(); }
    
    private void OnFallbackSelected(FallbackOption fallback) { selectedFallback = fallback; selectedAttack = null; selectedAbility = null; selectedSwapAttack = null; UpdateButtonSelections(); }
    
    private void OnSwapAttackSelected(Attack swapAttack) { selectedSwapAttack = swapAttack; selectedAttack = null; selectedAbility = null; selectedFallback = FallbackOption.None; UpdateButtonSelections(); }
    
    private void UpdateButtonSelections()
    {
        foreach (var buttonObj in spawnedButtons)
        {
            AttackButton attackButton = buttonObj.GetComponent<AttackButton>();
            
            if (buttonToAttack.ContainsKey(buttonObj))
            {
                Attack attack = buttonToAttack[buttonObj];
                bool isSelected = attack == selectedAttack;
                if (attackButton) attackButton.SetSelected(isSelected);
            }
            else if (buttonToAbility.ContainsKey(buttonObj))
            {
                AbilitySO abilitySO = buttonToAbility[buttonObj];
                bool isSelected = abilitySO == selectedAbility;
                if (attackButton) attackButton.SetSelected(isSelected);
            }
            else if (buttonToSwapAttack.ContainsKey(buttonObj))
            {
                Attack swapAttack = buttonToSwapAttack[buttonObj];
                bool isSelected = swapAttack == selectedSwapAttack;
                if (attackButton) attackButton.SetSelected(isSelected);
            }
            else if (buttonToFallback.ContainsKey(buttonObj))
            {
                // Fallback buttons still use manual color setting since they don't use AttackButton
                FallbackOption fallback = buttonToFallback[buttonObj];
                Image bgImage = buttonObj.GetComponent<Image>();
                if (bgImage != null)
                {
                    if (fallback == selectedFallback) bgImage.color = Color.green;
                    else bgImage.color = fallback == FallbackOption.Heal ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 1f, 0.5f);
                }
            }
        }
    }

    private void OnContinueClicked() 
    { 
        if (isTransitioning) return;
        
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }
        
        currentTransition = StartCoroutine(CloseUI()); 
    }

    private IEnumerator CloseUI()
    {
        isTransitioning = true;
        
        ApplySelection();
        
        if (uiAnimator)
        {
            uiAnimator.SetBool(openBool, false);
            uiAnimator.Update(0f);
        }
        
        StartCoroutine(DOFLerp(false));
        
        yield return new WaitForSecondsRealtime(ANIM_TIME);
        
        // Don't switch camera here - let ClassicModeManager handle it
        ClearCurrentAbilityDisplays();
        ClearCurrentAttackDisplay();
        if (nonUI) nonUI.SetActive(true);
        
        if (playerMovement) playerMovement.SetFrozen(false);
        if (mouseLookAt) mouseLookAt.SetFrozen(false);
        if (attackManager) attackManager.SetFrozen(false);
        
        foreach (BaseAbility ability in frozenAbilities)
        {
            if (ability) ability.SetFrozen(false);
        }
        frozenAbilities.Clear();
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        if (waveManager) waveManager.OnAttackSelected();
        
        foreach (AppleEnemy enemy in frozenEnemies)
        {
            if (enemy) enemy.SetFrozen(false);
        }
        frozenEnemies.Clear();
        
        isUIOpen = false;
        isTransitioning = false;
        currentTransition = null;
        
        if (uiAnimator)
        {
            uiAnimator.SetBool(openBool, false);
        }
        
        // Notify ClassicModeManager that menu is closing - it will restore the correct camera
        if (ClassicModeManager.Instance != null)
        {
            ClassicModeManager.Instance.OnMenuClosed();
        }
        else if (cameraManager)
        {
            // Fallback if no ClassicModeManager - switch to normal camera
            cameraManager.SwitchToNormalCamera();
        }
    }
    
    private void ApplySelection()
    {
        if (selectedFallback != FallbackOption.None) { ApplyFallbackOption(); return; }
        if (selectedSwapAttack != null) { ApplyAttackSwap(); return; }
        if (selectedAbility != null && abilityManager != null) { abilityManager.AddAbility(selectedAbility.abilityPrefab, selectedAbility); return; }
        if (selectedAttack != null && attackManager != null)
        {
            bool ownsAttack = attackManager.HasAttack(selectedAttack);
            
            if (ownsAttack)
            {
                if (selectedAttack.CanUpgrade()) selectedAttack.TryUpgrade();
            }
            else
            {
                attackManager.AddAttack(selectedAttack);
            }
        }
    }
    
    private void ApplyAttackSwap()
    {
        if (selectedSwapAttack == null || attackManager == null) return;
        
        Attack currentAttack = attackManager.GetCurrentAttack();
        if (currentAttack == null) return;
        
        int currentLevel = currentAttack.GetCurrentLevel();
        int maxSwapLevel = selectedSwapAttack.GetMaxLevel();
        int targetLevel = Mathf.Min(currentLevel, maxSwapLevel);
        
        // Clear the current attack
        attackManager.ClearAllAttacks();
        
        // Add the new attack
        attackManager.AddAttack(selectedSwapAttack);
        
        // Upgrade it to the target level (level 1 is base, so upgrade targetLevel - 1 times)
        for (int i = 1; i < targetLevel; i++)
        {
            if (selectedSwapAttack.CanUpgrade())
            {
                selectedSwapAttack.TryUpgrade();
            }
            else
            {
                Debug.LogWarning($"Could not upgrade {selectedSwapAttack.attackName} to level {i + 1}");
                break;
            }
        }
        
        Debug.Log($"Swapped attack to {selectedSwapAttack.attackName} at level {selectedSwapAttack.GetCurrentLevel()}");
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
            
            AbilitySO matchingSO = abilityManager.GetAbilitySO(ability);
            if (matchingSO == null) matchingSO = FindAbilitySOForAbility(ability);
            
            bool isPassive = matchingSO == null || matchingSO.abilityType == AbilityType.Passive;
            
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
        
        foreach (AbilitySO so in possibleAbilities)
        {
            if (so && so.abilityPrefab && abilityName.Contains(so.abilityPrefab.name))
                return so;
        }
        
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
        foreach (var d in spawnedPassiveAbilityDisplays) if (d) Destroy(d);
        spawnedPassiveAbilityDisplays.Clear();
        
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
        
        foreach (AppleEnemy enemy in FindObjectsByType<AppleEnemy>(FindObjectsSortMode.None))
        {
            if (enemy)
            {
                enemy.SetFrozen(true);
                frozenEnemies.Add(enemy);
            }
        }
        
        if (playerMovement) playerMovement.SetFrozen(true);
        if (mouseLookAt) mouseLookAt.SetFrozen(true);
        if (attackManager) attackManager.SetFrozen(true);
        
        if (abilityManager)
        {
            foreach (BaseAbility ability in abilityManager.GetActiveAbilities())
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
        foreach (AppleEnemy enemy in frozenEnemies)
        {
            if (enemy) enemy.SetFrozen(false);
        }
        frozenEnemies.Clear();
        
        if (playerMovement) playerMovement.SetFrozen(false);
        if (mouseLookAt) mouseLookAt.SetFrozen(false);
        if (attackManager) attackManager.SetFrozen(false);
        
        foreach (BaseAbility ability in frozenAbilities)
        {
            if (ability) ability.SetFrozen(false);
        }
        frozenAbilities.Clear();
        
        isUIOpen = false;
    }
    
    private class AttackSwapOption
    {
        public Attack swapToAttack;
    }
}