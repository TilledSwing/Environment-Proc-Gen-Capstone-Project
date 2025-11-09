using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;

public class GlobalNavMeshUpdater : MonoBehaviour
{
    public NavMeshSurface landSurface;
    private NavMeshSurface waterSurface;

    private AsyncOperation currentOp;
    private bool isBuildingLandNavMesh = false;
    private bool isBuildingWaterNavMesh = false;

    private NavMeshBuildSettings landBuildSettings;
    private NavMeshBuildSettings waterBuildSettings;
    private List<ChunkGenNetwork.TerrainChunk> changedLandChunks = new List<ChunkGenNetwork.TerrainChunk>();
    private List<ChunkGenNetwork.TerrainChunk> changedWaterChunks = new List<ChunkGenNetwork.TerrainChunk>();
    public Bounds? currentWaterBounds = null;
    public Bounds? currentLandBounds = null;
    private readonly object waterChunksLock = new object();
    private readonly object landChunksLock = new object();
    private float navMeshUpdateInterval = 5f;



    private void Awake()
    {
        if (ChunkGenNetwork.Instance != null)
        {
            ChunkGenNetwork.Instance.OnTerrainReady += StartNavMeshBuilds;
        }
        landSurface = gameObject.AddComponent<NavMeshSurface>();
        landSurface.name = "Land Surface";
        landSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        landSurface.overrideVoxelSize = true;
        landSurface.voxelSize = 0.1f;
        landSurface.overrideTileSize = true;
        landSurface.tileSize = 256;

        landBuildSettings = landSurface.GetBuildSettings();
        landBuildSettings.agentSlope = 70f;   // Allow very steep slopes
        landBuildSettings.agentClimb = 1.5f;  // Increase step/climb height
        landBuildSettings.agentRadius = 0.3f;

        waterSurface = gameObject.AddComponent<NavMeshSurface>();
        waterSurface.name = "Water Surface";
        waterSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        waterSurface.overrideVoxelSize = true;
        waterSurface.voxelSize = 0.2f;
        waterSurface.overrideTileSize = true;
        waterSurface.tileSize = 256;

        waterBuildSettings = waterSurface.GetBuildSettings();
        waterBuildSettings.agentSlope = 0f; // flat surfaces only
        waterBuildSettings.agentClimb = 0f;
        waterBuildSettings.agentRadius = 0.5f;

        // Get the agentTypeID for each agent type name
        int landID = GetAgentTypeIDByName("Land Agent");
        int waterID = GetAgentTypeIDByName("Water Agent");

        landSurface.agentTypeID = landID;
        waterSurface.agentTypeID = waterID;

    }

    private void OnDestroy()
    {
        // Always unsubscribe to avoid memory leaks
        if (ChunkGenNetwork.Instance != null)
        {
            ChunkGenNetwork.Instance.OnTerrainReady -= StartNavMeshBuilds;
        }
    }

    private void StartNavMeshBuilds()
    {
        // Start your repeated incremental updates
        InvokeRepeating(nameof(ProcessNavMeshUpdates), 0.1f, navMeshUpdateInterval);
    }

