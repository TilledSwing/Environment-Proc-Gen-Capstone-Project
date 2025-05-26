using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Mathematics;
using UnityEngine;

public class WaterPlaneGenerator : MonoBehaviour
{
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    public MeshFilter meshFilter;
    public Vector3Int chunkPos;
    public TerrainDensityData1 terrainDensityData;

    public void UpdateMesh() {
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
    /// <summary>
    /// [Needs to be updated to new mesh setup api]
    /// Generate a simple water plane square at the water level
    /// </summary>
    private void GenerateWaterPlane() {
        vertices.Clear();
        triangles.Clear();

        for(int x = 0; x < terrainDensityData.width; x++) {
            for(int z = 0; z < terrainDensityData.width; z++) {
                Vector3 vertex00 = new Vector3(chunkPos.x + x, terrainDensityData.waterLevel, chunkPos.z + z);
                Vector3 vertex10 = new Vector3(chunkPos.x + x+1, terrainDensityData.waterLevel, chunkPos.z + z);
                Vector3 vertex01 = new Vector3(chunkPos.x + x, terrainDensityData.waterLevel, chunkPos.z + z+1);
                Vector3 vertex11 = new Vector3(chunkPos.x + x+1, terrainDensityData.waterLevel, chunkPos.z + z+1);
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