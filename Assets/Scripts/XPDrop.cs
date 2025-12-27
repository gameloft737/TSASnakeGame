using UnityEngine;

/// <summary>
/// XP drop that can be collected by the player
/// </summary>
public class XPDrop : MonoBehaviour
{
    [Header("XP Settings")]
    [SerializeField] private int xpValue = 10;
    
    [Header("Movement Settings")]
    [SerializeField] private float attractSpeed = 15f;
    [SerializeField] private float attractRange = 5f;
    [SerializeField] private float collectRange = 1f;
    [SerializeField] private float initialScatterForce = 3f;
    [SerializeField] private float scatterDuration = 0.3f;
    
    [Header("Visual Settings")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private float rotationSpeed = 90f;
    
    [Header("Lifetime")]
    [SerializeField] private float lifetime = 30f;
    
    private Transform player;
    private Rigidbody rb;
    private bool isBeingAttracted = false;
    private float spawnTime;
    private Vector3 startPosition;
    private bool hasScattered = false;
    private float scatterEndTime;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        spawnTime = Time.time;
        startPosition = transform.position;
        
        // Find player (snake head)
        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            player = playerMovement.transform;
        }
        
        // Apply initial scatter force
        if (rb != null)
        {
            Vector3 scatterDirection = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(0.5f, 1f),
                Random.Range(-1f, 1f)
            ).normalized;
            
            rb.AddForce(scatterDirection * initialScatterForce, ForceMode.Impulse);
            scatterEndTime = Time.time + scatterDuration;
        }
        
        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }
    
    private void Update()
    {
        if (player == null) return;
        
        // Wait for scatter to finish
        if (Time.time < scatterEndTime) return;
        
        if (!hasScattered)
        {
            hasScattered = true;
            // Stop physics movement after scatter
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            startPosition = transform.position;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Check for collection
        if (distanceToPlayer <= collectRange)
        {
            Collect();
            return;
        }
        
        // Attract to player if in range
        if (distanceToPlayer <= attractRange)
        {
            isBeingAttracted = true;
            
            // Move towards player
            Vector3 direction = (player.position - transform.position).normalized;
            float speed = attractSpeed * (1f - (distanceToPlayer / attractRange) * 0.5f); // Faster when closer
            transform.position += direction * speed * Time.deltaTime;
        }
        else
        {
            isBeingAttracted = false;
            
            // Bob up and down when not being attracted
            float bobOffset = Mathf.Sin((Time.time - spawnTime) * bobSpeed) * bobHeight;
            transform.position = new Vector3(
                startPosition.x,
                startPosition.y + bobOffset,
                startPosition.z
            );
        }
        
        // Always rotate
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
    
    /// <summary>
    /// Initialize the XP drop with a specific value
    /// </summary>
    public void Initialize(int xp)
    {
        xpValue = xp;
        
        // Scale visual based on XP value
        float scale = Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(xp / 50f));
        transform.localScale = Vector3.one * scale;
    }
    
    /// <summary>
    /// Collect the XP
    /// </summary>
    private void Collect()
    {
        if (XPManager.Instance != null)
        {
            XPManager.Instance.AddXP(xpValue);
        }
        
        // TODO: Play collection effect/sound
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Get the XP value of this drop
    /// </summary>
    public int GetXPValue() => xpValue;
    
    private void OnDrawGizmosSelected()
    {
        // Draw attract range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attractRange);
        
        // Draw collect range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, collectRange);
    }
}