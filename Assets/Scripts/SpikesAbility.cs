using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Places spikes on body parts that damage enemies on contact.
/// Similar to BombPlacementAbility but spikes persist and deal damage when touched.
/// </summary>
public class SpikesAbility : BaseAbility
{
    [Header("Spike Settings")]
    [SerializeField] private GameObject spikePrefab;
    [SerializeField] private float baseDamage = 15f;
    [SerializeField] private float baseDamageCooldown = 0.5f; // Cooldown per enemy before they can be hit again
    
    [Header("References")]
    [SerializeField] private SnakeBody snakeBody;
    
    private List<GameObject> activeSpikes = new List<GameObject>();
    private List<SpikeInstance> spikeInstances = new List<SpikeInstance>();
    private bool hasPlacedSpikes = false;
    
    // Custom stat names for upgrade data
    private const string STAT_SPIKE_COUNT = "spikeCount";

    private void OnEnable()
    {
        SnakeBody.OnBodyPartsInitialized += PlaceSpikes;
    }

    private void OnDisable()
    {
        SnakeBody.OnBodyPartsInitialized -= PlaceSpikes;
    }

    protected override void Awake()
    {
        isActive = true;
    }

    protected override void Update()
    {
        if (isFrozen) return;
        
        // Update spike damage cooldowns
        foreach (var spike in spikeInstances)
        {
            if (spike != null)
            {
                spike.UpdateCooldowns(Time.deltaTime);
            }
        }
    }

    private void Start()
    {
        if (snakeBody == null)
        {
            snakeBody = GetComponentInParent<SnakeBody>();
        }
        
        if (snakeBody != null && snakeBody.bodyParts != null && snakeBody.bodyParts.Count > 0)
        {
            PlaceSpikes();
        }
    }

    private void PlaceSpikes()
    {
        if (hasPlacedSpikes) return;
        
        if (snakeBody == null)
        {
            snakeBody = GetComponentInParent<SnakeBody>();
            if (snakeBody == null)
            {
                Debug.LogWarning("SpikesAbility: SnakeBody not found!");
                return;
            }
        }

        List<BodyPart> bodyParts = snakeBody.bodyParts;
        
        if (bodyParts == null || bodyParts.Count == 0)
        {
            Debug.LogWarning("SpikesAbility: Body parts list is empty!");
            return;
        }
        
        int totalParts = bodyParts.Count;

        // Need at least 5 parts to place any spikes
        if (totalParts < 5)
        {
            Debug.Log("SpikesAbility: Not enough body parts to place spikes!");
            return;
        }

        // Calculate spike placement based on level
        // Level 1: Every 5th segment
        // Level 2: Every 4th segment
        // Level 3: Every 3rd segment
        int spacing = Mathf.Max(3, 6 - currentLevel);
        int startIndex = totalParts - 4; // Start from 4th from last
        int endIndex = 2; // Stop before first 2 (tail area)

        for (int i = startIndex; i > endIndex; i -= spacing)
        {
            BodyPart part = bodyParts[i];
            
            GameObject spike;
            if (spikePrefab != null)
            {
                spike = Instantiate(spikePrefab, part.transform.position, Quaternion.identity);
            }
            else
            {
                // Create a simple spike visual if no prefab assigned
                spike = CreateDefaultSpikeVisual();
            }
            
            // Parent to body part so it follows
            spike.transform.SetParent(part.transform);
            spike.transform.localPosition = Vector3.zero;
            
            // Add spike instance component for damage handling
            SpikeInstance spikeInstance = spike.AddComponent<SpikeInstance>();
            spikeInstance.Initialize(this, GetSpikeDamage(), GetDamageCooldown());
            
            activeSpikes.Add(spike);
            spikeInstances.Add(spikeInstance);
        }

        hasPlacedSpikes = true;
        Debug.Log($"SpikesAbility: Placed {activeSpikes.Count} spikes at level {currentLevel}!");
    }
    
