using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Unity.VisualScripting;

public class MarchingCubes : MonoBehaviour
{
    public float[,,] heights;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector3> normals = new List<Vector3>();
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Renderer mat;
    public TerrainDensityData terrainDensityData;
    public GameObject waterPlaneGenerator;
    private FastNoiseLite noiseGenerator = new FastNoiseLite();
    private FastNoiseLite domainWarp = new FastNoiseLite();
    private int cubesProcessed = 0;
    private int width;
    private int height;
    private float noiseScale;
    private float isolevel;
    private bool lerp;
    WaterPlaneGenerator waterGen;
    private AssetSpawner assetSpawner;
    public Vector3Int chunkPos;

    void Awake()
    {
        InitializeChunk();
    }

    void Start()
    {
        GenerateTerrainData();
    }

    public void InitializeChunk() { 
        gameObject.AddComponent<MeshRenderer>();
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        terrainDensityData = Resources.Load<TerrainDensityData>("TerrainDensityData");
        assetSpawner = gameObject.GetComponent<AssetSpawner>();
        // assetSpawner = gameObject.AddComponent<AssetSpawner>();
        // assetSpawner.chunkPos = chunkPos;
        mat = GetComponent<Renderer>();
        Material terrainMaterial = Resources.Load<Material>("Materials/TerrainTexture");
        mat.sharedMaterial = terrainMaterial;
        mat.sharedMaterial.SetFloat("_UnderwaterTexHeightEnd", terrainDensityData.waterLevel-15f);
        mat.sharedMaterial.SetFloat("_Tex1HeightStart", terrainDensityData.waterLevel-18f);
        waterPlaneGenerator = new GameObject("Water");
        waterPlaneGenerator.transform.SetParent(transform);
        waterGen = waterPlaneGenerator.AddComponent<WaterPlaneGenerator>();
    }

    /// <summary>
    /// Generates the terrain data for new seeds.
    /// </summary>
    public void GenerateTerrainData()
    {
        terrainDensityData.noiseSeed = UnityEngine.Random.Range(0, 10000);
        terrainDensityData.domainWarpSeed = UnityEngine.Random.Range(0, 10000);
        UpdateMesh();
    }

    /// <summary>
    /// Updates the terrain mesh
    /// </summary>
    public void UpdateMesh() {
        SetNoiseSetting();
        SetHeights();
        if(Mathf.RoundToInt(chunkPos.y/terrainDensityData.width) == 0) {
            waterGen.UpdateMesh();
        }
        mat.sharedMaterial.SetFloat("_UnderwaterTexHeightEnd", terrainDensityData.waterLevel-15f);
        mat.sharedMaterial.SetFloat("_Tex1HeightStart", terrainDensityData.waterLevel-18f);
        StartCoroutine(MarchCubes());
        // assetSpawner.ClearAssets();
        if(!terrainDensityData.polygonizationVisualization) {
            assetSpawner.SpawnAssets();
        }
    }

