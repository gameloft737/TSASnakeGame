using UnityEngine;

/// <summary>
/// Component for apples in classic snake mode.
/// Handles collection and notifies the ClassicModeManager.
/// </summary>
public class ClassicApple : MonoBehaviour
{
    private ClassicModeManager modeManager;
    private bool isCollected = false;
    
    // Visual animation
    private float bobSpeed = 2f;
    private float bobAmount = 0.1f;
    private float rotateSpeed = 45f;
    private Vector3 startPosition;
    private float bobOffset;
    
    public void Initialize(ClassicModeManager manager)
    {
        modeManager = manager;
        startPosition = transform.position;
        bobOffset = Random.Range(0f, Mathf.PI * 2f); // Random phase offset
    }
    
    private void Start()
    {
        // If not initialized via Initialize(), try to find the manager
        if (modeManager == null)
        {
            modeManager = ClassicModeManager.Instance;
        }
        
        startPosition = transform.position;
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
    }
    
    private void Update()
    {
        // Simple bobbing animation
        float bob = Mathf.Sin((Time.time + bobOffset) * bobSpeed) * bobAmount;
        transform.position = startPosition + Vector3.up * bob;
        
        // Slow rotation
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }
    
    public void Collect()
    {
        if (isCollected) return;
        isCollected = true;
        
        // Play collection effect (optional)
        PlayCollectionEffect();
        
        // Notify manager
        if (modeManager != null)
        {
            modeManager.OnAppleCollected(gameObject);
        }
        else
        {
            // Fallback: just destroy
            Destroy(gameObject);
        }
    }
    
    private void PlayCollectionEffect()
    {
        // Simple scale pop effect before destruction
        // In a full implementation, you might spawn particles here
        transform.localScale *= 1.2f;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Alternative collection via trigger
        if (other.CompareTag("Player") || other.GetComponent<ClassicSnakeController>() != null)
        {
            Collect();
        }
    }
}