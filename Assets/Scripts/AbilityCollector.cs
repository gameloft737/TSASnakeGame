using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(AbilityManager))]
public class AbilityCollector : MonoBehaviour
{
    [Header("Collection Settings")]
    [SerializeField] private bool autoCollect = true;

    [Header("Available Abilities")]
    [SerializeField] private List<AbilitySO> availableAbilities = new List<AbilitySO>();
    [SerializeField] private int abilitiesToShow = 3;

    [Header("Audio")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioClip upgradeSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject collectionEffectPrefab;
    [SerializeField] private GameObject upgradeEffectPrefab;

    [Header("UI References")]
    [SerializeField] private Animator uiAnimator;
    [SerializeField] private string animatorBoolParameter = "isOpen";
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject uiContainer;
    [SerializeField] private GameObject nonUI;

    [Header("Ability Selection UI")]
    [SerializeField] private Transform abilityButtonContainer;
    [SerializeField] private GameObject abilityButtonPrefab;
    
    [Header("Current Abilities Display (Drop Menu - Passive)")]
    [SerializeField] private Transform passiveAbilitiesContainer;
    [SerializeField] private GameObject passiveAbilityDisplayPrefab;
    
    [Header("Current Abilities Display (Drop Menu - Active)")]
    [SerializeField] private Transform activeAbilitiesContainer;
    [SerializeField] private GameObject activeAbilityDisplayPrefab;
    
    [Header("Current Attack Display (Drop Menu)")]
    [SerializeField] private Transform currentAttackContainer;
    [SerializeField] private GameObject currentAttackDisplayPrefab;

    [Header("References")]
    [SerializeField] private CursorLock cursorLock;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLookAt mouseLookAt;
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private AttackManager attackManager;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private AttackSelectionUI attackSelectionUI;
    [SerializeField] private Volume postProcessVolume;
    
    [Header("DOF Settings")]
    [SerializeField] private float targetStart = 3f;
    [SerializeField] private float targetEnd = 10f;

    private AbilityManager abilityManager;
    private DepthOfField dof;
    
    private List<GameObject> spawnedButtons = new List<GameObject>();
    private List<GameObject> spawnedPassiveAbilityDisplays = new List<GameObject>();
    private List<GameObject> spawnedActiveAbilityDisplays = new List<GameObject>();
    private List<GameObject> spawnedAttackDisplays = new List<GameObject>();
    private List<AppleEnemy> frozenEnemies = new List<AppleEnemy>();
    private List<BaseAbility> frozenAbilities = new List<BaseAbility>();
    
    private bool isUIOpen = false;
    private const float ANIM_TIME = 0.66f;

    private void Awake()
    {
        abilityManager = GetComponent<AbilityManager>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        FindMissingReferences();
    }

    private void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(CloseCanvas);
        if (uiContainer != null) uiContainer.SetActive(false);
        
        if (postProcessVolume != null)
        {
            postProcessVolume.profile.TryGet(out dof);
            if (dof != null) dof.active = false;
        }
        
        isUIOpen = false;
    }
    
    private void FindMissingReferences()
    {
        if (playerMovement == null) playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (mouseLookAt == null) mouseLookAt = FindFirstObjectByType<MouseLookAt>();
        if (cameraManager == null) cameraManager = FindFirstObjectByType<CameraManager>();
        if (attackManager == null) attackManager = FindFirstObjectByType<AttackManager>();
        if (enemySpawner == null) enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (attackSelectionUI == null) attackSelectionUI = FindFirstObjectByType<AttackSelectionUI>();
        if (cursorLock == null) cursorLock = FindFirstObjectByType<CursorLock>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!autoCollect || isUIOpen) return;
        AbilityDrop drop = other.GetComponent<AbilityDrop>();
        if (drop != null && drop.IsGrounded() && !drop.IsCollected())
        {
            TryCollectDrop(drop);
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (!autoCollect || isUIOpen) return;
        AbilityDrop drop = other.GetComponent<AbilityDrop>();
        if (drop != null && drop.IsGrounded() && !drop.IsCollected())
        {
            TryCollectDrop(drop);
        }
    }

    public bool TryCollectDrop(AbilityDrop drop)
    {
        if (drop == null || drop.IsCollected() || !drop.IsGrounded() || isUIOpen) return false;
        if (attackSelectionUI != null && attackSelectionUI.IsUIOpen()) return false;

        drop.Collect();
        StartCoroutine(OpenUI());
        return true;
    }

