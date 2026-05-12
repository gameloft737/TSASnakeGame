using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SetTerrainObstacles : MonoBehaviour
{
    // Per-prototype collider cache so we don't call GetComponent<Collider>() (and
    // its subtype lookups) for every tree when there can be hundreds/thousands of
    // trees. Also spreads obstacle creation across frames on WebGL to avoid
    // multi-second main-thread stalls at scene start.
    [Header("WebGL Optimization")]
    [Tooltip("If true, obstacle creation is spread across multiple frames to avoid a long stall on startup.")]
    [SerializeField] private bool spreadOverFrames = true;
    [Tooltip("How many obstacles to create per frame when spreading.")]
    [SerializeField] private int obstaclesPerFrame = 64;

    TreeInstance[] Obstacle;
    Terrain terrain;
    float width;
    float lenght;
    float hight;
    bool isError;

    void Start()
    {
        terrain = Terrain.activeTerrain;
        if (terrain == null) return;

        Obstacle = terrain.terrainData.treeInstances;

        lenght = terrain.terrainData.size.z;
        width = terrain.terrainData.size.x;
        hight = terrain.terrainData.size.y;

        if (spreadOverFrames && Obstacle != null && Obstacle.Length > obstaclesPerFrame)
        {
            StartCoroutine(BuildObstaclesSpread());
        }
        else
        {
            BuildObstaclesImmediate();
        }
    }

    private void BuildObstaclesImmediate()
    {
        GameObject parent = new GameObject("Tree_Obstacles");
        var colliderCache = new Dictionary<int, Collider>();

        for (int i = 0; i < Obstacle.Length; i++)
        {
            if (!CreateObstacle(i, parent.transform, colliderCache)) break;
        }
    }

    private IEnumerator BuildObstaclesSpread()
    {
        GameObject parent = new GameObject("Tree_Obstacles");
        var colliderCache = new Dictionary<int, Collider>();

        int created = 0;
        for (int i = 0; i < Obstacle.Length; i++)
        {
            if (!CreateObstacle(i, parent.transform, colliderCache)) break;
            created++;
            if (created >= obstaclesPerFrame)
            {
                created = 0;
                yield return null;
            }
        }
    }

    /// <summary>
    /// Creates a single obstacle. Returns false on a fatal error so the caller
    /// can break out.
    /// </summary>
    private bool CreateObstacle(int i, Transform parent, Dictionary<int, Collider> colliderCache)
    {
        TreeInstance tree = Obstacle[i];

        Vector3 worldPosition = Vector3.Scale(tree.position, terrain.terrainData.size) + terrain.transform.position;
        Quaternion tempRot = Quaternion.AngleAxis(tree.rotation * Mathf.Rad2Deg, Vector3.up);

        GameObject obs = new GameObject("Obstacle" + i);
        obs.transform.SetParent(parent);
        obs.transform.position = worldPosition;
        obs.transform.rotation = tempRot;

        NavMeshObstacle obsElement = obs.AddComponent<NavMeshObstacle>();
        obsElement.carving = true;
        obsElement.carveOnlyStationary = true;

        int protoIdx = tree.prototypeIndex;
        if (!colliderCache.TryGetValue(protoIdx, out Collider coll))
        {
            var prefab = terrain.terrainData.treePrototypes[protoIdx].prefab;
            coll = prefab != null ? prefab.GetComponent<Collider>() : null;
            colliderCache[protoIdx] = coll;
        }

        if (coll == null)
        {
            isError = true;
            #if UNITY_EDITOR
            Debug.LogError("ERROR No CapsuleCollider or BoxCollider on tree prototype " + protoIdx);
            #endif
            return false;
        }

        if (coll is CapsuleCollider capsuleColl)
        {
            obsElement.shape = NavMeshObstacleShape.Capsule;
            obsElement.center = capsuleColl.center;
            obsElement.radius = capsuleColl.radius;
            obsElement.height = capsuleColl.height;
        }
        else if (coll is BoxCollider boxColl)
        {
            obsElement.shape = NavMeshObstacleShape.Box;
            obsElement.center = boxColl.center;
            obsElement.size = boxColl.size;
        }
        else
        {
            isError = true;
            #if UNITY_EDITOR
            Debug.LogError("ERROR Unsupported collider type on tree prototype " + protoIdx);
            #endif
            return false;
        }

        return true;
    }
}
