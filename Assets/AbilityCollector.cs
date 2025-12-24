using UnityEngine;

/// <summary>
/// Collects ability drops and adds them to the AbilityManager
/// Attach this to your snake/player object
/// </summary>
[RequireComponent(typeof(AbilityManager))]
public class AbilityCollector : MonoBehaviour
{
    [Header("Collection Settings")]
    [SerializeField] private bool autoCollect = true;
    [SerializeField] private float collectionRange = 2f;
    [SerializeField] private LayerMask dropLayer = -1;
    
    [Header("Audio")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Visual Feedback")]
    [SerializeField] private bool showCollectionEffect = true;
    [SerializeField] private GameObject collectionEffectPrefab;
    
    private AbilityManager abilityManager;

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
        if (drop != null && drop.IsCracked())
        {
            TryCollectDrop(drop);
        }
    }

    /// <summary>
    /// Attempts to collect a specific drop
    /// </summary>
    public bool TryCollectDrop(AbilityDrop drop)
    {
        if (drop == null || drop.IsCollected())
        {
            return false;
        }
        
        GameObject abilityPrefab = drop.GetAbility();
        if (abilityPrefab == null)
        {
            Debug.LogWarning("Drop has no ability assigned!");
            drop.Collect(); // Still collect it to remove the drop
            return false;
        }
        
        // Add ability to the manager
        BaseAbility addedAbility = abilityManager.AddAbility(abilityPrefab);
        
        if (addedAbility != null)
        {
            OnAbilityCollected(drop, addedAbility);
            drop.Collect();
            return true;
        }
        else
        {
            Debug.LogWarning($"Failed to add ability: {abilityPrefab.name}");
            drop.Collect(); // Still collect it to remove the drop
            return false;
        }
    }

    private void OnAbilityCollected(AbilityDrop drop, BaseAbility ability)
    {
        // Play sound
        if (collectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collectSound);
        }
        
        // Spawn visual effect
        if (showCollectionEffect)
        {
            SpawnCollectionEffect(drop.transform.position);
        }
        
        // Log message
        Debug.Log($"Collected ability: {ability.GetType().Name}");
    }

    private void SpawnCollectionEffect(Vector3 position)
    {
        if (collectionEffectPrefab != null)
        {
            GameObject effect = Instantiate(collectionEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        else
        {
            // Create a simple particle effect as fallback
            CreateSimpleEffect(position);
        }
    }

    private void CreateSimpleEffect(Vector3 position)
    {
        GameObject effect = new GameObject("CollectionEffect");
        effect.transform.position = position;
        
        // Add particle system
        ParticleSystem ps = effect.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.5f;
        main.startLifetime = 0.5f;
        main.startSpeed = 2f;
        main.startSize = 0.2f;
        main.startColor = Color.yellow;
        main.maxParticles = 20;
        
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 20)
        });
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        
        ps.Play();
        Destroy(effect, 1f);
    }
}