    private IEnumerator OpenUI()
    {
        isUIOpen = true;
        
        FreezeAll();
        if (cameraManager != null)
        {
            cameraManager.SetFrozen(true);
            cameraManager.SwitchToPauseCamera();
        }
        if (cursorLock != null) cursorLock.StartAbilitySelection();
        
        if (nonUI != null) nonUI.SetActive(false);
        if (uiContainer != null) uiContainer.SetActive(true);
        
        PopulateAbilityButtons();
        PopulateCurrentAbilities();
        PopulateCurrentAttack();
        
        if (uiAnimator != null) uiAnimator.SetBool(animatorBoolParameter, true);
        StartCoroutine(DOFLerp(true));
        
        yield return new WaitForSecondsRealtime(ANIM_TIME);
    }

    public void SelectAbility(AbilitySO abilitySO)
    {
        if (!isUIOpen || abilitySO == null || abilitySO.abilityPrefab == null) return;

        BaseAbility newAbility = abilityManager.AddAbility(abilitySO.abilityPrefab, abilitySO);
        if (newAbility != null) PlayEffect(transform.position, false);
        
        CloseCanvas();
    }

    private void CloseCanvas()
    {
        if (!isUIOpen) return;
        StartCoroutine(CloseUI());
    }

    private IEnumerator CloseUI()
    {
        if (uiAnimator != null) uiAnimator.SetBool(animatorBoolParameter, false);
        StartCoroutine(DOFLerp(false));
        
        yield return new WaitForSecondsRealtime(ANIM_TIME);
        
        if (uiContainer != null) uiContainer.SetActive(false);
        if (nonUI != null) nonUI.SetActive(true);
        
        ClearAbilityButtons();
        ClearCurrentAbilityDisplays();
        ClearCurrentAttackDisplay();
        
        if (cameraManager != null)
        {
            cameraManager.SetFrozen(false);
            cameraManager.SwitchToNormalCamera();
        }
        if (cursorLock != null) cursorLock.StopAbilitySelection();
        
        UnfreezeAll();
        
        isUIOpen = false;
    }
    
    private IEnumerator DOFLerp(bool enable)
    {
        if (dof == null) yield break;
        
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
        
        if (!enable) dof.active = false;
    }
    
    private void FreezeAll()
    {
        frozenEnemies.Clear();
        frozenAbilities.Clear();

        if (playerMovement != null) playerMovement.SetFrozen(true);
        if (mouseLookAt != null) mouseLookAt.SetFrozen(true);
        if (attackManager != null) attackManager.SetFrozen(true);

        AppleEnemy[] enemies = FindObjectsByType<AppleEnemy>(FindObjectsSortMode.None);
        foreach (AppleEnemy enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.SetFrozen(true);
                frozenEnemies.Add(enemy);
            }
        }

        if (abilityManager != null)
        {
            List<BaseAbility> activeAbilities = abilityManager.GetActiveAbilities();
            foreach (BaseAbility ability in activeAbilities)
            {
                if (ability != null)
                {
                    ability.SetFrozen(true);
                    frozenAbilities.Add(ability);
                }
            }
        }

