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
    public ComputeShader caveNoiseComputeShader;
    public ComputeShader terraformComputeShader;
    public Material terrainMaterial;
    public Material waterMaterial;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    // public List<Vector3> vertices = new List<Vector3>();
    // public List<Vertex> verticesNormals = new List<Vertex>();
    // private List<int> triangles = new List<int>();
    public TerrainDensityData1 terrainDensityData;
    public GameObject waterPlaneGenerator;
    WaterPlaneGenerator waterGen;
    private AssetSpawner assetSpawner;
    public Vector3Int chunkPos;
    public ComputeBuffer heightsBuffer;
    public ChunkGenNetwork chunkGenNetwork;

    public struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
    }

    public struct Triangle
    {
        public Vertex v1;
        public Vertex v2;
        public Vertex v3;
    }

    void Start()
    {
        ChunkSetup();
        SetNoiseSetting();
        GenerateMesh();
        // StartCoroutine(GenerateMesh());
    }
    // Set up the chunk data
    public void ChunkSetup()
    {
        meshFilter = gameObject.GetComponent<MeshFilter>();
        meshCollider = gameObject.GetComponent<MeshCollider>();
        assetSpawner = gameObject.GetComponent<AssetSpawner>();
        // Set up and start water generator
        waterPlaneGenerator = new GameObject("Water");
        waterPlaneGenerator.transform.SetParent(transform);
        waterPlaneGenerator.AddComponent<MeshFilter>();
        MeshRenderer waterMat = waterPlaneGenerator.AddComponent<MeshRenderer>();
        waterMat.material = waterMaterial;
        waterGen = waterPlaneGenerator.AddComponent<WaterPlaneGenerator>();
        waterGen.terrainDensityData = terrainDensityData;
        waterGen.marchingCubes = this;
    }
    // Set all the noise settings from the TerrainDensityData scriptable object
    private void SetNoiseSetting()
    {
        // Noise and Fractal Values
        terrainNoiseComputeShader.SetInt("noiseDimension", (int)terrainDensityData.noiseDimension);
        terrainNoiseComputeShader.SetInt("noiseType", (int)terrainDensityData.noiseType);
        terrainNoiseComputeShader.SetInt("noiseFractalType", (int)terrainDensityData.noiseFractalType);
        terrainNoiseComputeShader.SetInt("rotationType3D", (int)terrainDensityData.rotationType3D);
        terrainNoiseComputeShader.SetInt("noiseSeed", terrainDensityData.noiseSeed);
        terrainNoiseComputeShader.SetInt("noiseFractalOctaves", terrainDensityData.noiseFractalOctaves);
        terrainNoiseComputeShader.SetFloat("noiseFractalLacunarity", terrainDensityData.noiseFractalLacunarity);
        terrainNoiseComputeShader.SetFloat("noiseFractalGain", terrainDensityData.noiseFractalGain);
        terrainNoiseComputeShader.SetFloat("fractalWeightedStrength", terrainDensityData.fractalWeightedStrength);
        terrainNoiseComputeShader.SetFloat("noiseFrequency", terrainDensityData.noiseFrequency);

        // Domain Warp Values
        terrainNoiseComputeShader.SetBool("domainWarpToggle", terrainDensityData.domainWarpToggle);
        terrainNoiseComputeShader.SetInt("domainWarpType", (int)terrainDensityData.domainWarpType);
        terrainDensityComputeShader.SetInt("domainWarpFractalType", (int)terrainDensityData.domainWarpFractalType);
        terrainNoiseComputeShader.SetFloat("domainWarpAmplitude", terrainDensityData.domainWarpAmplitude);
        terrainNoiseComputeShader.SetInt("domainWarpSeed", terrainDensityData.domainWarpSeed);
        terrainNoiseComputeShader.SetInt("domainWarpFractalOctaves", terrainDensityData.domainWarpFractalOctaves);
        terrainNoiseComputeShader.SetFloat("domainWarpFractalLacunarity", terrainDensityData.domainWarpFractalLacunarity);
        terrainNoiseComputeShader.SetFloat("domainWarpFractalGain", terrainDensityData.domainWarpFractalGain);
        terrainNoiseComputeShader.SetFloat("domainWarpFrequency", terrainDensityData.domainWarpFrequency);
        // Cellular(Voronoi) Values
        terrainNoiseComputeShader.SetInt("cellularDistanceFunction", (int)terrainDensityData.cellularDistanceFunction);
        terrainNoiseComputeShader.SetInt("cellularReturnType", (int)terrainDensityData.cellularReturnType);
        terrainNoiseComputeShader.SetFloat("cellularJitter", terrainDensityData.cellularJitter);
        // Terrain Values
        terrainNoiseComputeShader.SetFloat("noiseScale", terrainDensityData.noiseScale);
        terrainNoiseComputeShader.SetInt("ChunkSize", terrainDensityData.width);
        terrainNoiseComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);

        // Cave Noise and Fractal Values
        caveNoiseComputeShader.SetInt("noiseDimension", (int)terrainDensityData.caveNoiseDimension);
        caveNoiseComputeShader.SetInt("noiseType", (int)terrainDensityData.caveNoiseType);
        caveNoiseComputeShader.SetInt("noiseFractalType", (int)terrainDensityData.caveNoiseFractalType);
        caveNoiseComputeShader.SetInt("rotationType3D", (int)terrainDensityData.caveRotationType3D);
        caveNoiseComputeShader.SetInt("noiseSeed", terrainDensityData.caveNoiseSeed);
        caveNoiseComputeShader.SetInt("noiseFractalOctaves", terrainDensityData.caveNoiseFractalOctaves);
        caveNoiseComputeShader.SetFloat("noiseFractalLacunarity", terrainDensityData.caveNoiseFractalLacunarity);
        caveNoiseComputeShader.SetFloat("noiseFractalGain", terrainDensityData.caveNoiseFractalGain);
        caveNoiseComputeShader.SetFloat("fractalWeightedStrength", terrainDensityData.caveFractalWeightedStrength);
        caveNoiseComputeShader.SetFloat("noiseFrequency", terrainDensityData.caveNoiseFrequency);
        // Domain Warp Values
        caveNoiseComputeShader.SetBool("domainWarpToggle", terrainDensityData.caveDomainWarpToggle);
        caveNoiseComputeShader.SetInt("domainWarpType", (int)terrainDensityData.caveDomainWarpType);
        caveNoiseComputeShader.SetInt("domainWarpFractalType", (int)terrainDensityData.caveDomainWarpFractalType);
        caveNoiseComputeShader.SetFloat("domainWarpAmplitude", terrainDensityData.caveDomainWarpAmplitude);
        caveNoiseComputeShader.SetInt("domainWarpSeed", terrainDensityData.caveDomainWarpSeed);
        caveNoiseComputeShader.SetInt("domainWarpFractalOctaves", terrainDensityData.caveDomainWarpFractalOctaves);
        caveNoiseComputeShader.SetFloat("domainWarpFractalLacunarity", terrainDensityData.caveDomainWarpFractalLacunarity);
        caveNoiseComputeShader.SetFloat("domainWarpFractalGain", terrainDensityData.caveDomainWarpFractalGain);
        caveNoiseComputeShader.SetFloat("domainWarpFrequency", terrainDensityData.caveDomainWarpFrequency);
        // Cellular(Voronoi) Values
        caveNoiseComputeShader.SetInt("cellularDistanceFunction", (int)terrainDensityData.caveCellularDistanceFunction);
        caveNoiseComputeShader.SetInt("cellularReturnType", (int)terrainDensityData.caveCellularReturnType);
        caveNoiseComputeShader.SetFloat("cellularJitter", terrainDensityData.caveCellularJitter);
        // Terrain Values
        caveNoiseComputeShader.SetFloat("noiseScale", terrainDensityData.caveNoiseScale);
        caveNoiseComputeShader.SetInt("ChunkSize", terrainDensityData.width);
        caveNoiseComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);

        // Terrain Values
        terrainDensityComputeShader.SetInt("height", terrainDensityData.height);
        terrainDensityComputeShader.SetBool("terracing", terrainDensityData.terracing);
        terrainDensityComputeShader.SetInt("terraceHeight", terrainDensityData.terraceHeight);
        terrainDensityComputeShader.SetInt("ChunkSize", terrainDensityData.width);
        terrainDensityComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);
        terrainDensityComputeShader.SetFloat("isolevel", terrainDensityData.isolevel);
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

        if (!chunkGenNetwork.initialLoadComplete)
        {
            SyncMarchingCubes(heightsBuffer, false);
        }
        else
        {
            AsyncMarchingCubes(heightsBuffer, false);
        }
    }
    /// <summary>
    /// Set up the density values for the chunk using compute shaders
    /// </summary>
    /// <returns>The buffer the density values are stored in</returns>
    public ComputeBuffer SetHeights()
    {
        int terrainNoiseKernel = terrainNoiseComputeShader.FindKernel("TerrainNoise");
        int caveNoiseKernel = caveNoiseComputeShader.FindKernel("CaveNoise");
        int densityKernel = terrainDensityComputeShader.FindKernel("TerrainDensity");

        ComputeBuffer terrainNoiseBuffer = new ComputeBuffer((terrainDensityData.width + 1) * (terrainDensityData.width + 1) * (terrainDensityData.width + 1), sizeof(float));
        ComputeBuffer caveNoiseBuffer = new ComputeBuffer((terrainDensityData.width + 1) * (terrainDensityData.width + 1) * (terrainDensityData.width + 1), sizeof(float));
        heightsBuffer = new ComputeBuffer((terrainDensityData.width + 1) * (terrainDensityData.width + 1) * (terrainDensityData.width + 1), sizeof(float));

        terrainNoiseComputeShader.SetBuffer(terrainNoiseKernel, "TerrainNoiseBuffer", terrainNoiseBuffer);
        caveNoiseComputeShader.SetBuffer(caveNoiseKernel, "CaveNoiseBuffer", caveNoiseBuffer);

        terrainDensityComputeShader.SetBuffer(densityKernel, "TerrainNoiseBuffer", terrainNoiseBuffer);
        terrainDensityComputeShader.SetBuffer(densityKernel, "CaveNoiseBuffer", caveNoiseBuffer);
        terrainDensityComputeShader.SetBuffer(densityKernel, "HeightsBuffer", heightsBuffer);

        terrainNoiseComputeShader.Dispatch(terrainNoiseKernel, Mathf.CeilToInt(terrainDensityData.width / 4f) + 1, Mathf.CeilToInt(terrainDensityData.width / 4f) + 1, Mathf.CeilToInt(terrainDensityData.width / 4f) + 1);
        caveNoiseComputeShader.Dispatch(caveNoiseKernel, Mathf.CeilToInt(terrainDensityData.width / 4f) + 1, Mathf.CeilToInt(terrainDensityData.width / 4f) + 1, Mathf.CeilToInt(terrainDensityData.width / 4f) + 1);
        terrainDensityComputeShader.Dispatch(densityKernel, Mathf.CeilToInt(terrainDensityData.width / 4f) + 1, Mathf.CeilToInt(terrainDensityData.width / 4f) + 1, Mathf.CeilToInt(terrainDensityData.width / 4f) + 1);

        terrainNoiseBuffer.Release();
        caveNoiseBuffer.Release();

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
        ComputeBuffer vertexBuffer = new ComputeBuffer(terrainDensityData.width * terrainDensityData.width * terrainDensityData.width * 5, sizeof(float) * 18, ComputeBufferType.Append);
        marchingCubesComputeShader.SetBuffer(marchingKernel, "VertexBuffer", vertexBuffer);

        marchingCubesComputeShader.SetInt("ChunkSize", terrainDensityData.width);
        marchingCubesComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);
        marchingCubesComputeShader.SetFloat("isolevel", terrainDensityData.isolevel);
        marchingCubesComputeShader.SetBool("lerpToggle", terrainDensityData.lerp);

        vertexBuffer.SetCounterValue(0);
        marchingCubesComputeShader.Dispatch(marchingKernel, Mathf.CeilToInt(terrainDensityData.width / 4f), Mathf.CeilToInt(terrainDensityData.width / 4f), Mathf.CeilToInt(terrainDensityData.width / 4f));

        ComputeBuffer vertexCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        ComputeBuffer.CopyCount(vertexBuffer, vertexCountBuffer, 0);

        chunkGenNetwork.pendingReadbacks.Enqueue(new ChunkGenNetwork.ReadbackRequest(vertexCountBuffer, (AsyncGPUReadbackRequest countRequest) =>
        {
            if (countRequest.hasError)
            {
                Debug.LogError("Failed to read vertex count.");
                vertexCountBuffer.Release();
                vertexBuffer.Release();
                return;
            }

            int vertexCount = countRequest.GetData<int>()[0];
            vertexCountBuffer.Release();

            chunkGenNetwork.pendingReadbacks.Enqueue(new ChunkGenNetwork.ReadbackRequest(vertexBuffer, (AsyncGPUReadbackRequest dataRequest) =>
            {
                if (dataRequest.hasError)
                {
                    Debug.LogError("Failed to read vertex buffer.");
                    vertexBuffer.Release();
                    return;
                }

                Triangle[] vertexArray = new Triangle[vertexCount];
                NativeArray<Triangle> rawData = dataRequest.GetData<Triangle>();

                for (int i = 0; i < vertexCount; i++)
                {
                    vertexArray[i] = rawData[i];
                }

                vertexBuffer.Release();

                if (Mathf.RoundToInt(chunkPos.y / terrainDensityData.width) == 0)
                {
                    // waterGen.UpdateMesh();
                }

                if (vertexCount > 0)
                {
                    SetMeshValuesPerformant(vertexCount, vertexArray, terraforming);
                    // chunkGenNetwork.pendingMeshInits.Enqueue(() =>
                    //     SetMeshValuesPerformant(vertexCount, vertexArray, terraforming)
                    // );
                }
            }));
        }));

        // int[] vertexCountArray = { 0 };
        // vertexCountBuffer.GetData(vertexCountArray);

        // vertexCountBuffer.Release();

        // int vertexCount = vertexCountArray[0];

        // Triangle[] vertexArray = new Triangle[vertexCount];
        // vertexBuffer.GetData(vertexArray, 0, 0, vertexCount);

        // vertexBuffer.Release();

        // SetMeshValues(vertexCount, vertexArray);
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
        ComputeBuffer vertexBuffer = new ComputeBuffer(terrainDensityData.width * terrainDensityData.width * terrainDensityData.width * 5, sizeof(float) * 18, ComputeBufferType.Append);
        marchingCubesComputeShader.SetBuffer(marchingKernel, "VertexBuffer", vertexBuffer);

        marchingCubesComputeShader.SetInt("ChunkSize", terrainDensityData.width);
        marchingCubesComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);
        marchingCubesComputeShader.SetFloat("isolevel", terrainDensityData.isolevel);
        marchingCubesComputeShader.SetBool("lerpToggle", terrainDensityData.lerp);

        vertexBuffer.SetCounterValue(0);
        marchingCubesComputeShader.Dispatch(marchingKernel, Mathf.CeilToInt(terrainDensityData.width / 4f), Mathf.CeilToInt(terrainDensityData.width / 4f), Mathf.CeilToInt(terrainDensityData.width / 4f));

        ComputeBuffer vertexCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        ComputeBuffer.CopyCount(vertexBuffer, vertexCountBuffer, 0);

        AsyncGPUReadback.Request(vertexCountBuffer, (AsyncGPUReadbackRequest countRequest) =>
        {
            if (countRequest.hasError)
            {
                Debug.LogError("Failed to read vertex count.");
                vertexCountBuffer.Release();
                vertexBuffer.Release();
                return;
            }

            int vertexCount = countRequest.GetData<int>()[0];
            vertexCountBuffer.Release();

            AsyncGPUReadback.Request(vertexBuffer, (AsyncGPUReadbackRequest dataRequest) =>
            {
                if (dataRequest.hasError)
                {
                    Debug.LogError("Failed to read vertex buffer.");
                    vertexBuffer.Release();
                    return;
                }

                Triangle[] vertexArray = new Triangle[vertexCount];
                NativeArray<Triangle>rawData = dataRequest.GetData<Triangle>();

                for (int i = 0; i < vertexCount; i++)
                {
                    vertexArray[i] = rawData[i];
                }

                vertexBuffer.Release();

                if (Mathf.RoundToInt(chunkPos.y / terrainDensityData.width) == 0)
                {
                    // waterGen.UpdateMesh();
                }

                if (vertexCount > 0)
                {
                    SetMeshValuesPerformant(vertexCount, vertexArray, terraforming);
                }
            });
        });
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
            assetSpawner.worldVertices = vertexBuffer.ToArray();
        }

        Mesh mesh = new Mesh();
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh, MeshUpdateFlags.Default);

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
        mesh.bounds = new Bounds(chunkPos + (new Vector3(0.5f, 0.5f, 0.5f) * terrainDensityData.width), Vector3.one * terrainDensityData.width);

        if (!terraforming)
        {
            assetSpawner.SpawnAssets();
        }
    }
    // Releases height buffers when the application is closed
    void OnApplicationQuit()
    {
        if (heightsBuffer != null)
        {
            heightsBuffer.Release();
        }
    }
    /// <summary>
    /// Draws wireframe cubes to visualize chunks
    /// </summary>
    void OnDrawGizmos()
    {
        if (terrainDensityData == null)
        {
            terrainDensityData = Resources.Load<TerrainDensityData1>("TerrainDensityData1");
            if (terrainDensityData == null) return; // still not found
        }
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
