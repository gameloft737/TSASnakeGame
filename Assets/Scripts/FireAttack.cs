using UnityEngine;
using System.Collections.Generic;

public class FireAttack : Attack
{
    [Header("Fire Settings")]
    [SerializeField] private ParticleSystem fireParticles;
    [SerializeField] private FireParticleDamage particleDamage;
    
    // Backup reference to ensure we never lose the main fire particles
    private ParticleSystem mainFireParticlesBackup;
    
    [Header("Animation")]
    [SerializeField] private string animationBoolName = "Fire";
    [SerializeField] private Animator animator;
    
    [Header("Body Fire Evolution")]
    [Tooltip("Prefab containing the fire particle system to spawn on body parts. Should have BodyFireEmitter and FireParticleDamage components.")]
    [SerializeField] private GameObject bodyFirePrefab;
    [Tooltip("Number of body segments to place fire emitters on (default 2). Each segment gets 2 fires - one left, one right.")]
    [SerializeField] private int bodyFireSegmentCount = 2;
    
    // Store original particle settings to scale from
    private float originalStartLifetime;
    private float originalStartSpeed;
    private bool hasStoredOriginals = false;
    
    // Evolution stats
    private float lifeStealPercent = 0f;
    private float critChance = 0f;
    private float critMultiplier = 2f;
    private float burnDamagePercent = 0f;
    private float burnDuration = 0f;
    
    // Body fire emitters for evolution
    private List<BodyFireEmitter> bodyFireEmitters = new List<BodyFireEmitter>();
    private bool hasPlacedBodyFires = false;
    private SnakeBody snakeBody;

    private void Awake()
    {
        attackType = AttackType.Continuous;
        
        // CRITICAL: Store backup reference to main fire particles immediately
        // This ensures we never lose the reference even if something tries to overwrite it
        if (fireParticles != null)
        {
            mainFireParticlesBackup = fireParticles;
            Debug.Log($"FireAttack Awake: Stored backup reference to main fire particles: {fireParticles.name}");
        }
        else
        {
            Debug.LogError("FireAttack Awake: fireParticles is NULL! Make sure it's assigned in the Inspector.");
        }
        
        // Initialize particle damage with this attack's damage stat and evolution features
        UpdateParticleDamage();
        
        // Store original particle settings
        StoreOriginalParticleSettings();
    }
    
    /// <summary>
    /// Ensures the fireParticles reference is valid, restoring from backup if needed
    /// </summary>
    private void EnsureFireParticlesReference()
    {
        if (fireParticles == null && mainFireParticlesBackup != null)
        {
            Debug.LogWarning("FireAttack: Main fireParticles reference was lost! Restoring from backup...");
            fireParticles = mainFireParticlesBackup;
        }
        
        if (fireParticles == null)
        {
            Debug.LogError("FireAttack: Cannot restore fireParticles - both main and backup references are NULL!");
        }
    }
    
    /// <summary>
    /// Updates the particle damage component with current stats and evolution features
    /// </summary>
    private void UpdateParticleDamage()
    {
        if (particleDamage != null)
        {
            particleDamage.Initialize(damage, lifeStealPercent, critChance, critMultiplier, burnDamagePercent, burnDuration);
        }
    }
    
    private void StoreOriginalParticleSettings()
    {
        if (fireParticles != null && !hasStoredOriginals)
        {
            var main = fireParticles.main;
            originalStartLifetime = main.startLifetime.constant;
            originalStartSpeed = main.startSpeed.constant;
            hasStoredOriginals = true;
        }
    }

    protected override void OnActivate()
    {
        // Always ensure we have a valid reference before trying to use it
        EnsureFireParticlesReference();
        
        if (fireParticles != null)
        {
            // Ensure the particle system GameObject is active
            if (!fireParticles.gameObject.activeSelf)
            {
                fireParticles.gameObject.SetActive(true);
                Debug.LogWarning("FireAttack: Main fire particles GameObject was inactive! Reactivating...");
            }
            
            // Apply range multiplier to particle system range
            ApplyRangeMultiplierToParticles();
            fireParticles.Play();
            
            Debug.Log($"FireAttack: OnActivate - Fire particles playing: {fireParticles.isPlaying}, GameObject: {fireParticles.gameObject.name}");
        }
        else
        {
            Debug.LogError("FireAttack: OnActivate called but fireParticles is NULL even after restore attempt!");
        }
        
        if (animator != null)
        {
            animator.SetBool(animationBoolName, true);
        }
        
        // Play fire breath sound (looping)
        SoundManager.Play("FireBreath", gameObject);
    }
    
