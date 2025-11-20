using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class AttackManager : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private List<Attack> attacks = new List<Attack>();
    [SerializeField] private int currentAttackIndex = 0;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private Attack CurrentAttack => attacks.Count > 0 && currentAttackIndex < attacks.Count ? attacks[currentAttackIndex] : null;
    
    private bool isHoldingAttack = false;

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
    }

    private void Update()
    {
        // Handle continuous attacks while holding
        if (isHoldingAttack && CurrentAttack != null)
        {
            CurrentAttack.HoldUpdate();
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (CurrentAttack == null) return;

        // Button pressed
        if (context.started)
        {
            if (CurrentAttack.TryActivate())
            {
                // For burst attacks, TryActivate handles everything and immediately deactivates
                // For continuous attacks, we need to track that we're holding
                if (CurrentAttack.GetAttackType() == Attack.AttackType.Continuous)
                {
                    isHoldingAttack = true;
                }
            }
        }
        // Button released
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
        // Stop current attack if switching
        if (isHoldingAttack && CurrentAttack != null)
        {
            CurrentAttack.StopUsing();
            isHoldingAttack = false;
        }

        if (index >= 0 && index < attacks.Count)
        {
            currentAttackIndex = index;
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

    public Animator GetAnimator() => animator;
}