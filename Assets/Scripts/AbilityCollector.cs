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
    [SerializeField] private List<AbilitySO> availableAbilities = new List<AbilitySO>(); // List of abilities player can choose from

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

    // Reference to CursorLock script
    [SerializeField] private CursorLock cursorLock; // Drag and drop the CursorLock script here

    // New delay variable for stopping time after the menu opens
    [Header("Time Control Settings")]
    [SerializeField] private float timeStopDelay = 0.5f; // Delay in seconds before stopping time

    private AbilityManager abilityManager;
    private List<GameObject> spawnedButtons = new List<GameObject>(); // Track spawned buttons for cleanup

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
    /// Populates the ability button container with buttons for each available ability
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

        // Create a button for each available ability
        foreach (AbilitySO ability in availableAbilities)
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
        // Resume the game time right before the closing animation starts
        Time.timeScale = 1;

        if (uiAnimator != null)
        {
            uiAnimator.SetBool(openTrigger, false); // Trigger the "close" animation
        }

        // Start coroutine to wait for animation to finish before hiding the UI container
        StartCoroutine(WaitForCloseAnimation());
    }

    private IEnumerator WaitForCloseAnimation()
    {
        // Wait until the current close animation finishes
        AnimatorStateInfo stateInfo = uiAnimator.GetCurrentAnimatorStateInfo(0);

        // Loop until the animation reaches the end (normalizedTime reaches 1)
        while (stateInfo.normalizedTime < 1f)
        {
            yield return null;

            // Update the stateInfo in case the animation has advanced
            stateInfo = uiAnimator.GetCurrentAnimatorStateInfo(0);
        }

        // After animation finishes, hide the UI container
        if (uiContainer != null)
        {
            uiContainer.SetActive(false); // Hide the UI container
        }

        // Lock the cursor back in the center of the screen after the UI is closed
        if (cursorLock != null)
        {
            cursorLock.StopAbilitySelection();
        }
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

        uiAnimator.SetBool(openTrigger, true); // Trigger the "open" animation

        // Unlock the cursor and start the ability selection phase
        if (cursorLock != null)
        {
            cursorLock.StartAbilitySelection();
        }

        // Start the coroutine to stop time after a delay
        StartCoroutine(WaitForAnimationToFinish());
    }

    private IEnumerator WaitForAnimationToFinish()
    {
        // Log to confirm coroutine is being started
        Debug.Log("Coroutine started.");

        // Wait for the specified delay before stopping time
        yield return new WaitForSeconds(timeStopDelay); // Wait for the user-defined delay

        // Stop the game time
        Time.timeScale = 0;

        // Log to check if animation is finished
        Debug.Log("Time stopped after delay.");
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
}
