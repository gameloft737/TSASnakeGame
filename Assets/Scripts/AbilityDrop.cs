using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AbilityDrop : MonoBehaviour
{
    [Header("Drop Settings")]
    [SerializeField] private float lifetimeAfterGround = 10f;
    [SerializeField] private float groundedCheckTime = 1f;
    [SerializeField] private float stillThreshold = 0.1f;
    
    [Header("Ability Pool")]
    [SerializeField] private AbilitySO[] possibleAbilities;
    
    [Header("UI References")]
    [SerializeField] private Transform uiContainer;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private string closeTrigger = "Close";
    
    public AbilitySO selectedAbility;
    private DropType dropType;
    private Rigidbody rb;
    public bool isGrounded = false;
    private bool isCollected = false;
    private bool isDying = false;
    private float lifetimeTimer = 0f;
    private float stillTimer = 0f;
    private AbilityManager playerAbilityManager;
    private DropShower dropShower;
    private Camera worldSpaceCamera;

    public enum DropType
    {
        New,
        Upgrade,
        Duration
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Find animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
        
        AbilityCollector collector = FindFirstObjectByType<AbilityCollector>();
        if (collector != null)
        {
            playerAbilityManager = collector.GetComponent<AbilityManager>();
            if (playerAbilityManager != null)
            {
                worldSpaceCamera = playerAbilityManager.GetWorldSpaceCamera();
            }
        }
        
        if (uiContainer != null)
        {
            dropShower = uiContainer.GetComponent<DropShower>();
            if (dropShower == null)
            {
                dropShower = uiContainer.gameObject.AddComponent<DropShower>();
            }
            
            // Set camera for world space canvas
            Canvas canvas = uiContainer.GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && worldSpaceCamera != null)
            {
                canvas.worldCamera = worldSpaceCamera;
            }
            
            uiContainer.gameObject.SetActive(false);
        }
        
        SelectRandomAbility();
    }

    private void Update()
    {
        if (isCollected) return;
        
        if (!isGrounded && rb != null)
        {
            float velocity = rb.linearVelocity.magnitude;
            
            if (velocity < stillThreshold)
            {
                stillTimer += Time.deltaTime;
                
                if (stillTimer >= groundedCheckTime)
                {
                    OnGrounded();
                }
            }
            else
            {
                stillTimer = 0f;
            }
        }
        
        if (isGrounded && !isDying)
        {
            lifetimeTimer += Time.deltaTime;
            
            // Start death animation 1 second before actual destruction
            if (lifetimeTimer >= lifetimeAfterGround - 1f)
            {
                StartDeathAnimation();
            }
        }
    }

    private void SelectRandomAbility()
    {
        if (possibleAbilities == null || possibleAbilities.Length == 0)
        {
            Debug.LogWarning("No abilities assigned to drop!");
            return;
        }
        
        selectedAbility = possibleAbilities[Random.Range(0, possibleAbilities.Length)];
        DetermineDropType();
    }

    private void DetermineDropType()
    {
        if (playerAbilityManager == null || selectedAbility == null)
        {
            dropType = DropType.New;
            return;
        }
        
        BaseAbility existingAbility = playerAbilityManager.GetAbility(selectedAbility.abilityPrefab);
        
        if (existingAbility == null)
        {
            dropType = DropType.New;
        }
        else if (existingAbility.IsMaxLevel())
        {
            dropType = DropType.Duration;
        }
        else
        {
            dropType = DropType.Upgrade;
        }
    }

    private void OnGrounded()
    {
        isGrounded = true;
        
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        // Trigger open animation
        if (animator != null)
        {
            animator.SetTrigger(openTrigger);
        }
        
        if (dropShower != null && selectedAbility != null)
        {
            int level = 1;
            if (playerAbilityManager != null)
            {
                BaseAbility existing = playerAbilityManager.GetAbility(selectedAbility.abilityPrefab);
                if (existing != null)
                {
                    level = existing.GetCurrentLevel() + (dropType == DropType.Upgrade ? 1 : 0);
                }
            }
            
            uiContainer.gameObject.SetActive(true);
            dropShower.Initialize(selectedAbility, dropType, level, worldSpaceCamera);
        }
    }
    
    private void StartDeathAnimation()
    {
        if (isDying) return;
        isDying = true;
        
        // Trigger close animation
        if (animator != null)
        {
            animator.SetTrigger(closeTrigger);
        }
        
        // Hide UI
        if (uiContainer != null)
        {
            uiContainer.gameObject.SetActive(false);
        }
        
        // Destroy after animation time
        Destroy(gameObject, 1f);
    }

    public AbilitySO GetAbilitySO() => selectedAbility;
    public DropType GetDropType() => dropType;
    public bool IsGrounded() => isGrounded;
    public bool IsCollected() => isCollected;
    
    public void Collect()
    {
        if (isCollected) return;
        isCollected = true;
        
        // Trigger close animation on collection too
        if (animator != null)
        {
            animator.SetTrigger(closeTrigger);
        }
        
        // Hide UI immediately
        if (uiContainer != null)
        {
            uiContainer.gameObject.SetActive(false);
        }
        
        // Destroy after brief delay for animation
        Destroy(gameObject, 0.3f);
    }
}