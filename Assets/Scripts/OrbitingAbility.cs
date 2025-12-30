using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Active ability that spawns orbiting objects around the snake's head.
/// The objects spin around and damage enemies on contact (like King Bible in Vampire Survivors).
/// Uses AbilityUpgradeData for level-based stats.
/// </summary>
public class OrbitingAbility : BaseAbility
{
    [Header("Orbit Settings (Fallback if no UpgradeData)")]
    [SerializeField] private int baseOrbitCount = 2; // Number of orbiting objects
    [SerializeField] private float baseOrbitRadius = 3f; // Distance from player
    [SerializeField] private float baseRotationSpeed = 180f; // Degrees per second
    [SerializeField] private float baseDamage = 15f;
    [SerializeField] private float baseDamageCooldown = 0.5f; // Cooldown per enemy before they can be hit again
    
    [Header("Orbiting Object Settings")]
    [SerializeField] private GameObject orbitingObjectPrefab;
    [SerializeField] private float objectScale = 0.5f;
    [SerializeField] private Color orbitColor = new Color(1f, 0.8f, 0.2f, 1f); // Golden yellow
    
    [Header("Visual Settings")]
    [SerializeField] private bool showDebugGizmos = true;
    
    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioSource audioSource;
    
    // Custom stat names for upgrade data
    private const string STAT_ORBIT_COUNT = "orbitCount";
    private const string STAT_ORBIT_RADIUS = "orbitRadius";
    private const string STAT_ROTATION_SPEED = "rotationSpeed";
    
    private Transform playerTransform;
    private List<GameObject> orbitingObjects = new List<GameObject>();
    private float currentAngle = 0f;
    private Dictionary<AppleEnemy, float> enemyHitCooldowns = new Dictionary<AppleEnemy, float>();
    private List<AppleEnemy> cooldownsToRemove = new List<AppleEnemy>();
    
