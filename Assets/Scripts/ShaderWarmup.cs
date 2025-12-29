using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShaderWarmup : MonoBehaviour
{
    [Header("Auto-Detect Materials")]
    [SerializeField] private bool autoDetectFromAttacks = true;
    [SerializeField] private AttackManager attackManager;
    
    [Header("Manual Material List (Optional)")]
    [SerializeField] private Material[] additionalMaterials;
    
    [Header("Settings")]
    [SerializeField] private bool warmupOnStart = true;
    [SerializeField] private bool hideWarmupObject = true;
    
    private void Start()
    {
        if (warmupOnStart)
        {
            StartCoroutine(WarmupShaders());
        }
    }
    
    private IEnumerator WarmupShaders()
    {
        // Collect all materials that need warmup
        List<Material> materialsToWarmup = new List<Material>();
        
        // Auto-detect from AttackManager
        if (autoDetectFromAttacks)
        {
            if (attackManager == null)
            {
                attackManager = FindFirstObjectByType<AttackManager>();
            }
            
            if (attackManager != null)
            {
                foreach (Attack attack in attackManager.attacks)
                {
                    // Get evolution materials from the attack's upgrade data
                    AttackUpgradeData upgradeData = attack.GetUpgradeData();
                    if (upgradeData != null && upgradeData.evolutionData != null)
                    {
                        foreach (EvolutionRequirement evolution in upgradeData.evolutionData.evolutions)
                        {
                            if (evolution.evolutionHeadMaterial != null && !materialsToWarmup.Contains(evolution.evolutionHeadMaterial))
                            {
                                materialsToWarmup.Add(evolution.evolutionHeadMaterial);
                            }
                            if (evolution.evolutionBodyMaterial != null && !materialsToWarmup.Contains(evolution.evolutionBodyMaterial))
                            {
                                materialsToWarmup.Add(evolution.evolutionBodyMaterial);
                            }
                        }
                    }
                }
            }
        }
        
        // Add any manually specified materials
        if (additionalMaterials != null)
        {
            foreach (Material mat in additionalMaterials)
            {
                if (mat != null && !materialsToWarmup.Contains(mat))
                {
                    materialsToWarmup.Add(mat);
                }
            }
        }
        
        if (materialsToWarmup.Count == 0)
        {
            Debug.LogWarning("ShaderWarmup: No materials found to warm up!");
            yield break;
        }
        
        Debug.Log($"ShaderWarmup: Warming up {materialsToWarmup.Count} materials...");
        
        // Create temporary warmup object off-screen
        GameObject warmupObj = new GameObject("_ShaderWarmup");
        if (hideWarmupObject)
        {
            warmupObj.transform.position = new Vector3(0, -1000, 0);
        }
        
        MeshRenderer renderer = warmupObj.AddComponent<MeshRenderer>();
        MeshFilter filter = warmupObj.AddComponent<MeshFilter>();
        
        // Use a simple cube mesh
        filter.mesh = CreateSimpleCube();
        
        // Cycle through each material to force shader compilation
        foreach (Material mat in materialsToWarmup)
        {
            renderer.material = mat;
            yield return null; // Wait one frame for shader compilation
        }
        
        // Clean up
        Destroy(warmupObj);
        Debug.Log("ShaderWarmup: Complete!");
    }
    
    // Create a simple cube mesh if built-in resource isn't available
    private Mesh CreateSimpleCube()
    {
        // Try to use built-in cube first
        Mesh builtinCube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        if (builtinCube != null) return builtinCube;
        
        // Fallback: create a simple cube
        Mesh mesh = new Mesh();
        
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f,  0.5f),
        };
        
        int[] triangles = new int[]
        {
            0, 2, 1, 0, 3, 2,
            1, 6, 5, 1, 2, 6,
            5, 7, 4, 5, 6, 7,
            4, 3, 0, 4, 7, 3,
            3, 6, 2, 3, 7, 6,
            4, 1, 5, 4, 0, 1
        };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    // Optional: Call this manually if you want to warmup at a specific time
    public void ManualWarmup()
    {
        StartCoroutine(WarmupShaders());
    }
}