using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;

public class AttackManager : MonoBehaviour
{
    public static event Action OnAttacksChanged;
    
    [Header("Attack Settings")]
    [Tooltip("The player's single attack. Player can only have one attack that they upgrade.")]
    public List<Attack> attacks = new List<Attack>();

    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    [Header("References")]
    [SerializeField] private SnakeBody snakeBody;
[SerializeField] private GameObject fuelObj;
    // The player's single attack
    private Attack CurrentAttack => attacks.Count > 0 ? attacks[0] : null;
    
    private bool isHoldingAttack = false;
    private bool isFrozen = false; // Whether attacks are frozen (for ability selection)
    [SerializeField] private WaveManager waveManager;

    private void Start()
    {
        fuelObj.SetActive(false);
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
        
        if (snakeBody == null)
        {
            snakeBody = GetComponent<SnakeBody>();
        }
        
        // Always start with no attack - player selects their attack after wave 1
        ClearAllAttacks();
    }

    private void Update()
    {
        if (isFrozen) return; // Skip updates when frozen
        if (waveManager != null && waveManager.IsInChoicePhase()) return;
        
        if (isHoldingAttack && CurrentAttack != null)
        {
            CurrentAttack.HoldUpdate();
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (isFrozen) return; // Block attack input when frozen
        if (waveManager != null && waveManager.IsInChoicePhase()) return;
        
        if (CurrentAttack == null) return;

        if (context.started)
        {
            if (CurrentAttack.TryActivate())
            {
                if (CurrentAttack.GetAttackType() == Attack.AttackType.Continuous)
                {
                    isHoldingAttack = true;
                }
            }
        }
        else if (context.canceled)
        {
            if (isHoldingAttack)
            {
                CurrentAttack.StopUsing();
                isHoldingAttack = false;
            }
        }
    }

    /// <summary>
    /// Sets the player's attack (can only have one attack)
    /// </summary>
    public bool AddAttack(Attack newAttack)
    {
        if (newAttack == null) return false;
        
        fuelObj.SetActive(true);
        // Player can only have one attack
        if (attacks.Count > 0)
        {
            Debug.LogWarning($"Player already has an attack: {attacks[0].attackName}. Cannot add {newAttack.attackName}");
            return false;
        }
        
        attacks.Add(newAttack);
        Debug.Log($"Set player attack: {newAttack.attackName}");
        
        ApplyCurrentVariation();
        CurrentAttack?.SetAsCurrentAttack();
        
        OnAttacksChanged?.Invoke();
        return true;
    }
    
    /// <summary>
    /// Checks if the player owns a specific attack
    /// </summary>
    public bool HasAttack(Attack attack)
    {
        return attack != null && attacks.Contains(attack);
    }
    
    /// <summary>
    /// Checks if a new attack can be added (only if player has no attack)
    /// </summary>
    public bool CanAddAttack()
    {
        return attacks.Count == 0;
    }
    
    /// <summary>
    /// Clears all attacks from the player's collection
    /// </summary>
    public void ClearAllAttacks()
    {
        if (isHoldingAttack && CurrentAttack != null)
        {
            CurrentAttack.StopUsing();
            isHoldingAttack = false;
        }
        
        attacks.Clear();
        Debug.Log("Cleared all attacks");
        OnAttacksChanged?.Invoke();
    }
    
    /// <summary>
    /// Called when entering/exiting pause state
    /// </summary>
    public void SetPaused(bool paused)
    {
        // Stop any held attacks when pausing
        if (paused && isHoldingAttack && CurrentAttack != null)
        {
            CurrentAttack.StopUsing();
            isHoldingAttack = false;
        }
        
        // Notify all attacks about pause state
        Attack.SetPaused(paused);
    }
    
    /// <summary>
    /// Freezes or unfreezes attacks (for ability selection)
    /// </summary>
    public void SetFrozen(bool frozen)
    {
        // Stop any held attacks when freezing
        if (frozen && isHoldingAttack && CurrentAttack != null)
        {
            CurrentAttack.StopUsing();
            isHoldingAttack = false;
        }
        
        isFrozen = frozen;
    }
    
    /// <summary>
    /// Returns whether attacks are currently frozen
    /// </summary>
    public bool IsFrozen() => isFrozen;
    
    /// <summary>
    /// Apply the visual variation for the current attack.
    /// Only applies attachment objects for regular attacks.
    /// Material changes are only applied for evolution attacks.
    /// </summary>
    private void ApplyCurrentVariation()
    {
        if (snakeBody == null || CurrentAttack == null) return;
        
        AttackVariation variation = CurrentAttack.GetVisualVariation();
        
        // Check if this attack is at an evolution level
        if (CurrentAttack.IsAtEvolutionLevel())
        {
            // Apply evolution visuals (including materials)
            ApplyEvolutionVisuals(CurrentAttack);
        }
        else
        {
            // For non-evolution attacks, only apply attachment (no material changes)
            if (variation != null)
            {
                snakeBody.ApplyAttackVariation(null, null, variation.attachmentObject);
            }
        }
    }
    
    /// <summary>
    /// Applies evolution-specific visuals including materials
    /// </summary>
    private void ApplyEvolutionVisuals(Attack attack)
    {
        // Try to find snakeBody if not assigned
        if (snakeBody == null)
        {
            snakeBody = GetComponent<SnakeBody>();
            if (snakeBody == null)
            {
                snakeBody = FindFirstObjectByType<SnakeBody>();
            }
        }
        
        if (snakeBody == null)
        {
            Debug.LogWarning("AttackManager: Cannot apply evolution visuals - SnakeBody not found!");
            return;
        }
        
        if (attack == null)
        {
            Debug.LogWarning("AttackManager: Cannot apply evolution visuals - Attack is null!");
            return;
        }
        
        EvolutionRequirement evolution = attack.GetCurrentEvolution();
        if (evolution == null)
        {
            Debug.LogWarning($"AttackManager: No evolution found for {attack.attackName} at level {attack.GetCurrentLevel()}");
            return;
        }
        
        // Verify that the evolution is actually unlocked before applying visuals
        AbilityManager abilityManager = FindFirstObjectByType<AbilityManager>();
        AttackUpgradeData upgradeData = attack.GetUpgradeData();
        
        if (upgradeData != null && upgradeData.evolutionData != null && abilityManager != null)
        {
            // Check if this evolution is actually unlocked
            if (!upgradeData.evolutionData.IsEvolutionUnlocked(evolution, abilityManager))
            {
                Debug.LogWarning($"Evolution {evolution.evolutionName} for {attack.attackName} is not unlocked - skipping visual application");
                return;
            }
        }
        
        // Check if materials are assigned
        if (evolution.evolutionHeadMaterial == null && evolution.evolutionBodyMaterial == null)
        {
            Debug.LogWarning($"Evolution {evolution.evolutionName} has no materials assigned! Please assign evolutionHeadMaterial and/or evolutionBodyMaterial in the EvolutionData asset.");
        }
        
        // Apply evolution materials and attachment
        snakeBody.ApplyAttackVariation(
            evolution.evolutionHeadMaterial,
            evolution.evolutionBodyMaterial,
            evolution.evolutionAttachment
        );
        
        Debug.Log($"Applied evolution visuals for {attack.attackName}: {evolution.evolutionName}" +
                  $" (Head Material: {(evolution.evolutionHeadMaterial != null ? evolution.evolutionHeadMaterial.name : "none")}" +
                  $", Body Material: {(evolution.evolutionBodyMaterial != null ? evolution.evolutionBodyMaterial.name : "none")})");
    }
    
    /// <summary>
    /// Forces application of evolution visuals for the current attack if it's evolved
    /// </summary>
    public void RefreshEvolutionVisuals()
    {
        Debug.Log($"RefreshEvolutionVisuals called. CurrentAttack: {(CurrentAttack != null ? CurrentAttack.attackName : "null")}");
        
        if (CurrentAttack == null)
        {
            Debug.LogWarning("RefreshEvolutionVisuals: No current attack!");
            return;
        }
        
        if (CurrentAttack.IsAtEvolutionLevel())
        {
            Debug.Log($"CurrentAttack {CurrentAttack.attackName} is at evolution level {CurrentAttack.GetCurrentLevel()}");
            ApplyEvolutionVisuals(CurrentAttack);
        }
        else
        {
            Debug.Log($"CurrentAttack {CurrentAttack.attackName} is NOT at evolution level (level {CurrentAttack.GetCurrentLevel()})");
        }
    }

    public Attack GetCurrentAttack() => CurrentAttack;
    
    public int GetCurrentAttackIndex() => 0; // Only one attack, always at index 0
    
    public Attack GetAttackAtIndex(int index)
    {
        if (index >= 0 && index < attacks.Count)
            return attacks[index];
        return null;
    }
    
    public int GetAttackCount() => attacks.Count;
    
    public float GetFuelPercentage()
    {
        if (CurrentAttack == null) return 0f;
        return CurrentAttack.GetFuelPercentage();
    }

    public bool CanActivateCurrentAttack()
    {
        if (CurrentAttack == null) return false;
        return CurrentAttack.CanActivate();
    }

    public void TriggerAnimation(string triggerName)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
        {
            animator.SetTrigger(triggerName);
        }
    }
    
    public void SetBool(string boolName, bool value)
    {
        if (animator != null && !string.IsNullOrEmpty(boolName))
        {
            animator.SetBool(boolName, value);
        }
    }
    
    public Animator GetAnimator() => animator;
}