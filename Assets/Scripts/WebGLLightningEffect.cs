using UnityEngine;
using System.Collections;

/// <summary>
/// WebGL-friendly lightning effect that uses LineRenderer and particle systems
/// instead of complex VFX Graph effects that don't render well on WebGL.
/// This is a lightweight alternative to the Vefects Zap VFX.
/// </summary>
public class WebGLLightningEffect : MonoBehaviour
{
    [Header("Lightning Bolt Settings")]
    [SerializeField] private int segmentCount = 8;
    [SerializeField] private float jaggedness = 0.5f;
    [SerializeField] private float boltWidth = 0.15f;
    [SerializeField] private float boltWidthEnd = 0.05f;
    
    [Header("Colors")]
    [SerializeField] private Color primaryColor = new Color(0.5f, 0.8f, 1f, 1f);
    [SerializeField] private Color secondaryColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color glowColor = new Color(0.3f, 0.6f, 1f, 0.5f);
    
    [Header("Animation")]
    [SerializeField] private float flickerSpeed = 20f;
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private int flickerCount = 3;
    [SerializeField] private bool autoDestroy = true;
    
    [Header("Glow Effect")]
    [SerializeField] private bool useGlowBolt = true;
    [SerializeField] private float glowWidthMultiplier = 2.5f;
    
    [Header("Impact Effect")]
    [SerializeField] private bool spawnImpactParticles = true;
    [SerializeField] private int impactParticleCount = 15;
    [SerializeField] private float impactParticleSpeed = 3f;
    
    private LineRenderer mainBolt;
    private LineRenderer glowBolt;
    private ParticleSystem impactParticles;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private float timer;
    private bool isActive;
    
    /// <summary>
    /// Creates and plays a lightning effect between two points
    /// </summary>
    public static WebGLLightningEffect Create(Vector3 start, Vector3 end, float duration = 0.3f)
    {
        GameObject effectObj = new GameObject("WebGLLightningEffect");
        WebGLLightningEffect effect = effectObj.AddComponent<WebGLLightningEffect>();
        effect.duration = duration;
        effect.Initialize(start, end);
        return effect;
    }
    
    /// <summary>
    /// Creates a lightning strike from above (sky) to target position
    /// </summary>
    public static WebGLLightningEffect CreateStrike(Vector3 targetPosition, float height = 15f, float duration = 0.3f)
    {
        Vector3 start = targetPosition + Vector3.up * height;
        // Add some random horizontal offset to the start position
        start += new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
        return Create(start, targetPosition, duration);
    }
    
    private void Awake()
    {
        CreateLineRenderers();
        CreateImpactParticles();
    }
    
    private void CreateLineRenderers()
    {
        // Create main lightning bolt
        GameObject mainBoltObj = new GameObject("MainBolt");
        mainBoltObj.transform.SetParent(transform);
        mainBolt = mainBoltObj.AddComponent<LineRenderer>();
        SetupLineRenderer(mainBolt, boltWidth, boltWidthEnd, primaryColor, secondaryColor);
        
        // Create glow bolt (wider, more transparent)
        if (useGlowBolt)
        {
            GameObject glowBoltObj = new GameObject("GlowBolt");
            glowBoltObj.transform.SetParent(transform);
            glowBolt = glowBoltObj.AddComponent<LineRenderer>();
            SetupLineRenderer(glowBolt, boltWidth * glowWidthMultiplier, boltWidthEnd * glowWidthMultiplier, glowColor, glowColor);
            glowBolt.sortingOrder = -1; // Render behind main bolt
        }
    }
    
