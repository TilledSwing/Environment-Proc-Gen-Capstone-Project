using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeMarchingCubes : MonoBehaviour
{
    ComputeShader marchingCubesComputeShader;
    ComputeShader terrainDensityComputeShader;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    public float[] flatHeights;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private Renderer mat;
    public TerrainDensityData1 terrainDensityData;
    public GameObject waterPlaneGenerator;
    WaterPlaneGenerator waterGen;
    private AssetSpawner assetSpawner;
    public Vector3Int chunkPos;

    public struct Triangle
    {
        public Vector3 v1;
        public Vector3 v2;
        public Vector3 v3;
    }

    void Start()
    {
        InitializeChunk();
        SetNoiseSetting();
        SetHeights();
        if(Mathf.RoundToInt(chunkPos.y/terrainDensityData.width) == 0) {
            waterGen.UpdateMesh();
        }
        GenerateMesh();
        assetSpawner.SpawnAssets();
        SetTexture();
    }

    public void GenerateMesh() {
        vertices.Clear();
        triangles.Clear();
        int kernel = marchingCubesComputeShader.FindKernel("MarchingCubes");

        ComputeBuffer heightsBuffer = new ComputeBuffer(flatHeights.Length, sizeof(float));
        heightsBuffer.SetData(flatHeights);
        marchingCubesComputeShader.SetBuffer(kernel, "HeightsBuffer", heightsBuffer);
        ComputeBuffer vertexBuffer = new ComputeBuffer(terrainDensityData.width*terrainDensityData.width*terrainDensityData.width*5, sizeof(float) * 9, ComputeBufferType.Append);
        marchingCubesComputeShader.SetBuffer(kernel, "VertexBuffer", vertexBuffer);

        marchingCubesComputeShader.SetInt("ChunkSize", terrainDensityData.width);
        marchingCubesComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);
        marchingCubesComputeShader.SetFloat("isolevel", terrainDensityData.isolevel);
        marchingCubesComputeShader.SetBool("lerpToggle", terrainDensityData.lerp);

        vertexBuffer.SetCounterValue(0);
        marchingCubesComputeShader.Dispatch(kernel, Mathf.CeilToInt(terrainDensityData.width / 8f), Mathf.CeilToInt(terrainDensityData.width / 8f), Mathf.CeilToInt(terrainDensityData.width / 8f));

        heightsBuffer.Release();

        ComputeBuffer vertexCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        ComputeBuffer.CopyCount(vertexBuffer, vertexCountBuffer, 0);
        int[] vertexCountArray = { 0 };
        vertexCountBuffer.GetData(vertexCountArray);
        vertexCountBuffer.Release();
        int vertexCount = vertexCountArray[0];

        Triangle[] vertexArray = new Triangle[vertexCount];
        vertexBuffer.GetData(vertexArray, 0, 0, vertexCount);
        vertexBuffer.Release();

        StartCoroutine(SetupMesh(vertexCount, vertexArray));
    }

    public IEnumerator SetupMesh(int vertexCount, Triangle[] vertexArray) {
        for (int i = 0; i < vertexCount; i++) {
            Triangle t = vertexArray[i];
            vertices.Add(t.v1);
            vertices.Add(t.v2);
            vertices.Add(t.v3);
            triangles.Add(i * 3);
            triangles.Add(i * 3 + 1);
            triangles.Add(i * 3 + 2);
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
        yield return null;
    }

    public void InitializeChunk() {
        terrainDensityData = Resources.Load<TerrainDensityData1>("TerrainDensityData1");
        marchingCubesComputeShader = Resources.Load<ComputeShader>("Compute Shaders/MarchingCubes");
        terrainDensityComputeShader = Resources.Load<ComputeShader>("Compute Shaders/ComputeTerrainDensity");
        gameObject.AddComponent<MeshRenderer>();
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        assetSpawner = gameObject.GetComponent<AssetSpawner>();
        waterPlaneGenerator = new GameObject("Water");
        waterPlaneGenerator.transform.SetParent(transform);
        waterGen = waterPlaneGenerator.AddComponent<WaterPlaneGenerator>();
    }

    public void SetTexture() {
        mat = GetComponent<Renderer>();
        Material terrainMaterial = Resources.Load<Material>("Materials/TerrainTexture");
        mat.sharedMaterial = terrainMaterial;
        mat.sharedMaterial.SetFloat("_UnderwaterTexHeightEnd", terrainDensityData.waterLevel-15f);
        mat.sharedMaterial.SetFloat("_Tex1HeightStart", terrainDensityData.waterLevel-18f);
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

    /// <summary>
    /// Essentially the density function that will dictate the heights of the terrain
    /// </summary>
    private void SetHeights() {
        flatHeights = new float[(terrainDensityData.width+1) * (terrainDensityData.width+1) * (terrainDensityData.width+1)];

        int kernel = terrainDensityComputeShader.FindKernel("TerrainDensity");

        ComputeBuffer heightsBuffer = new ComputeBuffer(flatHeights.Length, sizeof(float));
        terrainDensityComputeShader.SetBuffer(kernel, "HeightsBuffer", heightsBuffer);
        terrainDensityComputeShader.Dispatch(kernel, Mathf.CeilToInt(terrainDensityData.width / 8f)+1, Mathf.CeilToInt(terrainDensityData.width / 8f)+1, Mathf.CeilToInt(terrainDensityData.width / 8f)+1);
        heightsBuffer.GetData(flatHeights);
        heightsBuffer.Release();
    }
}
