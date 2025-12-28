using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AbilityManager))]
public class AbilityCollector : MonoBehaviour
{
    [Header("Collection Settings")]
    [SerializeField] private bool autoCollect = true;

    [Header("Available Abilities")]
    [SerializeField] private List<AbilitySO> availableAbilities = new List<AbilitySO>(); // Pool of all abilities
    [SerializeField] private int abilitiesToShow = 3; // Number of random abilities to show each time

    [Header("Audio")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioClip upgradeSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject collectionEffectPrefab;
    [SerializeField] private GameObject upgradeEffectPrefab;

    [Header("UI References")]
    [SerializeField] private Animator uiAnimator; // Reference to the UI Animator
    [SerializeField] private string openTrigger = "isOpen"; // The boolean parameter in the Animator (for example: "isOpen")

    [SerializeField] private Button closeButton; // Drag and drop the close button here
    [SerializeField] private GameObject uiContainer; // The UI container to show/hide before animation

    [Header("Ability Selection UI")]
    [SerializeField] private Transform abilityButtonContainer; // Container to hold ability buttons
    [SerializeField] private GameObject abilityButtonPrefab; // Prefab for ability buttons
    
    [Header("Current Abilities Display (Drop Menu - Passive)")]
    [SerializeField] private Transform passiveAbilitiesContainer; // Container for passive abilities in drop menu
    [SerializeField] private GameObject passiveAbilityDisplayPrefab; // Prefab for displaying passive abilities
    
    [Header("Current Abilities Display (Drop Menu - Active)")]
    [SerializeField] private Transform activeAbilitiesContainer; // Container for active abilities in drop menu
    [SerializeField] private GameObject activeAbilityDisplayPrefab; // Prefab for displaying active abilities
    
    [Header("Current Attack Display")]
    [SerializeField] private Transform currentAttackContainer; // Container to show current attack on the side
    [SerializeField] private GameObject currentAttackDisplayPrefab; // Prefab for displaying current attack

    // Reference to CursorLock script
    [SerializeField] private CursorLock cursorLock; // Drag and drop the CursorLock script here

    [Header("Freeze References")]
    [SerializeField] private PlayerMovement playerMovement; // Reference to player movement to freeze
    [SerializeField] private MouseLookAt mouseLookAt; // Reference to mouse look to freeze
    [SerializeField] private CameraManager cameraManager; // Reference to camera manager to freeze
    [SerializeField] private AttackManager attackManager; // Reference to attack manager to freeze
    [SerializeField] private EnemySpawner enemySpawner; // Reference to enemy spawner to pause spawning
    
    [Header("Post-Selection")]
    [SerializeField] private float enemyFreezeDelayAfterClose = 0.5f; // How long enemies stay frozen after menu closes

    private AbilityManager abilityManager;
    private List<GameObject> spawnedButtons = new List<GameObject>(); // Track spawned buttons for cleanup
    private List<GameObject> spawnedPassiveAbilityDisplays = new List<GameObject>(); // Track passive ability displays
    private List<GameObject> spawnedActiveAbilityDisplays = new List<GameObject>(); // Track active ability displays
    private GameObject spawnedCurrentAttackDisplay; // Track current attack display
    private List<AppleEnemy> frozenEnemies = new List<AppleEnemy>(); // Track frozen enemies
    private List<BaseAbility> frozenAbilities = new List<BaseAbility>(); // Track frozen abilities

    private void Start()
    {
        // If the close button is assigned, set it up to close the canvas
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseCanvas); // Assign the close button to trigger CloseCanvas method
        }

        // Optionally ensure UI is hidden initially
        if (uiAnimator != null)
        {
            uiAnimator.SetBool(openTrigger, false); // Ensure it's initially closed
        }

