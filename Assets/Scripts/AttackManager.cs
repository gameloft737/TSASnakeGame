using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class AttackManager : MonoBehaviour
{
    [Header("Attack Settings")]
    public List<Attack> attacks = new List<Attack>();
    [SerializeField] private int currentAttackIndex = 0;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    [Header("References")]
    [SerializeField] private SnakeBody snakeBody;

    private Attack CurrentAttack => attacks.Count > 0 && currentAttackIndex < attacks.Count ? attacks[currentAttackIndex] : null;
    
    private bool isHoldingAttack = false;
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
        
        // Apply initial variation and set as current attack
        ApplyCurrentVariation();
        if (CurrentAttack != null)
        {
            CurrentAttack.SetAsCurrentAttack();
        }
    }

    private void Update()
    {
        if (waveManager != null && waveManager.IsInChoicePhase()) return;
        
        if (isHoldingAttack && CurrentAttack != null)
        {
            CurrentAttack.HoldUpdate();
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
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

    public void SetAttackIndex(int index)
    {
        if (isHoldingAttack && CurrentAttack != null)
        {
            CurrentAttack.StopUsing();
            isHoldingAttack = false;
        }

        if (index >= 0 && index < attacks.Count)
        {
            currentAttackIndex = index;
            ApplyCurrentVariation();
            
            // Set the new attack as the current one for fuel recharging
            if (CurrentAttack != null)
            {
                CurrentAttack.SetAsCurrentAttack();
            }
        }
    }

    public void NextAttack()
    {
        if (attacks.Count > 0)
        {
            SetAttackIndex((currentAttackIndex + 1) % attacks.Count);
        }
    }

    public void PreviousAttack()
    {
        if (attacks.Count > 0)
        {
            SetAttackIndex((currentAttackIndex - 1 + attacks.Count) % attacks.Count);
        }
    }

    public void AddAttack(Attack newAttack)
    {
        if (!attacks.Contains(newAttack))
        {
            attacks.Add(newAttack);
        }
    }
    
    /// <summary>
    /// Apply the visual variation for the current attack
    /// </summary>
    private void ApplyCurrentVariation()
    {
        if (snakeBody == null || CurrentAttack == null) return;
        
        AttackVariation variation = CurrentAttack.GetVisualVariation();
        if (variation == null) return;
        
        snakeBody.ApplyAttackVariation(
            variation.headMaterial,
            variation.bodyMaterial,
            variation.attachmentObject
        );
    }
    
    /// <summary>
    /// Manually apply a specific variation from an attack
    /// </summary>
    public void ApplyVariation(int attackIndex)
    {
        if (snakeBody == null || attackIndex < 0 || attackIndex >= attacks.Count) return;
        
        AttackVariation variation = attacks[attackIndex].GetVisualVariation();
        if (variation == null) return;
        
        snakeBody.ApplyAttackVariation(
            variation.headMaterial,
            variation.bodyMaterial,
            variation.attachmentObject
        );
    }

    public Attack GetCurrentAttack() => CurrentAttack;
    
    public int GetCurrentAttackIndex() => currentAttackIndex;
    
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