    private GameObject CreateDefaultSpikeVisual()
    {
        GameObject spike = new GameObject("Spike");
        
        // Create multiple spike cones around the body part
        for (int i = 0; i < 4; i++)
        {
            GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cone.name = $"SpikeCone_{i}";
            cone.transform.SetParent(spike.transform);
            
            // Scale to look like a spike
            cone.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
            
            // Position around the body part
            float angle = i * 90f * Mathf.Deg2Rad;
            cone.transform.localPosition = new Vector3(
                Mathf.Cos(angle) * 0.4f,
                0,
                Mathf.Sin(angle) * 0.4f
            );
            
            // Rotate to point outward
            cone.transform.LookAt(spike.transform.position + cone.transform.localPosition * 2);
            cone.transform.Rotate(90, 0, 0);
            
            // Set up material
            Renderer renderer = cone.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Gray metallic
                mat.SetFloat("_Metallic", 0.8f);
                mat.SetFloat("_Glossiness", 0.6f);
                renderer.material = mat;
            }
            
            // Remove default collider - we handle collision detection manually
            Collider col = cone.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
        }
        
        // Add a sphere collider for detection
        SphereCollider sphereCol = spike.AddComponent<SphereCollider>();
        sphereCol.radius = 0.6f;
        sphereCol.isTrigger = true;
        
        return spike;
    }

    private float GetSpikeDamage()
    {
        float damage;
        if (upgradeData != null)
        {
            damage = GetDamage();
        }
        else
        {
            damage = baseDamage + (currentLevel - 1) * 5f;
        }
        
        float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetDamageMultiplier() : 1f;
        return damage * multiplier;
    }
    
    private float GetDamageCooldown()
    {
        if (upgradeData != null)
        {
            float cooldown = GetCooldown();
            return cooldown > 0 ? cooldown : baseDamageCooldown;
        }
        return Mathf.Max(0.2f, baseDamageCooldown - (currentLevel - 1) * 0.1f);
    }

    public override bool LevelUp()
    {
        if (currentLevel >= maxLevel)
        {
            Debug.Log($"{GetType().Name} already at max level!");
            return false;
        }
        
        currentLevel++;
        Debug.Log($"{GetType().Name} leveled up to {currentLevel}!");
        
        // Clear existing spikes and reposition
        ClearSpikes();
        hasPlacedSpikes = false;
        PlaceSpikes();
        
        return true;
    }

    public void ClearSpikes()
    {
        foreach (GameObject spike in activeSpikes)
        {
            if (spike != null)
            {
                Destroy(spike);
            }
        }
        activeSpikes.Clear();
        spikeInstances.Clear();
    }

    private void OnDestroy()
    {
        ClearSpikes();
    }
}

/// <summary>
/// Component attached to each spike to handle damage dealing
/// </summary>
public class SpikeInstance : MonoBehaviour
{
    private SpikesAbility parentAbility;
    private float damage;
    private float damageCooldown;
    private Dictionary<AppleEnemy, float> enemyCooldowns = new Dictionary<AppleEnemy, float>();
    private List<AppleEnemy> cooldownsToRemove = new List<AppleEnemy>();
    
    public void Initialize(SpikesAbility ability, float dmg, float cooldown)
    {
        parentAbility = ability;
        damage = dmg;
        damageCooldown = cooldown;
    }
    
    public void UpdateCooldowns(float deltaTime)
    {
        cooldownsToRemove.Clear();
        
        List<AppleEnemy> keys = new List<AppleEnemy>(enemyCooldowns.Keys);
        foreach (AppleEnemy enemy in keys)
        {
            if (enemy == null)
            {
                cooldownsToRemove.Add(enemy);
                continue;
            }
            
            enemyCooldowns[enemy] -= deltaTime;
            
            if (enemyCooldowns[enemy] <= 0)
            {
                cooldownsToRemove.Add(enemy);
            }
        }
        
        foreach (AppleEnemy enemy in cooldownsToRemove)
        {
            enemyCooldowns.Remove(enemy);
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        AppleEnemy enemy = other.GetComponentInParent<AppleEnemy>();
        if (enemy == null || enemy.IsFrozen()) return;
        
        // Check if enemy is on cooldown
        if (enemyCooldowns.ContainsKey(enemy) && enemyCooldowns[enemy] > 0)
            return;
        
        // Deal damage
        enemy.TakeDamage(damage);
        
        // Set cooldown
        enemyCooldowns[enemy] = damageCooldown;
        
        Debug.Log($"Spike hit {enemy.name} for {damage} damage!");
    }
}