    /// <summary>
    /// Applies the range multiplier from PlayerStats to the fire particle system
    /// This scales the startLifetime to make particles travel further
    /// </summary>
    private void ApplyRangeMultiplierToParticles()
    {
        if (fireParticles == null || !hasStoredOriginals) return;
        
        float rangeMultiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetRangeMultiplier() : 1f;
        
        var main = fireParticles.main;
        // Scale lifetime to increase range (particles travel further before dying)
        main.startLifetime = originalStartLifetime * rangeMultiplier;
        // Optionally also scale speed slightly for more dramatic effect
        main.startSpeed = originalStartSpeed * Mathf.Sqrt(rangeMultiplier); // Square root for balanced scaling
    }

    protected override void OnHoldUpdate()
    {
        // Always ensure we have a valid reference
        EnsureFireParticlesReference();
        
        // Keep particles playing while held
        if (fireParticles != null)
        {
            // Ensure the particle system GameObject is active
            if (!fireParticles.gameObject.activeSelf)
            {
                fireParticles.gameObject.SetActive(true);
                Debug.LogWarning("FireAttack: Main fire particles GameObject became inactive during hold! Reactivating...");
            }
            
            if (!fireParticles.isPlaying)
            {
                ApplyRangeMultiplierToParticles();
                fireParticles.Play();
                Debug.LogWarning("FireAttack: Main fire particles stopped during hold! Restarting...");
            }
        }
        else
        {
            Debug.LogError("FireAttack: OnHoldUpdate - fireParticles is NULL!");
        }
    }

    protected override void OnDeactivate()
    {
        // Ensure we have a valid reference
        EnsureFireParticlesReference();
        
        if (fireParticles != null)
        {
            fireParticles.Stop();
            // Restore original settings when deactivated
            RestoreOriginalParticleSettings();
        }
        
        if (animator != null)
        {
            animator.SetBool(animationBoolName, false);
        }
        
        // Fade out fire breath sound instead of abrupt stop
        SoundManager.FadeOut("FireBreath", gameObject, 0.3f);
    }
    
    /// <summary>
    /// Restores the original particle settings
    /// </summary>
    private void RestoreOriginalParticleSettings()
    {
        if (fireParticles == null || !hasStoredOriginals) return;
        
        var main = fireParticles.main;
        main.startLifetime = originalStartLifetime;
        main.startSpeed = originalStartSpeed;
    }
    
    /// <summary>
    /// Applies custom stats from the upgrade data (including evolution stats)
    /// </summary>
    protected override void ApplyCustomStats(AttackLevelStats stats)
    {
        base.ApplyCustomStats(stats);
        
        // Get evolution-specific stats
        lifeStealPercent = GetCustomStat("lifeSteal", 0f);
        critChance = GetCustomStat("critChance", 0f);
        critMultiplier = GetCustomStat("critMultiplier", 2f);
        burnDamagePercent = GetCustomStat("burnDamage", 0f);
        burnDuration = GetCustomStat("burnDuration", 0f);
        
        // Update particle damage with new stats
        UpdateParticleDamage();
        
        Debug.Log($"FireAttack: Applied stats - LifeSteal: {lifeStealPercent:P0}, Crit: {critChance:P0}x{critMultiplier}, Burn: {burnDamagePercent:P0} for {burnDuration}s");
    }
    
    /// <summary>
    /// Called when the attack is upgraded
    /// </summary>
    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        
        // Re-apply range multiplier if particles are playing
        if (fireParticles != null && fireParticles.isPlaying)
        {
            ApplyRangeMultiplierToParticles();
        }
        
