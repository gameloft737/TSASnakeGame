using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(ObjectiveTrigger))]
public class ObjectiveOutline : MonoBehaviour
{
    public enum OutlineMethod
    {
        FloatingIndicator,
        EmissionGlow,
        LightBeacon
    }

    [Header("Outline Method")]
    public OutlineMethod outlineMethod = OutlineMethod.FloatingIndicator;

    [Header("Outline Settings")]
    public Color outlineColor = new Color(1f, 0.8f, 0f, 1f);
    public bool pulseEffect = true;
    [Range(0.1f, 5f)] public float pulseSpeed = 1.5f;
    [Range(0.5f, 1f)] public float pulseMinScale = 0.8f;
    [Range(1f, 2f)] public float pulseMaxScale = 1.2f;

    [Header("Floating Indicator")]
    public float floatHeight = 2f;
    public float indicatorSize = 0.5f;
    public bool bobAnimation = true;
    public float bobAmplitude = 0.3f;
    public float bobSpeed = 2f;
    public bool rotateIndicator = true;
    public float rotationSpeed = 90f;

    [Header("Light Beacon")]
    public float beaconHeight = 10f;
    public float beaconWidth = 1f;
    [Range(0f, 5f)] public float beaconIntensity = 2f;

    [Header("Emission Glow")]
    [Range(0f, 10f)] public float emissionIntensity = 3f;

    [Header("Target Renderers")]
    public Renderer[] targetRenderers;

    [Header("Particle Effect")]
    public ParticleSystem glowParticles;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private ObjectiveTrigger objectiveTrigger;
    private bool isOutlineActive = false;
    private List<Material> createdMaterials = new List<Material>();
    private Dictionary<Renderer, Material[]> rendererOriginalMaterials = new Dictionary<Renderer, Material[]>();
    private Coroutine animationCoroutine;
    private GameObject floatingIndicator;
    private GameObject lightBeacon;
    private Light beaconLight;
    
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        objectiveTrigger = GetComponent<ObjectiveTrigger>();
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>();
        
        foreach (Renderer r in targetRenderers)
            if (r != null) rendererOriginalMaterials[r] = r.sharedMaterials;
        
