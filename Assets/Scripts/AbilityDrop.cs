using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class AbilityDrop : MonoBehaviour, IPooledObject
{
    [Header("Drop Settings")]
    [SerializeField] private float groundedCheckTime = 0.3f; // Time with low velocity before considered grounded
    [SerializeField] private float stillThreshold = 0.15f; // Velocity threshold for "still"
    [SerializeField] private float collectionRadius = 2.5f; // Detection radius
    [Tooltip("Short grace period after spawn during which the drop cannot be picked up, to let it settle away from the player.")]
    [SerializeField] private float pickupGracePeriod = 0.25f;
    [Tooltip("Max time the drop will wait before force-grounding itself. Prevents drops that can't settle (e.g. on the snake's body) from being uncollectable.")]
    [SerializeField] private float forceGroundAfter = 2.5f;

    [Header("UI References")]
    [SerializeField] private Transform uiContainer;

    [Header("Visual Feedback")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;

    private Rigidbody rb;
    private SphereCollider sphereCollider;
    private bool isGrounded = false;
    private bool isCollected = false;
    private bool isDying = false;
    private float stillTimer = 0f;
    private float spawnTime = 0f;
    private Camera worldSpaceCamera;
    private Vector3 originalScale;
    private Transform playerTransform;
    private AbilityCollector cachedCollector;
    private Animator animator;
    private PooledObject pooledObject;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;
        pooledObject = GetComponent<PooledObject>();

        // Get or add sphere collider for reliable collection
        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            sphereCollider = gameObject.AddComponent<SphereCollider>();
        }
        // Ensure collider is set as trigger for collection
        sphereCollider.isTrigger = true;
        sphereCollider.radius = collectionRadius;

        // Use continuous collision detection to prevent the player from tunneling
        // through the drop at high speed. Must be non-kinematic for continuous modes.
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        // Find and disable any animator to prevent animations
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        if (animator != null)
        {
            animator.enabled = false;
        }

        CachePlayerReferences();
        SetupUIContainer();
    }

    private void OnEnable()
    {
        ResetDropState();
    }

    // IPooledObject: reset state every time the drop is pulled from the pool.
    // Without this, a previously-collected drop could come back with isCollected = true
    // and be silently uncollectable — the classic "walk through it, nothing happens" bug.
    public void OnSpawnFromPool()
    {
        ResetDropState();
        CachePlayerReferences(); // Re-cache in case the player was replaced (scene reload, etc.)
    }

    public void OnReturnToPool()
    {
        // Stop any ongoing work before the drop sits in the pool.
        CancelInvoke();
        StopAllCoroutines();
        if (uiContainer != null) uiContainer.gameObject.SetActive(false);
        transform.localScale = originalScale;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    private void ResetDropState()
    {
        isGrounded = false;
        isCollected = false;
        isDying = false;
        stillTimer = 0f;
        spawnTime = Time.time;
        transform.localScale = originalScale;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (uiContainer != null)
        {
            uiContainer.gameObject.SetActive(false);
        }
    }

    private void CachePlayerReferences()
    {
        cachedCollector = FindFirstObjectByType<AbilityCollector>();
        if (cachedCollector != null)
        {
            playerTransform = cachedCollector.transform;

            AbilityManager abilityManager = cachedCollector.GetComponent<AbilityManager>();
            if (abilityManager != null)
            {
                worldSpaceCamera = abilityManager.GetWorldSpaceCamera();
            }
        }
    }

    private void SetupUIContainer()
    {
        if (uiContainer != null)
        {
            Canvas canvas = uiContainer.GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && worldSpaceCamera != null)
            {
                canvas.worldCamera = worldSpaceCamera;
            }
            uiContainer.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isCollected || isDying) return;

        UpdateGroundedState();
        TryContinuousCollect();
        UpdatePulse();
    }

    private void UpdateGroundedState()
    {
        if (isGrounded) return;

        if (rb != null)
        {
            float velocity = rb.linearVelocity.magnitude;

            if (velocity < stillThreshold)
            {
                stillTimer += Time.deltaTime;
                if (stillTimer >= groundedCheckTime)
                {
                    OnGrounded();
                    return;
                }
            }
            else
            {
                stillTimer = 0f;
            }
        }

        // Safety net: if the drop lands on the snake's moving body (or any surface
        // that keeps nudging it), its velocity may never drop below the still
        // threshold and it would be uncollectable forever. Force-ground it after
        // a fixed timeout so the player can always pick it up.
        if (Time.time - spawnTime >= forceGroundAfter)
        {
            OnGrounded();
        }
    }

    private void TryContinuousCollect()
    {
        // Fallback distance-based collection. Runs independent of physics triggers
        // to guarantee pickup even if the trigger path misses due to high speed,
        // overlapping colliders, or physics timing quirks.
        if (!CanBeCollected()) return;
        if (playerTransform == null) return;
        if (Time.time - spawnTime < pickupGracePeriod) return;

        // Compare squared distance to avoid a sqrt every frame.
        Vector3 delta = transform.position - playerTransform.position;
        float sqrDist = delta.sqrMagnitude;
        float radius = collectionRadius;
        if (sqrDist <= radius * radius)
        {
            AttemptCollect();
        }
    }

    private void UpdatePulse()
    {
        if (isGrounded && !isDying)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = originalScale * pulse;
        }
    }

    private void OnGrounded()
    {
        if (isGrounded) return;
        isGrounded = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (uiContainer != null)
        {
            uiContainer.gameObject.SetActive(true);
        }
    }

    public bool IsGrounded() => isGrounded;
    public bool IsCollected() => isCollected;

    /// <summary>
    /// True if this drop is eligible for collection. Intentionally does NOT require
    /// full grounding — that was the source of the "walk-through" bug: drops that
    /// never settled (because they landed on the moving snake body, a slope, etc.)
    /// were permanently un-pickupable. We only gate on the short initial grace
    /// period so freshly-spawned drops don't auto-collect before falling.
    /// </summary>
    public bool CanBeCollected()
    {
        if (isCollected || isDying) return false;
        if (Time.time - spawnTime < pickupGracePeriod) return false;
        return true;
    }

    public void Collect()
    {
        if (isCollected) return;
        isCollected = true;
        isDying = true;

        transform.localScale = originalScale;
        CancelInvoke();
        StopAllCoroutines();

        if (uiContainer != null)
        {
            uiContainer.gameObject.SetActive(false);
        }

        // Route through the object pool when available so we don't leak
        // pooled instances by destroying them. Falls back to Destroy otherwise.
        if (pooledObject != null)
        {
            pooledObject.ReturnToPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!CanBeCollected()) return;
        TryCollectViaOther(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!CanBeCollected()) return;
        TryCollectViaOther(other);
    }

    private void TryCollectViaOther(Collider other)
    {
        // Accept the collector either on the collider or its rigidbody (common
        // when the player has child colliders) to catch more contact cases.
        AbilityCollector collector = other.GetComponent<AbilityCollector>();
        if (collector == null && other.attachedRigidbody != null)
        {
            collector = other.attachedRigidbody.GetComponent<AbilityCollector>();
        }
        if (collector == null && other.transform.root != null)
        {
            collector = other.transform.root.GetComponentInChildren<AbilityCollector>();
        }
        if (collector != null)
        {
            collector.TryCollectDrop(this);
        }
    }

    private void AttemptCollect()
    {
        if (cachedCollector == null)
        {
            CachePlayerReferences();
        }
        if (cachedCollector != null)
        {
            cachedCollector.TryCollectDrop(this);
        }
    }

    private void OnDestroy()
    {
        CancelInvoke();
        StopAllCoroutines();
    }
}
