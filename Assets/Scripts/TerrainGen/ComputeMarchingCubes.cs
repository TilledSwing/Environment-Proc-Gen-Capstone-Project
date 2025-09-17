using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class ComputeMarchingCubes : MonoBehaviour
{
    public ComputeShader marchingCubesComputeShader;
    public ComputeShader terrainDensityComputeShader;
    public ComputeShader terrainNoiseComputeShader;
    public ComputeShader terraformComputeShader;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    // public List<Vector3> vertices = new List<Vector3>();
    // public List<Vertex> verticesNormals = new List<Vertex>();
    // private List<int> triangles = new List<int>();
    public TerrainDensityData terrainDensityData;
    public WaterPlaneGenerator waterGen;
    public AssetSpawner assetSpawner;
    public Vector3Int chunkPos;
    public ComputeBuffer heightsBuffer;
    public bool initialLoadComplete = false;
    public ChunkGenNetwork.LOD currentLOD;
    public Mesh lod1Mesh;
    public Mesh lod2Mesh;
    public Mesh lod3Mesh;
    public Mesh lod6Mesh;
    public struct Vertex
    {
        public float3 position;
        public float3 normal;
    }

    public struct Triangle
    {
        public Vertex v1;
        public Vertex v2;
        public Vertex v3;
    }

    void Start()
    {
        SetTerrainSettings();
        GenerateMesh();
    }
    public void Regen()
    {
        SetTerrainSettings();
        GenerateMesh();
    }
    private void SetTerrainSettings()
    {
        // Terrain Values
        terrainDensityComputeShader.SetInt("height", terrainDensityData.height);
        terrainDensityComputeShader.SetBool("terracing", terrainDensityData.terracing);
        terrainDensityComputeShader.SetInt("terraceHeight", terrainDensityData.terraceHeight);
        terrainDensityComputeShader.SetInt("ChunkSize", terrainDensityData.width);
        terrainDensityComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);
        terrainDensityComputeShader.SetFloat("isolevel", terrainDensityData.isolevel);
        terrainDensityComputeShader.SetInt("MaxWorldYChunks", ChunkGenNetwork.Instance.maxWorldYChunks);
    }
    // Set all the noise settings from the TerrainDensityData scriptable object
    private void SetNoiseSettings(NoiseGenerator noiseGenerator)
    {
        // Noise and Fractal Values
        terrainNoiseComputeShader.SetInt("noiseDimension", (int)noiseGenerator.noiseDimension);
        terrainNoiseComputeShader.SetInt("noiseType", (int)noiseGenerator.noiseType);
        terrainNoiseComputeShader.SetInt("noiseFractalType", (int)noiseGenerator.noiseFractalType);
        terrainNoiseComputeShader.SetInt("rotationType3D", (int)noiseGenerator.rotationType3D);
        terrainNoiseComputeShader.SetInt("noiseSeed", noiseGenerator.noiseSeed);
        terrainNoiseComputeShader.SetInt("noiseFractalOctaves", noiseGenerator.noiseFractalOctaves);
        terrainNoiseComputeShader.SetFloat("noiseFractalLacunarity", noiseGenerator.noiseFractalLacunarity);
        terrainNoiseComputeShader.SetFloat("noiseFractalGain", noiseGenerator.noiseFractalGain);
        terrainNoiseComputeShader.SetFloat("fractalWeightedStrength", noiseGenerator.fractalWeightedStrength);
        terrainNoiseComputeShader.SetFloat("noiseFrequency", noiseGenerator.noiseFrequency);
        // Domain Warp Values
        terrainNoiseComputeShader.SetBool("domainWarpToggle", noiseGenerator.domainWarpToggle);
        terrainNoiseComputeShader.SetInt("domainWarpType", (int)noiseGenerator.domainWarpType);
        terrainNoiseComputeShader.SetInt("domainWarpFractalType", (int)noiseGenerator.domainWarpFractalType);
        terrainNoiseComputeShader.SetFloat("domainWarpAmplitude", noiseGenerator.domainWarpAmplitude);
        terrainNoiseComputeShader.SetInt("domainWarpSeed", noiseGenerator.domainWarpSeed);
        terrainNoiseComputeShader.SetInt("domainWarpFractalOctaves", noiseGenerator.domainWarpFractalOctaves);
        terrainNoiseComputeShader.SetFloat("domainWarpFractalLacunarity", noiseGenerator.domainWarpFractalLacunarity);
        terrainNoiseComputeShader.SetFloat("domainWarpFractalGain", noiseGenerator.domainWarpFractalGain);
        terrainNoiseComputeShader.SetFloat("domainWarpFrequency", noiseGenerator.domainWarpFrequency);
        // Cellular(Voronoi) Values
        terrainNoiseComputeShader.SetInt("cellularDistanceFunction", (int)noiseGenerator.cellularDistanceFunction);
        terrainNoiseComputeShader.SetInt("cellularReturnType", (int)noiseGenerator.cellularReturnType);
        terrainNoiseComputeShader.SetFloat("cellularJitter", noiseGenerator.cellularJitter);
        // Terrain Values
        terrainNoiseComputeShader.SetFloat("noiseScale", noiseGenerator.noiseScale);
        terrainNoiseComputeShader.SetInt("ChunkSize", noiseGenerator.width);
        terrainNoiseComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);
    }
    /// <summary>
    /// Set density and generate terrain mesh
    /// </summary>
    public void GenerateMesh()
    {
        heightsBuffer = SetHeights();

        // Wait for heights buffer to be set
        // float[] sync = new float[1];
        // heightsBuffer.GetData(sync);
        // yield return null;

        if (!initialLoadComplete)
        {
            // foreach(ChunkGenNetwork.LODData lodData in ChunkGenNetwork.Instance.lodData) {
            //     SyncMarchingCubes(heightsBuffer, false, lodData);
            // }
            SyncMarchingCubes(heightsBuffer, false);
        }
        else
        {
            // foreach(ChunkGenNetwork.LODData lodData in ChunkGenNetwork.Instance.lodData) {
            //     AsyncMarchingCubes(heightsBuffer, false, lodData);
            // }
            AsyncMarchingCubes(heightsBuffer, false);
        }
    }
    public void UpdateMesh(ChunkGenNetwork.LOD lod)
    {
        if (currentLOD == lod) return;
        if (lod == ChunkGenNetwork.LOD.LOD1 && lod1Mesh != null) meshFilter.mesh = lod1Mesh;
        if (lod == ChunkGenNetwork.LOD.LOD2 && lod2Mesh != null) meshFilter.mesh = lod2Mesh;
        if (lod == ChunkGenNetwork.LOD.LOD3 && lod3Mesh != null) meshFilter.mesh = lod3Mesh;
        if (lod == ChunkGenNetwork.LOD.LOD6 && lod6Mesh != null) meshFilter.mesh = lod6Mesh;
        currentLOD = lod;
    }
    /// <summary>
    /// Set up the density values for the chunk using compute shaders
    /// </summary>
    /// <returns>The buffer the density values are stored in</returns>
    public ComputeBuffer SetHeights()
    {
        int terrainNoiseKernel = terrainNoiseComputeShader.FindKernel("TerrainNoise");
        int densityKernel = terrainDensityComputeShader.FindKernel("TerrainDensity");

        List<ComputeBuffer> noiseBuffers = new();
        
        foreach (NoiseGenerator noiseGenerator in terrainDensityData.noiseGenerators)
        {
            if (!noiseGenerator.activated) continue;
            SetNoiseSettings(noiseGenerator);
            ComputeBuffer noiseBuffer = ComputeBufferPoolManager.Instance.GetComputeBuffer("NoiseBuffer", (terrainDensityData.width + 1) * (terrainDensityData.width + 1) * (terrainDensityData.width + 1), sizeof(float));
            terrainNoiseComputeShader.SetBuffer(terrainNoiseKernel, "TerrainNoiseBuffer", noiseBuffer);
            terrainNoiseComputeShader.Dispatch(terrainNoiseKernel, Mathf.CeilToInt(terrainDensityData.width / 4f) + 1, Mathf.CeilToInt(terrainDensityData.width / 4f) + 1, Mathf.CeilToInt(terrainDensityData.width / 4f) + 1);
            if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.BaseGenerator)
            {
                terrainDensityComputeShader.SetBuffer(densityKernel, "BaseNoiseBuffer", noiseBuffer);
            }
            if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.LargeCaveGenerator)
            {
                terrainDensityComputeShader.SetBool("LargeCaveNoiseActivated", true);
                terrainDensityComputeShader.SetBuffer(densityKernel, "LargeCaveNoiseBuffer", noiseBuffer);
            }
            if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.CaveDetailGenerator)
            {
                terrainDensityComputeShader.SetBool("CaveDetailNoiseActivated", true);
                terrainDensityComputeShader.SetBuffer(densityKernel, "CaveDetailNoiseBuffer", noiseBuffer);
            }
            if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.ContinentalnessGenerator)
            {
                terrainDensityComputeShader.SetBool("ContinentalnessNoiseActivated", true);
                terrainDensityComputeShader.SetBuffer(densityKernel, "ContinentalnessNoiseBuffer", noiseBuffer);
            }
            if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.TemperatureMapGenerator)
            {
                terrainDensityComputeShader.SetBool("TemperatureCaveNoiseActivated", true);
                terrainDensityComputeShader.SetBuffer(densityKernel, "TemperatureNoiseBuffer", noiseBuffer);
            }
            if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.HumidityMapGenerator)
            {
                terrainDensityComputeShader.SetBool("HumidityNoiseActivated", true);
                terrainDensityComputeShader.SetBuffer(densityKernel, "HumidityNoiseBuffer", noiseBuffer);
            }
            if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.PeaksAndValleysMapGenerator)
            {
                terrainDensityComputeShader.SetBool("PeaksAndValleysNoiseActivated", true);
                terrainDensityComputeShader.SetBuffer(densityKernel, "PeaksAndValleysNoiseBuffer", noiseBuffer);
            }
            if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.ErosionMapGenerator)
            {
                terrainDensityComputeShader.SetBool("ErosionNoiseActivated", true);
                terrainDensityComputeShader.SetBuffer(densityKernel, "ErosionNoiseBuffer", noiseBuffer);
            }
            noiseBuffers.Add(noiseBuffer);
        }

        heightsBuffer = new ComputeBuffer((terrainDensityData.width + 1) * (terrainDensityData.width + 1) * (terrainDensityData.width + 1), sizeof(float));

        terrainDensityComputeShader.SetBuffer(densityKernel, "HeightsBuffer", heightsBuffer);
        terrainDensityComputeShader.Dispatch(densityKernel, Mathf.CeilToInt(terrainDensityData.width / 4f) + 1, Mathf.CeilToInt(terrainDensityData.width / 4f) + 1, Mathf.CeilToInt(terrainDensityData.width / 4f) + 1);

        foreach (ComputeBuffer noiseBuffer in noiseBuffers) {
            ComputeBufferPoolManager.Instance.ReturnComputeBuffer("NoiseBuffer", noiseBuffer);
        }

        return heightsBuffer;
    }
    /// <summary>
    /// Perform marching cubes in a compute shader and trigger mesh generation and asset spawning
    /// </summary>
    /// <param name="heightsBuffer">The buffer containing the chunks density field</param>
    /// <param name="terraforming">Whether the user is terraforming</param>
    public void AsyncMarchingCubes(ComputeBuffer heightsBuffer, bool terraforming)
    {
        int marchingKernel = marchingCubesComputeShader.FindKernel("MarchingCubes");

        marchingCubesComputeShader.SetBuffer(marchingKernel, "HeightsBuffer", heightsBuffer);
        ComputeBuffer vertexBuffer = ComputeBufferPoolManager.Instance.GetComputeBuffer("VertexBuffer", terrainDensityData.width * terrainDensityData.width * terrainDensityData.width, sizeof(float) * 18, ComputeBufferType.Append);
        marchingCubesComputeShader.SetBuffer(marchingKernel, "VertexBuffer", vertexBuffer);

        marchingCubesComputeShader.SetInt("ChunkSize", terrainDensityData.width);
        marchingCubesComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);
        marchingCubesComputeShader.SetFloat("isolevel", terrainDensityData.isolevel);
        marchingCubesComputeShader.SetBool("lerpToggle", terrainDensityData.lerp);
        marchingCubesComputeShader.SetInt("Resolution", ChunkGenNetwork.Instance.resolution);

        vertexBuffer.SetCounterValue(0);
        marchingCubesComputeShader.Dispatch(marchingKernel, Mathf.CeilToInt(terrainDensityData.width / ChunkGenNetwork.Instance.resolution / 4f), Mathf.CeilToInt(terrainDensityData.width / ChunkGenNetwork.Instance.resolution / 4f), Mathf.CeilToInt(terrainDensityData.width / ChunkGenNetwork.Instance.resolution / 4f));

        ComputeBuffer vertexCountBuffer = ComputeBufferPoolManager.Instance.GetComputeBuffer("VertexCountBuffer", 1, sizeof(int), ComputeBufferType.Raw);
        ComputeBuffer.CopyCount(vertexBuffer, vertexCountBuffer, 0);

        ChunkGenNetwork.Instance.pendingReadbacks.Enqueue(new ChunkGenNetwork.ReadbackRequest(vertexCountBuffer, (AsyncGPUReadbackRequest countRequest) =>
        {
            if (countRequest.hasError)
            {
                Debug.LogError("Failed to read vertex count.");
                return;
            }

            int vertexCount = countRequest.GetData<int>()[0];
            ComputeBufferPoolManager.Instance.ReturnComputeBuffer("VertexCountBuffer", vertexCountBuffer);

            ChunkGenNetwork.Instance.pendingReadbacks.Enqueue(new ChunkGenNetwork.ReadbackRequest(vertexBuffer, (AsyncGPUReadbackRequest dataRequest) =>
            {
                if (dataRequest.hasError)
                {
                    Debug.LogError("Failed to read vertex buffer.");
                    return;
                }

                Triangle[] vertexArray = new Triangle[vertexCount];
                NativeArray<Triangle> rawData = dataRequest.GetData<Triangle>();

                for (int i = 0; i < vertexCount; i++)
                {
                    vertexArray[i] = rawData[i];
                }

                ComputeBufferPoolManager.Instance.ReturnComputeBuffer("VertexBuffer", vertexBuffer);

                if (terrainDensityData.waterLevel > chunkPos.y && terrainDensityData.waterLevel < Mathf.RoundToInt(chunkPos.y + terrainDensityData.width))
                {
                    waterGen.UpdateMesh();
                }

                if (vertexCount > 0)
                {
                    SetMeshValuesPerformant(vertexCount, vertexArray, terraforming);
                    // ChunkGenNetwork.Instance.pendingMeshInits.Enqueue(() =>
                    //     SetMeshValuesPerformant(vertexCount, vertexArray, terraforming)
                    // );
                }
            }));
        }));
    }
    /// <summary>
    /// Perform marching cubes in a compute shader and trigger mesh generation and asset spawning
    /// </summary>
    /// <param name="heightsBuffer">The buffer containing the chunks density field</param>
    /// <param name="terraforming">Whether the user is terraforming</param>
    public void SyncMarchingCubes(ComputeBuffer heightsBuffer, bool terraforming)
    {
        int marchingKernel = marchingCubesComputeShader.FindKernel("MarchingCubes");

        marchingCubesComputeShader.SetBuffer(marchingKernel, "HeightsBuffer", heightsBuffer);
        ComputeBuffer vertexBuffer = ComputeBufferPoolManager.Instance.GetComputeBuffer("VertexBuffer", terrainDensityData.width * terrainDensityData.width * terrainDensityData.width, sizeof(float) * 18, ComputeBufferType.Append);
        marchingCubesComputeShader.SetBuffer(marchingKernel, "VertexBuffer", vertexBuffer);

        marchingCubesComputeShader.SetInt("ChunkSize", terrainDensityData.width);
        marchingCubesComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);
        marchingCubesComputeShader.SetFloat("isolevel", terrainDensityData.isolevel);
        marchingCubesComputeShader.SetBool("lerpToggle", terrainDensityData.lerp);
        marchingCubesComputeShader.SetInt("Resolution", ChunkGenNetwork.Instance.resolution);

        vertexBuffer.SetCounterValue(0);
        marchingCubesComputeShader.Dispatch(marchingKernel, Mathf.CeilToInt(terrainDensityData.width / ChunkGenNetwork.Instance.resolution / 4f), Mathf.CeilToInt(terrainDensityData.width / ChunkGenNetwork.Instance.resolution / 4f), Mathf.CeilToInt(terrainDensityData.width / ChunkGenNetwork.Instance.resolution / 4f));

        ComputeBuffer vertexCountBuffer = ComputeBufferPoolManager.Instance.GetComputeBuffer("VertexCountBuffer", 1, sizeof(int), ComputeBufferType.Raw);
        ComputeBuffer.CopyCount(vertexBuffer, vertexCountBuffer, 0);

        int[] vertexCountArray = { 0 };
        vertexCountBuffer.GetData(vertexCountArray);

        ComputeBufferPoolManager.Instance.ReturnComputeBuffer("VertexCountBuffer", vertexCountBuffer);

        int vertexCount = vertexCountArray[0];

        Triangle[] vertexArray = new Triangle[vertexCount];
        vertexBuffer.GetData(vertexArray, 0, 0, vertexCount);

        ComputeBufferPoolManager.Instance.ReturnComputeBuffer("VertexBuffer", vertexBuffer);

        if (terrainDensityData.waterLevel > chunkPos.y && terrainDensityData.waterLevel < Mathf.RoundToInt(chunkPos.y + terrainDensityData.width))
        {
            waterGen.UpdateMesh();
        }

        SetMeshValuesPerformant(vertexCount, vertexArray, terraforming);
        // ChunkGenNetwork.Instance.pendingMeshInits.Enqueue(() =>
        //     SetMeshValuesPerformant(vertexCount, vertexArray, terraforming)
        // );
    }
    /// <summary>
    /// Sets up a mesh given a vertex array and count using lower level api for better performance
    /// </summary>
    /// <param name="vertexCount">The amount of items in the vertex array</param>
    /// <param name="vertexArray">An array of vertices given by marching cubes</param>
    /// <param name="terraforming">Whether the user is terraforming</param>
    public void SetMeshValuesPerformant(int vertexCount, Triangle[] vertexArray, bool terraforming)
    {
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        meshData.SetVertexBufferParams(vertexCount * 3,
        new VertexAttributeDescriptor(VertexAttribute.Position),
        new VertexAttributeDescriptor(VertexAttribute.Normal));

        var vertexBuffer = meshData.GetVertexData<Vertex>(0);

        meshData.SetIndexBufferParams(vertexCount * 3, IndexFormat.UInt32);
        var indexBuffer = meshData.GetIndexData<int>();

        for (int i = 0; i < vertexCount; i++)
        {
            int start = i * 3;
            Triangle t = vertexArray[i];

            vertexBuffer[start] = t.v1;
            vertexBuffer[start + 1] = t.v2;
            vertexBuffer[start + 2] = t.v3;

            indexBuffer[start] = start;
            indexBuffer[start + 1] = start + 1;
            indexBuffer[start + 2] = start + 2;
        }

        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, vertexCount * 3, MeshTopology.Triangles));

        if (!terraforming)
        {
            assetSpawner.chunkVertices = new NativeArray<Vertex>(vertexBuffer.Length, Allocator.Persistent);
            assetSpawner.chunkVertices.CopyFrom(vertexBuffer);
        }

        Mesh mesh = new Mesh();
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh, MeshUpdateFlags.Default);
        // mesh.bounds = new Bounds(chunkPos + (new Vector3(0.5f, 0.5f, 0.5f) * terrainDensityData.width), Vector3.one * terrainDensityData.width);

        // if (lodData.lod == ChunkGenNetwork.LOD.LOD1)
        // {
        //     lod1Mesh = mesh;
        //     meshCollider.sharedMesh = mesh;
        // }
        // if (lodData.lod == ChunkGenNetwork.LOD.LOD2) lod2Mesh = mesh;
        // if (lodData.lod == ChunkGenNetwork.LOD.LOD3) lod3Mesh = mesh;
        // if (lodData.lod == ChunkGenNetwork.LOD.LOD6) lod6Mesh = mesh;

        // if (lodData.lod == currentLOD)
        // {
            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;
            mesh.bounds = new Bounds(chunkPos + (new Vector3(0.5f, 0.5f, 0.5f) * terrainDensityData.width), Vector3.one * terrainDensityData.width);

            if (!terraforming)
            {
                assetSpawner.SpawnAssets();
            }
        // }
    }
    // Releases height buffers when the application is closed/stopped
    void OnDestroy()
    {
        if (heightsBuffer != null && heightsBuffer.IsValid())
        {
            heightsBuffer.Release();
        }
    }
    void OnDisable()
    {
        if (heightsBuffer != null && heightsBuffer.IsValid())
        {
            heightsBuffer.Release();
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
    
    // Old mesh setup code saved for reference

    // public void SetMeshValues(int vertexCount, Triangle[] vertexArray, bool terraforming)
    // {
    //     vertices.Clear();
    //     triangles.Clear();
    //     verticesNormals.Clear();
    //     vertices.Capacity = vertexCount * 3;
    //     verticesNormals.Capacity = vertexCount * 3;
    //     triangles.Capacity = vertexCount * 3;
    //     for (int i = 0; i < vertexCount; i++)
    //     {
    //         Triangle t = vertexArray[i];
    //         vertices.Add(t.v1.position);
    //         vertices.Add(t.v2.position);
    //         vertices.Add(t.v3.position);

    //         Vertex v1;
    //         v1.position = t.v1.position;
    //         v1.normal = t.v1.normal;
    //         verticesNormals.Add(v1);
    //         Vertex v2;
    //         v2.position = t.v2.position;
    //         v2.normal = t.v2.normal;
    //         verticesNormals.Add(v2);
    //         Vertex v3;
    //         v3.position = t.v3.position;
    //         v3.normal = t.v3.normal;
    //         verticesNormals.Add(v3);

    //         triangles.Add(i * 3);
    //         triangles.Add(i * 3 + 1);
    //         triangles.Add(i * 3 + 2);
    //     }
    //     SetupMesh(terraforming);
    // }

    // public void SetupMesh(bool terraforming)
    // {
    //     Mesh mesh = new Mesh();
    //     mesh.indexFormat = IndexFormat.UInt32;
    //     mesh.SetVertices(vertices);
    //     mesh.SetTriangles(triangles, 0);
    //     List<Vector3> normals = new List<Vector3>(verticesNormals.Count);
    //     for (int i = 0; i < verticesNormals.Count; i++)
    //     {
    //         normals.Add(verticesNormals[i].normal);
    //     }
    //     mesh.SetNormals(normals);
    //     mesh.RecalculateBounds();


    //     meshFilter.mesh = mesh;
    //     meshCollider.sharedMesh = mesh;

    //     assetSpawner.worldVertices = verticesNormals.ToArray();
    //     if (!terraforming)
    //     {
    //         assetSpawner.SpawnAssets();
    //     }

    //     if (Mathf.RoundToInt(chunkPos.y / terrainDensityData.width) == 0)
    //     {
    //         waterGen.UpdateMesh();
    //     }

    //     vertices.Clear();
    //     triangles.Clear();
    //     verticesNormals.Clear();
    // }
}
