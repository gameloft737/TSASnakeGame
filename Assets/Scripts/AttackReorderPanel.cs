using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Panel that displays the player's current attacks and allows drag-and-drop reordering.
/// The first slot is always the active attack.
/// </summary>
public class AttackReorderPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AttackManager attackManager;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject attackSlotPrefab;
    
    [Header("Settings")]
    [SerializeField] private int maxSlots = 4;
    
    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private string titleFormat = "Your Attacks ({0}/{1})";
    
    private List<DraggableAttackSlot> slots = new List<DraggableAttackSlot>();
    private DraggableAttackSlot currentlyDragging = null;
    private int previewFromIndex = -1;
    private int previewToIndex = -1;
    
    public DraggableAttackSlot CurrentlyDragging => currentlyDragging;
    
    private void OnEnable()
    {
        if (attackManager == null)
        {
            attackManager = FindFirstObjectByType<AttackManager>();
        }
        
        AttackManager.OnAttacksChanged += RefreshSlots;
        RefreshSlots();
    }
    
    private void OnDisable()
    {
        AttackManager.OnAttacksChanged -= RefreshSlots;
    }
    
    /// <summary>
    /// Refresh all slots to match the current attack list
    /// </summary>
    public void RefreshSlots()
    {
        if (attackManager == null) return;
        
        // Clear existing slots from our list
        foreach (var slot in slots)
        {
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }
        slots.Clear();
        
        // Also destroy any children in the container that we didn't create
        // This handles the case where the prefab was accidentally placed in the scene
        if (slotsContainer != null)
        {
            // Destroy all children except the ones we're about to create
            for (int i = slotsContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = slotsContainer.GetChild(i);
                // Check if this is the prefab itself (shouldn't be in scene)
                if (child.gameObject != attackSlotPrefab)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        
        // Only create slots for attacks the player actually owns (no empty slots)
        int attackCount = attackManager.GetAttackCount();
        
        // Only show slots if player has attacks
        if (attackCount == 0)
        {
            // Update title to show 0 attacks
            if (titleText != null)
            {
                titleText.text = string.Format(titleFormat, 0, maxSlots);
            }
            return;
        }
        
        for (int i = 0; i < attackCount; i++)
        {
            CreateSlot(i);
        }
        
        // Update title
        if (titleText != null)
        {
            titleText.text = string.Format(titleFormat, attackCount, maxSlots);
        }
    }
    
    private void CreateSlot(int index)
    {
        if (attackSlotPrefab == null || slotsContainer == null) return;
        
        Attack attack = attackManager.GetAttackAtIndex(index);
        
        // Don't create slots for null attacks
        if (attack == null) return;
        
        GameObject slotObj = Instantiate(attackSlotPrefab, slotsContainer);
        DraggableAttackSlot slot = slotObj.GetComponent<DraggableAttackSlot>();
        
        if (slot == null)
        {
            slot = slotObj.AddComponent<DraggableAttackSlot>();
        }
        
        slot.Initialize(attack, index, this);
        slots.Add(slot);
    }
    
    /// <summary>
    /// Called when a slot starts being dragged
    /// </summary>
    public void OnSlotDragStarted(DraggableAttackSlot slot)
    {
        currentlyDragging = slot;
        previewFromIndex = slot.SlotIndex;
        previewToIndex = -1;
    }
    
    /// <summary>
    /// Called when a slot stops being dragged
    /// </summary>
    public void OnSlotDragEnded(DraggableAttackSlot slot)
    {
        currentlyDragging = null;
        previewFromIndex = -1;
        previewToIndex = -1;
        
        // Reset all slot visuals
        foreach (var s in slots)
        {
            s.ClearPreview();
            s.UpdateVisuals();
        }
    }
    
    /// <summary>
    /// Called when hovering over a slot during drag
    /// </summary>
    public void OnSlotHovered(DraggableAttackSlot hoveredSlot)
    {
        if (currentlyDragging == null || hoveredSlot == currentlyDragging) return;
        
        previewToIndex = hoveredSlot.SlotIndex;
        
        // Update all slots to show preview
        UpdatePreview();
    }
    
    /// <summary>
    /// Called when leaving a slot during drag
    /// </summary>
    public void OnSlotUnhovered(DraggableAttackSlot unhoveredSlot)
    {
        if (currentlyDragging == null) return;
        
        previewToIndex = -1;
        
        // Reset preview
        foreach (var s in slots)
        {
            s.ClearPreview();
            s.UpdateVisuals();
        }
    }
    
    /// <summary>
    /// Update the preview to show what the INSERT would look like (not swap)
    /// When dragging to a position, other attacks shift to make room
    /// </summary>
    private void UpdatePreview()
    {
        if (previewFromIndex < 0 || previewToIndex < 0 || previewFromIndex == previewToIndex) return;
        
        // Build a preview list showing what the order would look like after the move
        List<Attack> previewOrder = new List<Attack>();
        int attackCount = attackManager.GetAttackCount();
        
        for (int i = 0; i < attackCount; i++)
        {
            if (i != previewFromIndex)
            {
                previewOrder.Add(attackManager.GetAttackAtIndex(i));
            }
        }
        
        // Insert the dragged attack at the target position
        Attack draggedAttack = attackManager.GetAttackAtIndex(previewFromIndex);
        int insertIndex = previewToIndex;
        if (previewFromIndex < previewToIndex)
        {
            insertIndex--; // Adjust for the removal
        }
        insertIndex = Mathf.Clamp(insertIndex, 0, previewOrder.Count);
        previewOrder.Insert(insertIndex, draggedAttack);
        
        // Update each slot to show the preview order
        for (int i = 0; i < slots.Count && i < previewOrder.Count; i++)
        {
            Attack previewAttack = previewOrder[i];
            bool isTheDraggedAttack = (previewAttack == draggedAttack);
            
            // Show preview - only the dragged attack position is highlighted
            slots[i].ShowPreview(previewAttack, i, isTheDraggedAttack);
        }
    }
    
    /// <summary>
    /// Move an attack from one position to another (insert, not swap)
    /// </summary>
    public void SwapSlots(int fromIndex, int toIndex)
    {
        if (attackManager == null) return;
        
        Debug.Log($"[AttackReorderPanel] Moving slot {fromIndex} -> {toIndex}");
        attackManager.MoveAttack(fromIndex, toIndex);
        
        // RefreshSlots will be called automatically via the OnAttacksChanged event
    }
    
    /// <summary>
    /// Move an attack to a specific slot
    /// </summary>
    public void MoveToSlot(int fromIndex, int toIndex)
    {
        if (attackManager == null) return;
        
        Debug.Log($"[AttackReorderPanel] Moving slot {fromIndex} -> {toIndex}");
        attackManager.MoveAttack(fromIndex, toIndex);
    }
    
    /// <summary>
    /// Set an attack as the active one (move to slot 0)
    /// </summary>
    public void SetAsActive(int index)
    {
        if (attackManager == null || index <= 0) return;
        
        Debug.Log($"[AttackReorderPanel] Setting slot {index} as active");
        attackManager.SetActiveAttack(index);
    }
}