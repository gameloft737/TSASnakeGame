using UnityEngine;

/// <summary>
/// Represents a single ability drop that falls, cracks open, and can be collected
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class AbilityDrop : MonoBehaviour
{
    [Header("Drop Settings")]
    [SerializeField] private float lifetimeAfterCrack = 10f;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string crackAnimationTrigger = "Crack";
    [SerializeField] private string fadeAnimationTrigger = "Fade";
    
    [Header("Visual")]
    [SerializeField] private GameObject visualObject;
    
    private GameObject abilityPrefab;
    private Rigidbody rb;
    private bool hasLanded = false;
    private bool isCracked = false;
    private bool isCollected = false;
    private float lifetimeTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Find visual object if not assigned
        if (visualObject == null)
        {
            visualObject = transform.Find("DropVisual")?.gameObject;
        }
        
        // Try to find animator
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
    }

    private void Update()
    {
        // Handle lifetime after cracking
        if (isCracked && !isCollected)
        {
            lifetimeTimer += Time.deltaTime;
            
            if (lifetimeTimer >= lifetimeAfterCrack)
            {
                StartFade();
            }
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Land when we collide with anything
        if (!hasLanded)
        {
            OnLanded();
        }
    }

    /// <summary>
    /// Sets the ability that this drop will give
    /// </summary>
    public void SetAbility(GameObject ability)
    {
        abilityPrefab = ability;
    }

    /// <summary>
    /// Gets the ability prefab from this drop
    /// </summary>
    public GameObject GetAbility()
    {
        return abilityPrefab;
    }
    
    /// <summary>
    /// Returns true if the drop has cracked open
    /// </summary>
    public bool IsCracked()
    {
        return isCracked;
    }
    
    /// <summary>
    /// Returns true if the drop has been collected
    /// </summary>
    public bool IsCollected()
    {
        return isCollected;
    }

    /// <summary>
    /// Called when the drop is collected
    /// </summary>
    public void Collect()
    {
        if (isCollected) return;
        
        isCollected = true;
        StartFade();
    }

    private void OnLanded()
    {
        hasLanded = true;
        
        // Stop physics movement
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        // Trigger crack animation
        Crack();
    }

    private void Crack()
    {
        isCracked = true;
        
        // Play crack animation
        if (animator != null)
        {
            animator.SetTrigger(crackAnimationTrigger);
        }
        else
        {
            // Fallback visual effect if no animator
            if (visualObject != null)
            {
                visualObject.transform.localScale *= 1.2f;
            }
        }
    }

    private void StartFade()
    {
        // Play fade animation
        if (animator != null)
        {
            animator.SetTrigger(fadeAnimationTrigger);
            // Destroy after animation completes (assuming 2 second fade)
            Destroy(gameObject, 2f);
        }
        else
        {
            // Fallback fade
            StartCoroutine(FadeOut());
        }
    }

    private System.Collections.IEnumerator FadeOut()
    {
        float fadeDuration = 1f;
        float elapsed = 0f;
        
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / fadeDuration);
            
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    Color color = renderer.material.color;
                    color.a = alpha;
                    renderer.material.color = color;
                }
            }
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
}