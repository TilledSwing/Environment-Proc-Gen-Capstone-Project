using Unity.Mathematics;
using UnityEngine;

public class GrassRender : MonoBehaviour
{
    public ComputeShader grassPositionComputeShader;
    public Mesh grassMesh;
    public Material grassMaterial;
    public int grassDensity;
    public ComputeMarchingCubes.Vertex[] grassPositions;
    public Bounds bounds;
    RenderParams rp;
    ComputeBuffer grassPositionBuffer;
    int grassPositionKernel;
    int chunkSize;
    public Vector3Int chunkPos;
    void Start()
    {
        grassPositionKernel = grassPositionComputeShader.FindKernel("GrassCompute");
        chunkSize = ChunkGenNetwork.Instance.terrainDensityData.width;
        // chunkSize = 10;
        grassPositionComputeShader.SetInt("ChunkSize", chunkSize);
        grassPositionComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);
        grassPositionComputeShader.SetInt("GrassDensity", grassDensity);
        grassPositionBuffer = ComputeBufferPoolManager.Instance.GetComputeBuffer("GrassPositionBuffer", grassPositions.Length * grassDensity  * grassDensity, sizeof(float) * 6);
        grassPositionBuffer.SetData(grassPositions);
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassPositionsBuffer", grassPositionBuffer);
        grassPositionComputeShader.Dispatch(grassPositionKernel, Mathf.CeilToInt(grassPositions.Length * grassDensity / 8), Mathf.CeilToInt(grassPositions.Length * grassDensity / 8), 1);
        grassMaterial.enableInstancing = true;
        rp = new RenderParams(grassMaterial)
        {
            matProps = new MaterialPropertyBlock(),
            worldBounds = bounds
        };
        rp.matProps.SetBuffer("_Positions", grassPositionBuffer);
    }

    void OnDisable()
    {
        ComputeBufferPoolManager.Instance.ReturnComputeBuffer("GrassPositionBuffer", grassPositionBuffer);
    }

    void Update()
    {
        Graphics.RenderMeshPrimitives(rp, grassMesh, 0, grassPositions.Length * grassDensity * grassDensity);
    }
}
