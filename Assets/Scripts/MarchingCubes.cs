using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MarchingCubes : MonoBehaviour
{
    
    [SerializeField] private int width = 32;
    [SerializeField] private int height = 12;
    [SerializeField] private float noiseResolution = 0.1f;
    [SerializeField] private float isolevel = 0.5f;
    private float[,,] heights;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        StartCoroutine(UpdateMesh());
    }

    /// <summary>
    /// For The purposes of updating mesh in real time when adjusting parameters during runtime
    /// </summary>
    /// <returns> Waits for a set period of time before looping again </returns>
    private IEnumerator UpdateMesh() {
        while(true) {
            SetHeights();
            MarchCubes();
            SetupMesh();
            yield return new WaitForSeconds(1f);
        }
    }

    /// <summary>
    /// Set up the MeshFilter's mesh with the given vertices and triangle
    /// </summary>
    private void SetupMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    /// <summary>
    /// Essentially the density function that will dictate the heights of the terrain
    /// </summary>
    private void SetHeights() {
        heights = new float[width+1, height+1, width+1];

        for(int x = 0; x < width+1; x++) {
            for(int y = 0; y < height+1; y++) {
                for(int z = 0; z < width+1; z++) {
                    float currentHeight = height * Mathf.PerlinNoise(x * noiseResolution, z * noiseResolution);
                    float newHeight;

                    if(y > currentHeight) {
                        newHeight = y - currentHeight;
                    }
                    else {
                        newHeight = currentHeight - y;
                    }

                    heights[x,y,z] = newHeight;
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

                    MarchCube(new Vector3(x,y,z), GetCubeConfiguration(cubeVertices));
                }
            }
        }
    }

/// <summary>
/// Polygonizes for a single given cube.
/// </summary>
/// <param name="cubePosition"> The world space position of the cube within the chunk </param>
/// <param name="configurationIndex"> The configuration index for the marching cubes triangulation table </param>
    public void MarchCube(Vector3 cubePosition, int configurationIndex) {
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

                Vector3 edgeV1 = cubePosition + MarchingCubesTables.edgeTable[edgeValue, 0];
                Vector3 edgeV2 = cubePosition + MarchingCubesTables.edgeTable[edgeValue, 1];

                Vector3 vertex = (edgeV1 + edgeV2) / 2;

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
        for(int i = 0; i < 8; i++) {
            if(cubeValues[i] > isolevel) cubeIndex |= 1 << i;
        }
        // if (cubeValues[0] < isolevel) cubeIndex |= 1;
        // if (cubeValues[1] < isolevel) cubeIndex |= 2;
        // if (cubeValues[2] < isolevel) cubeIndex |= 4;
        // if (cubeValues[3] < isolevel) cubeIndex |= 8;
        // if (cubeValues[4] < isolevel) cubeIndex |= 16;
        // if (cubeValues[5] < isolevel) cubeIndex |= 32;
        // if (cubeValues[6] < isolevel) cubeIndex |= 64;
        // if (cubeValues[7] < isolevel) cubeIndex |= 128;

        return cubeIndex;
    }
}
