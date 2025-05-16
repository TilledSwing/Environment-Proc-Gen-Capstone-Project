using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class ComputeMarchingCubes : MonoBehaviour
{
    ComputeShader marchingCubesComputeShader;
    ComputeShader terrainDensityComputeShader;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    public List<Vector3> vertices = new List<Vector3>();
    public List<Vertex> verticesNormals = new List<Vertex>();
    private List<int> triangles = new List<int>();
    private Renderer mat;
    public TerrainDensityData1 terrainDensityData;
    public GameObject waterPlaneGenerator;
    WaterPlaneGenerator waterGen;
    private AssetSpawner assetSpawner;
    public Vector3Int chunkPos;

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
        InitializeChunk();
        SetNoiseSetting();
        GenerateMesh();
        if(Mathf.RoundToInt(chunkPos.y/terrainDensityData.width) == 0) {
            waterGen.UpdateMesh();
        }
        assetSpawner.SpawnAssets();
        SetTexture();
    }

    void OnMeshReady() {
        // if(Mathf.RoundToInt(chunkPos.y/terrainDensityData.width) == 0) {
        //     waterGen.UpdateMesh();
        // }
        // assetSpawner.SpawnAssets();
        SetTexture();
    }

    public void GenerateMesh() {
        int marchingKernel = marchingCubesComputeShader.FindKernel("MarchingCubes");
        int densityKernel = terrainDensityComputeShader.FindKernel("TerrainDensity");

        ComputeBuffer heightsBuffer = new ComputeBuffer((terrainDensityData.width+1) * (terrainDensityData.width+1) * (terrainDensityData.width+1), sizeof(float));
        terrainDensityComputeShader.SetBuffer(densityKernel, "HeightsBuffer", heightsBuffer);
        marchingCubesComputeShader.SetBuffer(marchingKernel, "HeightsBuffer", heightsBuffer);
        ComputeBuffer vertexBuffer = new ComputeBuffer(terrainDensityData.width*terrainDensityData.width*terrainDensityData.width*5, sizeof(float) * 18, ComputeBufferType.Append);
        marchingCubesComputeShader.SetBuffer(marchingKernel, "VertexBuffer", vertexBuffer);

        marchingCubesComputeShader.SetInt("ChunkSize", terrainDensityData.width);
        marchingCubesComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);
        marchingCubesComputeShader.SetFloat("isolevel", terrainDensityData.isolevel);
        marchingCubesComputeShader.SetBool("lerpToggle", terrainDensityData.lerp);

        vertexBuffer.SetCounterValue(0);
        terrainDensityComputeShader.Dispatch(densityKernel, Mathf.CeilToInt(terrainDensityData.width / 8f)+1, Mathf.CeilToInt(terrainDensityData.width / 8f)+1, Mathf.CeilToInt(terrainDensityData.width / 8f)+1);
        marchingCubesComputeShader.Dispatch(marchingKernel, Mathf.CeilToInt(terrainDensityData.width / 8f), Mathf.CeilToInt(terrainDensityData.width / 8f), Mathf.CeilToInt(terrainDensityData.width / 8f));

        heightsBuffer.Release();

        ComputeBuffer vertexCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        ComputeBuffer.CopyCount(vertexBuffer, vertexCountBuffer, 0);

        // AsyncGPUReadback.Request(vertexCountBuffer, (AsyncGPUReadbackRequest countRequest) => {
        //     if (countRequest.hasError)
        //     {
        //         Debug.LogError("Failed to read vertex count.");
        //         vertexCountBuffer.Release();
        //         vertexBuffer.Release();
        //         return ;
        //     }

        //     int vertexCount = countRequest.GetData<int>()[0];
        //     vertexCountBuffer.Release();

        //     AsyncGPUReadback.Request(vertexBuffer, (AsyncGPUReadbackRequest dataRequest) => {
        //         if (dataRequest.hasError)
        //         {
        //             Debug.LogError("Failed to read vertex buffer.");
        //             vertexBuffer.Release();
        //             return ;
        //         }

        //         Triangle[] vertexArray = new Triangle[vertexCount];
        //         NativeArray<Triangle> rawData = dataRequest.GetData<Triangle>();

        //         for (int i = 0; i < vertexCount; i++) {
        //             vertexArray[i] = rawData[i];
        //         }

        //         vertexBuffer.Release();

        //         SetupMesh(vertexCount, vertexArray);
        //         OnMeshReady();
        //     });
        // });

        int[] vertexCountArray = { 0 };
        vertexCountBuffer.GetData(vertexCountArray);
        vertexCountBuffer.Release();
        int vertexCount = vertexCountArray[0];

        Triangle[] vertexArray = new Triangle[vertexCount];
        vertexBuffer.GetData(vertexArray, 0, 0, vertexCount);
        vertexBuffer.Release();

        SetupMesh(vertexCount, vertexArray);
    }

    public void SetupMesh(int vertexCount, Triangle[] vertexArray) {
        vertices.Clear();
        triangles.Clear();
        vertices.Capacity = vertexCount * 3;
        verticesNormals.Capacity = vertexCount * 3;
        triangles.Capacity = vertexCount * 3;
        for (int i = 0; i < vertexCount; i++) {
            Triangle t = vertexArray[i];
            vertices.Add(t.v1.position);
            vertices.Add(t.v2.position);
            vertices.Add(t.v3.position);
            
            Vertex v1;
            v1.position = t.v1.position;
            v1.normal = t.v1.normal;
            verticesNormals.Add(v1);
            Vertex v2;
            v2.position = t.v2.position;
            v2.normal = t.v2.normal;
            verticesNormals.Add(v2);
            Vertex v3;
            v3.position = t.v3.position;
            v3.normal = t.v3.normal;
            verticesNormals.Add(v3);

            triangles.Add(i * 3);
            triangles.Add(i * 3 + 1);
            triangles.Add(i * 3 + 2);
        }
        assetSpawner.worldVertices = verticesNormals.ToArray();

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    public void InitializeChunk() {
        terrainDensityData = Resources.Load<TerrainDensityData1>("TerrainDensityData1");
        marchingCubesComputeShader = Resources.Load<ComputeShader>("Compute Shaders/MarchingCubes");
        terrainDensityComputeShader = Resources.Load<ComputeShader>("Compute Shaders/TerrainDensity");
        gameObject.AddComponent<MeshRenderer>();
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshCollider = gameObject.GetComponent<MeshCollider>();
        assetSpawner = gameObject.GetComponent<AssetSpawner>();
        waterPlaneGenerator = new GameObject("Water");
        waterPlaneGenerator.transform.SetParent(transform);
        waterGen = waterPlaneGenerator.AddComponent<WaterPlaneGenerator>();
    }

    public void SetTexture() {
        mat = GetComponent<Renderer>();
        Material terrainMaterial = Resources.Load<Material>("Materials/TerrainTexture");
        mat.material = terrainMaterial;
        mat.material.SetFloat("_UnderwaterTexHeightEnd", terrainDensityData.waterLevel-15f);
        mat.material.SetFloat("_Tex1HeightStart", terrainDensityData.waterLevel-18f);
    }

    private void SetNoiseSetting() {
        // Noise and Fractal Values
        terrainDensityComputeShader.SetInt("noiseDimension", (int)terrainDensityData.noiseDimension);
        terrainDensityComputeShader.SetInt("noiseType", (int)terrainDensityData.noiseType );
        terrainDensityComputeShader.SetInt("noiseFractalType", (int)terrainDensityData.noiseFractalType);
        terrainDensityComputeShader.SetInt("rotationType3D", (int)terrainDensityData.rotationType3D);
        terrainDensityComputeShader.SetInt("noiseSeed", terrainDensityData.noiseSeed);
        terrainDensityComputeShader.SetInt("noiseFractalOctaves", terrainDensityData.noiseFractalOctaves);
        terrainDensityComputeShader.SetFloat("noiseFractalLacunarity", terrainDensityData.noiseFractalLacunarity);
        terrainDensityComputeShader.SetFloat("noiseFractalGain", terrainDensityData.noiseFractalGain);
        terrainDensityComputeShader.SetFloat("fractalWeightedStrength",terrainDensityData.fractalWeightedStrength );
        terrainDensityComputeShader.SetFloat("noiseFrequency", terrainDensityData.noiseFrequency);
        // Domain Warp Values
        terrainDensityComputeShader.SetBool("domainWarpToggle", terrainDensityData.domainWarpToggle);
        terrainDensityComputeShader.SetInt("domainWarpType", (int)terrainDensityData.domainWarpType);
        terrainDensityComputeShader.SetInt("domainWarpFractalType", (int)terrainDensityData.domainWarpFractalType);
        terrainDensityComputeShader.SetFloat("domainWarpAmplitude", terrainDensityData.domainWarpAmplitude);
        terrainDensityComputeShader.SetInt("domainWarpSeed", terrainDensityData.domainWarpSeed);
        terrainDensityComputeShader.SetInt("domainWarpFractalOctaves", terrainDensityData.domainWarpFractalOctaves);
        terrainDensityComputeShader.SetFloat("domainWarpFractalLacunarity", terrainDensityData.domainWarpFractalLacunarity);
        terrainDensityComputeShader.SetFloat("domainWarpFractalGain", terrainDensityData.domainWarpFractalGain);
        terrainDensityComputeShader.SetFloat("domainWarpFrequency", terrainDensityData.domainWarpFrequency);
        // Cellular(Voronoi) Values
        terrainDensityComputeShader.SetInt("cellularDistanceFunction", (int)terrainDensityData.cellularDistanceFunction);
        terrainDensityComputeShader.SetInt("cellularReturnType", (int)terrainDensityData.cellularReturnType);
        terrainDensityComputeShader.SetFloat("cellularJitter", terrainDensityData.cellularJitter);
        // Terrain Values
        terrainDensityComputeShader.SetInt("height", terrainDensityData.height);
        terrainDensityComputeShader.SetFloat("noiseScale", terrainDensityData.noiseScale);
        terrainDensityComputeShader.SetBool("terracing", terrainDensityData.terracing);
        terrainDensityComputeShader.SetInt("terraceHeight", terrainDensityData.terraceHeight);
        terrainDensityComputeShader.SetInt("ChunkSize", terrainDensityData.width);
        terrainDensityComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);
    }
}