    /// <summary>
    /// Set up the MeshFilter's mesh with the given vertices and triangle
    /// </summary>
    private void SetupMesh() {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    private void SetNoiseSetting() {
        width = terrainDensityData.width;
        height = terrainDensityData.height;
        noiseScale = terrainDensityData.noiseScale;
        isolevel = terrainDensityData.isolevel;
        lerp = terrainDensityData.lerp;
        // Noise Values
        noiseGenerator.SetNoiseType(terrainDensityData.noiseType);
        noiseGenerator.SetFractalType(terrainDensityData.noiseFractalType);
        noiseGenerator.SetSeed(terrainDensityData.noiseSeed);
        noiseGenerator.SetFractalOctaves(terrainDensityData.noiseFractalOctaves);
        noiseGenerator.SetFractalLacunarity(terrainDensityData.noiseFractalLacunarity);
        noiseGenerator.SetFractalGain(terrainDensityData.noiseFractalGain);
        noiseGenerator.SetFractalWeightedStrength(terrainDensityData.fractalWeightedStrength);
        noiseGenerator.SetFrequency(terrainDensityData.noiseFrequency);
        // Cellular Values
        if(terrainDensityData.noiseType == FastNoiseLite.NoiseType.Cellular) {
            noiseGenerator.SetCellularDistanceFunction(terrainDensityData.cellularDistanceFunction);
            noiseGenerator.SetCellularReturnType(terrainDensityData.cellularReturnType);
            noiseGenerator.SetCellularJitter(terrainDensityData.cellularJitter);
        }
        // Domain Warp Values
        if(terrainDensityData.domainWarpToggle) {
            domainWarp.SetNoiseType(terrainDensityData.noiseType);
            domainWarp.SetFractalType(terrainDensityData.noiseFractalType);
            domainWarp.SetDomainWarpType(terrainDensityData.domainWarpType);
            domainWarp.SetDomainWarpAmp(terrainDensityData.domainWarpAmplitude);
            domainWarp.SetSeed(terrainDensityData.domainWarpSeed);
            domainWarp.SetFractalOctaves(terrainDensityData.domainWarpFractalOctaves);
            domainWarp.SetFractalLacunarity(terrainDensityData.domainWarpFractalLacunarity);
            domainWarp.SetFractalGain(terrainDensityData.domainWarpFractalGain);
            domainWarp.SetFrequency(terrainDensityData.domainWarpFrequency);
        }
    }

    /// <summary>
    /// Essentially the density function that will dictate the heights of the terrain
    /// </summary>
    private void SetHeights() {
        heights = new float[width+1, width+1, width+1];

        float xWarp = 0;
        float yWarp = 0;
        float zWarp = 0;

        for(int x = 0; x < width+1; x++) {
            for(int y = 0; y < width+1; y++) {
                for(int z = 0; z < width+1; z++) {
                    float currentHeight = 0;
                    xWarp = (x + chunkPos.x) * noiseScale;
                    yWarp = (y + chunkPos.y) * noiseScale;
                    zWarp = (z + chunkPos.z) * noiseScale;
                    if(terrainDensityData.domainWarpToggle) {
                        if(terrainDensityData.noiseDimension == TerrainDensityData.NoiseDimension._2D) {
                            domainWarp.DomainWarp(ref xWarp, ref zWarp);
                        }
                        else {
                            domainWarp.DomainWarp(ref xWarp, ref yWarp, ref zWarp);
                        }
                    }
                    if(terrainDensityData.noiseDimension == TerrainDensityData.NoiseDimension._2D) {
                        currentHeight = height * ((noiseGenerator.GetNoise(xWarp, zWarp)+1)/2) + (terrainDensityData.terracing ? (y%terrainDensityData.terraceHeight) : 0);
                    }
                    else {
                        currentHeight = height * ((noiseGenerator.GetNoise(xWarp, yWarp, zWarp)+1)/2) + (terrainDensityData.terracing ? (y%terrainDensityData.terraceHeight) : 0);
                    }

                    heights[x, y, z] = chunkPos.y + y - currentHeight;
                }
            }
        }
    }

    /// <summary>
    /// Marches through every cube in a given chunk
    /// </summary>
    public IEnumerator MarchCubes() {
        vertices.Clear();
        triangles.Clear();

        for(int x = 0; x < width; x++) {
            for(int y = 0; y < width; y++) {
                for(int z = 0; z < width; z++) {
                    float[] cubeVertices = new float[8];
                    for(int i = 0; i < 8; i++) {
                        Vector3Int vertex = new Vector3Int(x,y,z) + MarchingCubesTables.vertexOffsetTable[i];
                        cubeVertices[i] = heights[vertex.x,vertex.y,vertex.z];
                    }

                    MarchCube(new Vector3(chunkPos.x + x,chunkPos.y + y,chunkPos.z + z), cubeVertices);
                    cubesProcessed++;

                    if(cubesProcessed % terrainDensityData.polygonizationVisualizationRate == 0 && terrainDensityData.polygonizationVisualization) {
                        // Update the mesh after each cube
                        SetupMesh();
                        
                        // Wait for a frame or time delay to visualize it
                        yield return null; // Adjust delay as needed
                    }
                }
            }
        }
        SetupMesh();
        assetSpawner.SpawnAssets();
    }

    /// <summary>
    /// Polygonizes for a single given cube.
    /// </summary>
    /// <param name="cubePosition"> The world space position of the cube within the chunk </param>
    /// <param name="cubeVertices"> The values of the vertices for the given cube to be marched which will be used to get the configuration </param>
    public void MarchCube(Vector3 cubePosition, float[] cubeVertices) {
        int configurationIndex = GetCubeConfiguration(cubeVertices);

        if(configurationIndex == 0 || configurationIndex == 255) {
            return ;
        }

        int edgeIndex = 0;

        for(int tri = 0; tri < 5; tri++) {
            for(int vert = 0; vert < 3; vert++) {
                int edgeValue = MarchingCubesTables.triangleTable[configurationIndex, edgeIndex];

                if(edgeValue == -1) {
                    return ;
                }

                Vector3 edgeV1 = cubePosition + MarchingCubesTables.vertexOffsetTable[MarchingCubesTables.edgeIndexTable[edgeValue, 0]];
                Vector3 edgeV2 = cubePosition + MarchingCubesTables.vertexOffsetTable[MarchingCubesTables.edgeIndexTable[edgeValue, 1]];

                Vector3 vertex;
                if(lerp) {
                    vertex = Vector3.Lerp(edgeV1, edgeV2, 
                    (isolevel - cubeVertices[MarchingCubesTables.edgeIndexTable[edgeValue, 0]]) / (cubeVertices[MarchingCubesTables.edgeIndexTable[edgeValue, 1]] - cubeVertices[MarchingCubesTables.edgeIndexTable[edgeValue, 0]]));
                }
                else {
                    vertex = (edgeV1 + edgeV2) / 2;
                }

                vertices.Add(vertex);
                triangles.Add(vertices.Count - 1);

                Vector3 faceNormal = Vector3.Cross(edgeV2 - vertex, edgeV1 - vertex).normalized;
                normals.Add(faceNormal);

                edgeIndex++;
            }
        }
    }

    /// <summary>
    /// Retrieve the configuration index based on a cube's vertice's values
    /// </summary>
    /// <param name="cubeValues"> The values of the vertices for the given cube </param>
    /// <returns></returns>
    private int GetCubeConfiguration(float[] cubeValues) {
        int cubeIndex = 0;
        // Compare the value of each vertice with the iso level and do bitshifting
        for(int i = 0; i < 8; i++) {
            if(cubeValues[i] > isolevel) cubeIndex |= 1 << i;
        }

        return cubeIndex;
    }
}
