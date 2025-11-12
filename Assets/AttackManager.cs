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

    private void Start()
    {
        // Auto-find animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed && CurrentAttack != null)
        {
            CurrentAttack.TryUse();
        }
    }

    public void SetAttackIndex(int index)
    {
        if (index >= 0 && index < attacks.Count)
        {
            currentAttackIndex = index;
        }
    }

    public void NextAttack()
    {
        if (attacks.Count > 0)
        {
            currentAttackIndex = (currentAttackIndex + 1) % attacks.Count;
        }
    }

    public void PreviousAttack()
    {
        if (attacks.Count > 0)
        {
            currentAttackIndex = (currentAttackIndex - 1 + attacks.Count) % attacks.Count;
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
    
    public float GetCooldownProgress()
    {
        if (CurrentAttack == null) return 1f;
        return 1f - (CurrentAttack.GetCooldownRemaining() / CurrentAttack.GetCooldown());
    }

    // New method for attacks to trigger animations
    public void TriggerAnimation(string triggerName)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
        {
            animator.SetTrigger(triggerName);
        }
    }

    public Animator GetAnimator() => animator;
}