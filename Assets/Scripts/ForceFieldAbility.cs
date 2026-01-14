
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Active ability that creates a force field around the snake's head.
/// Enemies that enter the force field take damage over time (like Garlic in Vampire Survivors).
/// Uses AbilityUpgradeData for level-based stats.
/// Visual: Base radial gradient circle with 3 concentric emissive circles on top.
/// </summary>
public class ForceFieldAbility : BaseAbility
{
    [Header("Force Field Settings (Fallback if no UpgradeData)")]
    [SerializeField] private float baseRadius = 3f;
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float baseDamageInterval = 0.5f;

    [Header("Visual Settings")]
    [SerializeField] private Color forceFieldColor = new Color(0.3f, 0.7f, 1f, 1f);
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private float visualYOffset = -0.5f;
    [SerializeField] private int numberOfCircles = 3;
    [SerializeField] private float lineWidth = 0.15f;
    [SerializeField] [Range(0f, 10f)] private float emissionIntensity = 3f;
    [SerializeField] [Range(0f, 1f)] private float baseCircleAlpha = 0.4f;

    [Header("Pulse Animation")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] [Range(0f, 0.5f)] private float pulseAmount = 0.1f;
    [SerializeField] private float rotationSpeed = 30f;

    [Header("Audio")]
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioSource audioSource;

    private const string STAT_RADIUS = "radius";

    private Transform playerTransform;
    private GameObject forceFieldVisual;
    private GameObject baseCircleVisual;
    private ParticleSystem forceFieldParticleSystem;
    private float damageTimer = 0f;
    private HashSet<AppleEnemy> enemiesInField = new HashSet<AppleEnemy>();
    private List<AppleEnemy> enemiesToRemove = new List<AppleEnemy>();
    private List<LineRenderer> circleLineRenderers = new List<LineRenderer>();
    private Material emissiveMaterial;
    private Material baseCircleMaterial;