        if (enemySpawner != null) enemySpawner.StopSpawning();
    }
    
    private void UnfreezeAll()
    {
        if (playerMovement != null) playerMovement.SetFrozen(false);
        if (mouseLookAt != null) mouseLookAt.SetFrozen(false);
        if (attackManager != null) attackManager.SetFrozen(false);
        
        foreach (BaseAbility ability in frozenAbilities)
        {
            if (ability != null) ability.SetFrozen(false);
        }
        frozenAbilities.Clear();
        
        foreach (AppleEnemy enemy in frozenEnemies)
        {
            if (enemy != null) enemy.SetFrozen(false);
        }
        frozenEnemies.Clear();
        
        WaveManager waveManager = FindFirstObjectByType<WaveManager>();
        if (waveManager != null) waveManager.ResumeWave();
        else if (enemySpawner != null) enemySpawner.ResumeSpawning();
    }

    private void PopulateAbilityButtons()
    {
        ClearAbilityButtons();
        if (abilityButtonContainer == null || abilityButtonPrefab == null) return;

        List<AbilitySO> randomAbilities = GetRandomAbilities(abilitiesToShow);
        foreach (AbilitySO ability in randomAbilities)
        {
            if (ability == null) continue;
            GameObject buttonObj = Instantiate(abilityButtonPrefab, abilityButtonContainer);
            spawnedButtons.Add(buttonObj);
            AbilityButton abilityButton = buttonObj.GetComponent<AbilityButton>();
            if (abilityButton != null) abilityButton.Initialize(ability, this);
        }
    }

    private List<AbilitySO> GetRandomAbilities(int count)
    {
        List<AbilitySO> validAbilities = new List<AbilitySO>();
        foreach (AbilitySO ability in availableAbilities)
        {
            if (ability != null && ability.abilityPrefab != null)
            {
                if (abilityManager.CanAddOrUpgradeAbility(ability))
                {
                    validAbilities.Add(ability);
                }
            }
        }
        
        if (validAbilities.Count == 0) return new List<AbilitySO>();
        if (validAbilities.Count <= count) return new List<AbilitySO>(validAbilities);

        List<AbilitySO> shuffled = new List<AbilitySO>(validAbilities);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            AbilitySO temp = shuffled[i];
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }

        List<AbilitySO> result = new List<AbilitySO>();
        for (int i = 0; i < count && i < shuffled.Count; i++)
        {
            result.Add(shuffled[i]);
        }
        return result;
    }
    
    private void PopulateCurrentAbilities()
    {
        ClearCurrentAbilityDisplays();
        if (abilityManager == null) return;
        
        List<BaseAbility> allAbilities = abilityManager.GetActiveAbilities();
        foreach (BaseAbility ability in allAbilities)
        {
            if (ability == null) continue;
            
            AbilitySO matchingSO = abilityManager.GetAbilitySO(ability);
            if (matchingSO == null) matchingSO = FindAbilitySOForAbility(ability);
            
            bool isPassive = matchingSO == null || matchingSO.abilityType == AbilityType.Passive;
            Transform targetContainer = isPassive ? passiveAbilitiesContainer : activeAbilitiesContainer;
            GameObject prefab = isPassive ? passiveAbilityDisplayPrefab : activeAbilityDisplayPrefab;
            List<GameObject> displayList = isPassive ? spawnedPassiveAbilityDisplays : spawnedActiveAbilityDisplays;
            
            if (targetContainer == null) continue;
            
            if (prefab != null)
            {
                GameObject displayObj = Instantiate(prefab, targetContainer);
                displayList.Add(displayObj);
                CurrentAbilityDisplay display = displayObj.GetComponent<CurrentAbilityDisplay>();
                if (display != null) display.Initialize(ability, matchingSO);
                else SetupBasicAbilityDisplay(displayObj, ability, matchingSO);
            }
            else
            {
                CreateSimpleAbilityDisplay(ability, matchingSO, targetContainer, displayList);
            }
        }
    }
    
    private AbilitySO FindAbilitySOForAbility(BaseAbility ability)
    {
        if (ability == null) return null;
        string abilityName = ability.gameObject.name;
        foreach (AbilitySO so in availableAbilities)
        {
            if (so != null && so.abilityPrefab != null)
            {
                if (abilityName.Contains(so.abilityPrefab.name)) return so;
            }
        }
        return null;
    }
    
    private void SetupBasicAbilityDisplay(GameObject displayObj, BaseAbility ability, AbilitySO abilitySO)
    {
        TMPro.TextMeshProUGUI nameText = displayObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (nameText != null)
        {
            string abilityName = abilitySO != null ? abilitySO.abilityName : ability.gameObject.name;
            nameText.text = $"{abilityName} Lv.{ability.GetCurrentLevel()}";
        }
        
        if (abilitySO != null && abilitySO.icon != null)
        {
            UnityEngine.UI.Image iconImage = displayObj.transform.Find("Icon")?.GetComponent<UnityEngine.UI.Image>();
            if (iconImage != null) iconImage.sprite = abilitySO.icon;
        }
    }
    
    private void CreateSimpleAbilityDisplay(BaseAbility ability, AbilitySO abilitySO, Transform container, List<GameObject> displayList)
    {
        GameObject displayObj = new GameObject("AbilityDisplay");
        displayObj.transform.SetParent(container);
        displayObj.transform.localScale = Vector3.one;
        TMPro.TextMeshProUGUI text = displayObj.AddComponent<TMPro.TextMeshProUGUI>();
        string abilityName = abilitySO != null ? abilitySO.abilityName : ability.gameObject.name;
        text.text = $"{abilityName} Lv.{ability.GetCurrentLevel()}";
        text.fontSize = 14;
        text.alignment = TMPro.TextAlignmentOptions.Left;
        displayList.Add(displayObj);
    }
    
    private void ClearCurrentAbilityDisplays()
    {
        foreach (GameObject display in spawnedPassiveAbilityDisplays)
        {
            if (display != null) Destroy(display);
        }
        spawnedPassiveAbilityDisplays.Clear();
        
        foreach (GameObject display in spawnedActiveAbilityDisplays)
        {
            if (display != null) Destroy(display);
        }
        spawnedActiveAbilityDisplays.Clear();
    }

    private void ClearAbilityButtons()
    {
        foreach (GameObject button in spawnedButtons)
        {
            if (button != null) Destroy(button);
        }
        spawnedButtons.Clear();
    }
    
    private void PopulateCurrentAttack()
    {
        ClearCurrentAttackDisplay();
        if (currentAttackContainer == null || attackManager == null) return;
        
        int attackCount = attackManager.GetAttackCount();
        if (attackCount == 0) return;
        
        for (int i = 0; i < attackCount; i++)
        {
            Attack attack = attackManager.GetAttackAtIndex(i);
            if (attack == null) continue;
            bool isActive = (i == 0);
            
            if (currentAttackDisplayPrefab != null)
            {
                GameObject displayObj = Instantiate(currentAttackDisplayPrefab, currentAttackContainer);
                spawnedAttackDisplays.Add(displayObj);
                CurrentAttackDisplay display = displayObj.GetComponent<CurrentAttackDisplay>();
                if (display != null) display.Initialize(attack, isActive);
                else SetupBasicAttackDisplay(displayObj, attack, isActive);
            }
            else
            {
                CreateSimpleAttackDisplay(attack, isActive);
            }
        }
    }
    
    private void SetupBasicAttackDisplay(GameObject displayObj, Attack attack, bool isActive)
    {
        TMPro.TextMeshProUGUI nameText = displayObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (nameText != null) nameText.text = attack.attackName;
        
        TMPro.TextMeshProUGUI levelText = displayObj.transform.Find("Level")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (levelText == null) levelText = displayObj.transform.Find("LevelText")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (levelText != null) levelText.text = $"Lvl {attack.GetCurrentLevel()}";
        
        if (attack.attackIcon != null)
        {
            UnityEngine.UI.Image iconImage = displayObj.transform.Find("Icon")?.GetComponent<UnityEngine.UI.Image>();
            if (iconImage != null) iconImage.sprite = attack.attackIcon;
        }
        
        UnityEngine.UI.Image backgroundImage = displayObj.GetComponent<UnityEngine.UI.Image>();
        if (backgroundImage != null)
        {
            backgroundImage.color = isActive ? new Color(0.3f, 1f, 0.3f, 1f) : new Color(0.8f, 0.8f, 0.8f, 1f);
        }
        
        GameObject activeIndicator = displayObj.transform.Find("ActiveIndicator")?.gameObject;
        if (activeIndicator != null) activeIndicator.SetActive(isActive);
    }
    
    private void CreateSimpleAttackDisplay(Attack attack, bool isActive)
    {
        GameObject displayObj = new GameObject("AttackDisplay");
        displayObj.transform.SetParent(currentAttackContainer);
        displayObj.transform.localScale = Vector3.one;
        TMPro.TextMeshProUGUI text = displayObj.AddComponent<TMPro.TextMeshProUGUI>();
        string activeMarker = isActive ? " [ACTIVE]" : "";
        text.text = $"{attack.attackName} Lvl {attack.GetCurrentLevel()}{activeMarker}";
        text.fontSize = 14;
        text.alignment = TMPro.TextAlignmentOptions.Left;
        text.color = isActive ? new Color(0.3f, 1f, 0.3f, 1f) : Color.white;
        spawnedAttackDisplays.Add(displayObj);
    }
    
    private void ClearCurrentAttackDisplay()
    {
        foreach (GameObject display in spawnedAttackDisplays)
        {
            if (display != null) Destroy(display);
        }
        spawnedAttackDisplays.Clear();
    }

    private void PlayEffect(Vector3 position, bool isUpgrade)
    {
        AudioClip sound = isUpgrade ? upgradeSound : collectSound;
        if (sound != null && audioSource != null) audioSource.PlayOneShot(sound);

        GameObject effectPrefab = isUpgrade ? upgradeEffectPrefab : collectionEffectPrefab;
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    public void AddAbilityToPlayer(AbilitySO abilitySO)
    {
        if (abilitySO == null || abilitySO.abilityPrefab == null) return;
        abilityManager.AddAbility(abilitySO.abilityPrefab, abilitySO);
    }
    
    public AbilityManager GetAbilityManager() => abilityManager;
    public List<AbilitySO> GetAvailableAbilities() => availableAbilities;
    public bool IsUIOpen() => isUIOpen;

    public void AddAvailableAbility(AbilitySO ability)
    {
        if (ability != null && !availableAbilities.Contains(ability))
        {
            availableAbilities.Add(ability);
        }
    }

    public void RemoveAvailableAbility(AbilitySO ability)
    {
        if (ability != null && availableAbilities.Contains(ability))
        {
            availableAbilities.Remove(ability);
        }
    }
}