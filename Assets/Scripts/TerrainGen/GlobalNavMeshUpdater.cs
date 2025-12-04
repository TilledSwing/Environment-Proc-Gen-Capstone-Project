using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using System.Linq;



public class GlobalNavMeshUpdater : MonoBehaviour
{
    public static GlobalNavMeshUpdater Instance;
    public NavMeshSurface landSurface;
    public NavMeshSurface waterSurface;
    public WaterLinkManager linkManager;  

    private bool isBuildingLandNavMesh = false;
    private bool isBuildingWaterNavMesh = false;
    private bool landNavMeshNeedsRebuild = false;
    private bool waterNavMeshNeedsRebuild = false;
    NavMeshBuildSettings waterBuildSettings;
    private Dictionary<Vector3, NavMeshBuildSource> landSources = new Dictionary<Vector3, NavMeshBuildSource>();
    private Dictionary<Vector3, WaterChunkSources> waterSources = new Dictionary<Vector3, WaterChunkSources>();
    public Bounds? currentWaterBounds = null;
    public Bounds? currentLandBounds = null;
    private readonly object landSettingLock = new object();
    private readonly object waterSettingsLock = new object();
    private float navMeshUpdateInterval = 15f;
    private const float planeCount = 2;
    public List<Vector3> changedWaterChunks = new();
    private NavMeshDataInstance landNavMeshInstance;
    private NavMeshDataInstance waterNavMeshInstance;
    public float chunkHeight = 0;
    public float planeWidth = 0; // same width as the chunk
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (ChunkGenNetwork.Instance != null)
        {
            ChunkGenNetwork.Instance.OnTerrainReady += StartNavMeshBuilds;
        }
        
        landSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        landSurface.overrideVoxelSize = true;
        landSurface.voxelSize = 0.1f;
        landSurface.overrideTileSize = true;
        landSurface.tileSize = 256;

        waterSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        waterSurface.overrideVoxelSize = true;
        waterSurface.voxelSize = 0.2f;
        waterSurface.overrideTileSize = true;
        waterSurface.tileSize = 256;
    }

    private void OnDestroy()
    {
        // Always unsubscribe to avoid memory leaks
        if (ChunkGenNetwork.Instance != null)
        {
            ChunkGenNetwork.Instance.OnTerrainReady -= StartNavMeshBuilds;
        }

        if (landNavMeshInstance.valid)
        landNavMeshInstance.Remove();

        if (waterNavMeshInstance.valid)
            waterNavMeshInstance.Remove();

        // Also destroy the NavMeshData objects
        if (landSurface?.navMeshData != null)
            Destroy(landSurface.navMeshData);
        
        if (waterSurface?.navMeshData != null)
            Destroy(waterSurface.navMeshData);
    }

    public void StartNavMeshBuilds()
    {
        Debug.Log("Invoke repeating called");
        InvokeRepeating(nameof(ProcessNavMeshUpdates), .1f, navMeshUpdateInterval);
    }

    public void ProcessNavMeshUpdates()
    {
        // if (!isBuildingLandNavMesh && landNavMeshNeedsRebuild)
        //     StartCoroutine(RebuildLandNavMeshBatched());

        // if (!isBuildingWaterNavMesh && waterNavMeshNeedsRebuild)
        //     StartCoroutine(RebuildAndLinkWaterCoroutine(waterSources, 1.5f));
    }
    /// <summary>
    /// Adds a chunk to the list of chunks to be included in the next navmesh rebuild
    /// </summary>
    /// <param name="chunk"></param>
    public void AddChunkForNavMeshUpdate(ChunkGenNetwork.TerrainChunk chunk)
    {
        // addLandChunk(chunk);
        // if (chunk.isWater)
        //     addWaterChunk(chunk);
    }
    private void addWaterChunk(ChunkGenNetwork.TerrainChunk chunk)
    {
        var center = chunk.bounds.center;
        changedWaterChunks.Add(center);
        lock (waterSettingsLock)
        {
            waterSources.Remove(center);
        }
            
        var waterLevel = ChunkGenNetwork.Instance.terrainDensityData.waterLevel;

        float bottomY = chunk.bounds.min.y;
        float topY = Mathf.Min(chunk.bounds.max.y, waterLevel);
        float height = topY - bottomY;
        Vector3 size = chunk.bounds.size;
        if (height <= 0f)
            return;

        float layerY;
        List<NavMeshBuildSource> waterPlanes = new List<NavMeshBuildSource>();  
        float terrainHeight = SampleTerrainHeight(chunk.bounds);

        for (float i = 0; i < planeCount; i++)
        {
            layerY = bottomY + i / planeCount * height;
            if (layerY < terrainHeight)
                continue;
            Mesh prism = WaterMeshCache.GetQuad(); // cached unit cube
                Matrix4x4 transformMatrix = Matrix4x4.TRS(
                new Vector3(center.x, layerY, center.z),
                Quaternion.identity,
                new Vector3(size.x, height, size.z)
            );

            var prismSource = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Mesh,
                sourceObject = prism,
                transform = transformMatrix,
                area = 3 // your Water area
            };
            waterPlanes.Add(prismSource);
        }
        
        NavMeshBuildSource? terrainSource = null;
        if (chunk.marchingCubes.meshFilter.sharedMesh != null && chunk.marchingCubes.meshFilter.sharedMesh.vertexCount != 0)        {
            terrainSource = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Mesh,
                sourceObject = chunk.marchingCubes.meshFilter.sharedMesh,
                transform = chunk.marchingCubes.meshFilter.transform.localToWorldMatrix,
                area = 1
            };
        }
        
        lock (waterSettingsLock)
        {
            waterSources[center] = new WaterChunkSources
            {
                waterPlanes = waterPlanes,
                terrainMesh = terrainSource,
            };
        }

        waterNavMeshNeedsRebuild = true;

        if (currentWaterBounds == null)
        {
            currentWaterBounds = chunk.bounds;
            chunkHeight = chunk.bounds.max.y - chunk.bounds.min.y;
            planeWidth = chunk.bounds.size.x;

        }
        else
            currentWaterBounds = ExpandBounds(currentWaterBounds.Value, chunk.bounds);
    }
    float SampleTerrainHeight(Bounds bounds)
    {
        LayerMask terrainLayer = LayerMask.GetMask("Terrain Layer");

        float startY = bounds.max.y + 1f;  
        float maxDistance = bounds.size.y + 5f;

        Ray ray = new Ray(new Vector3(bounds.center.x, startY, bounds.center.z), Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, terrainLayer))
            return hit.point.y;

        return float.NegativeInfinity; // no hit
    }
    /// <summary>
    /// Adds a land chunk to the list of chunks to be included in the next navmesh rebuild
    /// </summary>
    /// <param name="chunk"></param>
    private void addLandChunk(ChunkGenNetwork.TerrainChunk chunk)
    {
        if (chunk.marchingCubes.meshFilter.sharedMesh == null || chunk.marchingCubes.meshFilter.sharedMesh.vertexCount == 0) 
            return;

        var center = chunk.bounds.center;
        lock (landSettingLock)
        {
            landSources.Remove(center);
        }

        var mesh = chunk.marchingCubes.meshFilter.sharedMesh;
        var source = new NavMeshBuildSource
        {
            shape = NavMeshBuildSourceShape.Mesh,
            sourceObject = mesh,
            transform = chunk.marchingCubes.meshFilter.transform.localToWorldMatrix,
            area = 0
        };

        lock (landSettingLock)
        {
            landSources[center] = source;
        }

        landNavMeshNeedsRebuild = true;
        if (currentLandBounds == null)
            currentLandBounds = chunk.bounds;
        else
            currentLandBounds = ExpandBounds(currentLandBounds.Value, chunk.bounds);
    }
    private Bounds ExpandBounds(Bounds original, Bounds toAdd)
    {
        original.Encapsulate(toAdd);
        return original;
    }
    
    /// <summary>
    /// Rebuilds the land navmesh for all changed land chunks in a batched manner
    /// </summary>
    /// <param name="chunkDict"></param>
    /// <param name="updateRegion"></param>
    /// <returns></returns>
    public IEnumerator RebuildLandNavMeshBatched()
    {

        if (isBuildingLandNavMesh || currentLandBounds == null)
        {
            Debug.Log($"Skipped land navmesh rebuild - already building = {isBuildingLandNavMesh}  or no bounds = {currentLandBounds == null}");
            yield break;
        }
        isBuildingLandNavMesh = true;
        var sources = new List<NavMeshBuildSource>();
        lock (landSettingLock)
        {
            sources = landSources.Values.ToList();
        }

        if (landSurface.navMeshData == null)
        {
            landSurface.navMeshData = new NavMeshData(landSurface.agentTypeID);
            landNavMeshInstance = NavMesh.AddNavMeshData(landSurface.navMeshData);
        }

        var op = NavMeshBuilder.UpdateNavMeshDataAsync(
            landSurface.navMeshData,
            landSurface.GetBuildSettings(),
            sources,
            currentLandBounds.Value
        );

        while (!op.isDone)
            yield return null;

        isBuildingLandNavMesh = false;
        landNavMeshNeedsRebuild = false;
        Debug.Log("Land NavMesh rebuilt");
    }

    private IEnumerator RebuildAndLinkWaterCoroutine(Dictionary<Vector3, WaterChunkSources> waterSources, float agentStepHeight)
    {
        List<Vector3> changedChunks;
        Dictionary<Vector3, WaterChunkSources> copyWater;
        lock (waterSettingsLock)
        { 
            changedChunks = new List<Vector3>(changedWaterChunks);
            copyWater = new Dictionary<Vector3, WaterChunkSources>(waterSources);
            changedWaterChunks.Clear();
        }
        yield return null;

        yield return RebuildWaterNavMeshBatched(copyWater);

        // Small yield to ensure NavMesh is fully updated
        yield return null;
        
        yield return StartCoroutine(linkManager.UpdateWaterLinksIncremental(changedChunks, copyWater, agentStepHeight, chunkHeight, planeWidth, waterSurface.agentTypeID));
        Debug.Log($"Water NavMesh rebuilt and off-mesh links created.");
    }
    /// <summary>
    /// Rebuilds the water navmesh for all changed water chunks in a batched manner
    /// </summary>
    private IEnumerator RebuildWaterNavMeshBatched(Dictionary<Vector3, WaterChunkSources> waterSrc)
    {
        if (isBuildingWaterNavMesh || currentWaterBounds == null) yield break;
        isBuildingWaterNavMesh = true;
        
        var sources = new List<NavMeshBuildSource>();
        
        sources = waterSrc.Values
        .SelectMany(x =>
        {
            var list = new List<NavMeshBuildSource>();

            // Add all per-chunk swim layers
            if (x.waterPlanes != null && x.waterPlanes.Count > 0)
                list.AddRange(x.waterPlanes);

            // Add terrain mesh if it exists
            if (x.terrainMesh.HasValue)
                list.Add(x.terrainMesh.Value);

            return list;
        })
        .ToList();
        
        if (waterSurface.navMeshData == null)
        {
            waterSurface.navMeshData = new NavMeshData(waterSurface.agentTypeID);
            waterNavMeshInstance = NavMesh.AddNavMeshData(waterSurface.navMeshData);
        }
        yield return null;
        var op = NavMeshBuilder.UpdateNavMeshDataAsync(
            waterSurface.navMeshData,
            waterSurface.GetBuildSettings(),
            sources,
            currentWaterBounds.Value
        );

        while (!op.isDone)
            yield return null;
        isBuildingWaterNavMesh = false;
        waterNavMeshNeedsRebuild = false;
        Debug.Log("Water NavMesh rebuilt");
    }

    public struct WaterChunkSources
    {
        public List<NavMeshBuildSource> waterPlanes;
        public NavMeshBuildSource? terrainMesh;
    }   
}


public static class WaterMeshCache
{
    private static Mesh waterQuad;
    public static Mesh GetQuad()
    {
        if (waterQuad != null) return waterQuad;

        // Create a 1x1 quad centered at origin on the XZ plane
        waterQuad = new Mesh();
        waterQuad.name = "WaterQuad";

        waterQuad.vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f, 0.5f),
            new Vector3(0.5f, 0f, 0.5f)
        };

        waterQuad.triangles = new int[]
        {
            0, 2, 1,
            2, 3, 1
        };

        waterQuad.RecalculateNormals();
        waterQuad.RecalculateBounds();
        return waterQuad;
    }
}


