using UnityEngine;

public class ObjectiveTrigger : MonoBehaviour
{
    [Header("Objective Settings")]
    public int myObjectiveIndex;           // Index in ObjectiveManager
    public string objectiveName = "Objective"; // Name shown in top-left UI

    [Header("Interaction Settings")]
    public string interactionMessage = "Press E to interact";

    [Header("Optional Animation Settings")]
    public Animator animator;
    public string animationTriggerName;
    public Animation legacyAnimation;
    public AnimationClip animationClip;

    private bool playerInRange = false;
    private bool completed = false;

    // Called by ObjectiveManager to initialize/reset objective
    public void AssignObjective(int index)
    {
        myObjectiveIndex = index;
        completed = false;
    }

    // Checks if this objective is currently active
    private bool IsActiveObjective()
    {
        return ObjectiveManager.Instance.CurrentObjectiveIndex == myObjectiveIndex && !completed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!IsActiveObjective()) return;

        playerInRange = true;
        InteractionPromptUI.Instance.ShowMessage(interactionMessage);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!IsActiveObjective()) return;

        playerInRange = false;
        InteractionPromptUI.Instance.HideMessage();
    }

    private void Update()
    {
        if (!playerInRange || !IsActiveObjective()) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            CompleteObjective();
        }
    }

    private void CompleteObjective()
    {
        completed = true;
        playerInRange = false;
        InteractionPromptUI.Instance.HideMessage();

        // Play Animator trigger if assigned
        if (animator != null && !string.IsNullOrEmpty(animationTriggerName))
            animator.SetTrigger(animationTriggerName);

        // Play legacy Animation clip if assigned
        if (legacyAnimation != null && animationClip != null)
        {
            legacyAnimation.Stop();
            legacyAnimation.clip = animationClip;
            legacyAnimation.Play();
        }

        // Notify ObjectiveManager
        ObjectiveManager.Instance.CompleteObjective(myObjectiveIndex);
    }
}
