
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Active ability that creates a force field around the snake's head.
/// Enemies that enter the force field take damage over time (like Garlic in Vampire Survivors).
/// Uses AbilityUpgradeData for level-based stats.
/// </summary>
public class ForceFieldAbility : BaseAbility
{
    [Header("Force Field Settings (Fallback if no UpgradeData)")]
    [SerializeField] private float baseRadius = 3f;
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float baseDamageInterval = 0.5f; // How often to deal damage
    
    [Header("Visual Settings")]
    [SerializeField] private Color forceFieldColor = new Color(0.3f, 0.7f, 1f, 0.4f); // Light blue
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private float visualYOffset = -0.5f; // How far below the snake the visual sits
    
    [Header("Pulse Animation")]
    [SerializeField] private float pulseSpeed = 2f; // How fast the circle pulses
    [SerializeField] [Range(0f, 0.5f)] private float pulseAmount = 0.1f; // Size variation (0 = no pulse, 0.1 = 10% variation)
    
    [Header("Audio")]
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioSource audioSource;
    
    // Custom stat names for upgrade data
    private const string STAT_RADIUS = "radius";
    
    private Transform playerTransform;
    private GameObject forceFieldVisual;
    private ParticleSystem forceFieldParticleSystem;
    private float damageTimer = 0f;
    private HashSet<AppleEnemy> enemiesInField = new HashSet<AppleEnemy>();
    private List<AppleEnemy> enemiesToRemove = new List<AppleEnemy>();
    
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
        
