using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class ComputeMarchingCubes : MonoBehaviour
{
    public ChunkGenNetwork.TerrainChunk owner;
    public GrassRender grass;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    public TerrainDensityData terrainDensityData;
    public AssetSpawner assetSpawner;
    public Vector3Int chunkCoord;
    public Vector3Int chunkPos;
    public ComputeBuffer vertexBuffer;
    public NativeArray<float> heightsArray;
    public bool edited = false;
    public JobHandle noiseDensityJobHandler;
    public JobHandle marchingCubesJobHandler;
    public NativeList<Triangle> triangleArray;
    public Mesh mesh;
    int marchingCubesIterations;
    /// <summary>
    /// Struct for vertex data
    /// </summary>
    public struct Vertex
    {
        public float3 position;
        public float3 normal;
    }
    /// <summary>
    /// Struct for triangle data
    /// </summary>
    public struct Triangle
    {
        public Vertex v1;
        public Vertex v2;
        public Vertex v3;
    }
    void Start()
    {
        marchingCubesIterations = Mathf.CeilToInt(terrainDensityData.chunkSize / ChunkGenNetwork.Instance.resolution) * Mathf.CeilToInt(terrainDensityData.chunkSize / ChunkGenNetwork.Instance.resolution) * Mathf.CeilToInt(terrainDensityData.chunkSize / ChunkGenNetwork.Instance.resolution);
    }
    public void InitializeChunk
        (ChunkGenNetwork.TerrainChunk owner,
         Mesh mesh,
         MeshFilter meshFilter,
         MeshCollider meshCollider,
         Vector3Int chunkCoord,
         Vector3Int chunkPos,
         AssetSpawner assetSpawner,
         TerrainDensityData terrainDensityData
        )
        {
            this.owner = owner;
            this.mesh = mesh;
            this.meshFilter = meshFilter;
            this.meshCollider = meshCollider;
            this.chunkCoord = chunkCoord;
            this.chunkPos = chunkPos;
            this.assetSpawner = assetSpawner;
            this.terrainDensityData = terrainDensityData;
        }
    public void GenerateChunk()
    {
        SetHeights();
    }
    public void DisposalReleaseHandler() 
    {
        if (heightsArray.IsCreated)
        {
            noiseDensityJobHandler.Complete();
            marchingCubesJobHandler.Complete();
            heightsArray.Dispose();
        }
    }
    /// <summary>
    /// Release associated buffers
    /// </summary>
    void OnDisable()
    {
        DisposalReleaseHandler();
    }
    /// <summary>
    /// Release associated buffers
    /// </summary>
    void OnApplicationQuit()
    {
        DisposalReleaseHandler();
    }
    /// <summary>
    /// Set up the density values for the chunk using compute shaders
    /// </summary>
    public void SetHeights()
    {
        int size = terrainDensityData.chunkSize + 1;
        if (!heightsArray.IsCreated)
        {
            heightsArray = new(size * size * size, Allocator.Persistent);
        }
        NoiseDensityJob noiseDensityJob = new NoiseDensityJob
        {
            heightsArray = heightsArray,
            chunkPos = new float3(chunkPos.x, chunkPos.y, chunkPos.z),
            chunkSize = terrainDensityData.chunkSize,
            seed = ChunkGenNetwork.Instance.seed
        };
        noiseDensityJobHandler = noiseDensityJob.Schedule();
        ChunkGenNetwork.Instance.terrainDensityJobList.Add(new ChunkGenNetwork.TerrainJobObject(owner, noiseDensityJobHandler, false, owner.chunkID));
    }
    /// <summary>
    /// Sets up a mesh given a triangle array and count using lower level api for better performance
    /// </summary>
    /// <param name="triangleCount">The amount of items in the triangle array</param>
    /// <param name="triangleArray">An array of triangles given by marching cubes</param>
    /// <param name="terraforming">Whether the user is terraforming</param>
    public void SetMeshValuesPerformant(bool terraforming)
    {
        if (!heightsArray.IsCreated)
            return;
        int triangleCount = triangleArray.Length;
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        meshData.SetVertexBufferParams(triangleCount * 3,
                                       new VertexAttributeDescriptor(VertexAttribute.Position),
                                       new VertexAttributeDescriptor(VertexAttribute.Normal)
                                      );
        var vertexBuffer = meshData.GetVertexData<Vertex>(0);
        var vertexArray = triangleArray.AsArray().Reinterpret<Vertex>(sizeof(float) * 18);
        vertexBuffer.CopyFrom(vertexArray);
        vertexArray.Dispose();

        meshData.SetIndexBufferParams(triangleCount * 3, IndexFormat.UInt32);
        var indexBuffer = meshData.GetIndexData<int>();
        NativeSlice<int> slice = ChunkGenNetwork.Instance.staticMaxSizeVertexIndexArray.Slice(0, triangleCount * 3);
        slice.CopyTo(indexBuffer);

        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleCount * 3, MeshTopology.Triangles));

        if (!terraforming) 
        {
            assetSpawner.owner = owner;
            assetSpawner.chunkVertices = new NativeArray<Vertex>(vertexBuffer, Allocator.Persistent);
            assetSpawner.heightsArray = new(heightsArray, Allocator.Persistent);
        }

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh, MeshUpdateFlags.DontValidateIndices);

        meshFilter.mesh = mesh;
        mesh.RecalculateBounds();
        ChunkGenNetwork.Instance.collisionMeshBakeQueue.Enqueue(new ChunkGenNetwork.MeshBake(owner, mesh, meshCollider, owner.chunkID));

        if (!terraforming)
        {
            VertexSortJob vertexSortJob = new VertexSortJob { vertexArray = assetSpawner.chunkVertices };
            JobHandle vertexSortJobHandler = vertexSortJob.Schedule();
            ChunkGenNetwork.Instance.vertexSortJobList.Add(new ChunkGenNetwork.VertexSortJob(vertexSortJobHandler, assetSpawner, owner.chunkID));
            ChunkGenNetwork.Instance.grassProcessQueue.Enqueue(new ChunkGenNetwork.GrassObject(owner, triangleCount, owner.chunkID));
        }
        else
        {
            triangleArray.Dispose();
        }
    }
    public void ProcessGrass(int triangleCount)
    {
        if(chunkPos.y >= terrainDensityData.waterLevel && triangleCount > ChunkGenNetwork.Instance.landGrass.maxBladesPerTriangle)
        {
            if (grass == null)
                grass = gameObject.AddComponent<GrassRender>();
            grass.enabled = true;
            grass.InitializeGrassRenderer(
                chunkPos, 
                ChunkGenNetwork.Instance.landGrass,
                terrainDensityData.waterLevel,
                52,
                ChunkGenNetwork.Instance.grassPositionComputeShader,
                ChunkGenNetwork.Instance.grassUpdateComputeShader,
                triangleCount,
                triangleArray.AsArray(),
                mesh.bounds,
                false
            );
            grass.SetupGrass();
            grass.renderGrass = true;
        }
        else if(chunkPos.y <= terrainDensityData.waterLevel - terrainDensityData.chunkSize && triangleCount > ChunkGenNetwork.Instance.seaGrass.maxBladesPerTriangle && terrainDensityData.water)
        {
            if (grass == null)
                grass = gameObject.AddComponent<GrassRender>();
            grass.enabled = true;
            grass.InitializeGrassRenderer(
                chunkPos, 
                ChunkGenNetwork.Instance.seaGrass,
                -500,
                terrainDensityData.waterLevel - terrainDensityData.chunkSize,
                ChunkGenNetwork.Instance.grassPositionComputeShader,
                ChunkGenNetwork.Instance.grassUpdateComputeShader,
                triangleCount,
                triangleArray.AsArray(),
                mesh.bounds,
                true
            );
            grass.SetupGrass();
            grass.renderGrass = true;
        }
        else if (grass != null)
        {
            grass.enabled = false;
        }
        triangleArray.Dispose();
    }
    /// <summary>
    /// Handler for performing the marching cubes job and generating a mesh
    /// </summary>
    /// <param name="heights">A native array containg density data for the given chunk</param>
    /// <param name="terraforming">Boolean indicating whether this is a terraforming call</param>
    public void MarchingCubesJobHandler(bool terraforming)
    {
        if (terraforming) edited = true;
        if (!heightsArray.IsCreated) return;

        triangleArray = new(marchingCubesIterations, Allocator.Persistent);

        MarchingCubesJob marchingCubesJob = new MarchingCubesJob
        {
            triangleArray = triangleArray.AsParallelWriter(),
            heightsArray = heightsArray,
            vertexOffsetTable = ChunkGenNetwork.Instance.vertexOffsetTable,
            edgeIndexTable = ChunkGenNetwork.Instance.edgeIndexTable,
            triangleTable = ChunkGenNetwork.Instance.triangleTable,
            chunkSize = terrainDensityData.chunkSize,
            chunkPos = new int3(chunkPos.x, chunkPos.y, chunkPos.z),
            isolevel = terrainDensityData.isolevel,
            lerpToggle = terrainDensityData.lerp,
            resolution = ChunkGenNetwork.Instance.resolution,
        };
        marchingCubesJobHandler = marchingCubesJob.Schedule(marchingCubesIterations, 16, noiseDensityJobHandler);

        if (terraforming)
        {
            marchingCubesJobHandler.Complete();
            SetMeshValuesPerformant(terraforming);
        }
        else
        {
            ChunkGenNetwork.Instance.terrainPolygonizationJobList.Add(new ChunkGenNetwork.TerrainJobObject(owner, marchingCubesJobHandler, terraforming, owner.chunkID));
        }
    }
    /// <summary>
    /// Marching Cubes Burst Compiled Multithreaded job
    /// </summary>
    [BurstCompile]
    private struct MarchingCubesJob : IJobParallelFor
    {
        public NativeList<Triangle>.ParallelWriter triangleArray;
        [ReadOnly] public NativeArray<float> heightsArray;
        [ReadOnly] public NativeArray<float3> vertexOffsetTable;
        [ReadOnly] public NativeArray<int> edgeIndexTable;
        [ReadOnly] public NativeArray<int> triangleTable;
        public int chunkSize;
        public int3 chunkPos;
        public float isolevel;
        public bool lerpToggle;
        public int resolution;
        public void Execute(int index)
        {
            int adjustedSize = chunkSize / resolution;
            int x = index % adjustedSize;
            int y = index / adjustedSize % adjustedSize;
            int z = index / (adjustedSize * adjustedSize);

            if (x >= chunkSize || y >= chunkSize || z >= chunkSize)
                return;

            CubeVertices cubeVertices;
            float adjustedIdx = x * resolution;
            float adjustedIdy = y * resolution;
            float adjustedIdz = z * resolution;
            float3 adjustedPos = new float3(adjustedIdx, adjustedIdy, adjustedIdz);
            cubeVertices.v0 = heightsArray[FlattenIndex(adjustedPos + (vertexOffsetTable[0] * resolution), chunkSize)];
            cubeVertices.v1 = heightsArray[FlattenIndex(adjustedPos + (vertexOffsetTable[1] * resolution), chunkSize)];
            cubeVertices.v2 = heightsArray[FlattenIndex(adjustedPos + (vertexOffsetTable[2] * resolution), chunkSize)];
            cubeVertices.v3 = heightsArray[FlattenIndex(adjustedPos + (vertexOffsetTable[3] * resolution), chunkSize)];
            cubeVertices.v4 = heightsArray[FlattenIndex(adjustedPos + (vertexOffsetTable[4] * resolution), chunkSize)];
            cubeVertices.v5 = heightsArray[FlattenIndex(adjustedPos + (vertexOffsetTable[5] * resolution), chunkSize)];
            cubeVertices.v6 = heightsArray[FlattenIndex(adjustedPos + (vertexOffsetTable[6] * resolution), chunkSize)];
            cubeVertices.v7 = heightsArray[FlattenIndex(adjustedPos + (vertexOffsetTable[7] * resolution), chunkSize)];

            float3 cubePosition = new float3(adjustedIdx + chunkPos.x, adjustedIdy + chunkPos.y, adjustedIdz + chunkPos.z);

            int configurationIndex = 0;

            if (cubeVertices.v0 > isolevel) configurationIndex |= 1;
            if (cubeVertices.v1 > isolevel) configurationIndex |= 2;
            if (cubeVertices.v2 > isolevel) configurationIndex |= 4;
            if (cubeVertices.v3 > isolevel) configurationIndex |= 8;
            if (cubeVertices.v4 > isolevel) configurationIndex |= 16;
            if (cubeVertices.v5 > isolevel) configurationIndex |= 32;
            if (cubeVertices.v6 > isolevel) configurationIndex |= 64;
            if (cubeVertices.v7 > isolevel) configurationIndex |= 128;

            if (configurationIndex == 0 || configurationIndex == 255)
            {
                return;
            }

            int edgeIndex = 0;
            int flattenedConfigurationIndex = configurationIndex << 4;

            for (int t = 0; t < 5; t++)
            {
                int edge1Value = triangleTable[flattenedConfigurationIndex + edgeIndex];
                int edge2Value = triangleTable[flattenedConfigurationIndex + edgeIndex + 1];
                int edge3Value = triangleTable[flattenedConfigurationIndex + edgeIndex + 2];

                if (edge1Value == -1 || edge2Value == -1 || edge3Value == -1)
                {
                    return;
                }

                int flattenedEdge1Value = edge1Value << 1;
                float3 edge1V1 = cubePosition + (vertexOffsetTable[edgeIndexTable[flattenedEdge1Value]] * resolution);
                float3 edge1V2 = cubePosition + (vertexOffsetTable[edgeIndexTable[flattenedEdge1Value + 1]] * resolution);
                
                int flattenedEdge2Value = edge2Value << 1;
                float3 edge2V1 = cubePosition + (vertexOffsetTable[edgeIndexTable[flattenedEdge2Value]] * resolution);
                float3 edge2V2 = cubePosition + (vertexOffsetTable[edgeIndexTable[flattenedEdge2Value + 1]] * resolution);

                int flattenedEdge3Value = edge3Value << 1;
                float3 edge3V1 = cubePosition + (vertexOffsetTable[edgeIndexTable[flattenedEdge3Value]] * resolution);
                float3 edge3V2 = cubePosition + (vertexOffsetTable[edgeIndexTable[flattenedEdge3Value + 1]] * resolution);

                float3 vertex1;
                float3 vertex2;
                float3 vertex3;

                if (lerpToggle)
                {
                    vertex1 = math.lerp(edge1V1, edge1V2, (isolevel - cubeVertices.GetCubeVertex(edgeIndexTable[flattenedEdge1Value])) / (cubeVertices.GetCubeVertex(edgeIndexTable[flattenedEdge1Value + 1]) - cubeVertices.GetCubeVertex(edgeIndexTable[flattenedEdge1Value])));
                    vertex2 = math.lerp(edge2V1, edge2V2, (isolevel - cubeVertices.GetCubeVertex(edgeIndexTable[flattenedEdge2Value])) / (cubeVertices.GetCubeVertex(edgeIndexTable[flattenedEdge2Value + 1]) - cubeVertices.GetCubeVertex(edgeIndexTable[flattenedEdge2Value])));
                    vertex3 = math.lerp(edge3V1, edge3V2, (isolevel - cubeVertices.GetCubeVertex(edgeIndexTable[flattenedEdge3Value])) / (cubeVertices.GetCubeVertex(edgeIndexTable[flattenedEdge3Value + 1]) - cubeVertices.GetCubeVertex(edgeIndexTable[flattenedEdge3Value])));
                }
                else
                {
                    vertex1 = (edge1V1 + edge1V2) / 2;
                    vertex2 = (edge2V1 + edge2V2) / 2;
                    vertex3 = (edge3V1 + edge3V2) / 2;
                }

                float3 normal = math.normalize(math.cross(vertex2 - vertex1, vertex3 - vertex1));

                Triangle tri;
                tri.v1.position = vertex1;
                tri.v2.position = vertex2;
                tri.v3.position = vertex3;

                tri.v1.normal = normal;
                tri.v2.normal = normal;
                tri.v3.normal = normal;
                triangleArray.AddNoResize(tri);


                edgeIndex += 3;
            }
        }
        /// <summary>
        /// Method for flattening a 3D array index into a 1D array index
        /// </summary>
        /// <param name="id">3D index</param>
        /// <param name="size">3D array dimensions</param>
        /// <returns>Flattened index</returns>
        int FlattenIndex(float3 id, int size)
        {
            return (int)(id.z * (size + 1) * (size + 1) + id.y * (size + 1) + id.x);
        }
        /// <summary>
        /// Struct for storing voxel vertex data
        /// </summary>
        struct CubeVertices
        {
            public float v0, v1, v2, v3, v4, v5, v6, v7;

            public float GetCubeVertex(int cubeVertIndex)
            {
                switch (cubeVertIndex)
                {
                    case 0: return v0;
                    case 1: return v1;
                    case 2: return v2;
                    case 3: return v3;
                    case 4: return v4;
                    case 5: return v5;
                    case 6: return v6;
                    case 7: return v7;
                    default: return 0f;
                }
            }
        }
    }
    /// <summary>
    /// Burst Compiled Job for sorting a vertex array by position and normal
    /// </summary>
    [BurstCompile]
    public struct VertexSortJob : IJob
    {
        public NativeArray<Vertex> vertexArray;
        public void Execute()
        {
            vertexArray.Sort(new VertexComparer());
        }

        public struct VertexComparer : IComparer<Vertex>
        {
            public int Compare(Vertex a, Vertex b)
            {
                int cmp;

                cmp = a.position.x < b.position.x ? -1 : (a.position.x > b.position.x ? 1 : 0);
                if (cmp != 0) return cmp;

                cmp = a.position.y < b.position.y ? -1 : (a.position.y > b.position.y ? 1 : 0);
                if (cmp != 0) return cmp;

                cmp = a.position.z < b.position.z ? -1 : (a.position.z > b.position.z ? 1 : 0);
                if (cmp != 0) return cmp;

                cmp = a.normal.x < b.normal.x ? -1 : (a.normal.x > b.normal.x ? 1 : 0);
                if (cmp != 0) return cmp;

                cmp = a.normal.y < b.normal.y ? -1 : (a.normal.y > b.normal.y ? 1 : 0);
                if (cmp != 0) return cmp;

                return a.normal.z < b.normal.z ? -1 : (a.normal.z > b.normal.z ? 1 : 0);
            }
        }
    }
    public struct NoiseDensityJob : IJob
    {
        public NativeArray<float> heightsArray;
        public float3 chunkPos;
        public int chunkSize;
        public int seed;
        public void Execute()
        {
            int size = chunkSize + 1;
            ChunkGenNetwork.Instance.noiseGenerator.GenUniformGrid3D(
                                                                heightsArray, chunkPos.x, chunkPos.y, chunkPos.z, 
                                                                size, size, size, 1, 1, 1, seed);
        }
    }
    /// <summary>
    /// Draws wireframe cubes to visualize chunks
    /// </summary>
    void OnDrawGizmos()
    {
        if (terrainDensityData == null || gameObject.GetComponent<MeshRenderer>().enabled == false) return; // still not found
        Gizmos.DrawWireCube(chunkPos + (new Vector3(0.5f, 0.5f, 0.5f) * terrainDensityData.chunkSize), Vector3.one * terrainDensityData.chunkSize);
    }
}