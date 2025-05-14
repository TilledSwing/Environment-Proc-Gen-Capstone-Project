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
    private MeshFilter meshFilter;
    private Renderer mat;
    public ComputeMarchingCubes marchingCubes;
    public TerrainDensityData terrainDensityData;
    private int width;
    private int waterLevel;

    void Awake()
    {
        gameObject.AddComponent<MeshRenderer>();
        meshFilter = gameObject.AddComponent<MeshFilter>();
        mat = GetComponent<Renderer>();
        Material waterMaterial = Resources.Load<Material>("Materials/WaterMat");
        mat.sharedMaterial = waterMaterial;
        marchingCubes = gameObject.GetComponentInParent<ComputeMarchingCubes>();
        terrainDensityData = Resources.Load<TerrainDensityData>("TerrainDensityData");
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
                Vector3 vertex00 = new Vector3(marchingCubes.chunkPos.x + x, waterLevel, marchingCubes.chunkPos.z + z);
                Vector3 vertex10 = new Vector3(marchingCubes.chunkPos.x + x+1, waterLevel, marchingCubes.chunkPos.z + z);
                Vector3 vertex01 = new Vector3(marchingCubes.chunkPos.x + x, waterLevel, marchingCubes.chunkPos.z + z+1);
                Vector3 vertex11 = new Vector3(marchingCubes.chunkPos.x + x+1, waterLevel, marchingCubes.chunkPos.z + z+1);
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