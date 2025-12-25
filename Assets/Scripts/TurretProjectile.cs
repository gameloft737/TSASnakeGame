using UnityEngine;

public class TurretProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float damage;
    private float lifetime;
    private float spawnTime;
    private bool isInitialized = false;
    [SerializeField]private Rigidbody rb;

    private void Awake()
    {
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = false; // No gravity
        rb.linearDamping = 0f; // No air resistance
        rb.angularDamping = 0f; // No rotation damping
        
    }

    public void Initialize(Vector3 dir, float spd, float dmg, float life)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        lifetime = life;
        spawnTime = Time.time;
        isInitialized = true;
        
        // Apply initial velocity using physics
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
    }

    private void FixedUpdate()
    {
        if (!isInitialized) return;
        
        // Check lifetime
        if (Time.time - spawnTime >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        AppleEnemy apple = other.GetComponentInParent<AppleEnemy>();
        
        if (apple != null && !apple.isMetal)
        {
            apple.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}