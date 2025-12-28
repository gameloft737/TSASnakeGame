using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// A draggable UI slot that represents an attack in the player's attack list.
/// Can be dragged and dropped to reorder attacks.
/// </summary>
public class DraggableAttackSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image activeIndicator;
    
    [Header("Drag Settings")]
    [SerializeField] private float dragAlpha = 0.6f;
    [SerializeField] private float previewAlpha = 0.5f;
    [SerializeField] private Color activeSlotColor = new Color(0.3f, 1f, 0.3f, 1f);
    [SerializeField] private Color normalSlotColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color emptySlotColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    [SerializeField] private Color previewColor = new Color(0.7f, 0.9f, 1f, 0.7f);
    
    private Attack attack;
    private int slotIndex;
    private AttackReorderPanel reorderPanel;
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private int originalSiblingIndex;
    private bool isDragging = false;
    private bool isShowingPreview = false;
    private Attack previewAttack = null;
    
    public Attack Attack => attack;
    public int SlotIndex => slotIndex;
    public bool IsEmpty => attack == null;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvas = GetComponentInParent<Canvas>();
    }
    
    /// <summary>
    /// Initialize the slot with an attack and its index
    /// </summary>
    public void Initialize(Attack attack, int index, AttackReorderPanel panel)
    {
        this.attack = attack;
        this.slotIndex = index;
        this.reorderPanel = panel;
        
        UpdateVisuals();
    }
    
    /// <summary>
    /// Set this slot as empty
    /// </summary>
    public void SetEmpty(int index, AttackReorderPanel panel)
    {
        this.attack = null;
        this.slotIndex = index;
        this.reorderPanel = panel;
        
        UpdateVisuals();
    }
    
    /// <summary>
    /// Update the visual appearance of the slot
    /// </summary>
    public void UpdateVisuals()
    {
        // If showing preview, don't update with normal visuals
        if (isShowingPreview) return;
        
        if (attack == null)
        {
            // Empty slot - hide it
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);
        
        // Filled slot
        if (iconImage != null)
        {
            if (attack.attackIcon != null)
            {
                iconImage.sprite = attack.attackIcon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }
        
        if (nameText != null) nameText.text = attack.attackName;
        if (levelText != null) levelText.text = $"Lvl {attack.GetCurrentLevel()}";
        
        // First slot (index 0) is the active attack
        bool isActive = slotIndex == 0;
        if (backgroundImage != null) backgroundImage.color = isActive ? activeSlotColor : normalSlotColor;
        if (activeIndicator != null) activeIndicator.gameObject.SetActive(isActive);
        
        // Reset alpha
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }
    
    /// <summary>
    /// Show a preview of what this slot would look like with a different attack
    /// </summary>
    /// <param name="previewAttack">The attack to show in this slot</param>
    /// <param name="previewIndex">The index this slot represents in the preview</param>
    /// <param name="isTheDraggedSlot">True if this is where the dragged attack will go (highlight it)</param>
    public void ShowPreview(Attack previewAttack, int previewIndex, bool isTheDraggedSlot = false)
    {
        isShowingPreview = true;
        this.previewAttack = previewAttack;
        
        if (previewAttack == null)
        {
            // Would become empty
            if (iconImage != null) iconImage.enabled = false;
            if (nameText != null) nameText.text = "Empty";
            if (levelText != null) levelText.text = "";
            if (backgroundImage != null) backgroundImage.color = emptySlotColor;
        }
        else
        {
            // Show preview of the attack
            if (iconImage != null)
            {
                if (previewAttack.attackIcon != null)
                {
                    iconImage.sprite = previewAttack.attackIcon;
                    iconImage.enabled = true;
                }
                else
                {
                    iconImage.enabled = false;
                }
            }
            
            if (nameText != null) nameText.text = previewAttack.attackName;
            if (levelText != null) levelText.text = $"Lvl {previewAttack.GetCurrentLevel()}";
            
            // Only highlight the slot where the dragged attack will go
            // Other slots show normal colors (just with different attacks)
            bool wouldBeActive = previewIndex == 0;
            if (isTheDraggedSlot)
            {
                // This is where the dragged attack will land - highlight it
                if (backgroundImage != null) backgroundImage.color = previewColor;
            }
            else
            {
                // This slot just shifted - show normal colors
                if (backgroundImage != null) backgroundImage.color = wouldBeActive ? activeSlotColor : normalSlotColor;
            }
            
            if (activeIndicator != null) activeIndicator.gameObject.SetActive(wouldBeActive);
        }
        
        // Only make the dragged slot position semi-transparent, others stay fully visible
        if (canvasGroup != null) canvasGroup.alpha = isTheDraggedSlot ? previewAlpha : 1f;
    }
    
    /// <summary>
    /// Clear the preview and return to normal display
    /// </summary>
    public void ClearPreview()
    {
        isShowingPreview = false;
        previewAttack = null;
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (attack == null) return; // Can't drag empty slots
        
        isDragging = true;
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        
        // Move to top of hierarchy so it renders on top
        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();
        
        canvasGroup.alpha = dragAlpha;
        canvasGroup.blocksRaycasts = false;
        
        if (reorderPanel != null)
        {
            reorderPanel.OnSlotDragStarted(this);
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        isDragging = false;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        
        // Return to original parent
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalSiblingIndex);
        rectTransform.anchoredPosition = originalPosition;
        
        if (reorderPanel != null)
        {
            reorderPanel.OnSlotDragEnded(this);
        }
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        DraggableAttackSlot draggedSlot = eventData.pointerDrag?.GetComponent<DraggableAttackSlot>();
        if (draggedSlot == null || draggedSlot == this) return;
        
        if (reorderPanel != null)
        {
            reorderPanel.SwapSlots(draggedSlot.SlotIndex, this.slotIndex);
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only respond if something is being dragged
        if (reorderPanel != null && reorderPanel.CurrentlyDragging != null && reorderPanel.CurrentlyDragging != this)
        {
            reorderPanel.OnSlotHovered(this);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        // Only respond if something is being dragged
        if (reorderPanel != null && reorderPanel.CurrentlyDragging != null)
        {
            reorderPanel.OnSlotUnhovered(this);
        }
    }
    
    /// <summary>
    /// Highlight this slot as a potential drop target (legacy, now using preview system)
    /// </summary>
    public void SetDropHighlight(bool highlight)
    {
        // Now handled by the preview system
    }
}