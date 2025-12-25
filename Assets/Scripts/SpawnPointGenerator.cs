using UnityEngine;

public class SpawnPointGenerator : MonoBehaviour
{
    public SpriteRenderer spawnMaskSprite; // Sprite scaled to fit island
    public float dropHeight = 50f;
    public int pointCount = 100;
    public LayerMask islandLayer;
    
    [Header("Generated Points")]
    public GameObject spawnPointsContainer; // Will be created and filled with transforms
    
    private Texture2D spawnMask;

    void Start()
    {
        // Get texture from sprite
        spawnMask = spawnMaskSprite.sprite.texture;
        
        if (!spawnMask.isReadable)
        {
            Debug.LogError("Sprite texture must be readable! Set Read/Write Enabled in import settings.");
            return;
        }
        
        GenerateSpawnPoints();
        
        // Hide the sprite after generating
        spawnMaskSprite.gameObject.SetActive(false);
    }

    void GenerateSpawnPoints()
    {
        // Create container GameObject
        spawnPointsContainer = new GameObject("SpawnPoints");
        int generated = 0;
        int attempts = 0;
        int maxAttempts = pointCount * 10;
        
        // Get sprite bounds in world space
        Bounds spriteBounds = spawnMaskSprite.bounds;
        
        while (generated < pointCount && attempts < maxAttempts)
        {
            attempts++;
            
            // Pick random point
            Vector2 randomUV = new Vector2(Random.value, Random.value);
            
            // Check mask
            Color pixelColor = spawnMask.GetPixelBilinear(randomUV.x, randomUV.y);
            if (pixelColor.a < 0.5f) continue;
            
            // Convert UV to world XZ using sprite's bounds
            Vector3 worldPos = new Vector3(
                Mathf.Lerp(spriteBounds.min.x, spriteBounds.max.x, randomUV.x),
                spriteBounds.center.y + dropHeight,
                Mathf.Lerp(spriteBounds.min.z, spriteBounds.max.z, randomUV.y)
            );
            
            // Raycast down to island surface
            RaycastHit hit;
            if (Physics.Raycast(worldPos, Vector3.down, out hit, dropHeight * 2, islandLayer))
            {
                // Create empty GameObject as spawn point
                GameObject spawnPoint = new GameObject($"SpawnPoint_{generated}");
                spawnPoint.transform.position = hit.point;
                spawnPoint.transform.parent = spawnPointsContainer.transform;
                
                generated++;
            }
        }
        
        Debug.Log($"Generated {generated} spawn points out of {pointCount} requested.");
    }
    
    // Call this to spawn at a random point
    public Vector3 GetRandomSpawnPoint()
    {
        if (spawnPointsContainer == null || spawnPointsContainer.transform.childCount == 0)
        {
            Debug.LogError("No spawn points available!");
            return Vector3.zero;
        }
        
        int randomIndex = Random.Range(0, spawnPointsContainer.transform.childCount);
        return spawnPointsContainer.transform.GetChild(randomIndex).position;
    }
}