        Debug.Log($"FireAttack upgraded to level {GetCurrentLevel()}!");
    }
    
    /// <summary>
    /// Called when an evolution level is reached
    /// </summary>
    protected override void OnEvolutionReached()
    {
        base.OnEvolutionReached();
        
        EvolutionRequirement evolution = GetCurrentEvolution();
        if (evolution != null)
        {
            Debug.Log($"FireAttack evolved to {evolution.evolutionName}!");
            
            // Store the current state of the main fire particles
            bool wasPlaying = fireParticles != null && fireParticles.isPlaying;
            
            // Place body fire emitters when evolution is reached
            PlaceBodyFireEmitters();
            
            // Ensure main fire particles are still properly configured after placing body fires
            // This protects against any accidental interference from the body fire prefab
            if (fireParticles != null)
            {
                Debug.Log($"FireAttack: Main fire particles status after evolution - GameObject active: {fireParticles.gameObject.activeSelf}, isPlaying: {fireParticles.isPlaying}");
                
                // If the main fire was playing before, make sure it's still playing
                if (wasPlaying && !fireParticles.isPlaying)
                {
                    Debug.LogWarning("FireAttack: Main fire particles were stopped during evolution! Restarting...");
                    fireParticles.Play();
                }
            }
            else
            {
                Debug.LogError("FireAttack: Main fireParticles reference is NULL after evolution!");
            }
        }
    }
    
    /// <summary>
    /// Places fire emitters on body parts similar to how bombs are placed.
    /// Each selected body segment gets TWO fire emitters - one shooting left, one shooting right.
    /// </summary>
    private void PlaceBodyFireEmitters()
    {
        if (bodyFirePrefab == null)
        {
            Debug.LogWarning("FireAttack: bodyFirePrefab is not assigned! Cannot place body fire emitters.");
            return;
        }
        
        // CRITICAL: Ensure we have the backup reference before doing anything
        EnsureFireParticlesReference();
        
        // Store the playing state before instantiating body fires
        bool mainFireWasPlaying = fireParticles != null && fireParticles.isPlaying;
        
        if (hasPlacedBodyFires)
        {
            // Update existing emitters with new stats
            UpdateBodyFireEmitterStats();
            return;
        }
        
        // Get SnakeBody reference
        if (snakeBody == null)
        {
            snakeBody = FindFirstObjectByType<SnakeBody>();
            if (snakeBody == null)
            {
                Debug.LogWarning("FireAttack: SnakeBody not found! Cannot place body fire emitters.");
                return;
            }
        }
        
        List<BodyPart> bodyParts = snakeBody.bodyParts;
        
        if (bodyParts == null || bodyParts.Count == 0)
        {
            Debug.LogWarning("FireAttack: Body parts list is empty!");
            return;
        }
        
        int totalParts = bodyParts.Count;
        
        // Need at least 7 parts (3 near head + 3 tapered tail + 1 for fire) to place any fire emitters
        if (totalParts < 7)
        {
            Debug.Log("FireAttack: Not enough body parts to place fire emitters!");
            return;
        }
        
        // Safe zones (same as bombs):
        // - First 3 segments near the head (indices 0, 1, 2)
        // - Last 3 segments are tapered tail (indices totalParts-3, totalParts-2, totalParts-1)
        int startIndex = 3;
        int endIndex = totalParts - 4;
        int availableSlots = endIndex - startIndex + 1;
        
        // Don't place more segment emitters than available slots
        int segmentCount = Mathf.Min(bodyFireSegmentCount, availableSlots);
        
        if (segmentCount <= 0)
        {
            Debug.Log("FireAttack: Not enough slots for fire emitters!");
            return;
        }
        
        // Calculate indices to place fire emitters evenly distributed
        List<int> emitterIndices = new List<int>();
        if (segmentCount == 1)
        {
            // Place single segment emitter in the middle
            emitterIndices.Add(startIndex + availableSlots / 2);
        }
        else
        {
            // Distribute segment emitters evenly across available slots
            for (int i = 0; i < segmentCount; i++)
            {
                float t = (float)i / (segmentCount - 1);
                int index = startIndex + Mathf.RoundToInt(t * (availableSlots - 1));
                emitterIndices.Add(index);
            }
        }
        
        // Place fire emitters at calculated indices
        // Each segment gets TWO fires - one pointing left, one pointing right
        foreach (int i in emitterIndices)
        {
            BodyPart part = bodyParts[i];
            
            // Create LEFT fire emitter (pointing to the left of the body segment)
            CreateBodyFireEmitter(part, new Vector3(0f, -90f, 90f), "Left");
            
            // Create RIGHT fire emitter (pointing to the right of the body segment)
            CreateBodyFireEmitter(part, new Vector3(0f, 90f, 90f), "Right");
        }
        
        hasPlacedBodyFires = true;
        Debug.Log($"FireAttack: Placed {bodyFireEmitters.Count} body fire emitters on {segmentCount} segments!");
        
        // CRITICAL: Restore the main fire particles reference if it was corrupted
        EnsureFireParticlesReference();
        
        // Ensure main fire is still playing if it was before
        if (mainFireWasPlaying && fireParticles != null && !fireParticles.isPlaying)
        {
            Debug.LogWarning("FireAttack: Main fire particles stopped during body fire placement! Restarting...");
            fireParticles.Play();
        }
    }
    
    /// <summary>
    /// Creates a single body fire emitter on a body part with the specified rotation
    /// </summary>
    private void CreateBodyFireEmitter(BodyPart part, Vector3 rotationOffset, string side)
    {
        // Instantiate fire emitter at body part position
        GameObject fireObj = Instantiate(bodyFirePrefab, part.transform.position, Quaternion.identity);
        fireObj.name = $"BodyFire_{side}";
        
        BodyFireEmitter emitter = fireObj.GetComponent<BodyFireEmitter>();
        
        if (emitter == null)
        {
            emitter = fireObj.AddComponent<BodyFireEmitter>();
        }
        
        // Initialize the emitter with current stats
        emitter.Initialize(this, damage, lifeStealPercent, critChance, critMultiplier, burnDamagePercent, burnDuration);
        
        // Parent to body part so it follows
        fireObj.transform.SetParent(part.transform);
        
        // Reset local position
        fireObj.transform.localPosition = Vector3.zero;
        
        // Set rotation so fire emits horizontally to the left or right
        // Rotation (0, -90, 90) = pointing left (local X-)
        // Rotation (0, 90, 90) = pointing right (local X+)
        fireObj.transform.localRotation = Quaternion.Euler(rotationOffset);
        
        bodyFireEmitters.Add(emitter);
    }
    
    /// <summary>
    /// Updates the stats on all body fire emitters
    /// </summary>
    private void UpdateBodyFireEmitterStats()
    {
        foreach (var emitter in bodyFireEmitters)
        {
            if (emitter != null)
            {
                emitter.UpdateStats(damage, lifeStealPercent, critChance, critMultiplier, burnDamagePercent, burnDuration);
            }
        }
    }
    
    /// <summary>
    /// Clears all body fire emitters
    /// </summary>
    public void ClearBodyFireEmitters()
    {
        foreach (var emitter in bodyFireEmitters)
        {
            if (emitter != null)
            {
                Destroy(emitter.gameObject);
            }
        }
        bodyFireEmitters.Clear();
        hasPlacedBodyFires = false;
    }
    
    private void OnDestroy()
    {
        ClearBodyFireEmitters();
    }
    
    // Getters for evolution stats (can be used by UI or other systems)
    public float GetLifeStealPercent() => lifeStealPercent;
    public float GetCritChance() => critChance;
    public float GetCritMultiplier() => critMultiplier;
    public float GetBurnDamagePercent() => burnDamagePercent;
    public float GetBurnDuration() => burnDuration;
    
    /// <summary>
    /// Returns whether body fire emitters have been placed
    /// </summary>
    public bool HasBodyFireEmitters() => hasPlacedBodyFires;
    
    /// <summary>
    /// Gets the list of body fire emitters
    /// </summary>
    public List<BodyFireEmitter> GetBodyFireEmitters() => bodyFireEmitters;
}