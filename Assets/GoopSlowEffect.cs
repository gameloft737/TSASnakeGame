using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Slows down an AppleEnemy when attached as a child
/// </summary>
public class GoopSlowEffect : MonoBehaviour
{
    [Header("Slow Settings")]
    [SerializeField] private float speedMultiplier = 0.5f; // 50% speed
    [SerializeField] private float effectDuration = 3f; // Lasts 3 seconds after leaving goop
    [SerializeField] private float contactRefreshRate = 0.2f; // How often to refresh while in contact
    
    [Header("Visual Feedback")]
    [SerializeField] private Color goopColor = new Color(0.5f, 0.8f, 0.3f, 1f);
    [SerializeField] private float tintStrength = 0.6f;
    
    private NavMeshAgent agent;
    private float originalSpeed;
    private float effectTimer;
    private bool isActive = false;
    private float lastRefreshTime;
    private List<Renderer> renderersList = new List<Renderer>();
    private List<Material> originalMaterials = new List<Material>();
    private List<Material> tintedMaterials = new List<Material>();
    
    private void Awake()
    {
        Debug.Log("========== GOOP SLOW EFFECT AWAKE CALLED ==========");
        Debug.Log("GoopSlowEffect: Starting on " + (transform.parent != null ? transform.parent.name : "NO PARENT"));
        
        // Get the NavMeshAgent from parent (AppleEnemy)
        agent = GetComponentInParent<NavMeshAgent>();
        
        if (agent == null)
        {
            Debug.LogWarning("GoopSlowEffect: No NavMeshAgent found on parent!");
            Destroy(gameObject);
            return;
        }
        
        Debug.Log("GoopSlowEffect: Found NavMeshAgent with speed: " + agent.speed);
        
        // Store original speed
        originalSpeed = agent.speed;
        
        // Apply slow effect
        ApplySlow();
        
        // Setup visual tint
        SetupColorTint();
        
        effectTimer = effectDuration;
        lastRefreshTime = Time.time;
    }
    
    private void Start()
    {
        Debug.Log("========== GOOP SLOW EFFECT START CALLED ==========");
    }
    
    private void Update()
    {
        if (!isActive) return;
        
        // Count down the timer
        effectTimer -= Time.deltaTime;
        
        if (effectTimer <= 0f)
        {
            Debug.Log("GoopSlowEffect: Effect timer expired, removing slow");
            RemoveSlow();
        }
    }
    
    private void ApplySlow()
    {
        if (agent != null && !isActive)
        {
            float newSpeed = originalSpeed * speedMultiplier;
            agent.speed = newSpeed;
            isActive = true;
            Debug.Log($"GoopSlowEffect: Applied slow. Original speed: {originalSpeed}, New speed: {newSpeed}");
        }
    }
    
    private void RemoveSlow()
    {
        if (agent != null && isActive)
        {
            agent.speed = originalSpeed;
            isActive = false;
            Debug.Log("GoopSlowEffect: Restored original speed: " + originalSpeed);
        }
        
        // Restore original materials
        RestoreOriginalMaterials();
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Refresh the effect duration (called when particle hits again)
    /// </summary>
    public void RefreshEffect()
    {
        // Only refresh if enough time has passed to prevent spam
        if (Time.time - lastRefreshTime >= contactRefreshRate)
        {
            effectTimer = effectDuration;
            lastRefreshTime = Time.time;
            Debug.Log("GoopSlowEffect: Effect refreshed");
            
            // Reapply slow in case it was removed
            if (!isActive)
            {
                ApplySlow();
            }
        }
    }
    
    private void SetupColorTint()
    {
        // Try multiple ways to find renderers
        
        // Method 1: Get renderers from parent
        Renderer[] renderersFromParent = GetComponentsInParent<Renderer>(true);
        Debug.Log($"GoopSlowEffect: Found {renderersFromParent.Length} renderers from parent");
        
        // Method 2: Get renderers from parent's children (including parent itself)
        Transform parent = transform.parent;
        if (parent != null)
        {
            Renderer[] allRenderers = parent.GetComponentsInChildren<Renderer>(true);
            Debug.Log($"GoopSlowEffect: Found {allRenderers.Length} renderers in parent's children");
            
            foreach (Renderer renderer in allRenderers)
            {
                if (renderer != null)
                {
                    Debug.Log($"  - Renderer on: {renderer.gameObject.name}, Material: {(renderer.material != null ? renderer.material.name : "NULL")}");
                    
                    if (renderer.material != null)
                    {
                        // Store original material
                        Material originalMat = renderer.material;
                        originalMaterials.Add(originalMat);
                        
                        // Create a new material instance for tinting
                        Material tintedMat = new Material(originalMat);
                        
                        Color originalColor = originalMat.color;
                        Color newColor = Color.Lerp(originalColor, goopColor, tintStrength);
                        tintedMat.color = newColor;
                        
                        // Apply the tinted material
                        renderer.material = tintedMat;
                        tintedMaterials.Add(tintedMat);
                        renderersList.Add(renderer);
                        
                        Debug.Log($"GoopSlowEffect: SUCCESS! Tinted '{renderer.name}' from {originalColor} to {newColor}");
                    }
                    else
                    {
                        Debug.LogWarning($"GoopSlowEffect: Renderer on '{renderer.gameObject.name}' has NULL material!");
                    }
                }
            }
        }
        
        if (renderersList.Count == 0)
        {
            Debug.LogWarning("GoopSlowEffect: FAILED to tint any renderers!");
        }
        else
        {
            Debug.Log($"GoopSlowEffect: Successfully tinted {renderersList.Count} renderers");
        }
    }
    
    private void RestoreOriginalMaterials()
    {
        Debug.Log($"GoopSlowEffect: Restoring {renderersList.Count} materials");
        
        for (int i = 0; i < renderersList.Count; i++)
        {
            if (renderersList[i] != null && i < originalMaterials.Count)
            {
                renderersList[i].material = originalMaterials[i];
            }
        }
        
        // Clean up tinted materials
        foreach (Material mat in tintedMaterials)
        {
            if (mat != null)
            {
                Destroy(mat);
            }
        }
    }
    
    private void OnDestroy()
    {
        Debug.Log("GoopSlowEffect: OnDestroy called");
        
        // Make sure to restore speed when destroyed
        if (agent != null && isActive)
        {
            agent.speed = originalSpeed;
        }
        
        // Clean up any remaining materials
        RestoreOriginalMaterials();
    }
}