    protected override void Awake()
    {
        base.Awake();

        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            playerTransform = player.transform;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }
        }
    }

    protected override void ActivateAbility()
    {
        base.ActivateAbility();

        if (playerTransform == null)
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("ForceFieldAbility: Could not find PlayerMovement!");
                return;
            }
        }

        CreateForceFieldVisual();
        SoundManager.Play("ForceField", gameObject);
        Debug.Log($"ForceFieldAbility: Activated at level {currentLevel} with radius {GetRadius()}");
    }

    protected override void Update()
    {
        base.Update();

        if (!isActive || isFrozen) return;

        UpdateVisualPosition();
        FindEnemiesInField();

        damageTimer += Time.deltaTime;
        float currentInterval = GetDamageInterval();

        if (damageTimer >= currentInterval)
        {
            DealDamageToEnemiesInField();
            damageTimer = 0f;
        }
    }

    private void CreateForceFieldVisual()
    {
        CreateBaseCircle();
        CreateConcentricCircles();
        UpdateVisualScale();
    }

    private void CreateBaseCircle()
    {
        baseCircleVisual = new GameObject("ForceFieldBaseCircle");
        baseCircleVisual.transform.SetParent(transform);
        baseCircleVisual.transform.position = playerTransform.position;

        MeshFilter meshFilter = baseCircleVisual.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = baseCircleVisual.AddComponent<MeshRenderer>();

        meshFilter.mesh = CreateCircleMesh(64);
        baseCircleMaterial = CreateCircleMaterial();
        meshRenderer.material = baseCircleMaterial;

        baseCircleVisual.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private Mesh CreateCircleMesh(int segments)
    {
        Mesh mesh = new Mesh();
        mesh.name = "ForceFieldCircle";

        Vector3[] vertices = new Vector3[segments + 1];
        Vector2[] uvs = new Vector2[segments + 1];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);

            vertices[i + 1] = new Vector3(x, y, 0f);
            uvs[i + 1] = new Vector2(x * 0.5f + 0.5f, y * 0.5f + 0.5f);
        }

        for (int i = 0; i < segments; i++)
        {
            int triIndex = i * 3;
            triangles[triIndex] = 0;
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

    private Material CreateCircleMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);

        Texture2D gradientTex = CreateRadialGradientTexture(128);
        mat.mainTexture = gradientTex;

        Color baseColor = forceFieldColor;
        baseColor.a = baseCircleAlpha;
        mat.color = baseColor;

        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1);
        }
        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 3);
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

                float alpha;
                if (distance < 0.5f)
                {
                    // Solid center - larger solid area
                    alpha = 1f;
                }
                else if (distance < 1f)
                {
                    // Fade from 1.0 to 0.3 (less transparent at edges)
                    // Linear fade instead of quadratic for less aggressive falloff
                    alpha = 1f - ((distance - 0.5f) / 0.5f) * 0.7f;
                }
                else
                {
                    alpha = 0.3f;
                }

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return texture;
    }

    private void CreateConcentricCircles()
    {
        forceFieldVisual = new GameObject("ForceFieldCircleVisual");
        forceFieldVisual.transform.SetParent(transform);
        forceFieldVisual.transform.position = playerTransform.position;

        emissiveMaterial = CreateEmissiveMaterial();

        circleLineRenderers.Clear();
        float radius = GetRadius();

        for (int i = 0; i < numberOfCircles; i++)
        {
            float circleRadius = radius * ((float)(i + 1) / numberOfCircles);

            GameObject circleObj = new GameObject($"Circle_{i}");
            circleObj.transform.SetParent(forceFieldVisual.transform);
            circleObj.transform.localPosition = Vector3.zero;

            LineRenderer lineRenderer = circleObj.AddComponent<LineRenderer>();
            SetupLineRenderer(lineRenderer, circleRadius);
            circleLineRenderers.Add(lineRenderer);
        }

        forceFieldVisual.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        StartCoroutine(PulseCircleVisual());
    }

    private void SetupLineRenderer(LineRenderer lineRenderer, float radius)
    {
        int segments = 64;

        lineRenderer.material = emissiveMaterial;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments;

        Vector3[] positions = new Vector3[segments];
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            positions[i] = new Vector3(x, y, 0f);
        }
        lineRenderer.SetPositions(positions);

        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
    }

    private Material CreateEmissiveMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);

        mat.color = forceFieldColor;

        Color emissionColor = forceFieldColor * emissionIntensity;

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        if (mat.HasProperty("_EmissiveColor"))
        {
            mat.SetColor("_EmissiveColor", emissionColor);
        }

        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", forceFieldColor);
        }

        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1);
        }
        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 3);
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

    private void UpdateCircleRadii(float radius)
    {
        for (int i = 0; i < circleLineRenderers.Count; i++)
        {
            LineRenderer lineRenderer = circleLineRenderers[i];
            if (lineRenderer == null) continue;

            float circleRadius = radius * ((float)(i + 1) / numberOfCircles);

            int segments = lineRenderer.positionCount;
            Vector3[] positions = new Vector3[segments];
            for (int j = 0; j < segments; j++)
            {
                float angle = (float)j / segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * circleRadius;
                float y = Mathf.Sin(angle) * circleRadius;
                positions[j] = new Vector3(x, y, 0f);
            }
            lineRenderer.SetPositions(positions);
        }
    }

    private System.Collections.IEnumerator PulseCircleVisual()
    {
        float rotationAngle = 0f;

        while (forceFieldVisual != null && isActive)
        {
            // Calculate the pulse value - same for both base circle and concentric circles
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            float baseRadiusValue = GetRadius();
            float pulsedRadius = baseRadiusValue * pulse;

            // Update concentric circle radii - outermost circle should match base circle size
            UpdateCircleRadii(pulsedRadius);

            // Update base circle scale - should be same size as outermost concentric circle
            if (baseCircleVisual != null)
            {
                baseCircleVisual.transform.localScale = new Vector3(pulsedRadius, pulsedRadius, 1f);
            }

            // Rotate the concentric circles for a dynamic effect
            rotationAngle += rotationSpeed * Time.deltaTime;
            if (rotationAngle >= 360f) rotationAngle -= 360f;

            forceFieldVisual.transform.localRotation = Quaternion.Euler(90f, rotationAngle, 0f);

            // Pulse the emission intensity in sync with the size pulse
            if (emissiveMaterial != null && emissiveMaterial.HasProperty("_EmissionColor"))
            {
                // Use the same pulse value for emission to keep everything synchronized
                Color emissionColor = forceFieldColor * emissionIntensity * pulse;
                emissiveMaterial.SetColor("_EmissionColor", emissionColor);
            }

            yield return null;
        }
    }

    private void UpdateVisualPosition()
    {
        if (playerTransform != null)
        {
            Vector3 position = playerTransform.position;
            position.y += visualYOffset;

            if (forceFieldVisual != null)
            {
                forceFieldVisual.transform.position = position;
            }

            if (baseCircleVisual != null)
            {
                baseCircleVisual.transform.position = position;
            }
        }
    }

    private void UpdateVisualScale()
    {
        float radius = GetRadius();

        if (baseCircleVisual != null)
        {
            baseCircleVisual.transform.localScale = new Vector3(radius, radius, 1f);
        }

        if (circleLineRenderers.Count > 0)
        {
            UpdateCircleRadii(radius);
        }

        if (forceFieldParticleSystem != null)
        {
            var shape = forceFieldParticleSystem.shape;
            shape.radius = radius;

            if (forceFieldVisual != null)
            {
                forceFieldVisual.transform.localScale = Vector3.one * (radius / baseRadius);
            }
        }
    }

    private void FindEnemiesInField()
    {
        if (playerTransform == null) return;

        float currentRadius = GetRadius();
        float radiusSqr = currentRadius * currentRadius;

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

        var allEnemies = AppleEnemy.GetAllActiveEnemies();

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

        foreach (AppleEnemy enemy in enemiesToRemove)
        {
            enemiesInField.Remove(enemy);
        }

        if (enemiesHit > 0)
        {
            Debug.Log($"ForceFieldAbility: Dealt {currentDamage:F1} damage to {enemiesHit} enemies!");
        }
    }

    private float GetRadius()
    {
        float radius;
        if (upgradeData != null)
        {
            radius = GetCustomStat(STAT_RADIUS, baseRadius);
        }
        else
        {
            radius = baseRadius + (currentLevel - 1) * 1f;
        }

        float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetRangeMultiplier() : 1f;
        return radius * multiplier;
    }

    private float GetFieldDamage()
    {
        float damage;
        if (upgradeData != null)
        {
            damage = GetDamage();
        }
        else
        {
            damage = baseDamage + (currentLevel - 1) * 5f;
        }

        float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetDamageMultiplier() : 1f;
        return damage * multiplier;
    }

    private float GetDamageInterval()
    {
        if (upgradeData != null)
        {
            float cooldown = GetCooldown();
            return cooldown > 0 ? cooldown : baseDamageInterval;
        }
        return Mathf.Max(0.2f, baseDamageInterval - (currentLevel - 1) * 0.05f);
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

        SoundManager.Stop("ForceField", gameObject);

        if (forceFieldVisual != null)
        {
            Destroy(forceFieldVisual);
        }

        if (baseCircleVisual != null)
        {
            Destroy(baseCircleVisual);
        }

        circleLineRenderers.Clear();

        if (emissiveMaterial != null)
        {
            Destroy(emissiveMaterial);
            emissiveMaterial = null;
        }

        if (baseCircleMaterial != null)
        {
            Destroy(baseCircleMaterial);
            baseCircleMaterial = null;
        }

        enemiesInField.Clear();
    }

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

            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.3f);
            Gizmos.DrawWireSphere(center, radius);

            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.5f);

            for (int c = 0; c < numberOfCircles; c++)
            {
                float circleRadius = radius * ((float)(c + 1) / numberOfCircles);

                int segments = 32;
                Vector3 prevPoint = center + new Vector3(circleRadius, 0, 0);
                for (int i = 1; i <= segments; i++)
                {
                    float angle = (float)i / segments * Mathf.PI * 2f;
                    Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * circleRadius, 0, Mathf.Sin(angle) * circleRadius);
                    Gizmos.DrawLine(prevPoint, newPoint);
                    prevPoint = newPoint;
                }
            }

            Gizmos.DrawLine(center + new Vector3(-radius, 0, 0), center + new Vector3(radius, 0, 0));
            Gizmos.DrawLine(center + new Vector3(0, 0, -radius), center + new Vector3(0, 0, radius));
        }
    }
}