using UnityEngine;

/// <summary>
/// XP drop that can be collected by the player
/// Optimized to use static player reference and reduced Update frequency
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
    
    // Static cached player reference - shared across all XP drops
    private static Transform s_cachedPlayer;
    private static bool s_playerSearched = false;
    
    private Rigidbody rb;
    private float spawnTime;
    private Vector3 startPosition;
    private bool hasScattered = false;
    private float scatterEndTime;
    
    // Cached squared distances for optimization
    private float attractRangeSqr;
    private float collectRangeSqr;
    
    private void Awake()
    {
        // Cache squared distances to avoid sqrt in distance checks
        attractRangeSqr = attractRange * attractRange;
        collectRangeSqr = collectRange * collectRange;
    }
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        spawnTime = Time.time;
        startPosition = transform.position;
        
        // Use static cached player - only search once across all XP drops
        if (!s_playerSearched)
        {
            PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
            if (playerMovement != null)
            {
                s_cachedPlayer = playerMovement.transform;
            }
            s_playerSearched = true;
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
        if (s_cachedPlayer == null) return;
        
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
        
        // Use sqrMagnitude instead of Distance to avoid sqrt
        Vector3 toPlayer = s_cachedPlayer.position - transform.position;
        float distanceSqr = toPlayer.sqrMagnitude;
        
        // Check for collection
        if (distanceSqr <= collectRangeSqr)
        {
            Collect();
            return;
        }
        
        // Attract to player if in range
        if (distanceSqr <= attractRangeSqr)
        {
            // Move towards player - normalize only when needed
            float distance = Mathf.Sqrt(distanceSqr);
            Vector3 direction = toPlayer / distance; // Faster than .normalized
            float speed = attractSpeed * (1f - (distance / attractRange) * 0.5f);
            transform.position += direction * speed * Time.deltaTime;
        }
        else
        {
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
    /// Static method to set the player reference directly (call from PlayerMovement.Start)
    /// </summary>
    public static void SetPlayerReference(Transform player)
    {
        s_cachedPlayer = player;
        s_playerSearched = true;
    }
    
    /// <summary>
    /// Clear cached references (call when player is destroyed or scene changes)
    /// </summary>
    public static void ClearCachedReferences()
    {
        s_cachedPlayer = null;
        s_playerSearched = false;
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
        // Play XP collection sound at this position (not attached to gameObject since it's being destroyed)
        SoundManager.PlayAtPoint("XPCollect", transform.position);
        
        if (XPManager.Instance != null)
        {
            XPManager.Instance.AddXP(xpValue);
        }
        
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