using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(AbilityManager))]
public class AbilityCollector : MonoBehaviour
{
    [Header("Collection Settings")]
    [SerializeField] private bool autoCollect = true;

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

    // Reference to CursorLock script
    [SerializeField] private CursorLock cursorLock; // Drag and drop the CursorLock script here


    private AbilityManager abilityManager;

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

    private void CloseCanvas()
    {
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

        // After animation finishes, hide the UI container and restore game time and cursor
        if (uiContainer != null)
        {
            uiContainer.SetActive(false); // Hide the UI container
        }

        Time.timeScale = 1; // Resume time
        Cursor.visible = false; // Hide the cursor
        Cursor.lockState = CursorLockMode.Locked; // Lock the cursor back in the center of the screen

        // Lock the cursor again after closing the ability selection UI
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

        AbilitySO abilitySO = drop.GetAbilitySO();
        if (abilitySO == null || abilitySO.abilityPrefab == null)
        {
            drop.Collect();
            return false;
        }

        AbilityDrop.DropType dropType = drop.GetDropType();
        BaseAbility existingAbility = abilityManager.GetAbility(abilitySO.abilityPrefab);

        bool abilityCollected = false;

        if (dropType == AbilityDrop.DropType.New)
        {
            BaseAbility newAbility = abilityManager.AddAbility(abilitySO.abilityPrefab);
            if (newAbility != null)
            {
                PlayEffect(drop.transform.position, false);
                drop.Collect();
                abilityCollected = true;
            }
        }
        else if (dropType == AbilityDrop.DropType.Upgrade && existingAbility != null)
        {
            existingAbility.LevelUp();
            PlayEffect(drop.transform.position, true);
            drop.Collect();
            abilityCollected = true;
        }
        else if (dropType == AbilityDrop.DropType.Duration && existingAbility != null)
        {
            existingAbility.LevelUp(); // This extends duration even at max level
            PlayEffect(drop.transform.position, false);
            drop.Collect();
            abilityCollected = true;
        }

        // If an ability is collected, trigger the open animation and start the coroutine
        if (abilityCollected && uiAnimator != null)
        {
            // Show the UI container before the animation starts
            if (uiContainer != null)
            {
                uiContainer.SetActive(true); // Unhide the UI container
            }

            uiAnimator.SetBool(openTrigger, true); // Trigger the "open" animation

            // Unlock the cursor and start the ability selection phase
            if (cursorLock != null)
            {
                cursorLock.StartAbilitySelection();
            }

            StartCoroutine(WaitForAnimationToFinish()); // Start coroutine to monitor animation and stop time
        }

        drop.Collect(); // In case we don’t exit early
        return abilityCollected;
    }

    private IEnumerator WaitForAnimationToFinish()
    {
        // Log to confirm coroutine is being started
        Debug.Log("Coroutine started.");

        // Wait until the current animation finishes
        AnimatorStateInfo stateInfo = uiAnimator.GetCurrentAnimatorStateInfo(0);

        // Loop until the animation reaches the end (normalizedTime reaches 1)
        while (stateInfo.normalizedTime < 1f)
        {
            yield return null;

            // Update the stateInfo in case the animation has advanced
            stateInfo = uiAnimator.GetCurrentAnimatorStateInfo(0);
        }

        // Log to check if animation is finished
        Debug.Log("Animation finished, stopping time and showing cursor.");

        // Once the animation finishes, stop time and unhide the cursor
        Cursor.visible = true; // Ensure cursor is visible
        Cursor.lockState = CursorLockMode.None; // Unlock the cursor

        // Optionally, for debugging, you can add a log to confirm the steps
        Debug.Log("Cursor visibility: " + Cursor.visible + ", Cursor lock state: " + Cursor.lockState);
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
}