    private void SetupLineRenderer(LineRenderer line, float startWidth, float endWidth, Color startColor, Color endColor)
    {
        line.positionCount = segmentCount;
        line.startWidth = startWidth;
        line.endWidth = endWidth;
        line.startColor = startColor;
        line.endColor = endColor;
        line.useWorldSpace = true;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        
        // Use a simple unlit shader that works on WebGL
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.SetColor("_Color", startColor);
        line.material = mat;
        
        // Set up gradient for better visual
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(startColor, 0.0f), 
                new GradientColorKey(endColor, 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1.0f, 0.0f), 
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(1.0f, 1.0f) 
            }
        );
        line.colorGradient = gradient;
    }
    
    private void CreateImpactParticles()
    {
        if (!spawnImpactParticles) return;
        
        GameObject particleObj = new GameObject("ImpactParticles");
        particleObj.transform.SetParent(transform);
        impactParticles = particleObj.AddComponent<ParticleSystem>();
        
        var main = impactParticles.main;
        main.startLifetime = 0.3f;
        main.startSpeed = impactParticleSpeed;
        main.startSize = 0.1f;
        main.startColor = primaryColor;
        main.maxParticles = impactParticleCount;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        
        var emission = impactParticles.emission;
        emission.enabled = false; // We'll use Emit() manually
        
        var shape = impactParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;
        
        var colorOverLifetime = impactParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient particleGradient = new Gradient();
        particleGradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(primaryColor, 0.0f), 
                new GradientColorKey(secondaryColor, 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1.0f, 0.0f), 
                new GradientAlphaKey(0.0f, 1.0f) 
            }
        );
        colorOverLifetime.color = particleGradient;
        
        var sizeOverLifetime = impactParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 0f);
        
        // Use simple particle material
        var renderer = impactParticles.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.material.SetColor("_Color", primaryColor);
    }
    
    /// <summary>
    /// Initialize the lightning effect with start and end positions
    /// </summary>
    public void Initialize(Vector3 start, Vector3 end)
    {
        startPosition = start;
        endPosition = end;
        transform.position = end;
        
        if (impactParticles != null)
        {
            impactParticles.transform.position = end;
        }
        
        isActive = true;
        timer = 0f;
        
        StartCoroutine(PlayEffect());
    }
    
    private IEnumerator PlayEffect()
    {
        float flickerDuration = duration / flickerCount;
        
        for (int i = 0; i < flickerCount; i++)
        {
            // Generate new bolt path
            GenerateBoltPath();
            
            // Show bolt
            SetBoltVisibility(true);
            
            // Spawn impact particles on first flicker
            if (i == 0 && impactParticles != null)
            {
                impactParticles.Emit(impactParticleCount);
            }
            
            // Wait for flicker
            yield return new WaitForSeconds(flickerDuration * 0.7f);
            
            // Hide bolt briefly (flicker effect)
            if (i < flickerCount - 1)
            {
                SetBoltVisibility(false);
                yield return new WaitForSeconds(flickerDuration * 0.3f);
            }
        }
        
        // Fade out
        yield return StartCoroutine(FadeOut(0.1f));
        
        if (autoDestroy)
        {
            Destroy(gameObject);
        }
    }
    
    private void GenerateBoltPath()
    {
        Vector3[] positions = new Vector3[segmentCount];
        positions[0] = startPosition;
        positions[segmentCount - 1] = endPosition;
        
        Vector3 direction = (endPosition - startPosition).normalized;
        float distance = Vector3.Distance(startPosition, endPosition);
        float segmentLength = distance / (segmentCount - 1);
        
        // Generate jagged path
        for (int i = 1; i < segmentCount - 1; i++)
        {
            float t = (float)i / (segmentCount - 1);
            Vector3 basePosition = Vector3.Lerp(startPosition, endPosition, t);
            
            // Add random offset perpendicular to the bolt direction
            Vector3 perpendicular1 = Vector3.Cross(direction, Vector3.up).normalized;
            Vector3 perpendicular2 = Vector3.Cross(direction, perpendicular1).normalized;
            
            float offsetAmount = jaggedness * segmentLength * (1f - Mathf.Abs(t - 0.5f) * 2f); // Less offset at ends
            Vector3 offset = perpendicular1 * Random.Range(-offsetAmount, offsetAmount) +
                           perpendicular2 * Random.Range(-offsetAmount, offsetAmount);
            
            positions[i] = basePosition + offset;
        }
        
        // Apply positions to line renderers
        mainBolt.SetPositions(positions);
        if (glowBolt != null)
        {
            glowBolt.SetPositions(positions);
        }
    }
    
    private void SetBoltVisibility(bool visible)
    {
        if (mainBolt != null)
        {
            mainBolt.enabled = visible;
        }
        if (glowBolt != null)
        {
            glowBolt.enabled = visible;
        }
    }
    
    private IEnumerator FadeOut(float fadeDuration)
    {
        float elapsed = 0f;
        Color startColorMain = mainBolt.startColor;
        Color endColorMain = mainBolt.endColor;
        Color startColorGlow = glowBolt != null ? glowBolt.startColor : Color.clear;
        Color endColorGlow = glowBolt != null ? glowBolt.endColor : Color.clear;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            float alpha = 1f - t;
            
            if (mainBolt != null)
            {
                mainBolt.startColor = new Color(startColorMain.r, startColorMain.g, startColorMain.b, startColorMain.a * alpha);
                mainBolt.endColor = new Color(endColorMain.r, endColorMain.g, endColorMain.b, endColorMain.a * alpha);
            }
            
            if (glowBolt != null)
            {
                glowBolt.startColor = new Color(startColorGlow.r, startColorGlow.g, startColorGlow.b, startColorGlow.a * alpha);
                glowBolt.endColor = new Color(endColorGlow.r, endColorGlow.g, endColorGlow.b, endColorGlow.a * alpha);
            }
            
            yield return null;
        }
        
        SetBoltVisibility(false);
    }
    
    /// <summary>
    /// Set custom colors for the lightning effect
    /// </summary>
    public void SetColors(Color primary, Color secondary, Color glow)
    {
        primaryColor = primary;
        secondaryColor = secondary;
        glowColor = glow;
        
        if (mainBolt != null)
        {
            mainBolt.startColor = primary;
            mainBolt.endColor = secondary;
            mainBolt.material.SetColor("_Color", primary);
        }
        
        if (glowBolt != null)
        {
            glowBolt.startColor = glow;
            glowBolt.endColor = glow;
            glowBolt.material.SetColor("_Color", glow);
        }
    }
}