        // Re-check for player transform in case it wasn't found in Awake
        if (playerTransform == null)
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("ForceFieldAbility: Could not find PlayerMovement! Ability will not work.");
                return;
            }
        }
        
        CreateForceFieldVisual();
        
        // Play force field sound (looping)
        SoundManager.Play("ForceField", gameObject);
        
        Debug.Log($"ForceFieldAbility: Activated at level {currentLevel} with radius {GetRadius()}");
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (!isActive || isFrozen) return;
        
        // Update visual position
        UpdateVisualPosition();
        
        // Find enemies in range
        FindEnemiesInField();
        
        // Deal damage at intervals
        damageTimer += Time.deltaTime;
        float currentInterval = GetDamageInterval();
        
        if (damageTimer >= currentInterval)
        {
            DealDamageToEnemiesInField();
            damageTimer = 0f;
        }
    }
    
    /// <summary>
    /// Creates the visual representation of the force field - a solid circular aura underneath the snake
    /// Similar to Garlic in Vampire Survivors or Forcefield in Survivor.io
    /// </summary>
    private void CreateForceFieldVisual()
    {
        // Always create the circle visual for the Survivor.io/Vampire Survivors style effect
        // This creates a solid circular aura underneath the snake
        CreateDefaultParticleSystem();
        
        UpdateVisualScale();
    }
    
    /// <summary>
    /// Creates a default visual for the force field - a solid circular aura underneath the snake
    /// Similar to Garlic in Vampire Survivors or Forcefield in Survivor.io
    /// </summary>
    private void CreateDefaultParticleSystem()
    {
        forceFieldVisual = new GameObject("ForceFieldCircleVisual");
        forceFieldVisual.transform.SetParent(transform);
        forceFieldVisual.transform.position = playerTransform.position;
        
        // Create a flat disc mesh for the solid circle effect
        MeshFilter meshFilter = forceFieldVisual.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = forceFieldVisual.AddComponent<MeshRenderer>();
        
        // Create the circle mesh
        meshFilter.mesh = CreateCircleMesh(64); // 64 segments for smooth circle
        
        // Create a transparent material for the circle
        Material circleMat = CreateCircleMaterial();
        meshRenderer.material = circleMat;
        
        // Rotate to be horizontal (flat on the ground)
        forceFieldVisual.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        
        // Add a subtle pulsing animation
        StartCoroutine(PulseCircleVisual());
    }
    
    /// <summary>
    /// Creates a circle mesh with the specified number of segments
    /// </summary>
    private Mesh CreateCircleMesh(int segments)
    {
        Mesh mesh = new Mesh();
        mesh.name = "ForceFieldCircle";
        
        // Vertices: center + edge vertices
        Vector3[] vertices = new Vector3[segments + 1];
        Vector2[] uvs = new Vector2[segments + 1];
        int[] triangles = new int[segments * 3];
        
        // Center vertex
        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);
        
        // Edge vertices
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);
            
            vertices[i + 1] = new Vector3(x, y, 0f);
            uvs[i + 1] = new Vector2(x * 0.5f + 0.5f, y * 0.5f + 0.5f);
        }
        
        // Triangles (fan from center)
        for (int i = 0; i < segments; i++)
        {
            int triIndex = i * 3;
            triangles[triIndex] = 0; // Center
            triangles[triIndex + 1] = i + 1;
            triangles[triIndex + 2] = (i + 1) % segments + 1;
        }
        
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    /// <summary>
    /// Creates a material for the circle with radial gradient effect
    /// </summary>
    private Material CreateCircleMaterial()
    {
        // Try to find a suitable shader for transparency
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Transparent");
        }
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        
        Material mat = new Material(shader);
        
        // Create a radial gradient texture for the circle
        Texture2D gradientTex = CreateRadialGradientTexture(128);
        mat.mainTexture = gradientTex;
        mat.color = forceFieldColor;
        
        // Enable transparency settings if available
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1); // Transparent
        }
        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 3); // Transparent mode for Standard shader
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
        
        return mat;
    }
    
    /// <summary>
    /// Creates a radial gradient texture (solid in center, fading to transparent at edges)
    /// </summary>
    private Texture2D CreateRadialGradientTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        
        float halfSize = size / 2f;
        Color[] pixels = new Color[size * size];
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - halfSize) / halfSize;
                float dy = (y - halfSize) / halfSize;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                
                // Create a smooth gradient from center to edge
                // Solid in the center (0-0.3), then fade out (0.3-1.0)
                float alpha;
                if (distance < 0.3f)
                {
                    alpha = 1f;
                }
                else if (distance < 1f)
                {
                    // Smooth fade from 0.3 to 1.0
                    alpha = 1f - ((distance - 0.3f) / 0.7f);
                    alpha = alpha * alpha; // Quadratic falloff for smoother look
                }
                else
                {
                    alpha = 0f;
                }
                
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return texture;
    }
    
    /// <summary>
    /// Coroutine that creates a subtle pulsing effect on the circle
    /// </summary>
    private System.Collections.IEnumerator PulseCircleVisual()
    {
        while (forceFieldVisual != null && isActive)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            float radius = GetRadius() * pulse;
            
            // Update scale (the mesh is unit-sized, so scale directly by radius)
            forceFieldVisual.transform.localScale = new Vector3(radius, radius, 1f);
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Creates a cylinder visual as fallback
    /// </summary>
    private void CreateCylinderVisual()
    {
        forceFieldVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        forceFieldVisual.name = "ForceFieldVisual";
        forceFieldVisual.transform.SetParent(transform);
        
        // Remove collider - we handle collision detection manually
        Collider col = forceFieldVisual.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        // Set up material with transparency
        Renderer renderer = forceFieldVisual.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Try to find a transparent shader
            Shader transparentShader = Shader.Find("Sprites/Default");
            if (transparentShader == null)
            {
                transparentShader = Shader.Find("Universal Render Pipeline/Lit");
            }
            
            Material mat = new Material(transparentShader);
            mat.color = forceFieldColor;
            
            // Enable transparency if using URP shader
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1); // 1 = Transparent
                mat.SetFloat("_Blend", 0); // Alpha blend
            }
            
            renderer.material = mat;
        }
    }
    
    /// <summary>
    /// Updates the visual position to follow the player, positioned underneath and parallel to the snake head
    /// </summary>
    private void UpdateVisualPosition()
    {
        if (forceFieldVisual != null && playerTransform != null)
        {
            // Position the visual underneath the snake
            Vector3 position = playerTransform.position;
            position.y += visualYOffset;
            forceFieldVisual.transform.position = position;
            
            // Make the circle parallel to the snake head by matching its rotation
            // The circle mesh is created in the XY plane, so we need to rotate it to be horizontal
            // and then apply the snake's Y rotation to keep it aligned with the snake's facing direction
            float snakeYRotation = playerTransform.eulerAngles.y;
            forceFieldVisual.transform.rotation = Quaternion.Euler(90f, snakeYRotation, 0f);
        }
    }
    
    /// <summary>
    /// Updates the visual scale based on current radius
    /// </summary>
    private void UpdateVisualScale()
    {
        if (forceFieldVisual == null) return;
        
        float radius = GetRadius();
        
        // If using particle system from prefab, update the shape radius
        if (forceFieldParticleSystem != null)
        {
            var shape = forceFieldParticleSystem.shape;
            shape.radius = radius;
            
            // Scale the visual object to match radius
            forceFieldVisual.transform.localScale = Vector3.one * (radius / baseRadius);
        }
        else if (forceFieldVisual.GetComponent<MeshFilter>() != null)
        {
            // Mesh-based circle visual - scale directly by radius
            // The mesh is unit-sized (radius 1), so scale equals radius
            forceFieldVisual.transform.localScale = new Vector3(radius, radius, 1f);
        }
        else
        {
            // Cylinder primitive fallback: X and Z control diameter, Y controls height
            // We want a flat disc, so make Y very small and X/Z large
            forceFieldVisual.transform.localScale = new Vector3(radius * 2f, 0.2f, radius * 2f);
        }
    }
    
    /// <summary>
    /// Finds all enemies within the force field radius
    /// </summary>
    private void FindEnemiesInField()
    {
        if (playerTransform == null) return;
        
        float currentRadius = GetRadius();
        float radiusSqr = currentRadius * currentRadius;
        
        // Clear enemies that are no longer in range or destroyed
        enemiesToRemove.Clear();
        foreach (AppleEnemy enemy in enemiesInField)
        {
            if (enemy == null)
            {
                enemiesToRemove.Add(enemy);
                continue;
            }
            
            float distanceSqr = (enemy.transform.position - playerTransform.position).sqrMagnitude;
            if (distanceSqr > radiusSqr)
            {
                enemiesToRemove.Add(enemy);
            }
        }
        
        foreach (AppleEnemy enemy in enemiesToRemove)
        {
            enemiesInField.Remove(enemy);
        }
        
        // Find new enemies in range
        AppleEnemy[] allEnemies = FindObjectsByType<AppleEnemy>(FindObjectsSortMode.None);
        
        foreach (AppleEnemy enemy in allEnemies)
        {
            if (enemy == null || enemy.IsFrozen()) continue;
            
            float distanceSqr = (enemy.transform.position - playerTransform.position).sqrMagnitude;
            if (distanceSqr <= radiusSqr)
            {
                enemiesInField.Add(enemy);
            }
        }
    }
    
    /// <summary>
    /// Deals damage to all enemies currently in the force field
    /// </summary>
    private void DealDamageToEnemiesInField()
    {
        if (enemiesInField.Count == 0) return;
        
        float currentDamage = GetFieldDamage();
        bool playedSound = false;
        int enemiesHit = 0;
        
        enemiesToRemove.Clear();
        
        foreach (AppleEnemy enemy in enemiesInField)
        {
            if (enemy == null)
            {
                enemiesToRemove.Add(enemy);
                continue;
            }
            
            enemy.TakeDamage(currentDamage);
            enemiesHit++;
            
            if (!playedSound && damageSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(damageSound);
                playedSound = true;
            }
        }
        
        // Clean up null references
        foreach (AppleEnemy enemy in enemiesToRemove)
        {
            enemiesInField.Remove(enemy);
        }
        
        if (enemiesHit > 0)
        {
            Debug.Log($"ForceFieldAbility: Dealt {currentDamage:F1} damage to {enemiesHit} enemies!");
        }
    }
    
    /// <summary>
    /// Gets the current radius based on level
    /// </summary>
    private float GetRadius()
    {
        float radius;
        if (upgradeData != null)
        {
            radius = GetCustomStat(STAT_RADIUS, baseRadius);
        }
        else
        {
            radius = baseRadius + (currentLevel - 1) * 1f; // +1 radius per level
        }
        
        // Apply range multiplier from PlayerStats
        float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetRangeMultiplier() : 1f;
        return radius * multiplier;
    }
    
    /// <summary>
    /// Gets the current damage based on level
    /// </summary>
    private float GetFieldDamage()
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
    /// Gets the current damage interval based on level
    /// </summary>
    private float GetDamageInterval()
    {
        if (upgradeData != null)
        {
            float cooldown = GetCooldown();
            return cooldown > 0 ? cooldown : baseDamageInterval;
        }
        return Mathf.Max(0.2f, baseDamageInterval - (currentLevel - 1) * 0.05f); // Faster at higher levels
    }
    
    protected override void OnLevelUp()
    {
        base.OnLevelUp();
        UpdateVisualScale();
        Debug.Log($"ForceFieldAbility: Level {currentLevel} - Radius: {GetRadius():F1}, Damage: {GetFieldDamage():F0}, Interval: {GetDamageInterval():F2}s");
    }
    
    protected override void DeactivateAbility()
    {
        base.DeactivateAbility();
        
        // Stop force field sound
        SoundManager.Stop("ForceField", gameObject);
        
        if (forceFieldVisual != null)
        {
            Destroy(forceFieldVisual);
        }
        
        enemiesInField.Clear();
    }
    
    /// <summary>
    /// Debug visualization - shows the force field as a flat disc
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
            float radius = Application.isPlaying ? GetRadius() : baseRadius;
            Vector3 center = drawTransform.position + Vector3.up * visualYOffset;
            
            // Draw a flat disc gizmo
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.3f);
            
            // Draw circle outline
            int segments = 32;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);
            for (int i = 1; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
            
            // Draw cross lines for visibility
            Gizmos.DrawLine(center + new Vector3(-radius, 0, 0), center + new Vector3(radius, 0, 0));
            Gizmos.DrawLine(center + new Vector3(0, 0, -radius), center + new Vector3(0, 0, radius));
        }
    }
}