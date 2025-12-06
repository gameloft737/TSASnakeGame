using UnityEngine;
using System.Collections;

public class ObjectiveTrigger : MonoBehaviour
{
    [Header("Optional Visual Changes")]
    public Light[] lightsToDisable;              // Lights to turn off
    public Light[] lightsToEnable;          // Lights to turn ON when completed
    public float enabledLightIntensity = 1f; // Optional intensity control
    public Renderer[] emissionRenderers;         // Renderers with emission you want to change
    public Color emissionOffColor = Color.black; // Emission color when off

    [Header("Emission Settings")]
    [Range(0f, 5f)]
    public float emissionOffStrength = 1f; // Multiplier applied to emissionOffColor

    [Header("Subtitles")]
    [TextArea(1, 3)]
    public string completionSubtitle;
    public float subtitleDuration = 3f;

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

    [Header("Sound Settings")]
    public AudioSource audioSource;        // AudioSource on this object
    public AudioClip soundBefore;          // Plays while objective is active
    public AudioClip soundAfter;           // Plays when objective completes
    public bool loopBeforeSound = true;    // Should the "before" sound loop?
    public bool loopAfterSound = false;    // Should the "after" sound loop?

    [Header("Additional One-Shot Completion Sound")]
    public AudioClip completionOneShotSound;   // Plays once on completion (different from the 'after' sound)
    public float completionOneShotVolume = 1f; // Optional volume control

    [Header("Scene Transition")]
    public string sceneToLoad;         // Leave empty to disable scene loading
    public float sceneLoadDelay = 0f;  // Delay before loading the scene





    private bool playerInRange = false;
    private bool completed = false;


    private IEnumerator LoadSceneAfterDelay()
    {
        if (sceneLoadDelay > 0f)
            yield return new WaitForSeconds(sceneLoadDelay);

        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
    }

    // Called by ObjectiveManager to initialize/reset objective
    public void AssignObjective(int index)
    {
        myObjectiveIndex = index;
        completed = false;

            // Handle sound for new active objective
    if (audioSource != null)
    {
        if (soundBefore != null)
        {
            audioSource.clip = soundBefore;
            audioSource.loop = loopBeforeSound;
            audioSource.Play();
        }
    }

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

    // Show subtitle on completion
    if (!string.IsNullOrEmpty(completionSubtitle))
    {
        SubtitleUI.Instance.ShowSubtitle(completionSubtitle, subtitleDuration);
    }


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

        // Turn off lights
    if (lightsToDisable != null)
    {
        foreach (Light l in lightsToDisable)
        {
            if (l != null)
                l.intensity = 0f;
        }
    }

    // Turn ON lights
    if (lightsToEnable != null)
    {
        foreach (Light l in lightsToEnable)
        {
            if (l != null)
                l.intensity = enabledLightIntensity;
        }
    }


    // Change emission color with strength multiplier
    if (emissionRenderers != null)
    {
        foreach (Renderer r in emissionRenderers)
        {
            if (r != null)
            {
                foreach (Material mat in r.materials)
                {
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        Color finalEmission = emissionOffColor * emissionOffStrength;
                        mat.SetColor("_EmissionColor", finalEmission);

                        // Ensure emission keyword is enabled
                        mat.EnableKeyword("_EMISSION");
                    }
                }
            }
        }
    }


    // Handle sound switching
    if (audioSource != null)
    {
        // Stop before sound
        audioSource.Stop();

        // Play the additional ONE-SHOT completion sound first
        if (completionOneShotSound != null)
        {
            audioSource.PlayOneShot(completionOneShotSound, completionOneShotVolume);
        }

        // Handle AFTER sound (looping or one-shot)
        if (soundAfter != null)
        {
            if (loopAfterSound)
            {
                // Loop the after sound
                audioSource.clip = soundAfter;
                audioSource.loop = true;
                audioSource.Play();
            }
            else
            {
                // Play the after sound once
                audioSource.loop = false;
                audioSource.PlayOneShot(soundAfter);
            }
        }
    }

    // Load scene if assigned
    if (!string.IsNullOrEmpty(sceneToLoad))
    {
        StartCoroutine(LoadSceneAfterDelay());
    }

    }//end of complete objective
}