        if (showDebugInfo)
            Debug.Log($"ObjectiveOutline [{gameObject.name}]: Found {targetRenderers.Length} renderers");
    }

    private void Start()
    {
        isOutlineActive = false;
        StartCoroutine(LateStart());
    }

    private IEnumerator LateStart()
    {
        yield return null;
        yield return null;
        
        if (showDebugInfo && ObjectiveManager.Instance != null)
            Debug.Log($"ObjectiveOutline [{gameObject.name}]: Current={ObjectiveManager.Instance.CurrentObjectiveIndex}, My={objectiveTrigger.myObjectiveIndex}");
        
        CheckAndUpdateOutline();
    }

    private void OnDisable() { DisableOutline(); }
    private void OnDestroy() { CleanupAll(); }

    private void CleanupAll()
    {
        foreach (Material m in createdMaterials) if (m != null) Destroy(m);
        createdMaterials.Clear();
        if (floatingIndicator != null) Destroy(floatingIndicator);
        if (lightBeacon != null) Destroy(lightBeacon);
    }

    public void CheckAndUpdateOutline()
    {
        if (ObjectiveManager.Instance == null) return;
        
        bool shouldBeActive = ObjectiveManager.Instance.CurrentObjectiveIndex == objectiveTrigger.myObjectiveIndex;
        
        if (showDebugInfo)
            Debug.Log($"ObjectiveOutline [{gameObject.name}]: shouldBeActive={shouldBeActive}, isActive={isOutlineActive}");
        
        if (shouldBeActive && !isOutlineActive) EnableOutline();
        else if (!shouldBeActive && isOutlineActive) DisableOutline();
    }

    public void EnableOutline()
    {
        if (isOutlineActive) return;
        isOutlineActive = true;

        if (showDebugInfo)
            Debug.Log($"ObjectiveOutline [{gameObject.name}]: Enabling with {outlineMethod}");

        switch (outlineMethod)
        {
            case OutlineMethod.FloatingIndicator: CreateFloatingIndicator(); break;
            case OutlineMethod.EmissionGlow: EnableEmissionGlow(); break;
            case OutlineMethod.LightBeacon: CreateLightBeacon(); break;
        }

        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateOutline());

        if (glowParticles != null) glowParticles.Play();
        
        Debug.Log($"ObjectiveOutline: ENABLED for {gameObject.name}");
    }

    public void DisableOutline()
    {
        if (!isOutlineActive) return;
        isOutlineActive = false;

        if (animationCoroutine != null) { StopCoroutine(animationCoroutine); animationCoroutine = null; }

        switch (outlineMethod)
        {
            case OutlineMethod.FloatingIndicator: if (floatingIndicator) { Destroy(floatingIndicator); floatingIndicator = null; } break;
            case OutlineMethod.EmissionGlow: RestoreOriginalMaterials(); break;
            case OutlineMethod.LightBeacon: if (lightBeacon) { Destroy(lightBeacon); lightBeacon = null; } break;
        }

        if (glowParticles != null) glowParticles.Stop();
        Debug.Log($"ObjectiveOutline: DISABLED for {gameObject.name}");
    }

    private void CreateFloatingIndicator()
    {
        if (floatingIndicator != null) return;

        floatingIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floatingIndicator.name = "ObjectiveIndicator";
        floatingIndicator.transform.SetParent(transform);
        floatingIndicator.transform.localPosition = new Vector3(0, floatHeight, 0);
        floatingIndicator.transform.localScale = Vector3.one * indicatorSize;
        floatingIndicator.transform.localRotation = Quaternion.Euler(45, 0, 45);

        Collider col = floatingIndicator.GetComponent<Collider>();
        if (col) Destroy(col);

        MeshRenderer rend = floatingIndicator.GetComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = outlineColor;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor(EmissionColorID, outlineColor * emissionIntensity);
        mat.renderQueue = 3100;
        rend.material = mat;
        createdMaterials.Add(mat);

        Debug.Log($"ObjectiveOutline: Created floating indicator at height {floatHeight}");
    }

    private void CreateLightBeacon()
    {
        if (lightBeacon != null) return;

        lightBeacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lightBeacon.name = "ObjectiveBeacon";
        lightBeacon.transform.SetParent(transform);
        lightBeacon.transform.localPosition = new Vector3(0, beaconHeight / 2f, 0);
        lightBeacon.transform.localScale = new Vector3(beaconWidth, beaconHeight / 2f, beaconWidth);

        Collider col = lightBeacon.GetComponent<Collider>();
        if (col) Destroy(col);

        MeshRenderer rend = lightBeacon.GetComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3000;
        Color c = outlineColor; c.a = 0.3f;
        mat.color = c;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor(EmissionColorID, outlineColor * beaconIntensity);
        rend.material = mat;
        createdMaterials.Add(mat);

        GameObject lightObj = new GameObject("BeaconLight");
        lightObj.transform.SetParent(lightBeacon.transform);
        lightObj.transform.localPosition = Vector3.zero;
        beaconLight = lightObj.AddComponent<Light>();
        beaconLight.type = LightType.Point;
        beaconLight.color = outlineColor;
        beaconLight.intensity = beaconIntensity;
        beaconLight.range = beaconHeight;

        Debug.Log($"ObjectiveOutline: Created light beacon");
    }

    private void EnableEmissionGlow()
    {
        foreach (Renderer r in targetRenderers)
        {
            if (r == null) continue;
            Material[] mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = new Material(mats[i]);
                m.EnableKeyword("_EMISSION");
                m.SetColor(EmissionColorID, outlineColor * emissionIntensity);
                mats[i] = m;
                createdMaterials.Add(m);
            }
            r.materials = mats;
        }
        Debug.Log($"ObjectiveOutline: Enabled emission glow");
    }

    private void RestoreOriginalMaterials()
    {
        foreach (var kvp in rendererOriginalMaterials)
            if (kvp.Key != null) kvp.Key.sharedMaterials = kvp.Value;
    }

    private IEnumerator AnimateOutline()
    {
        float time = 0f;
        float baseHeight = floatHeight;
        
        while (isOutlineActive)
        {
            time += Time.deltaTime;
            float pulse = Mathf.Lerp(pulseMinScale, pulseMaxScale, (Mathf.Sin(time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f);

            if (floatingIndicator != null)
            {
                if (pulseEffect)
                    floatingIndicator.transform.localScale = Vector3.one * indicatorSize * pulse;
                
                if (bobAnimation)
                {
                    float bob = Mathf.Sin(time * bobSpeed) * bobAmplitude;
                    floatingIndicator.transform.localPosition = new Vector3(0, baseHeight + bob, 0);
                }
                
                if (rotateIndicator)
                    floatingIndicator.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }

            if (lightBeacon != null && beaconLight != null)
            {
                float intensity = Mathf.Lerp(beaconIntensity * 0.5f, beaconIntensity, pulse);
                beaconLight.intensity = intensity;
            }

            if (outlineMethod == OutlineMethod.EmissionGlow)
            {
                Color pulsedColor = outlineColor * emissionIntensity * pulse;
                foreach (Renderer r in targetRenderers)
                {
                    if (r == null) continue;
                    foreach (Material m in r.materials)
                        if (m.HasProperty(EmissionColorID))
                            m.SetColor(EmissionColorID, pulsedColor);
                }
            }

            yield return null;
        }
    }

    public void OnObjectiveCompleted() { DisableOutline(); }
}