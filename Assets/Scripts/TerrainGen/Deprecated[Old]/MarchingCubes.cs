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
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Renderer mat;
    public TerrainDensityData terrainDensityData;
    public GameObject waterPlaneGenerator;
    // Base Noise
    private FastNoiseLite baseNoiseGenerator = new FastNoiseLite();
    private float[,,] baseNoise;
    private FastNoiseLite baseNoiseDomainWarp = new FastNoiseLite();
    private float baseNoiseScale;
    // Large Cave
    private FastNoiseLite largeCaveNoiseGenerator = new FastNoiseLite();
    private float[,,] largeCaveNoise;
    private FastNoiseLite largeCaveDomainWarp = new FastNoiseLite();
    private float largeCaveNoiseScale;
    // Cave Detail
    private FastNoiseLite caveDetailNoiseGenerator = new FastNoiseLite();
    private float[,,] caveDetailNoise;
    private FastNoiseLite caveDetailDomainWarp = new FastNoiseLite();
    private float caveDetailNoiseScale;
    // Continentalness
    private FastNoiseLite continentalnessNoiseGenerator = new FastNoiseLite();
    private float[,,] continentalnessNoise;
    private FastNoiseLite continentalnessDomainWarp = new FastNoiseLite();
    private float continentalnessNoiseScale;
    // Temperature
    private FastNoiseLite temperatureNoiseGenerator = new FastNoiseLite();
    private float[,,] temperatureNoise;
    private FastNoiseLite temperatureDomainWarp = new FastNoiseLite();
    private float temperatureNoiseScale;
    // Humidity
    private FastNoiseLite humidityNoiseGenerator = new FastNoiseLite();
    private float[,,] humidityNoise;
    private FastNoiseLite humidityDomainWarp = new FastNoiseLite();
    private float humidityNoiseScale;
    // Peaks and Valleys
    private FastNoiseLite peaksAndValleysNoiseGenerator = new FastNoiseLite();
    private float[,,] peaksAndValleysNoise;
    private FastNoiseLite peaksAndValleysDomainWarp = new FastNoiseLite();
    private float peaksAndValleysNoiseScale;
    // Erosion 
    private FastNoiseLite erosionNoiseGenerator = new FastNoiseLite();
    private float[,,] erosionNoise;
    private FastNoiseLite erosionDomainWarp = new FastNoiseLite();
    private float erosionNoiseScale;
    private AssetSpawner assetSpawner;
    public Vector3Int chunkPos;

    void Start()
    {
        GenerateTerrainData();
    }

    /// <summary>
    /// Generates the terrain data for new seeds.
    /// </summary>
    public void GenerateTerrainData()
    {
        UpdateMesh();
    }

    /// <summary>
    /// Updates the terrain mesh
    /// </summary>
    public void UpdateMesh() {
        foreach (NoiseGenerator noiseGenerator in terrainDensityData.noiseGenerators) {
            SetNoiseSetting(noiseGenerator);
        }
        // SetHeights();
        // MarchCubes();
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

    private void SetNoiseSetting(NoiseGenerator noiseGenerator)
    {
        FastNoiseLite generator = new();
        FastNoiseLite domainWarpGenerator = new();
        // Noise Values
        generator.SetNoiseType(noiseGenerator.noiseTypeOptions[(int)noiseGenerator.noiseType]);
        generator.SetFractalType(noiseGenerator.noiseFractalTypeOptions[(int)noiseGenerator.noiseFractalType]);
        generator.SetRotationType3D(noiseGenerator.rotationType3DOptions[(int)noiseGenerator.rotationType3D]);
        generator.SetSeed(noiseGenerator.noiseSeed);
        generator.SetFractalOctaves(noiseGenerator.noiseFractalOctaves);
        generator.SetFractalLacunarity(noiseGenerator.noiseFractalLacunarity);
        generator.SetFractalGain(noiseGenerator.noiseFractalGain);
        generator.SetFractalWeightedStrength(noiseGenerator.fractalWeightedStrength);
        generator.SetFrequency(noiseGenerator.noiseFrequency);
        // Cellular Values
        generator.SetCellularDistanceFunction(noiseGenerator.cellularDistanceFunctionOptions[(int)noiseGenerator.cellularDistanceFunction]);
        generator.SetCellularReturnType(noiseGenerator.cellularReturnTypeOptions[(int)noiseGenerator.cellularReturnType]);
        generator.SetCellularJitter(noiseGenerator.cellularJitter);
        // Domain Warp Values
        domainWarpGenerator.SetFractalType(noiseGenerator.domainWarpFractalTypeOptions[(int)noiseGenerator.domainWarpFractalType]);
        domainWarpGenerator.SetDomainWarpType(noiseGenerator.domainWarpTypeOptions[(int)noiseGenerator.domainWarpType]);
        domainWarpGenerator.SetDomainWarpAmp(noiseGenerator.domainWarpAmplitude);
        domainWarpGenerator.SetSeed(noiseGenerator.domainWarpSeed);
        domainWarpGenerator.SetFractalOctaves(noiseGenerator.domainWarpFractalOctaves);
        domainWarpGenerator.SetFractalLacunarity(noiseGenerator.domainWarpFractalLacunarity);
        domainWarpGenerator.SetFractalGain(noiseGenerator.domainWarpFractalGain);
        domainWarpGenerator.SetFrequency(noiseGenerator.domainWarpFrequency);
        if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.BaseGenerator)
        {
            baseNoiseGenerator = generator;
            baseNoiseDomainWarp = domainWarpGenerator;
            baseNoiseScale = noiseGenerator.noiseScale;
        }
        if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.LargeCaveGenerator)
        {
            largeCaveNoiseGenerator = generator;
            largeCaveDomainWarp = domainWarpGenerator;
            largeCaveNoiseScale = noiseGenerator.noiseScale;
        }
        if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.CaveDetail1Generator)
        {
            caveDetailNoiseGenerator = generator;
            caveDetailDomainWarp = domainWarpGenerator;
            caveDetailNoiseScale = noiseGenerator.noiseScale;
        }
        if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.ContinentalnessGenerator)
        {
            continentalnessNoiseGenerator = generator;
            continentalnessDomainWarp = domainWarpGenerator;
            continentalnessNoiseScale = noiseGenerator.noiseScale;
        }
        if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.TemperatureMapGenerator)
        {
            temperatureNoiseGenerator = generator;
            temperatureDomainWarp = domainWarpGenerator;
            temperatureNoiseScale = noiseGenerator.noiseScale;
        }
        if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.HumidityMapGenerator)
        {
            humidityNoiseGenerator = generator;
            humidityDomainWarp = domainWarpGenerator;
            humidityNoiseScale = noiseGenerator.noiseScale;
        }
        if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.PeaksAndValleysMapGenerator)
        {
            peaksAndValleysNoiseGenerator = generator;
            peaksAndValleysDomainWarp = domainWarpGenerator;
            peaksAndValleysNoiseScale = noiseGenerator.noiseScale;
        }
        if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.ErosionMapGenerator)
        {
            erosionNoiseGenerator = generator;
            erosionDomainWarp = domainWarpGenerator;
            erosionNoiseScale = noiseGenerator.noiseScale;
        }
    }

    /// <summary>
    /// Essentially the density function that will dictate the heights of the terrain
    /// </summary>
    // private void SetHeights() {
    //     heights = new float[terrainDensityData.width+1, terrainDensityData.width+1, terrainDensityData.width+1];

    //     float xWarp = 0;
    //     float yWarp = 0;
    //     float zWarp = 0;

    //     for(int x = 0; x < terrainDensityData.width+1; x++) {
    //         for(int y = 0; y < terrainDensityData.width+1; y++) {
    //             for(int z = 0; z < terrainDensityData.width+1; z++) {
    //                 float currentHeight = 0;
    //                 xWarp = (x + chunkPos.x) * noiseScale;
    //                 yWarp = (y + chunkPos.y) * noiseScale;
    //                 zWarp = (z + chunkPos.z) * noiseScale;
    //                 if(terrainDensityData.domainWarpToggle) {
    //                     if(terrainDensityData.noiseDimension == NoiseGenerator.fnl_noise_dimension._2D) {
    //                         domainWarp.DomainWarp(ref xWarp, ref zWarp);
    //                     }
    //                     else {
    //                         domainWarp.DomainWarp(ref xWarp, ref yWarp, ref zWarp);
    //                     }
    //                 }
    //                 if(terrainDensityData.noiseDimension == NoiseGenerator.fnl_noise_dimension._2D) {
    //                     currentHeight = terrainDensityData.height * ((baseNoiseGenerator.GetNoise(xWarp, zWarp)+1)/2) + (terrainDensityData.terracing ? (y%terrainDensityData.terraceHeight) : 0);
    //                 }
    //                 else {
    //                     currentHeight = terrainDensityData.height * ((baseNoiseGenerator.GetNoise(xWarp, yWarp, zWarp)+1)/2) + (terrainDensityData.terracing ? (y%terrainDensityData.terraceHeight) : 0);
    //                 }

    //                 heights[x, y, z] = chunkPos.y + y - currentHeight;
    //             }
    //         }
    //     }
    // }

    // /// <summary>
    // /// Marches through every cube in a given chunk
    // /// </summary>
    // public void MarchCubes() {
    //     vertices.Clear();
    //     triangles.Clear();

    //     for(int x = 0; x < terrainDensityData.width; x++) {
    //         for(int y = 0; y < terrainDensityData.width; y++) {
    //             for(int z = 0; z < terrainDensityData.width; z++) {
    //                 float[] cubeVertices = new float[8];
    //                 for(int i = 0; i < 8; i++) {
    //                     Vector3Int vertex = new Vector3Int(x,y,z) + MarchingCubesTables.vertexOffsetTable[i];
    //                     cubeVertices[i] = heights[vertex.x,vertex.y,vertex.z];
    //                 }

    //                 MarchCube(new Vector3(chunkPos.x + x,chunkPos.y + y,chunkPos.z + z), cubeVertices);
    //             }
    //         }
    //     }
    //     SetupMesh();
    // }

    // /// <summary>
    // /// Polygonizes for a single given cube.
    // /// </summary>
    // /// <param name="cubePosition"> The world space position of the cube within the chunk </param>
    // /// <param name="cubeVertices"> The values of the vertices for the given cube to be marched which will be used to get the configuration </param>
    // public void MarchCube(Vector3 cubePosition, float[] cubeVertices) {
    //     int configurationIndex = GetCubeConfiguration(cubeVertices);

    //     if(configurationIndex == 0 || configurationIndex == 255) {
    //         return ;
    //     }

    //     int edgeIndex = 0;

    //     for(int tri = 0; tri < 5; tri++) {
    //         for(int vert = 0; vert < 3; vert++) {
    //             int edgeValue = MarchingCubesTables.triangleTable[configurationIndex, edgeIndex];

    //             if(edgeValue == -1) {
    //                 return ;
    //             }

    //             Vector3 edgeV1 = cubePosition + MarchingCubesTables.vertexOffsetTable[MarchingCubesTables.edgeIndexTable[edgeValue, 0]];
    //             Vector3 edgeV2 = cubePosition + MarchingCubesTables.vertexOffsetTable[MarchingCubesTables.edgeIndexTable[edgeValue, 1]];

    //             Vector3 vertex;
    //             if(terrainDensityData.lerp) {
    //                 vertex = Vector3.Lerp(edgeV1, edgeV2, 
    //                 (terrainDensityData.isolevel - cubeVertices[MarchingCubesTables.edgeIndexTable[edgeValue, 0]]) / (cubeVertices[MarchingCubesTables.edgeIndexTable[edgeValue, 1]] - cubeVertices[MarchingCubesTables.edgeIndexTable[edgeValue, 0]]));
    //             }
    //             else {
    //                 vertex = (edgeV1 + edgeV2) / 2;
    //             }

    //             vertices.Add(vertex);
    //             triangles.Add(vertices.Count - 1);

    //             edgeIndex++;
    //         }
    //     }
    // }

    /// <summary>
    /// Retrieve the configuration index based on a cube's vertice's values
    /// </summary>
    /// <param name="cubeValues"> The values of the vertices for the given cube </param>
    /// <returns></returns>
    private int GetCubeConfiguration(float[] cubeValues) {
        int cubeIndex = 0;
        // Compare the value of each vertice with the iso level and do bitshifting
        for(int i = 0; i < 8; i++) {
            if(cubeValues[i] > terrainDensityData.isolevel) cubeIndex |= 1 << i;
        }

        return cubeIndex;
    }
}