    protected override void Awake()
    {
        base.Awake();
        
        // Find player transform
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        // Get or add audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound
            }
        }
    }
    
    protected override void ActivateAbility()
    {
        base.ActivateAbility();
        CreateOrbitingObjects();
        Debug.Log($"OrbitingAbility: Activated at level {currentLevel} with {GetOrbitCount()} orbiting objects");
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (!isActive || isFrozen) return;
        
        // Update rotation angle
        float rotationSpeed = GetRotationSpeed();
        currentAngle += rotationSpeed * Time.deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;
        
        // Update orbiting object positions
        UpdateOrbitingObjectPositions();
        
        // Check for collisions with enemies
        CheckEnemyCollisions();
        
        // Update cooldowns
        UpdateCooldowns();
    }
    
    /// <summary>
    /// Creates the orbiting objects around the player
    /// </summary>
    private void CreateOrbitingObjects()
    {
        // Clear existing objects
        ClearOrbitingObjects();
        
        int orbitCount = GetOrbitCount();
        float angleStep = 360f / orbitCount;
        
        for (int i = 0; i < orbitCount; i++)
        {
            GameObject orbitObj;
            
            if (orbitingObjectPrefab != null)
            {
                orbitObj = Instantiate(orbitingObjectPrefab);
            }
            else
            {
                // Create a simple visual if no prefab assigned
                orbitObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orbitObj.name = $"OrbitingObject_{i}";
                
                // Set up material
                Renderer renderer = orbitObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = orbitColor;
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", orbitColor * 0.5f);
                    renderer.material = mat;
                }
                
                // Remove default collider - we handle collision detection manually
                Collider col = orbitObj.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }
            
            orbitObj.transform.localScale = Vector3.one * objectScale;
            orbitObj.transform.SetParent(transform);
            
            orbitingObjects.Add(orbitObj);
        }
        
        // Initial position update
        UpdateOrbitingObjectPositions();
    }
    
    /// <summary>
    /// Updates the positions of all orbiting objects
    /// </summary>
    private void UpdateOrbitingObjectPositions()
    {
        if (playerTransform == null || orbitingObjects.Count == 0) return;
        
        float radius = GetOrbitRadius();
        int count = orbitingObjects.Count;
        float angleStep = 360f / count;
        
        for (int i = 0; i < count; i++)
        {
            if (orbitingObjects[i] == null) continue;
            
            float angle = currentAngle + (angleStep * i);
            float radians = angle * Mathf.Deg2Rad;
            
            Vector3 offset = new Vector3(
                Mathf.Cos(radians) * radius,
                0.5f, // Slight height offset
                Mathf.Sin(radians) * radius
            );
            
            orbitingObjects[i].transform.position = playerTransform.position + offset;
            
            // Make the object face the direction of movement (tangent to orbit)
            Vector3 tangent = new Vector3(-Mathf.Sin(radians), 0, Mathf.Cos(radians));
            if (tangent.sqrMagnitude > 0.01f)
            {
                orbitingObjects[i].transform.rotation = Quaternion.LookRotation(tangent);
            }
        }
    }
    
    /// <summary>
    /// Checks for collisions between orbiting objects and enemies
    /// </summary>
    private void CheckEnemyCollisions()
    {
        if (playerTransform == null) return;
        
        float damage = GetOrbitDamage();
        float hitRadius = objectScale * 0.75f; // Collision radius based on object size
        float hitRadiusSqr = hitRadius * hitRadius;
        
        AppleEnemy[] allEnemies = FindObjectsByType<AppleEnemy>(FindObjectsSortMode.None);
        
        foreach (GameObject orbitObj in orbitingObjects)
        {
            if (orbitObj == null) continue;
            
            Vector3 orbitPos = orbitObj.transform.position;
            
            foreach (AppleEnemy enemy in allEnemies)
            {
                if (enemy == null || enemy.IsFrozen()) continue;
                
                // Check if enemy is on cooldown
                if (enemyHitCooldowns.ContainsKey(enemy) && enemyHitCooldowns[enemy] > 0)
                    continue;
                
                // Check distance
                float distanceSqr = (enemy.transform.position - orbitPos).sqrMagnitude;
                
                // Account for enemy size (approximate)
                float enemyRadius = 0.5f;
                float totalRadiusSqr = (hitRadius + enemyRadius) * (hitRadius + enemyRadius);
                
                if (distanceSqr <= totalRadiusSqr)
                {
                    // Hit the enemy
                    enemy.TakeDamage(damage);
                    
                    // Set cooldown for this enemy
                    enemyHitCooldowns[enemy] = GetDamageCooldown();
                    
                    // Play hit sound
                    if (hitSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(hitSound, 0.5f);
                    }
                    
                    Debug.Log($"OrbitingAbility hit {enemy.name} for {damage} damage!");
                }
            }
        }
    }
    
    /// <summary>
    /// Updates the cooldown timers for enemies
    /// </summary>
    private void UpdateCooldowns()
    {
        cooldownsToRemove.Clear();
        
        List<AppleEnemy> keys = new List<AppleEnemy>(enemyHitCooldowns.Keys);
        foreach (AppleEnemy enemy in keys)
        {
            if (enemy == null)
            {
                cooldownsToRemove.Add(enemy);
                continue;
            }
            
            enemyHitCooldowns[enemy] -= Time.deltaTime;
            
            if (enemyHitCooldowns[enemy] <= 0)
            {
                cooldownsToRemove.Add(enemy);
            }
        }
        
        foreach (AppleEnemy enemy in cooldownsToRemove)
        {
            enemyHitCooldowns.Remove(enemy);
        }
    }
    
    /// <summary>
    /// Clears all orbiting objects
    /// </summary>
    private void ClearOrbitingObjects()
    {
        foreach (GameObject obj in orbitingObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        orbitingObjects.Clear();
    }
    
    /// <summary>
    /// Gets the current orbit count based on level
    /// </summary>
    private int GetOrbitCount()
    {
        if (upgradeData != null)
        {
            int count = Mathf.RoundToInt(GetCustomStat(STAT_ORBIT_COUNT, baseOrbitCount));
            return Mathf.Max(1, count);
        }
        return baseOrbitCount + (currentLevel - 1); // +1 object per level
    }
    
    /// <summary>
    /// Gets the current orbit radius based on level
    /// </summary>
    private float GetOrbitRadius()
    {
        float radius;
        if (upgradeData != null)
        {
            radius = GetCustomStat(STAT_ORBIT_RADIUS, baseOrbitRadius);
        }
        else
        {
            radius = baseOrbitRadius + (currentLevel - 1) * 0.5f; // +0.5 radius per level
        }
        
        // Apply range multiplier from PlayerStats
        float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetRangeMultiplier() : 1f;
        return radius * multiplier;
    }
    
    /// <summary>
    /// Gets the current rotation speed based on level
    /// </summary>
    private float GetRotationSpeed()
    {
        if (upgradeData != null)
        {
            float speed = GetCustomStat(STAT_ROTATION_SPEED, baseRotationSpeed);
            return speed > 0 ? speed : baseRotationSpeed;
        }
        return baseRotationSpeed + (currentLevel - 1) * 20f; // +20 degrees/sec per level
    }
    
    /// <summary>
    /// Gets the current damage based on level
    /// </summary>
    private float GetOrbitDamage()
    {
        float damage;
        if (upgradeData != null)
        {
            damage = GetDamage();
        }
        else
        {
            damage = baseDamage + (currentLevel - 1) * 5f; // +5 damage per level
        }
        
        // Apply damage multiplier from PlayerStats
        float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetDamageMultiplier() : 1f;
        return damage * multiplier;
    }
    
    /// <summary>
    /// Gets the damage cooldown (how long before an enemy can be hit again)
    /// </summary>
    private float GetDamageCooldown()
    {
        if (upgradeData != null)
        {
            float cooldown = GetCooldown();
            return cooldown > 0 ? cooldown : baseDamageCooldown;
        }
        return Mathf.Max(0.2f, baseDamageCooldown - (currentLevel - 1) * 0.05f);
    }
    
    protected override void OnLevelUp()
    {
        base.OnLevelUp();
        
        // Recreate orbiting objects with new count
        CreateOrbitingObjects();
        
        Debug.Log($"OrbitingAbility: Level {currentLevel} - Objects: {GetOrbitCount()}, Radius: {GetOrbitRadius():F1}, Speed: {GetRotationSpeed():F0}, Damage: {GetOrbitDamage():F0}");
    }
    
    protected override void DeactivateAbility()
    {
        base.DeactivateAbility();
        ClearOrbitingObjects();
        enemyHitCooldowns.Clear();
    }
    
    /// <summary>
    /// Debug visualization
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        Transform drawTransform = playerTransform;
        if (drawTransform == null)
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
            {
                drawTransform = player.transform;
            }
        }
        
        if (drawTransform != null)
        {
            float radius = Application.isPlaying ? GetOrbitRadius() : baseOrbitRadius;
            
            // Draw orbit path
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.5f);
            
            int segments = 32;
            float angleStep = 360f / segments;
            Vector3 prevPoint = drawTransform.position + new Vector3(radius, 0.5f, 0);
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 point = drawTransform.position + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0.5f,
                    Mathf.Sin(angle) * radius
                );
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
            
            // Draw orbiting object positions
            int orbitCount = Application.isPlaying ? GetOrbitCount() : baseOrbitCount;
            float objAngleStep = 360f / orbitCount;
            
            Gizmos.color = orbitColor;
            for (int i = 0; i < orbitCount; i++)
            {
                float angle = (Application.isPlaying ? currentAngle : 0) + (objAngleStep * i);
                float radians = angle * Mathf.Deg2Rad;
                Vector3 pos = drawTransform.position + new Vector3(
                    Mathf.Cos(radians) * radius,
                    0.5f,
                    Mathf.Sin(radians) * radius
                );
                Gizmos.DrawWireSphere(pos, objectScale * 0.5f);
            }
        }
    }
}