        // Ensure the UI container is hidden on start
        if (uiContainer != null)
        {
            uiContainer.SetActive(false); // Initially hide the container
        }
    }

    /// <summary>
    /// Populates the ability button container with random abilities from the pool
    /// </summary>
    private void PopulateAbilityButtons()
    {
        // Clear existing buttons
        ClearAbilityButtons();

        if (abilityButtonContainer == null || abilityButtonPrefab == null)
        {
            Debug.LogWarning("AbilityCollector: Button container or prefab not assigned!");
            return;
        }

        // Get random abilities from the pool
        List<AbilitySO> randomAbilities = GetRandomAbilities(abilitiesToShow);

        // Create a button for each selected ability
        foreach (AbilitySO ability in randomAbilities)
        {
            if (ability == null) continue;

            GameObject buttonObj = Instantiate(abilityButtonPrefab, abilityButtonContainer);
            spawnedButtons.Add(buttonObj);

            // Initialize the button with the ability data
            AbilityButton abilityButton = buttonObj.GetComponent<AbilityButton>();
            if (abilityButton != null)
            {
                abilityButton.Initialize(ability, this);
            }
        }
    }

    /// <summary>
    /// Gets a random selection of abilities from the available pool (excluding maxed out abilities)
    /// </summary>
    private List<AbilitySO> GetRandomAbilities(int count)
    {
        List<AbilitySO> result = new List<AbilitySO>();
        
        // Filter out null abilities and maxed out abilities
        List<AbilitySO> validAbilities = new List<AbilitySO>();
        foreach (AbilitySO ability in availableAbilities)
        {
            if (ability != null && ability.abilityPrefab != null)
            {
                // Check if this ability is already maxed out
                BaseAbility existingAbility = abilityManager.GetAbility(ability.abilityPrefab);
                if (existingAbility == null || !existingAbility.IsMaxLevel())
                {
                    validAbilities.Add(ability);
                }
            }
        }

        // If we have fewer abilities than requested, return all of them
        if (validAbilities.Count <= count)
        {
            return new List<AbilitySO>(validAbilities);
        }

        // Create a shuffled copy of the list
        List<AbilitySO> shuffled = new List<AbilitySO>(validAbilities);
        
        // Fisher-Yates shuffle
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            AbilitySO temp = shuffled[i];
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }

        // Take the first 'count' abilities
        for (int i = 0; i < count && i < shuffled.Count; i++)
        {
            result.Add(shuffled[i]);
        }

        return result;
    }
    
    /// <summary>
    /// Populates the current abilities containers with the player's abilities (separated by type)
    /// </summary>
    private void PopulateCurrentAbilities()
    {
        // Clear existing displays
        ClearCurrentAbilityDisplays();
        
        // Get all abilities from the ability manager
        List<BaseAbility> allAbilities = abilityManager.GetActiveAbilities();
        
        foreach (BaseAbility ability in allAbilities)
        {
            if (ability == null) continue;
            
            // First try to get the AbilitySO from the AbilityManager's stored mapping
            AbilitySO matchingSO = abilityManager.GetAbilitySO(ability);
            
            // Fallback to searching available abilities if not found in mapping
            if (matchingSO == null)
            {
                matchingSO = FindAbilitySOForAbility(ability);
            }
            
            // Determine if this is a passive or active ability
            // Default to passive if no SO found (safer default)
            bool isPassive = matchingSO == null || matchingSO.abilityType == AbilityType.Passive;
            
            Debug.Log($"[AbilityCollector] Ability: {ability.gameObject.name}, SO: {(matchingSO != null ? matchingSO.abilityName : "null")}, Type: {(matchingSO != null ? matchingSO.abilityType.ToString() : "unknown")}, isPassive: {isPassive}");
            
            // Choose the appropriate container and prefab based on ability type
            Transform targetContainer = isPassive ? passiveAbilitiesContainer : activeAbilitiesContainer;
            GameObject prefab = isPassive ? passiveAbilityDisplayPrefab : activeAbilityDisplayPrefab;
            List<GameObject> displayList = isPassive ? spawnedPassiveAbilityDisplays : spawnedActiveAbilityDisplays;
            
            if (targetContainer == null) continue;
            
            if (prefab != null)
            {
                // Use the prefab if assigned
                GameObject displayObj = Instantiate(prefab, targetContainer);
                displayList.Add(displayObj);
                
                // Try to initialize it (if it has a CurrentAbilityDisplay component)
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
                CreateSimpleAbilityDisplay(ability, matchingSO, targetContainer, displayList);
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
        TMPro.TextMeshProUGUI nameText = displayObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (nameText != null)
        {
            string abilityName = abilitySO != null ? abilitySO.abilityName : ability.gameObject.name;
            nameText.text = $"{abilityName} Lv.{ability.GetCurrentLevel()}";
        }
        
        // Try to find and set icon
        if (abilitySO != null && abilitySO.icon != null)
        {
            UnityEngine.UI.Image iconImage = displayObj.transform.Find("Icon")?.GetComponent<UnityEngine.UI.Image>();
            if (iconImage != null)
            {
                iconImage.sprite = abilitySO.icon;
            }
        }
    }
    
    /// <summary>
    /// Creates a simple text-based ability display
    /// </summary>
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
    
    /// <summary>
    /// Clears all spawned current ability displays
    /// </summary>
    private void ClearCurrentAbilityDisplays()
    {
        // Clear passive ability displays
        foreach (GameObject display in spawnedPassiveAbilityDisplays)
        {
            if (display != null)
            {
                Destroy(display);
            }
        }
        spawnedPassiveAbilityDisplays.Clear();
        
        // Clear active ability displays
        foreach (GameObject display in spawnedActiveAbilityDisplays)
        {
            if (display != null)
            {
                Destroy(display);
            }
        }
        spawnedActiveAbilityDisplays.Clear();
    }

    /// <summary>
    /// Clears all spawned ability buttons
    /// </summary>
    private void ClearAbilityButtons()
    {
        foreach (GameObject button in spawnedButtons)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }
        spawnedButtons.Clear();
    }
    
    /// <summary>
    /// Gets the AbilityManager reference
    /// </summary>
    public AbilityManager GetAbilityManager()
    {
        return abilityManager;
    }

    /// <summary>
    /// Called by AbilityButton when player selects an ability
    /// </summary>
    public void SelectAbility(AbilitySO abilitySO)
    {
        if (abilitySO == null || abilitySO.abilityPrefab == null) return;

        // Add the ability to the player
        BaseAbility newAbility = abilityManager.AddAbility(abilitySO.abilityPrefab);
        
        if (newAbility != null)
        {
            Debug.Log("Ability selected and added: " + abilitySO.abilityName);
            PlayEffect(transform.position, false);
        }

        // Close the UI after selection
        CloseCanvas();
    }

    private void CloseCanvas()
    {
        if (uiAnimator != null)
        {
            uiAnimator.SetBool(openTrigger, false); // Trigger the "close" animation
        }

        // Stop any existing close animation coroutine
        StopAllCoroutines();
        
        // Start coroutine to handle the close sequence with delayed enemy unfreeze
        StartCoroutine(CloseSequence());
    }

    private IEnumerator CloseSequence()
    {
        // Safety timeout to prevent infinite loops
        float timeout = 2f;
        float elapsed = 0f;
        
        // Wait for animator to transition if it exists
        if (uiAnimator != null)
        {
            // Wait a frame for the animator to start transitioning
            yield return null;
            
            AnimatorStateInfo stateInfo = uiAnimator.GetCurrentAnimatorStateInfo(0);

            // Loop until the animation reaches the end (normalizedTime reaches 1) or timeout
            while (stateInfo.normalizedTime < 1f && elapsed < timeout)
            {
                yield return null;
                elapsed += Time.deltaTime;

                // Update the stateInfo in case the animation has advanced
                stateInfo = uiAnimator.GetCurrentAnimatorStateInfo(0);
            }
        }
        else
        {
            // No animator, just wait a short time
            yield return new WaitForSeconds(0.3f);
        }

        // After animation finishes, hide the UI container
        if (uiContainer != null)
        {
            uiContainer.SetActive(false); // Hide the UI container
        }
        
        // Switch back to normal camera
        if (cameraManager != null)
        {
            cameraManager.SetFrozen(false);
            cameraManager.SwitchToNormalCamera();
        }
        
        // Clear the spawned buttons after UI is hidden
        ClearAbilityButtons();
        ClearCurrentAbilityDisplays();
        ClearCurrentAttackDisplay();

        // Lock the cursor back in the center of the screen after the UI is closed
        if (cursorLock != null)
        {
            cursorLock.StopAbilitySelection();
        }
        
        // Unfreeze player controls immediately so they can react
        UnfreezePlayerControls();
        
        // Unfreeze abilities
        foreach (BaseAbility ability in frozenAbilities)
        {
            if (ability != null)
            {
                ability.SetFrozen(false);
            }
        }
        frozenAbilities.Clear();
        
        // Keep enemies frozen for a short delay to give player time to react
        if (enemyFreezeDelayAfterClose > 0)
        {
            yield return new WaitForSeconds(enemyFreezeDelayAfterClose);
        }
        
        // Now unfreeze enemies
        UnfreezeEnemies();
        
        // Resume enemy spawning
        ResumeSpawning();
        
        Debug.Log("Close sequence complete - all entities unfrozen.");
    }
    
    /// <summary>
    /// Resumes enemy spawning after the ability selection menu closes
    /// </summary>
    private void ResumeSpawning()
    {
        if (enemySpawner == null)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }
        
        if (enemySpawner != null)
        {
            // Get the wave manager to resume spawning with current wave data
            WaveManager waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager != null)
            {
                WaveData currentWave = waveManager.GetWaveData(waveManager.GetCurrentWaveIndex());
                if (currentWave != null)
                {
                    enemySpawner.StartWaveSpawning(currentWave);
                }
            }
        }
    }
    
    /// <summary>
    /// Unfreezes only player controls (movement, look, attack)
    /// </summary>
    private void UnfreezePlayerControls()
    {
        // Unfreeze the player
        if (playerMovement != null)
        {
            playerMovement.SetFrozen(false);
        }

        // Unfreeze the mouse look
        if (mouseLookAt != null)
        {
            mouseLookAt.SetFrozen(false);
        }

        // Unfreeze the attack manager
        if (attackManager != null)
        {
            attackManager.SetFrozen(false);
        }
    }
    
    /// <summary>
    /// Unfreezes all frozen enemies
    /// </summary>
    private void UnfreezeEnemies()
    {
        foreach (AppleEnemy enemy in frozenEnemies)
        {
            if (enemy != null)
            {
                enemy.SetFrozen(false);
            }
        }
        frozenEnemies.Clear();
    }

    private void Awake()
    {
        abilityManager = GetComponent<AbilityManager>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!autoCollect) return;

        AbilityDrop drop = other.GetComponent<AbilityDrop>();
        if (drop != null && drop.IsGrounded() && !drop.IsCollected())
        {
            TryCollectDrop(drop);
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        // Also check on stay - in case the drop becomes grounded while player is already overlapping
        if (!autoCollect) return;

        AbilityDrop drop = other.GetComponent<AbilityDrop>();
        if (drop != null && drop.IsGrounded() && !drop.IsCollected())
        {
            TryCollectDrop(drop);
        }
    }

    public bool TryCollectDrop(AbilityDrop drop)
    {
        if (drop == null || drop.IsCollected() || !drop.IsGrounded())
        {
            return false;
        }

        // Collect the drop (remove it from the world)
        drop.Collect();

        // Show the ability selection UI - don't add any ability yet
        ShowAbilitySelectionUI();

        return true;
    }

    /// <summary>
    /// Shows the ability selection UI for the player to choose an ability
    /// </summary>
    private void ShowAbilitySelectionUI()
    {
        if (uiAnimator == null) return;

        // Show the UI container before the animation starts
        if (uiContainer != null)
        {
            uiContainer.SetActive(true); // Unhide the UI container
        }

        // Populate the ability buttons before showing the UI
        PopulateAbilityButtons();
        
        // Populate the current abilities display
        PopulateCurrentAbilities();
        
        // Populate the current attack display
        PopulateCurrentAttack();

        uiAnimator.SetBool(openTrigger, true); // Trigger the "open" animation

        // Unlock the cursor and start the ability selection phase
        if (cursorLock != null)
        {
            cursorLock.StartAbilitySelection();
        }

        // Freeze all entities immediately
        FreezeAllEntities();
        
        // Stop enemy spawning while menu is open
        if (enemySpawner != null)
        {
            enemySpawner.StopSpawning();
        }
        else
        {
            // Try to find enemy spawner if not assigned
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
            if (enemySpawner != null)
            {
                enemySpawner.StopSpawning();
            }
        }
    }

    /// <summary>
    /// Freezes the player, mouse look, camera, abilities, and all enemies in the scene
    /// </summary>
    private void FreezeAllEntities()
    {
        // Freeze the player movement
        if (playerMovement != null)
        {
            playerMovement.SetFrozen(true);
        }
        else
        {
            // Try to find player movement if not assigned
            playerMovement = FindFirstObjectByType<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.SetFrozen(true);
            }
        }

        // Freeze the mouse look
        if (mouseLookAt != null)
        {
            mouseLookAt.SetFrozen(true);
        }
        else
        {
            // Try to find mouse look if not assigned
            mouseLookAt = FindFirstObjectByType<MouseLookAt>();
            if (mouseLookAt != null)
            {
                mouseLookAt.SetFrozen(true);
            }
        }

        // Freeze the camera manager and switch to pause camera
        if (cameraManager != null)
        {
            cameraManager.SetFrozen(true);
            cameraManager.SwitchToPauseCamera(); // Switch to the spinning pause camera
        }
        else
        {
            // Try to find camera manager if not assigned
            cameraManager = FindFirstObjectByType<CameraManager>();
            if (cameraManager != null)
            {
                cameraManager.SetFrozen(true);
                cameraManager.SwitchToPauseCamera(); // Switch to the spinning pause camera
            }
        }

        // Freeze the attack manager (blocks mouse clicks/attacks)
        if (attackManager != null)
        {
            attackManager.SetFrozen(true);
        }
        else
        {
            // Try to find attack manager if not assigned
            attackManager = FindFirstObjectByType<AttackManager>();
            if (attackManager != null)
            {
                attackManager.SetFrozen(true);
            }
        }

        // Freeze all enemies
        frozenEnemies.Clear();
        AppleEnemy[] enemies = FindObjectsByType<AppleEnemy>(FindObjectsSortMode.None);
        foreach (AppleEnemy enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.SetFrozen(true);
                frozenEnemies.Add(enemy);
            }
        }

        // Freeze all active abilities
        frozenAbilities.Clear();
        List<BaseAbility> activeAbilities = abilityManager.GetActiveAbilities();
        foreach (BaseAbility ability in activeAbilities)
        {
            if (ability != null)
            {
                ability.SetFrozen(true);
                frozenAbilities.Add(ability);
            }
        }

        Debug.Log($"Frozen {frozenEnemies.Count} enemies, {frozenAbilities.Count} abilities, player, mouse look, and camera.");
    }

    /// <summary>
    /// Unfreezes the player, mouse look, camera, abilities, and all previously frozen enemies (immediate, no delay)
    /// Used for emergency/immediate unfreeze scenarios
    /// </summary>
    private void UnfreezeAllEntities()
    {
        // Unfreeze the player
        if (playerMovement != null)
        {
            playerMovement.SetFrozen(false);
        }

        // Unfreeze the mouse look
        if (mouseLookAt != null)
        {
            mouseLookAt.SetFrozen(false);
        }

        // Unfreeze the camera manager and switch back to normal camera
        if (cameraManager != null)
        {
            cameraManager.SetFrozen(false);
            cameraManager.SwitchToNormalCamera(); // Switch back to normal camera
        }

        // Unfreeze the attack manager
        if (attackManager != null)
        {
            attackManager.SetFrozen(false);
        }

        // Unfreeze all enemies that were frozen
        foreach (AppleEnemy enemy in frozenEnemies)
        {
            if (enemy != null)
            {
                enemy.SetFrozen(false);
            }
        }
        frozenEnemies.Clear();

        // Unfreeze all abilities that were frozen
        foreach (BaseAbility ability in frozenAbilities)
        {
            if (ability != null)
            {
                ability.SetFrozen(false);
            }
        }
        frozenAbilities.Clear();

        Debug.Log("All entities unfrozen (immediate).");
    }

    private void PlayEffect(Vector3 position, bool isUpgrade)
    {
        AudioClip sound = isUpgrade ? upgradeSound : collectSound;
        if (sound != null && audioSource != null)
        {
            audioSource.PlayOneShot(sound);
        }

        GameObject effectPrefab = isUpgrade ? upgradeEffectPrefab : collectionEffectPrefab;
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    // Add the ability to player (kept for backwards compatibility)
    public void AddAbilityToPlayer(AbilitySO abilitySO)
    {
        if (abilitySO == null || abilitySO.abilityPrefab == null) return;

        // Add the ability to the AbilityManager
        abilityManager.AddAbility(abilitySO.abilityPrefab);
        Debug.Log("Ability added: " + abilitySO.abilityPrefab.name);
    }

    /// <summary>
    /// Gets the list of available abilities
    /// </summary>
    public List<AbilitySO> GetAvailableAbilities()
    {
        return availableAbilities;
    }

    /// <summary>
    /// Adds an ability to the available abilities list
    /// </summary>
    public void AddAvailableAbility(AbilitySO ability)
    {
        if (ability != null && !availableAbilities.Contains(ability))
        {
            availableAbilities.Add(ability);
        }
    }

    /// <summary>
    /// Removes an ability from the available abilities list
    /// </summary>
    public void RemoveAvailableAbility(AbilitySO ability)
    {
        if (ability != null && availableAbilities.Contains(ability))
        {
            availableAbilities.Remove(ability);
        }
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
        TMPro.TextMeshProUGUI nameText = displayObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = $"{attack.attackName} Lv.{attack.GetCurrentLevel()}";
        }
        
        // Try to find and set icon
        if (attack.attackIcon != null)
        {
            UnityEngine.UI.Image iconImage = displayObj.transform.Find("Icon")?.GetComponent<UnityEngine.UI.Image>();
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
        
        TMPro.TextMeshProUGUI text = spawnedCurrentAttackDisplay.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = $"{attack.attackName} Lv.{attack.GetCurrentLevel()}";
        text.fontSize = 14;
        text.alignment = TMPro.TextAlignmentOptions.Left;
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
