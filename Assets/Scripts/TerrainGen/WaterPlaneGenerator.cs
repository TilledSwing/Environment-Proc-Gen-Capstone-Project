using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class WaterPlaneGenerator : MonoBehaviour
{
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public Vector3Int chunkPos;
    public ComputeMarchingCubes marchingCubes;
    public TerrainDensityData terrainDensityData;
    public struct Vertex
    {
        public float3 position;
    }
    public static Mesh PlaneGeneratorJobHandler(int size, int height)
    {
        int vertSize = size * size * 4;
        int indexSize = size * size * 6;
        NativeList<Vertex> vertexArray = new(vertSize, Allocator.Persistent);
        NativeList<int> indexArray = new(indexSize, Allocator.Persistent);
        PlaneGeneratorJob planeGeneratorJob = new PlaneGeneratorJob
        {
            vertexArray = vertexArray.AsParallelWriter(),
            indexArray = indexArray.AsParallelWriter(),
            waterLevel = height,
            chunkSize = size,
        };

        planeGeneratorJob.Run();

        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        meshData.SetVertexBufferParams(vertexArray.Length, new VertexAttributeDescriptor(VertexAttribute.Position));
        var vertexBuffer = meshData.GetVertexData<Vertex>(0);
        vertexBuffer.CopyFrom(vertexArray.AsArray());

        meshData.SetIndexBufferParams(indexArray.Length, IndexFormat.UInt32);
        var indexBuffer = meshData.GetIndexData<int>();
        indexBuffer.CopyFrom(indexArray.AsArray());

        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, indexArray.Length, MeshTopology.Triangles));

        Mesh mesh = new Mesh();
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh, MeshUpdateFlags.DontValidateIndices);
        vertexArray.Dispose();
        indexArray.Dispose();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }
    [BurstCompile]
    public struct PlaneGeneratorJob : IJob
    {
        public NativeList<Vertex>.ParallelWriter vertexArray;
        public NativeList<int>.ParallelWriter indexArray;
        public int waterLevel;
        public int chunkSize;
        public void Execute()
        {
            int vertCount = 0;
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    Vertex vertex00 = new Vertex{position = new float3(x, waterLevel, z)};
                    Vertex vertex10 = new Vertex{position = new float3(x + 1, waterLevel, z)};
                    Vertex vertex01 = new Vertex{position = new float3(x, waterLevel, z + 1)};
                    Vertex vertex11 = new Vertex{position = new float3(x + 1, waterLevel, z + 1)};

                    vertexArray.AddNoResize(vertex00);
                    vertexArray.AddNoResize(vertex10);

                    vertexArray.AddNoResize(vertex01);
                    vertexArray.AddNoResize(vertex11);

                    indexArray.AddNoResize(vertCount + 3);
                    indexArray.AddNoResize(vertCount + 1);
                    indexArray.AddNoResize(vertCount);

                    indexArray.AddNoResize(vertCount + 2);
                    indexArray.AddNoResize(vertCount + 3);
                    indexArray.AddNoResize(vertCount);
                    vertCount += 4;
                }
            }
        }
    }
}