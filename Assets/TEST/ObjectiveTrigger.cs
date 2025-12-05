using UnityEngine;

public class ObjectiveTrigger : MonoBehaviour
{
    [Header("Interaction Settings")]
    public string interactionMessage = "Press E to interact";

    [Header("Optional Animation Settings")]
    public Animator animator;               // Optional: for Animator controller
    public string animationTriggerName;     // Optional: trigger name for Animator
    public Animation legacyAnimation;       // Optional: legacy Animation component
    public AnimationClip animationClip;     // Optional: clip to play on interaction

    private int myObjectiveIndex;
    private bool playerInRange = false;
    private bool completed = false;

    // Called by ObjectiveManager when this becomes the active objective
    public void AssignObjective(int index)
    {
        myObjectiveIndex = index;
        completed = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (completed) return;

        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            InteractionPromptUI.Instance.ShowMessage(interactionMessage);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (completed) return;

        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            InteractionPromptUI.Instance.HideMessage();
        }
    }

    private void Update()
    {
        if (playerInRange && !completed && Input.GetKeyDown(KeyCode.E))
        {
            Complete();
        }
    }

    private void Complete()
    {
        completed = true;
        playerInRange = false;

        // Hide interaction prompt
        InteractionPromptUI.Instance.HideMessage();

        // Play Animator trigger if assigned
        if (animator != null && !string.IsNullOrEmpty(animationTriggerName))
        {
            animator.SetTrigger(animationTriggerName);
        }

        // Play legacy Animation clip if assigned
        if (legacyAnimation != null && animationClip != null)
        {
            legacyAnimation.Stop();
            legacyAnimation.clip = animationClip;
            legacyAnimation.Play();
        }

        // Notify ObjectiveManager
        ObjectiveManager.Instance.CompleteObjective(myObjectiveIndex);

        // Optional: disable the object to prevent re-triggering
        // gameObject.SetActive(false);
    }
}
