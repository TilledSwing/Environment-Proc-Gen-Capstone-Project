using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using Unity.Mathematics;
using UnityEngine;

public enum NoiseDimension {
    _2D,
    _3D
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MarchingCubes : MonoBehaviour
{
    private float[,,] heights;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    // Noise Settings
    private FastNoiseLite noiseGenerator = new FastNoiseLite();
    private FastNoiseLite domainWarp = new FastNoiseLite();
    public FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.OpenSimplex2;
    public FastNoiseLite.FractalType noiseFractalType = FastNoiseLite.FractalType.FBm;
    public int noiseSeed;
    public int noiseFractalOctaves = 5;
    public float noiseFractalLacunarity = 2;
    public float noiseFractalGain = 0.5f;
    public float fractalWeightedStrength = 0;
    public float noiseFrequency = 0.01f;
    public bool domainWarpToggle = false;
    public FastNoiseLite.DomainWarpType domainWarpType = FastNoiseLite.DomainWarpType.OpenSimplex2;
    public FastNoiseLite.FractalType domainWarpFractalType = FastNoiseLite.FractalType.None;
    public float domainWarpAmplitude = 1;
    public int domainWarpSeed;
    public int domainWarpFractalOctaves = 5;
    public float domainWarpFractalLacunarity = 2;
    public float domainWarpFractalGain = 0.5f;
    public float domainWarpFrequency = 0.01f;
    public FastNoiseLite.CellularDistanceFunction cellularDistanceFunction = FastNoiseLite.CellularDistanceFunction.EuclideanSq;
    public FastNoiseLite.CellularReturnType cellularReturnType = FastNoiseLite.CellularReturnType.Distance;
    public float cellularJitter = 1;
    public int width = 200;
    public int height = 50;
    public float noiseScale = 0.6f;
    public float isolevel = 5f;
    public bool lerp = true;
    public NoiseDimension noiseDimension = NoiseDimension._3D;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        noiseSeed = UnityEngine.Random.Range(0, 10000);
        domainWarpSeed = UnityEngine.Random.Range(0, 10000);
        UpdateMesh();
    }

    /// <summary>
    /// Updates the terrain mesh
    /// </summary>
    public void UpdateMesh() {
        SetNoiseSetting();
        SetHeights();
        MarchCubes();
        SetupMesh();
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
        // Noise Values
        noiseGenerator.SetNoiseType(noiseType);
        noiseGenerator.SetFractalType(noiseFractalType);
        noiseGenerator.SetSeed(noiseSeed);
        noiseGenerator.SetFractalOctaves(noiseFractalOctaves);
        noiseGenerator.SetFractalLacunarity(noiseFractalLacunarity);
        noiseGenerator.SetFractalGain(noiseFractalGain);
        noiseGenerator.SetFractalWeightedStrength(fractalWeightedStrength);
        noiseGenerator.SetFrequency(noiseFrequency);
        // Cellular Values
        if(noiseType == FastNoiseLite.NoiseType.Cellular) {
            noiseGenerator.SetCellularDistanceFunction(cellularDistanceFunction);
            noiseGenerator.SetCellularReturnType(cellularReturnType);
            noiseGenerator.SetCellularJitter(cellularJitter);
        }
        // Domain Warp Values
        if(domainWarpToggle) {
            domainWarp.SetNoiseType(noiseType);
            domainWarp.SetFractalType(noiseFractalType);
            domainWarp.SetDomainWarpType(domainWarpType);
            domainWarp.SetDomainWarpAmp(domainWarpAmplitude);
            domainWarp.SetSeed(domainWarpSeed);
            domainWarp.SetFractalOctaves(domainWarpFractalOctaves);
            domainWarp.SetFractalLacunarity(domainWarpFractalLacunarity);
            domainWarp.SetFractalGain(domainWarpFractalGain);
            domainWarp.SetFrequency(domainWarpFrequency);
        }
    }

    /// <summary>
    /// Essentially the density function that will dictate the heights of the terrain
    /// </summary>
    private void SetHeights() {
        heights = new float[width+1, height+1, width+1];

        float xWarp = 0;
        float yWarp = 0;
        float zWarp = 0;

        for(int x = 0; x < width+1; x++) {
            for(int y = 0; y < height+1; y++) {
                for(int z = 0; z < width+1; z++) {
                    float currentHeight = 0;
                    xWarp = x * noiseScale;
                    yWarp = y * noiseScale;
                    zWarp = z * noiseScale;
                    if(domainWarpToggle) {
                        if(noiseDimension == NoiseDimension._2D) {
                            domainWarp.DomainWarp(ref xWarp, ref zWarp);
                        }
                        else {
                            domainWarp.DomainWarp(ref xWarp, ref yWarp, ref zWarp);
                        }
                    }
                    if(noiseDimension == NoiseDimension._2D) {
                        currentHeight = height * noiseGenerator.GetNoise(xWarp, zWarp);
                    }
                    else {
                        currentHeight = height * noiseGenerator.GetNoise(xWarp, yWarp, zWarp);
                    }

                    if(currentHeight < 0) currentHeight = 0;

                    heights[x,y,z] = y - currentHeight;
                }
            }
        }
    }

    /// <summary>
    /// Marches through every cube in a given chunk
    /// </summary>
    public void MarchCubes() {
        vertices.Clear();
        triangles.Clear();

        for(int x = 0; x < width; x++) {
            for(int y = 0; y < height; y++) {
                for(int z = 0; z < width; z++) {
                    float[] cubeVertices = new float[8];
                    for(int i = 0; i < 8; i++) {
                        Vector3Int vertex = new Vector3Int(x, y, z) + MarchingCubesTables.vertexOffsetTable[i];
                        cubeVertices[i] = heights[vertex.x,vertex.y,vertex.z];
                    }

                    MarchCube(new Vector3(x,y,z), cubeVertices);
                }
            }
        }
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
