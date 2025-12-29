using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;

public class AttackManager : MonoBehaviour
{
    public static event Action OnAttacksChanged;
    
    [Header("Attack Settings")]
    [Tooltip("Attacks the player currently owns. First attack (index 0) is the active one. Leave empty to start with no attacks.")]
    public List<Attack> attacks = new List<Attack>();
    
    [Header("Attack Limits")]
    [SerializeField] private int maxAttacks = 4;
    
    [Header("Startup")]
    [Tooltip("If true, clears all attacks on game start so player starts with none.")]
    [SerializeField] private bool clearAttacksOnStart = true;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    [Header("References")]
    [SerializeField] private SnakeBody snakeBody;

    // The active attack is always the first one (index 0)
    private Attack CurrentAttack => attacks.Count > 0 ? attacks[0] : null;
    
    private bool isHoldingAttack = false;
    private bool isFrozen = false; // Whether attacks are frozen (for ability selection)
    [SerializeField] private WaveManager waveManager;

    private void Start()
    {
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
        
        // Clear attacks on start if configured (so player starts with no attacks)
        if (clearAttacksOnStart)
        {
            ClearAllAttacks();
        }
        else
        {
            // Apply initial variation and set as current attack
            ApplyCurrentVariation();
            if (CurrentAttack != null)
            {
                CurrentAttack.SetAsCurrentAttack();
            }
        }
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
    /// Moves an attack to the first slot, making it the active attack
    /// </summary>
    public void SetActiveAttack(int index)
    {
        if (index <= 0 || index >= attacks.Count) return;
        
        if (isHoldingAttack && CurrentAttack != null)
        {
            CurrentAttack.StopUsing();
            isHoldingAttack = false;
        }

        // Move the attack at index to position 0
        Attack attackToActivate = attacks[index];
        attacks.RemoveAt(index);
        attacks.Insert(0, attackToActivate);
        
        ApplyCurrentVariation();
        if (CurrentAttack != null)
        {
            CurrentAttack.SetAsCurrentAttack();
        }
        
        OnAttacksChanged?.Invoke();
    }
    
    /// <summary>
    /// Swaps two attacks in the list
    /// </summary>
    public void SwapAttacks(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= attacks.Count || indexB < 0 || indexB >= attacks.Count) return;
        if (indexA == indexB) return;
        
        if (isHoldingAttack && CurrentAttack != null)
        {
            CurrentAttack.StopUsing();
            isHoldingAttack = false;
        }
        
        Attack temp = attacks[indexA];
        attacks[indexA] = attacks[indexB];
        attacks[indexB] = temp;
        
        // If we swapped something into position 0, update the active attack
        if (indexA == 0 || indexB == 0)
        {
            ApplyCurrentVariation();
            if (CurrentAttack != null)
            {
                CurrentAttack.SetAsCurrentAttack();
            }
        }
        
        OnAttacksChanged?.Invoke();
    }
    
    /// <summary>
    /// Moves an attack from one index to another
    /// </summary>
    public void MoveAttack(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= attacks.Count || toIndex < 0 || toIndex >= attacks.Count) return;
        if (fromIndex == toIndex) return;
        
        if (isHoldingAttack && CurrentAttack != null)
        {
            CurrentAttack.StopUsing();
            isHoldingAttack = false;
        }
        
        Attack attackToMove = attacks[fromIndex];
        attacks.RemoveAt(fromIndex);
        attacks.Insert(toIndex, attackToMove);
        
        // If position 0 changed, update the active attack
        if (fromIndex == 0 || toIndex == 0)
        {
            ApplyCurrentVariation();
            if (CurrentAttack != null)
            {
                CurrentAttack.SetAsCurrentAttack();
            }
        }
        
        OnAttacksChanged?.Invoke();
    }

    /// <summary>
    /// Adds a new attack to the player's collection
    /// </summary>
    public bool AddAttack(Attack newAttack)
    {
        if (newAttack == null) return false;
        
        // Check if already owned
        if (attacks.Contains(newAttack))
        {
            Debug.Log($"Already own attack: {newAttack.attackName}");
            return false;
        }
        
        // Check max limit
        if (attacks.Count >= maxAttacks)
        {
            Debug.LogWarning($"Cannot add attack {newAttack.attackName}: max attacks ({maxAttacks}) reached!");
            return false;
        }
        
        attacks.Add(newAttack);
        Debug.Log($"Added attack: {newAttack.attackName}. Total attacks: {attacks.Count}");
        
        // If this is the first attack, make it active
        if (attacks.Count == 1)
        {
            ApplyCurrentVariation();
            CurrentAttack?.SetAsCurrentAttack();
        }
        
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
    /// Checks if a new attack can be added
    /// </summary>
    public bool CanAddAttack()
    {
        return attacks.Count < maxAttacks;
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
    /// Gets the maximum number of attacks allowed
    /// </summary>
    public int GetMaxAttacks() => maxAttacks;
    
    /// <summary>
    /// Legacy method - sets the attack at index as active by moving it to position 0
    /// </summary>
    public void SetAttackIndex(int index)
    {
        SetActiveAttack(index);
    }

    public void NextAttack()
    {
        // Cycle through attacks by moving the first one to the end
        if (attacks.Count > 1)
        {
            MoveAttack(0, attacks.Count - 1);
        }
    }

    public void PreviousAttack()
    {
        // Cycle through attacks by moving the last one to the front
        if (attacks.Count > 1)
        {
            MoveAttack(attacks.Count - 1, 0);
        }
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
        if (snakeBody == null || attack == null) return;
        
        EvolutionRequirement evolution = attack.GetCurrentEvolution();
        if (evolution == null) return;
        
        // Apply evolution materials and attachment
        snakeBody.ApplyAttackVariation(
            evolution.evolutionHeadMaterial,
            evolution.evolutionBodyMaterial,
            evolution.evolutionAttachment
        );
        
        Debug.Log($"Applied evolution visuals for {attack.attackName}: {evolution.evolutionName}");
    }
    
    /// <summary>
    /// Manually apply a specific variation from an attack.
    /// Only applies attachment objects for regular attacks.
    /// Material changes are only applied for evolution attacks.
    /// </summary>
    public void ApplyVariation(int attackIndex)
    {
        if (snakeBody == null || attackIndex < 0 || attackIndex >= attacks.Count) return;
        
        Attack attack = attacks[attackIndex];
        AttackVariation variation = attack.GetVisualVariation();
        
        // Check if this attack is at an evolution level
        if (attack.IsAtEvolutionLevel())
        {
            // Apply evolution visuals (including materials)
            ApplyEvolutionVisuals(attack);
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
    /// Forces application of evolution visuals for the current attack if it's evolved
    /// </summary>
    public void RefreshEvolutionVisuals()
    {
        if (CurrentAttack != null && CurrentAttack.IsAtEvolutionLevel())
        {
            ApplyEvolutionVisuals(CurrentAttack);
        }
    }

    public Attack GetCurrentAttack() => CurrentAttack;
    
    public int GetCurrentAttackIndex() => 0; // Active attack is always at index 0
    
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