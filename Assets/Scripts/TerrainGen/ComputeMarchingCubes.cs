using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class ComputeMarchingCubes : MonoBehaviour
{
    public ChunkGenNetwork.TerrainChunk owner;
    public ComputeShader terrainDensityComputeShader;
    public GrassRender grass;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    public Texture2D[] noiseGeneratorTextureArray;
    public TerrainDensityData terrainDensityData;
    public AssetSpawner assetSpawner;
    public Vector3Int chunkCoord;
    public Vector3Int chunkPos;
    public ComputeBuffer heightsBuffer;
    public ComputeBuffer vertexBuffer;
    public NativeArray<float> heightsArray;
    public bool initialLoadComplete = false;
    public bool edited = false;
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
        SetTerrainSettings();
        SetHeights();
        initialLoadComplete = true;
    }
    /// <summary>
    /// Release associated buffers
    /// </summary>
    void OnDisable()
    {
        if (heightsBuffer != null && heightsBuffer.IsValid())
        {
            heightsBuffer.Release();
        }

        if (heightsArray != null && heightsArray.IsCreated)
        {
            heightsArray.Dispose();
        }
    }
    /// <summary>
    /// Release associated buffers
    /// </summary>
    void OnApplicationQuit()
    {
        if (heightsBuffer != null && heightsBuffer.IsValid())
        {
            heightsBuffer.Release();
        }

        if (heightsArray != null && heightsArray.IsCreated)
        {
            heightsArray.Dispose();
        }
    }
    public void Regen()
    {
        SetTerrainSettings();
        SetHeights();

    }
    /// <summary>
    /// Set up for general terrain settings for the chunk
    /// </summary>
    private void SetTerrainSettings()
    {
        // Terrain Values
        terrainDensityComputeShader.SetInt("height", terrainDensityData.height);
        terrainDensityComputeShader.SetInt("ChunkSize", terrainDensityData.width);
        terrainDensityComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);
        terrainDensityComputeShader.SetFloat("isolevel", terrainDensityData.isolevel);
        terrainDensityComputeShader.SetInt("MaxWorldYChunks", ChunkGenNetwork.Instance.maxWorldYChunks);
    }
    /// <summary>
    /// Set up the density values for the chunk using compute shaders
    /// </summary>
    public void SetHeights()
    {
        int baseDensityKernel = ChunkGenNetwork.Instance.baseDensityKernel;
        int continentalnessDensityKernel = ChunkGenNetwork.Instance.continentalnessDensityKernel;
        int peaksAndValleysDensityKernel = ChunkGenNetwork.Instance.peaksAndValleysDensityKernel;
        int erosionDensityKernel = ChunkGenNetwork.Instance.erosionDensityKernel;
        int largeCaveDensityKernel = ChunkGenNetwork.Instance.largeCaveDensityKernel;
        int threadSize = Mathf.CeilToInt(terrainDensityData.width / 4f) + 1;
        int voxelSize = (terrainDensityData.width + 1) * (terrainDensityData.width + 1) * (terrainDensityData.width + 1);
        heightsBuffer = new ComputeBuffer(voxelSize, sizeof(float));

        int i = 0;
        foreach (NoiseGenerator noiseGenerator in terrainDensityData.noiseGenerators)
        {

            bool useRemoteTexture = noiseGenerator.remoteTexture != null;

            switch (noiseGenerator.noiseGeneratorType)
            {
                case NoiseGenerator.NoiseGeneratorType.BaseGenerator:
                    SetNoiseCurveTexture("BASE_INDEX", i, baseDensityKernel, "BaseCurveTexture", 
                                     useRemoteTexture ? noiseGenerator.remoteTexture : noiseGeneratorTextureArray[i]);
                    break;
                case NoiseGenerator.NoiseGeneratorType.ContinentalnessGenerator:
                    SetNoiseCurveTexture("CONTINENTALNESS_INDEX", i, continentalnessDensityKernel, "ContinentalnessCurveTexture", 
                                     useRemoteTexture ? noiseGenerator.remoteTexture : noiseGeneratorTextureArray[i]);
                    break;
                case NoiseGenerator.NoiseGeneratorType.PeaksAndValleysMapGenerator:
                    SetNoiseCurveTexture("PEAKSANDVALLEYS_INDEX", i, peaksAndValleysDensityKernel, "PeaksAndValleysCurveTexture", 
                                     useRemoteTexture ? noiseGenerator.remoteTexture : noiseGeneratorTextureArray[i]);
                    break;
                case NoiseGenerator.NoiseGeneratorType.ErosionMapGenerator:
                    SetNoiseCurveTexture("EROSION_INDEX", i, erosionDensityKernel, "ErosionCurveTexture", 
                                     useRemoteTexture ? noiseGenerator.remoteTexture : noiseGeneratorTextureArray[i]);
                    break;
                case NoiseGenerator.NoiseGeneratorType.LargeCaveGenerator:
                    SetNoiseCurveTexture("LARGECAVE_INDEX", i, largeCaveDensityKernel, "LargeCaveCurveTexture", 
                                     useRemoteTexture ? noiseGenerator.remoteTexture : noiseGeneratorTextureArray[i]);
                    break;
                case NoiseGenerator.NoiseGeneratorType.CaveDetailGenerator:
                    SetNoiseCurveTexture("CAVEDETAIL_INDEX", i, largeCaveDensityKernel, "CaveDetailCurveTexture", 
                                     useRemoteTexture ? noiseGenerator.remoteTexture : noiseGeneratorTextureArray[i]);
                    break;
            }
            i++;
        }

        // Run Density Kernels
        /* BASE KERNEL */
        terrainDensityComputeShader.SetBuffer(baseDensityKernel, "NoiseSettings", ChunkGenNetwork.Instance.noiseSettingsBuffer);
        terrainDensityComputeShader.SetBuffer(baseDensityKernel, "HeightsBuffer", heightsBuffer);
        terrainDensityComputeShader.Dispatch(baseDensityKernel, threadSize, threadSize, threadSize);
        /* CONTINENTALNESS KERNEL */
        terrainDensityComputeShader.SetBuffer(continentalnessDensityKernel, "NoiseSettings", ChunkGenNetwork.Instance.noiseSettingsBuffer);
        terrainDensityComputeShader.SetBuffer(continentalnessDensityKernel, "HeightsBuffer", heightsBuffer);
        terrainDensityComputeShader.Dispatch(continentalnessDensityKernel, threadSize, threadSize, threadSize);
        /* PEAKSANDVALLEYS KERNEL */
        terrainDensityComputeShader.SetBuffer(peaksAndValleysDensityKernel, "NoiseSettings", ChunkGenNetwork.Instance.noiseSettingsBuffer);
        terrainDensityComputeShader.SetBuffer(peaksAndValleysDensityKernel, "HeightsBuffer", heightsBuffer);
        terrainDensityComputeShader.Dispatch(peaksAndValleysDensityKernel, threadSize, threadSize, threadSize);
        /* EROSION KERNEL */
        terrainDensityComputeShader.SetBuffer(erosionDensityKernel, "NoiseSettings", ChunkGenNetwork.Instance.noiseSettingsBuffer);
        terrainDensityComputeShader.SetBuffer(erosionDensityKernel, "HeightsBuffer", heightsBuffer);
        terrainDensityComputeShader.Dispatch(erosionDensityKernel, threadSize, threadSize, threadSize);
        /* LARGECAVE KERNEL */
        terrainDensityComputeShader.SetBuffer(largeCaveDensityKernel, "NoiseSettings", ChunkGenNetwork.Instance.noiseSettingsBuffer);
        terrainDensityComputeShader.SetBuffer(largeCaveDensityKernel, "HeightsBuffer", heightsBuffer);
        terrainDensityComputeShader.Dispatch(largeCaveDensityKernel, threadSize, threadSize, threadSize);

        if (!initialLoadComplete)
        {
            float[] tempHeightsArray = new float[voxelSize];
            heightsBuffer.GetData(tempHeightsArray, 0, 0, voxelSize);
            heightsBuffer.Release();
            heightsArray = new(tempHeightsArray, Allocator.Persistent);
            MarchingCubesJobHandler(heightsArray, false);
        }
        else
        {
            Bounds bounds = new Bounds(chunkPos + (new Vector3(0.5f, 0.5f, 0.5f) * terrainDensityData.width), Vector3.one * terrainDensityData.width);
            float dst = bounds.SqrDistance(ChunkGenNetwork.Instance.viewerPos);
            ChunkGenNetwork.Instance.pendingReadbacks.Enqueue(new ChunkGenNetwork.ReadbackRequest(chunkCoord, heightsBuffer, (AsyncGPUReadbackRequest dataRequest) =>
            {
                if (dataRequest.hasError)
                {
                    Debug.LogError("Failed to read heights buffer.");
                    return;
                }

                NativeArray<float> raw = dataRequest.GetData<float>();
                heightsArray = new NativeArray<float>(raw.Length, Allocator.Persistent);
                heightsArray.CopyFrom(raw);
                heightsBuffer.Release();

                ChunkGenNetwork.Instance.marchingCubesJobQueue.Enqueue(new ChunkGenNetwork.MCQueueObject(owner, false));
            }), dst);
        }
    }
    /// <summary>
    /// Sets a given noise curbe texture in it's respective kernel
    /// </summary>
    /// <param name="noiseIndexName">The name of the noise's index variable in the shader</param>
    /// <param name="noiseIndex">The index to refer to the noise</param>
    /// <param name="kernel">The noise's respective kernel</param>
    /// <param name="noiseTextureName">The name of the noise's texture variable in the shader</param>
    /// <param name="texture">The noise texture to pass to the shader</param>
    public void SetNoiseCurveTexture(string noiseIndexName, int noiseIndex, int kernel, string noiseTextureName, Texture texture)
    {
        terrainDensityComputeShader.SetInt(noiseIndexName, noiseIndex);
        terrainDensityComputeShader.SetTexture(kernel, noiseTextureName, texture);
    }
    /// <summary>
    /// Sets up a mesh given a triangle array and count using lower level api for better performance
    /// </summary>
    /// <param name="triangleCount">The amount of items in the triangle array</param>
    /// <param name="triangleArray">An array of triangles given by marching cubes</param>
    /// <param name="terraforming">Whether the user is terraforming</param>
    public void SetMeshValuesPerformant(int triangleCount, NativeList<Triangle> triangleArray, bool terraforming)
    {
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
            assetSpawner.heightsArray = heightsArray;

            VertexSortJob vertexSortJob = new VertexSortJob { vertexArray = assetSpawner.chunkVertices };
            vertexSortJob.Run();
        }

        Mesh mesh = new Mesh();
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh, MeshUpdateFlags.DontValidateIndices);

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
        mesh.RecalculateBounds();

        if (!terraforming)
        {
            assetSpawner.SpawnAssets();
            if(chunkPos.y + terrainDensityData.width >= terrainDensityData.waterLevel && triangleCount > ChunkGenNetwork.Instance.landGrass.maxBladesPerTriangle)
            {
                grass = gameObject.AddComponent<GrassRender>();
                grass.chunkPos = chunkPos;
                grass.grassDensity = ChunkGenNetwork.Instance.landGrass.grassDensity;
                grass.maxBladesPerTriangle = ChunkGenNetwork.Instance.landGrass.maxBladesPerTriangle;
                grass.grassMaterial = ChunkGenNetwork.Instance.landGrass.grassMaterial;
                grass.grassMesh = ChunkGenNetwork.Instance.landGrass.grassMesh;
                grass.minHeight = terrainDensityData.waterLevel;
                grass.maxHeight = 52;
                grass.maxGrassSlope = ChunkGenNetwork.Instance.landGrass.maxGrassSlope;
                grass.grassHeightRange = ChunkGenNetwork.Instance.landGrass.grassHeightRange;
                grass.grassPositionComputeShader = ChunkGenNetwork.Instance.grassPositionComputeShader;
                grass.triangleCount = triangleCount;
                grass.grassTriangleBuffer = new(triangleCount, sizeof(float) * 18);
                grass.grassTriangleBuffer.SetData(triangleArray.AsArray());
                grass.bounds = mesh.bounds;
                grass.SetupGrass();
                triangleArray.Dispose();
            }
            else if(chunkPos.y + terrainDensityData.width <= terrainDensityData.waterLevel - terrainDensityData.width && triangleCount > ChunkGenNetwork.Instance.seaGrass.maxBladesPerTriangle)
            {
                grass = gameObject.AddComponent<GrassRender>();
                grass.chunkPos = chunkPos;
                grass.grassDensity = ChunkGenNetwork.Instance.seaGrass.grassDensity;
                grass.maxBladesPerTriangle = ChunkGenNetwork.Instance.seaGrass.maxBladesPerTriangle;
                grass.grassMaterial = ChunkGenNetwork.Instance.seaGrass.grassMaterial;
                grass.grassMesh = ChunkGenNetwork.Instance.seaGrass.grassMesh;
                grass.minHeight = -200;
                grass.maxHeight = terrainDensityData.waterLevel - terrainDensityData.width;
                grass.maxGrassSlope = ChunkGenNetwork.Instance.seaGrass.maxGrassSlope;
                grass.grassHeightRange = ChunkGenNetwork.Instance.seaGrass.grassHeightRange;
                grass.grassPositionComputeShader = ChunkGenNetwork.Instance.grassPositionComputeShader;
                grass.triangleCount = triangleCount;
                grass.grassTriangleBuffer = new(triangleCount, sizeof(float) * 18);
                grass.grassTriangleBuffer.SetData(triangleArray.AsArray());
                grass.bounds = mesh.bounds;
                grass.SetupGrass();
                triangleArray.Dispose();
            }
            else
            {
                triangleArray.Dispose();
            }
        }
        else
        {
            triangleArray.Dispose();
        }
    }
    /// <summary>
    /// Handler for performing the marching cubes job and generating a mesh
    /// </summary>
    /// <param name="heights">A native array containg density data for the given chunk</param>
    /// <param name="terraforming">Boolean indicating whether this is a terraforming call</param>
    public void MarchingCubesJobHandler(NativeArray<float> heights, bool terraforming)
    {
        if (terraforming) edited = true;
        if (!heights.IsCreated) return;
        int iterations = Mathf.CeilToInt(terrainDensityData.width / ChunkGenNetwork.Instance.resolution) * Mathf.CeilToInt(terrainDensityData.width / ChunkGenNetwork.Instance.resolution) * Mathf.CeilToInt(terrainDensityData.width / ChunkGenNetwork.Instance.resolution);

        NativeList<Triangle> triangleArray = new(iterations, Allocator.Persistent);

        MarchingCubesJob marchingCubesJob = new MarchingCubesJob
        {
            triangleArray = triangleArray.AsParallelWriter(),
            heightsArray = heights,
            vertexOffsetTable = ChunkGenNetwork.Instance.vertexOffsetTable,
            edgeIndexTable = ChunkGenNetwork.Instance.edgeIndexTable,
            triangleTable = ChunkGenNetwork.Instance.triangleTable,
            chunkSize = terrainDensityData.width,
            chunkPos = new int3(chunkPos.x, chunkPos.y, chunkPos.z),
            isolevel = terrainDensityData.isolevel,
            lerpToggle = terrainDensityData.lerp,
            resolution = ChunkGenNetwork.Instance.resolution,
        };

        JobHandle marchingCubesHandler = marchingCubesJob.Schedule(iterations, 16);
        marchingCubesHandler.Complete();

        SetMeshValuesPerformant(triangleArray.Length, triangleArray, terraforming);
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

            if (cubeVertices.v0 < isolevel) configurationIndex |= 1;
            if (cubeVertices.v1 < isolevel) configurationIndex |= 2;
            if (cubeVertices.v2 < isolevel) configurationIndex |= 4;
            if (cubeVertices.v3 < isolevel) configurationIndex |= 8;
            if (cubeVertices.v4 < isolevel) configurationIndex |= 16;
            if (cubeVertices.v5 < isolevel) configurationIndex |= 32;
            if (cubeVertices.v6 < isolevel) configurationIndex |= 64;
            if (cubeVertices.v7 < isolevel) configurationIndex |= 128;

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
    /// <summary>
    /// Draws wireframe cubes to visualize chunks
    /// </summary>
    void OnDrawGizmos()
    {
        if (terrainDensityData == null || gameObject.GetComponent<MeshRenderer>().enabled == false) return; // still not found
        Gizmos.DrawWireCube(chunkPos + (new Vector3(0.5f, 0.5f, 0.5f) * terrainDensityData.width), Vector3.one * terrainDensityData.width);
    }
}