    private void ProcessNavMeshUpdates()
    {
        if (!isBuildingLandNavMesh && changedLandChunks.Count > 0)
            StartCoroutine(RebuildLandNavMeshBatched());

        if (!isBuildingWaterNavMesh && changedWaterChunks.Count > 0)
            StartCoroutine(RebuildWaterNavMeshBatched());
    }
    /// <summary>
    /// Adds a chunk to the list of chunks to be included in the next navmesh rebuild
    /// </summary>
    /// <param name="chunk"></param>
    public void AddChunkForNavMeshUpdate(ChunkGenNetwork.TerrainChunk chunk)
    {
        lock (landChunksLock)
        {
            changedLandChunks.Add(chunk);
            if (currentLandBounds == null)
                currentLandBounds = chunk.bounds;
            else
                currentLandBounds = ExpandBounds(currentLandBounds.Value, chunk.bounds);
        }

        if (chunk.isWater)
        {
            lock (waterChunksLock)
            {
                changedWaterChunks.Add(chunk);
                if (currentWaterBounds == null)
                    currentWaterBounds = chunk.bounds;
                else
                    currentWaterBounds = ExpandBounds(currentWaterBounds.Value, chunk.bounds);
            }
        }
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
        List<ChunkGenNetwork.TerrainChunk> chunksToBuild;
        Bounds boundsToBuild;

        lock (landChunksLock)
        {
            if (changedLandChunks.Count == 0 || currentLandBounds == null || isBuildingLandNavMesh) yield break;
            
            isBuildingLandNavMesh = true;

            chunksToBuild = new List<ChunkGenNetwork.TerrainChunk>(changedLandChunks);
            boundsToBuild = currentLandBounds.Value;

            changedLandChunks.Clear();
            currentLandBounds = null;
        }

        List<Mesh> mergedMeshes = CombineChunksAndMeshes(chunksToBuild);

        var sources = new List<NavMeshBuildSource>();
        foreach (var mesh in mergedMeshes)
        {
            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Mesh,
                sourceObject = mesh,
                transform = Matrix4x4.identity,
                area = 0 // default area
            });
        }

        if (landSurface.navMeshData == null)
        {
            landSurface.navMeshData = new NavMeshData(landSurface.agentTypeID);
            NavMesh.AddNavMeshData(landSurface.navMeshData);
        }

        var op = NavMeshBuilder.UpdateNavMeshDataAsync(
            landSurface.navMeshData,
            landSurface.GetBuildSettings(),
            sources,
            boundsToBuild
        );

        while (!op.isDone)
            yield return null;

        lock (landChunksLock)
        {
            isBuildingLandNavMesh = false;
        }
    }

    /// <summary>
    /// Rebuilds the water navmesh for all changed water chunks in a batched manner
    /// </summary>
    private IEnumerator RebuildWaterNavMeshBatched()
    {

        List<ChunkGenNetwork.TerrainChunk> chunksToBuild;
        Bounds boundsToBuild;

        lock (waterChunksLock)
        {
            if (changedWaterChunks.Count == 0 || currentWaterBounds == null || isBuildingWaterNavMesh) yield break;
            
            isBuildingWaterNavMesh = true;

            chunksToBuild = new List<ChunkGenNetwork.TerrainChunk>(changedWaterChunks);
            boundsToBuild = currentWaterBounds.Value;

            changedWaterChunks.Clear();
            currentWaterBounds = null;
        }

        float waterLevel = ChunkGenNetwork.Instance.terrainDensityData.waterLevel;

        // Merge submerged meshes into fewer combined meshes
        List<Mesh> mergedMeshes = MergeSubmergedChunks(chunksToBuild, waterLevel);

        if (mergedMeshes.Count == 0)
        {
            isBuildingWaterNavMesh = false;
            yield break;
        }
        
        var sources = new List<NavMeshBuildSource>();
        foreach (var mesh in mergedMeshes)
        {
            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Mesh,
                sourceObject = mesh,
                transform = Matrix4x4.identity,
                area = 0 // custom WaterArea
            });
        }

        if (waterSurface.navMeshData == null)
        {
            waterSurface.navMeshData = new NavMeshData(waterSurface.agentTypeID);
            NavMesh.AddNavMeshData(waterSurface.navMeshData);
        }

        var op = NavMeshBuilder.UpdateNavMeshDataAsync(
            waterSurface.navMeshData,
            waterSurface.GetBuildSettings(),
            sources,
            boundsToBuild
        );
        
        while (!op.isDone)
            yield return null;

        lock (waterChunksLock)
        {
            isBuildingWaterNavMesh = false;
        }
    }
    
    /// <summary>
    /// Merges chunk meshes that are submerged below the water level
    /// </summary>
    /// <param name="chunks"></param>
    /// <param name="waterLevel"></param>
    /// <returns></returns>
    /// <summary>
    /// Merges submerged parts of chunks into larger meshes for efficient water NavMesh building.
    /// Uses a merge factor grid to group chunks.
    /// </summary>
    private List<Mesh> MergeSubmergedChunks(List<ChunkGenNetwork.TerrainChunk> chunks, float waterLevel, int mergeFactor = 10)
    {
        List<Mesh> output = new List<Mesh>();
        if (chunks.Count == 0) return output;

        // Group chunks by grid coordinates
        Dictionary<Vector2Int, List<MeshFilter>> mergeGroups = new Dictionary<Vector2Int, List<MeshFilter>>();

        foreach (var chunk in chunks)
        {
            if (chunk.marchingCubes?.meshFilter?.mesh == null) continue;

            Vector3 center = chunk.bounds.center;

            int gx = Mathf.FloorToInt(center.x / mergeFactor);
            int gz = Mathf.FloorToInt(center.z / mergeFactor);
            Vector2Int group = new Vector2Int(gx, gz);

            if (!mergeGroups.ContainsKey(group))
                mergeGroups[group] = new List<MeshFilter>();

            mergeGroups[group].Add(chunk.marchingCubes.meshFilter);
        }

        // Combine each group into one mesh
        foreach (var kvp in mergeGroups)
        {
            List<MeshFilter> filters = kvp.Value;
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            int vertexOffset = 0;

            foreach (var mf in filters)
            {
                Mesh submerged = GetSubmergedMesh(mf.mesh, mf.transform.localToWorldMatrix, waterLevel);
                if (submerged == null || submerged.vertexCount == 0) continue;

                vertices.AddRange(submerged.vertices);
                foreach (var t in submerged.triangles)
                    triangles.Add(t + vertexOffset);

                vertexOffset = vertices.Count;
            }

            if (vertices.Count > 0)
            {
                Mesh combined = new Mesh
                {
                    indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
                };
                combined.vertices = vertices.ToArray();
                combined.triangles = triangles.ToArray();
                combined.RecalculateNormals();
                output.Add(combined);
            }
        }
        return output;
    }



    /// <summary>
    /// Gets the agent type ID from the agent type name defined in Navigation settings
    /// </summary>
    /// <param name="agentTypeName"></param>
    /// <returns></returns>
    private static int GetAgentTypeIDByName(string agentTypeName)
    {
        int count = NavMesh.GetSettingsCount();

        for (int i = 0; i < count; i++)
        {
            NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(i);
            string name = NavMesh.GetSettingsNameFromID(settings.agentTypeID);
            if (name == agentTypeName)
                return settings.agentTypeID;
        }

        Debug.LogWarning($"Agent type '{agentTypeName}' not found. Check your Navigation > Agents tab.");
        return 0; // default
    }

    /// <summary>
    /// Combines chunk meshes into larger meshes for efficient navmesh building
    /// </summary>
    /// <param name="chunkDict"></param>
    /// <param name="outputMeshes"></param>
    /// <param name="mergeFactor"></param>
    public static List<Mesh> CombineChunksAndMeshes(List<ChunkGenNetwork.TerrainChunk> chunks, int mergeFactor = 10)
    {
        List<Mesh> output = new List<Mesh>();
        if (chunks.Count == 0) return output;

        // Group chunks by grid coordinates
        Dictionary<Vector2Int, List<MeshFilter>> mergeGroups = new Dictionary<Vector2Int, List<MeshFilter>>();

        foreach (var chunk in chunks)
        {
            if (chunk.marchingCubes?.meshFilter?.mesh == null) continue;

            // Use bounds.center for consistent spatial grouping
            Vector3 center = chunk.bounds.center;

            int gx = Mathf.FloorToInt(center.x / mergeFactor);
            int gz = Mathf.FloorToInt(center.z / mergeFactor);
            Vector2Int group = new Vector2Int(gx, gz);

            if (!mergeGroups.ContainsKey(group))
                mergeGroups[group] = new List<MeshFilter>();

            mergeGroups[group].Add(chunk.marchingCubes.meshFilter);
        }

        // Combine each group into a single mesh
        foreach (var kvp in mergeGroups)
        {
            List<MeshFilter> filters = kvp.Value;
            List<CombineInstance> combine = new List<CombineInstance>();

            foreach (var mf in filters)
            {
                combine.Add(new CombineInstance
                {
                    mesh = mf.sharedMesh,
                    transform = mf.transform.localToWorldMatrix
                });
            }

            if (combine.Count == 0) continue;

            Mesh mergedMesh = new Mesh
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 // support large meshes
            };
            mergedMesh.CombineMeshes(combine.ToArray(), true, true);

            output.Add(mergedMesh);
        }

        return output;
    }

    /// <summary>
    /// Generates a mesh containing only the submerged parts of the original mesh
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="transform"></param>
    /// <param name="waterLevel"></param>
    /// <returns></returns>
    private Mesh GetSubmergedMesh(Mesh mesh, Matrix4x4 transform, float waterLevel)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        List<Vector3> newVerts = new List<Vector3>();
        List<int> newTris = new List<int>();
        Dictionary<int, int> vertexMap = new Dictionary<int, int>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i0 = triangles[i];
            int i1 = triangles[i + 1];
            int i2 = triangles[i + 2];

            Vector3 v0 = transform.MultiplyPoint3x4(vertices[i0]);
            Vector3 v1 = transform.MultiplyPoint3x4(vertices[i1]);
            Vector3 v2 = transform.MultiplyPoint3x4(vertices[i2]);

            // Only keep triangles where **at least one vertex is below water level**
            if (v0.y <= waterLevel || v1.y <= waterLevel || v2.y <= waterLevel)
            {
                int newIndex0 = AddVertex(v0, newVerts, vertexMap, i0);
                int newIndex1 = AddVertex(v1, newVerts, vertexMap, i1);
                int newIndex2 = AddVertex(v2, newVerts, vertexMap, i2);

                newTris.Add(newIndex0);
                newTris.Add(newIndex1);
                newTris.Add(newIndex2);
            }
        }

        if (newVerts.Count == 0) return null;

        Mesh newMesh = new Mesh();
        newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // for large meshes
        newMesh.vertices = newVerts.ToArray();
        newMesh.triangles = newTris.ToArray();
        newMesh.RecalculateNormals();
        return newMesh;
    }

    /// <summary>
    /// Adds a vertex to the vertex list if not already present, returns its new index
    /// </summary>
    /// <param name="vertex"></param>
    /// <param name="vertexList"></param>
    /// <param name="map"></param>
    /// <param name="originalIndex"></param>
    /// <returns></returns>
    private int AddVertex(Vector3 vertex, List<Vector3> vertexList, Dictionary<int, int> map, int originalIndex)
    {
        if (map.TryGetValue(originalIndex, out int newIndex))
            return newIndex;

        newIndex = vertexList.Count;
        vertexList.Add(vertex);
        map[originalIndex] = newIndex;
        return newIndex;
    }
}