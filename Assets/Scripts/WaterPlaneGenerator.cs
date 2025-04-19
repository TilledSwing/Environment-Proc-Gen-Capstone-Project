using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Mathematics;
using UnityEngine;
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterPlaneGenerator : MonoBehaviour
{
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private MeshFilter meshFilter;
    public TerrainDensityData terrainDensityData;
    private int width;
    private int waterLevel;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        terrainDensityData = Resources.Load<TerrainDensityData>("TerrainDensityData");
        UpdateMesh();
    }

    public void UpdateMesh() {
        width = terrainDensityData.width;
        waterLevel = terrainDensityData.waterLevel;
        GenerateWaterPlane();
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
    }

    private void GenerateWaterPlane() {
        vertices.Clear();
        triangles.Clear();

        for(int x = 0; x < width; x++) {
            for(int z = 0; z < width; z++) {
                Vector3 vertex00 = new Vector3(x, waterLevel, z);
                Vector3 vertex10 = new Vector3(x+1, waterLevel, z);
                Vector3 vertex01 = new Vector3(x, waterLevel, z+1);
                Vector3 vertex11 = new Vector3(x+1, waterLevel, z+1);
                int vertCount = vertices.Count;
                vertices.Add(vertex00);
                vertices.Add(vertex10);
                vertices.Add(vertex01);
                vertices.Add(vertex11);

                triangles.Add(vertCount+3);
                triangles.Add(vertCount+1);
                triangles.Add(vertCount);

                triangles.Add(vertCount+2);
                triangles.Add(vertCount+3);
                triangles.Add(vertCount);
            }
        }
    }
}