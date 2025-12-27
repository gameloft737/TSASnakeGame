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
    
    [Header("Current Abilities Display")]
    [SerializeField] private Transform currentAbilitiesContainer; // Container to show current abilities on the side
    [SerializeField] private GameObject currentAbilityDisplayPrefab; // Prefab for displaying current abilities
    [SerializeField] private AbilityManager abilityManager; // Reference to get active abilities
    [SerializeField] private List<AbilitySO> availableAbilities = new List<AbilitySO>(); // Pool of all abilities for matching
    
    [Header("Current Attack Display")]
    [SerializeField] private Transform currentAttackContainer; // Container to show current attack on the side
    [SerializeField] private GameObject currentAttackDisplayPrefab; // Prefab for displaying current attack

    private DepthOfField dof;

    [SerializeField] private AttackManager attackManager;
    [SerializeField] private WaveManager waveManager;
    private List<GameObject> spawnedButtons = new List<GameObject>();
    private List<GameObject> spawnedCurrentAbilityDisplays = new List<GameObject>(); // Track current ability displays
    private GameObject spawnedCurrentAttackDisplay; // Track current attack display

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
        PopulateCurrentAbilities(); // Show current abilities on the side
        PopulateCurrentAttack(); // Show current attack on the side

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

            // Try to use the new AttackButton component
            AttackButton attackButton = buttonObj.GetComponent<AttackButton>();
            if (attackButton != null)
            {
                bool isSelected = attackIndex == attackManager.GetCurrentAttackIndex();
                attackButton.Initialize(attack, attackIndex, this, isSelected);
            }
            else
            {
                // Fallback to old behavior if AttackButton component not found
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
    }

    private void HideInstant()
    {
        if (selectionPanel != null) selectionPanel.SetActive(false);
        dof.active = false;
    }

    /// <summary>
    /// Called when an attack button is clicked
    /// </summary>
    public void OnAttackButtonClicked(int attackIndex)
    {
        attackIdxSelected = attackIndex;
        
        // Update all buttons to show selection state
        UpdateButtonSelections();
    }
    
    /// <summary>
    /// Updates all button visuals to reflect current selection
    /// </summary>
    private void UpdateButtonSelections()
    {
        foreach (var buttonObj in spawnedButtons)
        {
            AttackButton attackButton = buttonObj.GetComponent<AttackButton>();
            if (attackButton != null)
            {
                bool isSelected = attackButton.GetAttackIndex() == attackIdxSelected;
                attackButton.SetSelected(isSelected);
            }
            else
            {
                // Fallback for old button style
                Image buttonImage = buttonObj.GetComponent<Image>();
                if (buttonImage != null)
                {
                    AttackButton btn = buttonObj.GetComponent<AttackButton>();
                    int idx = btn != null ? btn.GetAttackIndex() : -1;
                    buttonImage.color = (idx == attackIdxSelected) ? Color.green : Color.white;
                }
            }
        }
    }

    private void OnContinueClicked()
    {
        StartCoroutine(CloseSequence());
    }

    private IEnumerator CloseSequence()
    {
        // Upgrade the selected attack before closing
        UpgradeSelectedAttack();
        
        // Start animation and DOF at the same time
        if (uiAnimator != null) uiAnimator.SetBool(openBool, false);
        
        if (dofRoutine != null) StopCoroutine(dofRoutine);
        dofRoutine = StartCoroutine(LerpDOF(false));
        
        // Wait for both to complete
        yield return dofRoutine;
        
        // Only lift pause after animation and DOF are done
        if (selectionPanel != null) selectionPanel.SetActive(false);
        cameraManager.SwitchToNormalCamera();
        
        // Clear ability and attack displays when closing
        ClearCurrentAbilityDisplays();
        ClearCurrentAttackDisplay();
        
        nonUI.SetActive(true);
        attackManager.SetAttackIndex(attackIdxSelected);
        if (waveManager != null) waveManager.OnAttackSelected();

        Cursor.visible = false; // Hide the cursor
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    /// <summary>
    /// Upgrades the selected attack if possible
    /// </summary>
    private void UpgradeSelectedAttack()
    {
        if (attackManager == null) return;
        if (attackIdxSelected < 0 || attackIdxSelected >= attackManager.attacks.Count) return;
        
        Attack selectedAttack = attackManager.attacks[attackIdxSelected];
        if (selectedAttack != null && selectedAttack.CanUpgrade())
        {
            selectedAttack.TryUpgrade();
            Debug.Log($"Upgraded {selectedAttack.attackName} to level {selectedAttack.GetCurrentLevel()}!");
        }
    }
    
    /// <summary>
    /// Populates the current abilities container with the player's active abilities
    /// </summary>
    private void PopulateCurrentAbilities()
    {
        // Clear existing displays
        ClearCurrentAbilityDisplays();
        
        if (currentAbilitiesContainer == null)
        {
            return; // No container assigned, skip
        }
        
        // Try to find ability manager if not assigned
        if (abilityManager == null)
        {
            abilityManager = FindFirstObjectByType<AbilityManager>();
        }
        
        if (abilityManager == null)
        {
            return; // No ability manager found
        }
        
        // Get all active abilities from the ability manager
        List<BaseAbility> activeAbilities = abilityManager.GetActiveAbilities();
        
        foreach (BaseAbility ability in activeAbilities)
        {
            if (ability == null) continue;
            
            // Find the matching AbilitySO for this ability
            AbilitySO matchingSO = FindAbilitySOForAbility(ability);
            
            if (currentAbilityDisplayPrefab != null)
            {
                // Use the prefab if assigned
                GameObject displayObj = Instantiate(currentAbilityDisplayPrefab, currentAbilitiesContainer);
                spawnedCurrentAbilityDisplays.Add(displayObj);
                
                // Try to initialize it with CurrentAbilityDisplay component
                CurrentAbilityDisplay display = displayObj.GetComponent<CurrentAbilityDisplay>();
                if (display != null)
                {
                    display.Initialize(ability, matchingSO);
                }
                else
                {
                    // Fallback: try to set up basic display elements
                    SetupBasicAbilityDisplay(displayObj, ability, matchingSO);
                }
            }
            else
            {
                // Create a simple text display if no prefab
                CreateSimpleAbilityDisplay(ability, matchingSO);
            }
        }
    }
    
    /// <summary>
    /// Finds the AbilitySO that matches the given ability
    /// </summary>
    private AbilitySO FindAbilitySOForAbility(BaseAbility ability)
    {
        if (ability == null) return null;
        
        string abilityName = ability.gameObject.name;
        
        foreach (AbilitySO so in availableAbilities)
        {
            if (so != null && so.abilityPrefab != null)
            {
                if (abilityName.Contains(so.abilityPrefab.name))
                {
                    return so;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Sets up basic display elements on a display object
    /// </summary>
    private void SetupBasicAbilityDisplay(GameObject displayObj, BaseAbility ability, AbilitySO abilitySO)
    {
        // Try to find and set text
        TextMeshProUGUI nameText = displayObj.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            string abilityName = abilitySO != null ? abilitySO.abilityName : ability.gameObject.name;
            nameText.text = $"{abilityName} Lv.{ability.GetCurrentLevel()}";
        }
        
        // Try to find and set icon
        if (abilitySO != null && abilitySO.icon != null)
        {
            Image iconImage = displayObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = abilitySO.icon;
            }
        }
    }
    
    /// <summary>
    /// Creates a simple text-based ability display
    /// </summary>
    private void CreateSimpleAbilityDisplay(BaseAbility ability, AbilitySO abilitySO)
    {
        GameObject displayObj = new GameObject("AbilityDisplay");
        displayObj.transform.SetParent(currentAbilitiesContainer);
        displayObj.transform.localScale = Vector3.one;
        
        TextMeshProUGUI text = displayObj.AddComponent<TextMeshProUGUI>();
        string abilityName = abilitySO != null ? abilitySO.abilityName : ability.gameObject.name;
        text.text = $"{abilityName} Lv.{ability.GetCurrentLevel()}";
        text.fontSize = 14;
        text.alignment = TextAlignmentOptions.Left;
        
        spawnedCurrentAbilityDisplays.Add(displayObj);
    }
    
    /// <summary>
    /// Clears all spawned current ability displays
    /// </summary>
    private void ClearCurrentAbilityDisplays()
    {
        foreach (GameObject display in spawnedCurrentAbilityDisplays)
        {
            if (display != null)
            {
                Destroy(display);
            }
        }
        spawnedCurrentAbilityDisplays.Clear();
    }
    
    /// <summary>
    /// Populates the current attack container with the player's current attack
    /// </summary>
    private void PopulateCurrentAttack()
    {
        // Clear existing display
        ClearCurrentAttackDisplay();
        
        if (currentAttackContainer == null)
        {
            return; // No container assigned, skip
        }
        
        if (attackManager == null)
        {
            return; // No attack manager
        }
        
        // Get the current attack
        Attack currentAttack = attackManager.GetCurrentAttack();
        if (currentAttack == null)
        {
            return; // No current attack
        }
        
        if (currentAttackDisplayPrefab != null)
        {
            // Use the prefab if assigned
            spawnedCurrentAttackDisplay = Instantiate(currentAttackDisplayPrefab, currentAttackContainer);
            
            // Try to initialize it with CurrentAttackDisplay component
            CurrentAttackDisplay display = spawnedCurrentAttackDisplay.GetComponent<CurrentAttackDisplay>();
            if (display != null)
            {
                display.Initialize(currentAttack);
            }
            else
            {
                // Fallback: try to set up basic display elements
                SetupBasicAttackDisplay(spawnedCurrentAttackDisplay, currentAttack);
            }
        }
        else
        {
            // Create a simple text display if no prefab
            CreateSimpleAttackDisplay(currentAttack);
        }
    }
    
    /// <summary>
    /// Sets up basic display elements on an attack display object
    /// </summary>
    private void SetupBasicAttackDisplay(GameObject displayObj, Attack attack)
    {
        // Try to find and set text
        TextMeshProUGUI nameText = displayObj.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = $"{attack.attackName} Lv.{attack.GetCurrentLevel()}";
        }
        
        // Try to find and set icon
        if (attack.attackIcon != null)
        {
            Image iconImage = displayObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = attack.attackIcon;
            }
        }
    }
    
    /// <summary>
    /// Creates a simple text-based attack display
    /// </summary>
    private void CreateSimpleAttackDisplay(Attack attack)
    {
        spawnedCurrentAttackDisplay = new GameObject("AttackDisplay");
        spawnedCurrentAttackDisplay.transform.SetParent(currentAttackContainer);
        spawnedCurrentAttackDisplay.transform.localScale = Vector3.one;
        
        TextMeshProUGUI text = spawnedCurrentAttackDisplay.AddComponent<TextMeshProUGUI>();
        text.text = $"{attack.attackName} Lv.{attack.GetCurrentLevel()}";
        text.fontSize = 14;
        text.alignment = TextAlignmentOptions.Left;
    }
    
    /// <summary>
    /// Clears the spawned current attack display
    /// </summary>
    private void ClearCurrentAttackDisplay()
    {
        if (spawnedCurrentAttackDisplay != null)
        {
            Destroy(spawnedCurrentAttackDisplay);
            spawnedCurrentAttackDisplay = null;
        }
    }
}