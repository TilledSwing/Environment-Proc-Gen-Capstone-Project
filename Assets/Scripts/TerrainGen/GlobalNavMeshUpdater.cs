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

    private bool isBuildingLandNavMesh = false;
    private bool isBuildingWaterNavMesh = false;
    private bool landNavMeshNeedsRebuild = false;
    private bool waterNavMeshNeedsRebuild = false;
    NavMeshBuildSettings waterBuildSettings;
    private Dictionary<Vector3, NavMeshBuildSource> landSources = new Dictionary<Vector3, NavMeshBuildSource>();
    private Dictionary<Vector3, NavMeshBuildSource> waterSources = new Dictionary<Vector3, NavMeshBuildSource>();
    public Bounds? currentWaterBounds = null;
    public Bounds? currentLandBounds = null;
    private readonly object landSettingLock = new object();
    private readonly object waterSettingsLock = new object();
    private float navMeshUpdateInterval = 5f;

    private NavMeshDataInstance landNavMeshInstance;
    private NavMeshDataInstance waterNavMeshInstance;



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

    private void StartNavMeshBuilds()
    {
        InvokeRepeating(nameof(ProcessNavMeshUpdates), 0.1f, navMeshUpdateInterval);
    }

    private void ProcessNavMeshUpdates()
    {
        // if (!isBuildingLandNavMesh && landNavMeshNeedsRebuild)
        //     StartCoroutine(RebuildLandNavMeshBatched());

        if (!isBuildingWaterNavMesh && waterNavMeshNeedsRebuild)
            StartCoroutine(RebuildWaterNavMeshBatched());
    }
    /// <summary>
    /// Adds a chunk to the list of chunks to be included in the next navmesh rebuild
    /// </summary>
    /// <param name="chunk"></param>
    public void AddChunkForNavMeshUpdate(ChunkGenNetwork.TerrainChunk chunk)
    {
        addLandChunk(chunk);
        addWaterChunk(chunk);
    }
    private void addWaterChunk(ChunkGenNetwork.TerrainChunk chunk)
    {
        Mesh mesh = chunk.marchingCubes.meshFilter.sharedMesh;
        var center = chunk.bounds.center;

        lock (waterSettingsLock)
        {
            waterSources.Remove(center);
        }
            
        var waterLevel = ChunkGenNetwork.Instance.terrainDensityData.waterLevel;
        bool meshEmpty = mesh == null || mesh.vertexCount == 0;

        // Only consider a chunk water if its lowest point is below the water level
        bool isWater = waterLevel > chunk.bounds.min.y;

        // Only fully water if the chunk actually has no vertices above water
        bool isFullyWater = (meshEmpty && isWater) || chunk.bounds.max.y <= waterLevel;    

        if (!isWater)
            return;

        if (isFullyWater)
        {
            // Use a simple flat quad for navmesh
            Mesh waterPlane = WaterMeshCache.GetQuad(); // pre-cached 1x1 quad mesh

            Vector3 chunkCenter = center;
            Vector3 size = chunk.bounds.size;

            Matrix4x4 transformMatrix = Matrix4x4.TRS(
                new Vector3(chunkCenter.x, waterLevel, chunkCenter.z),
                Quaternion.identity,
                new Vector3(size.x, 1f, size.z) // scale quad to chunk footprint
            );

            var waterSource = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Mesh,
                sourceObject = waterPlane,
                transform = transformMatrix,
                area = 0  // set your custom WaterArea
            };
            lock (waterSettingsLock)
            {
                waterSources[center] = waterSource;
            }
        }
        else if (isWater)
        {
            Mesh waterM = GetSubmergedMeshClipped(chunk.marchingCubes.meshFilter.sharedMesh,
                                            ChunkGenNetwork.Instance.terrainDensityData.waterLevel);
            if (waterM == null || waterM.vertexCount == 0) return;
            var waterSource = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Mesh,
                sourceObject = waterM,
                transform = chunk.marchingCubes.meshFilter.transform.localToWorldMatrix,
                area = 0 // custom WaterArea
            };
            lock (waterSettingsLock)
            {
                waterSources[center] = waterSource;
            }
        }

        waterNavMeshNeedsRebuild = true;

        if (currentWaterBounds == null)
            currentWaterBounds = chunk.bounds;
        else
            currentWaterBounds = ExpandBounds(currentWaterBounds.Value, chunk.bounds);

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

    /// <summary>
    /// Rebuilds the water navmesh for all changed water chunks in a batched manner
    /// </summary>
    private IEnumerator RebuildWaterNavMeshBatched()
    {
        if (isBuildingWaterNavMesh || currentWaterBounds == null) yield break;
        isBuildingWaterNavMesh = true;
        
        var sources = new List<NavMeshBuildSource>();
        lock (waterSettingsLock)
        {
            sources = waterSources.Values.ToList();
        }

        if (waterSurface.navMeshData == null)
        {
            waterSurface.navMeshData = new NavMeshData(waterSurface.agentTypeID);
            waterNavMeshInstance = NavMesh.AddNavMeshData(waterSurface.navMeshData);
        }

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

    private Mesh GetSubmergedMeshClipped(Mesh mesh, float waterLevel)
    {
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;

        List<Vector3> outVerts = new List<Vector3>();
        List<int> outTris = new List<int>();

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 a = verts[tris[i]];
            Vector3 b = verts[tris[i + 1]];
            Vector3 c = verts[tris[i + 2]];

            ClipTriangleAgainstPlane(a, b, c, waterLevel, outVerts, outTris);
        }

        if (outVerts.Count == 0)
            return null;

        Mesh submerged = new Mesh();
        submerged.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        submerged.SetVertices(outVerts);
        submerged.SetTriangles(outTris, 0, true);
        submerged.RecalculateNormals();
        return submerged;
    }

    private void ClipTriangleAgainstPlane(
        Vector3 a, Vector3 b, Vector3 c,
        float waterLevel,
        List<Vector3> outVerts,
        List<int> outTris)
    {
        // Signed distances from plane (negative = underwater)
        float da = a.y - waterLevel;
        float db = b.y - waterLevel;
        float dc = c.y - waterLevel;

        bool aUnder = da <= 0;
        bool bUnder = db <= 0;
        bool cUnder = dc <= 0;

        int idxStart = outVerts.Count;

        // CASE 1 — All three underwater → keep triangle
        if (aUnder && bUnder && cUnder)
        {
            outVerts.Add(a);
            outVerts.Add(b);
            outVerts.Add(c);
            outTris.Add(idxStart);
            outTris.Add(idxStart + 1);
            outTris.Add(idxStart + 2);
            return;
        }

        // CASE 2 — None underwater → discard
        if (!aUnder && !bUnder && !cUnder)
            return;

        // Helper to find intersection between edge and water plane
        Vector3 Interp(Vector3 p1, Vector3 p2, float d1, float d2)
        {
            float t = d1 / (d1 - d2);
            return p1 + t * (p2 - p1);
        }

        // CASE 3 — One vertex underwater → output one clipped triangle
        if (aUnder && !bUnder && !cUnder)
        {
            Vector3 i1 = Interp(a, b, da, db);
            Vector3 i2 = Interp(a, c, da, dc);

            outVerts.Add(a);
            outVerts.Add(i1);
            outVerts.Add(i2);

            outTris.Add(idxStart);
            outTris.Add(idxStart + 1);
            outTris.Add(idxStart + 2);
        }
        else if (bUnder && !aUnder && !cUnder)
        {
            Vector3 i1 = Interp(b, a, db, da);
            Vector3 i2 = Interp(b, c, db, dc);

            outVerts.Add(b);
            outVerts.Add(i1);
            outVerts.Add(i2);

            outTris.Add(idxStart);
            outTris.Add(idxStart + 1);
            outTris.Add(idxStart + 2);
        }
        else if (cUnder && !aUnder && !bUnder)
        {
            Vector3 i1 = Interp(c, a, dc, da);
            Vector3 i2 = Interp(c, b, dc, db);

            outVerts.Add(c);
            outVerts.Add(i1);
            outVerts.Add(i2);

            outTris.Add(idxStart);
            outTris.Add(idxStart + 1);
            outTris.Add(idxStart + 2);
        }
        // CASE 4 — Two vertices underwater → output a quad split into 2 triangles
        else
        {
            // Find the single vertex above water
            Vector3 over, u1, u2;
            float dover, du1, du2;

            if (!aUnder)
            {
                over = a; dover = da;
                u1 = b; du1 = db;
                u2 = c; du2 = dc;
            }
            else if (!bUnder)
            {
                over = b; dover = db;
                u1 = a; du1 = da;
                u2 = c; du2 = dc;
            }
            else
            {
                over = c; dover = dc;
                u1 = a; du1 = da;
                u2 = b; du2 = db;
            }

            Vector3 i1 = Interp(over, u1, dover, du1);
            Vector3 i2 = Interp(over, u2, dover, du2);

            // quad → two triangles
            outVerts.Add(u1);
            outVerts.Add(u2);
            outVerts.Add(i1);

            outVerts.Add(u2);
            outVerts.Add(i2);
            outVerts.Add(i1);

            outTris.Add(idxStart);
            outTris.Add(idxStart + 1);
            outTris.Add(idxStart + 2);

            outTris.Add(idxStart + 3);
            outTris.Add(idxStart + 4);
            outTris.Add(idxStart + 5);
        }
    }   

    void OnDrawGizmos()
    {
        if (waterSurface == null || waterSurface.navMeshData == null) return;
        var handle = NavMesh.AddNavMeshData(waterSurface.navMeshData);
        // Get the triangulation of the NavMesh
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();

        Gizmos.color = Color.cyan;

        // Iterate over the indices in steps of 3 (each triangle)
        for (int i = 0; i < tri.indices.Length; i += 3)
        {
            Vector3 v0 = tri.vertices[tri.indices[i]];
            Vector3 v1 = tri.vertices[tri.indices[i + 1]];
            Vector3 v2 = tri.vertices[tri.indices[i + 2]];

            Gizmos.DrawLine(v0, v1);
            Gizmos.DrawLine(v1, v2);
            Gizmos.DrawLine(v2, v0);
        }
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

        // Vertices
        waterQuad.vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0f, -0.5f), // bottom-left
            new Vector3( 0.5f, 0f, -0.5f), // bottom-right
            new Vector3(-0.5f, 0f,  0.5f), // top-left
            new Vector3( 0.5f, 0f,  0.5f)  // top-right
        };

        // Triangles (two triangles forming a quad)
        waterQuad.triangles = new int[]
        {
            0, 2, 1, // first triangle
            2, 3, 1  // second triangle
        };

        // Normals (facing up)
        waterQuad.RecalculateNormals();

        // Optionally, recalc bounds
        waterQuad.RecalculateBounds();

        return waterQuad;
    }
}
