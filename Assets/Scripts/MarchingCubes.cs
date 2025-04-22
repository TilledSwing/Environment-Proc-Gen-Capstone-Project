using System;
using System.Collections;
using System.Collections.Generic;
//using UnityEditor.VersionControl;
using Unity.Mathematics;
using UnityEngine;
using Unity.VisualScripting;
using FishNet.Connection;
using FishNet.Object;
using static TerrainDensityData;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class MarchingCubes : NetworkBehaviour
{
    private float[,,] heights;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    public TerrainDensityData terrainDensityData;
    private FastNoiseLite noiseGenerator = new FastNoiseLite();
    private FastNoiseLite domainWarp = new FastNoiseLite();
    private int width;
    private int height;
    private float noiseScale;
    private float isolevel;
    private bool lerp;

    private Vector2 noiseOffset;

    void Start()
    {
        //meshFilter = GetComponent<MeshFilter>();
        //meshCollider = GetComponent<MeshCollider>();
        //terrainDensityData = Resources.Load<TerrainDensityData>("TerrainDensityData");
        //terrainDensityData.noiseSeed = UnityEngine.Random.Range(0, 10000);
        //terrainDensityData.domainWarpSeed = UnityEngine.Random.Range(0, 10000);
        //UpdateMesh();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        terrainDensityData = Resources.Load<TerrainDensityData>("TerrainDensityData");
        terrainDensityData.noiseSeed = UnityEngine.Random.Range(0, 10000);
        terrainDensityData.domainWarpSeed = UnityEngine.Random.Range(0, 10000);
        // UpdateMesh();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        // if (base.IsServerStarted)
        // {
        //     // Server initialization
        //     terrainDensityData = Resources.Load<TerrainDensityData>("TerrainDensityData");
        //     terrainDensityData.noiseSeed = UnityEngine.Random.Range(0, 10000);
        //     terrainDensityData.domainWarpSeed = UnityEngine.Random.Range(0, 10000);
        //     UpdateMesh();
        // }
        // else
        // {
        // Client requests terrain data
        ClientReady(LocalConnection);
        // }
    }

    [ServerRpc(RequireOwnership = false)]
    void ClientReady(NetworkConnection target)
    {
        UpdateClientMesh(target, GetTerrainSettings(terrainDensityData));
    }


    [TargetRpc]
    void UpdateClientMesh(NetworkConnection conn, TerrainSettings settings)
    {
        ApplySettingsToDensityData(settings, terrainDensityData);
        UpdateMesh();
    }



    /// <summary>
    /// Gets the serializable struct of terrain settings back into standard form.
    /// </summary>
    /// <param name="settings">Serializable struct version</param>
    /// <param name="data">Standard version</param>
    private void ApplySettingsToDensityData(TerrainSettings settings, TerrainDensityData data)
    {
        // Noise and Fractal Settings
        data.selectedNoiseDimension = settings.selectedNoiseDimension;
        data.noiseDimension = (NoiseDimension)settings.noiseDimension;
        data.selectedNoiseType = settings.selectedNoiseType;
        data.noiseType = (FastNoiseLite.NoiseType)settings.noiseType;
        data.selectedNoiseFractalType = settings.selectedNoiseFractalType;
        data.noiseFractalType = (FastNoiseLite.FractalType)settings.noiseFractalType;
        data.noiseSeed = settings.noiseSeed;
        data.noiseFractalOctaves = settings.noiseFractalOctaves;
        data.noiseFractalLacunarity = settings.noiseFractalLacunarity;
        data.noiseFractalGain = settings.noiseFractalGain;
        data.fractalWeightedStrength = settings.fractalWeightedStrength;
        data.noiseFrequency = settings.noiseFrequency;

        // Domain Warp Values
        data.domainWarpToggle = settings.domainWarpToggle;
        data.selectedDomainWarpType = settings.selectedDomainWarpType;
        data.domainWarpType = (FastNoiseLite.DomainWarpType)settings.domainWarpType;
        data.selectedDomainWarpFractalType = settings.selectedDomainWarpFractalType;
        data.domainWarpFractalType = (FastNoiseLite.FractalType)settings.domainWarpFractalType;
        data.domainWarpAmplitude = settings.domainWarpAmplitude;
        data.domainWarpSeed = settings.domainWarpSeed;
        data.domainWarpFractalOctaves = settings.domainWarpFractalOctaves;
        data.domainWarpFractalLacunarity = settings.domainWarpFractalLacunarity;
        data.domainWarpFractalGain = settings.domainWarpFractalGain;
        data.domainWarpFrequency = settings.domainWarpFrequency;

        // Cellular (Voronoi) Values
        data.selectedCellularDistanceFunction = settings.selectedCellularDistanceFunction;
        data.cellularDistanceFunction = (FastNoiseLite.CellularDistanceFunction)settings.cellularDistanceFunction;
        data.selectedCellularReturnType = settings.selectedCellularReturnType;
        data.cellularReturnType = (FastNoiseLite.CellularReturnType)settings.cellularReturnType;
        data.cellularJitter = settings.cellularJitter;

        // Terrain Values
        data.width = settings.width;
        data.height = settings.height;
        data.noiseScale = settings.noiseScale;
        data.isolevel = settings.isolevel;
        data.lerp = settings.lerp;
    }

    /// <summary>
    /// Updates the terrain mesh
    /// </summary>
    public void UpdateMesh()
    {
        SetNoiseSetting();
        SetHeights();
        MarchCubes();
        SetupMesh();
    }

    /// <summary>
    /// Gets the TerrainDensityData into serializable form as a struct.
    /// </summary>
    /// <param name="terrainDensityData"></param>
    /// <returns></returns>
    private TerrainSettings GetTerrainSettings(TerrainDensityData terrainDensityData)
    {
        return new TerrainSettings
        {
            // Noise and Fractal Settings
            selectedNoiseDimension = terrainDensityData.selectedNoiseDimension,
            noiseDimension = (int)terrainDensityData.noiseDimension,
            selectedNoiseType = terrainDensityData.selectedNoiseType,
            noiseType = (int)terrainDensityData.noiseType,
            selectedNoiseFractalType = terrainDensityData.selectedNoiseFractalType,
            noiseFractalType = (int)terrainDensityData.noiseFractalType,
            noiseSeed = terrainDensityData.noiseSeed,
            noiseFractalOctaves = terrainDensityData.noiseFractalOctaves,
            noiseFractalLacunarity = terrainDensityData.noiseFractalLacunarity,
            noiseFractalGain = terrainDensityData.noiseFractalGain,
            fractalWeightedStrength = terrainDensityData.fractalWeightedStrength,
            noiseFrequency = terrainDensityData.noiseFrequency,

            // Domain Warp Values
            domainWarpToggle = terrainDensityData.domainWarpToggle,
            selectedDomainWarpType = terrainDensityData.selectedDomainWarpType,
            domainWarpType = (int)terrainDensityData.domainWarpType,
            selectedDomainWarpFractalType = terrainDensityData.selectedDomainWarpFractalType,
            domainWarpFractalType = (int)terrainDensityData.domainWarpFractalType,
            domainWarpAmplitude = terrainDensityData.domainWarpAmplitude,
            domainWarpSeed = terrainDensityData.domainWarpSeed,
            domainWarpFractalOctaves = terrainDensityData.domainWarpFractalOctaves,
            domainWarpFractalLacunarity = terrainDensityData.domainWarpFractalLacunarity,
            domainWarpFractalGain = terrainDensityData.domainWarpFractalGain,
            domainWarpFrequency = terrainDensityData.domainWarpFrequency,

            // Cellular (Voronoi) Values
            selectedCellularDistanceFunction = terrainDensityData.selectedCellularDistanceFunction,
            cellularDistanceFunction = (int)terrainDensityData.cellularDistanceFunction,
            selectedCellularReturnType = terrainDensityData.selectedCellularReturnType,
            cellularReturnType = (int)terrainDensityData.cellularReturnType,
            cellularJitter = terrainDensityData.cellularJitter,

            // Terrain Values
            width = terrainDensityData.width,
            height = terrainDensityData.height,
            noiseScale = terrainDensityData.noiseScale,
            isolevel = terrainDensityData.isolevel,
            lerp = terrainDensityData.lerp
        };
    }

    /// <summary>
    /// Struct of the TerrainDensityData Object class so it can be serialized over the network.
    /// </summary>
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct TerrainSettings
    {
        // Noise and Fractal Settings
        public int selectedNoiseDimension;
        public int noiseDimension;
        public int selectedNoiseType;
        public int noiseType;
        public int selectedNoiseFractalType;
        public int noiseFractalType;
        public int noiseSeed;
        public int noiseFractalOctaves;
        public float noiseFractalLacunarity;
        public float noiseFractalGain;
        public float fractalWeightedStrength;
        public float noiseFrequency;

        // Domain Warp Values
        public bool domainWarpToggle;
        public int selectedDomainWarpType;
        public int domainWarpType;
        public int selectedDomainWarpFractalType;
        public int domainWarpFractalType;
        public float domainWarpAmplitude;
        public int domainWarpSeed;
        public int domainWarpFractalOctaves;
        public float domainWarpFractalLacunarity;
        public float domainWarpFractalGain;
        public float domainWarpFrequency;

        // Cellular(Voronoi) Values
        public int selectedCellularDistanceFunction;
        public int cellularDistanceFunction;
        public int selectedCellularReturnType;
        public int cellularReturnType;
        public float cellularJitter;

        // Terrain Values
        public int width;
        public int height;
        public float noiseScale;
        public float isolevel;
        public bool lerp;
    }

    /// <summary>
    /// Set up the MeshFilter's mesh with the given vertices and triangle
    /// </summary>
    private void SetupMesh()
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    private void SetNoiseSetting()
    {
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
        if (terrainDensityData.noiseType == FastNoiseLite.NoiseType.Cellular)
        {
            noiseGenerator.SetCellularDistanceFunction(terrainDensityData.cellularDistanceFunction);
            noiseGenerator.SetCellularReturnType(terrainDensityData.cellularReturnType);
            noiseGenerator.SetCellularJitter(terrainDensityData.cellularJitter);
        }
        // Domain Warp Values
        if (terrainDensityData.domainWarpToggle)
        {
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
    private void SetHeights()
    {
        heights = new float[width + 1, height + 1, width + 1];

        float xWarp = 0;
        float yWarp = 0;
        float zWarp = 0;

        for (int x = 0; x < width + 1; x++)
        {
            for (int y = 0; y < height + 1; y++)
            {
                for (int z = 0; z < width + 1; z++)
                {
                    float currentHeight = 0;
                    xWarp = x * noiseScale;
                    yWarp = y * noiseScale;
                    zWarp = z * noiseScale;
                    if (terrainDensityData.domainWarpToggle)
                    {
                        if (terrainDensityData.noiseDimension == TerrainDensityData.NoiseDimension._2D)
                        {
                            domainWarp.DomainWarp(ref xWarp, ref zWarp);
                        }
                        else
                        {
                            domainWarp.DomainWarp(ref xWarp, ref yWarp, ref zWarp);
                        }
                    }
                    if (terrainDensityData.noiseDimension == TerrainDensityData.NoiseDimension._2D)
                    {
                        currentHeight = height * ((noiseGenerator.GetNoise(xWarp, zWarp) + 1) / 2);
                    }
                    else
                    {
                        currentHeight = height * ((noiseGenerator.GetNoise(xWarp, yWarp, zWarp) + 1) / 2);
                    }

                    // if(currentHeight < 0) currentHeight = 0;

                    heights[x, y, z] = y - currentHeight;
                }
            }
        }
    }

    /// <summary>
    /// Marches through every cube in a given chunk
    /// </summary>
    public void MarchCubes()
    {
        vertices.Clear();
        triangles.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < width; z++)
                {
                    float[] cubeVertices = new float[8];
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int vertex = new Vector3Int(x, y, z) + MarchingCubesTables.vertexOffsetTable[i];
                        cubeVertices[i] = heights[vertex.x, vertex.y, vertex.z];
                    }

                    MarchCube(new Vector3(x, y, z), cubeVertices);
                }
            }
        }
    }

    /// <summary>
    /// Polygonizes for a single given cube.
    /// </summary>
    /// <param name="cubePosition"> The world space position of the cube within the chunk </param>
    /// <param name="cubeVertices"> The values of the vertices for the given cube to be marched which will be used to get the configuration </param>
    public void MarchCube(Vector3 cubePosition, float[] cubeVertices)
    {
        int configurationIndex = GetCubeConfiguration(cubeVertices);

        if (configurationIndex == 0 || configurationIndex == 255)
        {
            return;
        }

        int edgeIndex = 0;

        for (int tri = 0; tri < 5; tri++)
        {
            for (int vert = 0; vert < 3; vert++)
            {
                int edgeValue = MarchingCubesTables.triangleTable[configurationIndex, edgeIndex];

                if (edgeValue == -1)
                {
                    return;
                }

                Vector3 edgeV1 = cubePosition + MarchingCubesTables.vertexOffsetTable[MarchingCubesTables.edgeIndexTable[edgeValue, 0]];
                Vector3 edgeV2 = cubePosition + MarchingCubesTables.vertexOffsetTable[MarchingCubesTables.edgeIndexTable[edgeValue, 1]];

                Vector3 vertex;
                if (lerp)
                {
                    vertex = Vector3.Lerp(edgeV1, edgeV2,
                    (isolevel - cubeVertices[MarchingCubesTables.edgeIndexTable[edgeValue, 0]]) / (cubeVertices[MarchingCubesTables.edgeIndexTable[edgeValue, 1]] - cubeVertices[MarchingCubesTables.edgeIndexTable[edgeValue, 0]]));
                }
                else
                {
                    vertex = (edgeV1 + edgeV2) / 2;
                }

                vertices.Add(vertex);
                triangles.Add(vertices.Count - 1);

                edgeIndex++;
            }
        }
    }

    /// <summary>
    /// Retrieve the configuration index based on a cube's vertice's values
    /// </summary>
    /// <param name="cubeValues"> The values of the vertices for the given cube </param>
    /// <returns></returns>
    private int GetCubeConfiguration(float[] cubeValues)
    {
        int cubeIndex = 0;
        // Compare the value of each vertice with the iso level and do bitshifting
        for (int i = 0; i < 8; i++)
        {
            if (cubeValues[i] > isolevel) cubeIndex |= 1 << i;
        }

        return cubeIndex;
    }
}
