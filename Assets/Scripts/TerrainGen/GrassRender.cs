using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class GrassRender : MonoBehaviour
{
    public ComputeShader grassPositionComputeShader;
    public GrassProfile grassProfile;
    List<Mesh> meshes;
    public List<Material> materials;
    public int minHeight;
    public int maxHeight;
    public Vector3Int chunkPos;
    public Bounds bounds;
    public RenderParams rp;
    List<RenderParams> renderParams;
    public int triangleCount;
    public ComputeBuffer grassTriangleBuffer;
    List<GraphicsBuffer> positionsBuffers;
    GraphicsBuffer grassPositionBuffer;
    GraphicsBuffer argsBuffer;
    List<GraphicsBuffer> argsBuffers;
    public bool underwater;
    public void InitializeGrassRenderer(Vector3Int chunkPos,
                                        GrassProfile grassProfile,
                                        int minHeight,
                                        int maxHeight,
                                        ComputeShader grassPositionComputeShader,
                                        int triangleCount,
                                        NativeArray<ComputeMarchingCubes.Triangle> triangleArray,
                                        Bounds bounds,
                                        bool underwater
                                        )
    {
        this.chunkPos = chunkPos;
        this.grassProfile = grassProfile;
        this.minHeight = minHeight;
        this.maxHeight = maxHeight;
        this.grassPositionComputeShader = grassPositionComputeShader;
        this.triangleCount = triangleCount;
        grassTriangleBuffer = new(triangleCount, sizeof(float) * 18);
        grassTriangleBuffer.SetData(triangleArray);
        this.bounds = bounds;
        this.underwater = underwater;
    }
    public void SetupGrass()
    {
        int grassPositionKernel = grassPositionComputeShader.FindKernel("GrassCompute");
        grassPositionComputeShader.SetInt("TriangleCount", triangleCount);
        grassPositionComputeShader.SetInt("GrassDensity", grassProfile.grassDensity);
        grassPositionComputeShader.SetInt("MinHeight", minHeight);
        grassPositionComputeShader.SetInt("MaxHeight", maxHeight);
        grassPositionComputeShader.SetInt("MaxBladesPerTriangle", grassProfile.maxBladesPerTriangle);
        grassPositionComputeShader.SetVector("GrassHeightRange", grassProfile.grassHeightRange);
        grassPositionComputeShader.SetVector("GrassCurveRange", grassProfile.grassCurveRange);
        grassPositionComputeShader.SetFloat("MaxSlope", Mathf.Cos(grassProfile.maxGrassSlope * Mathf.Deg2Rad));

        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassTriangleBuffer", grassTriangleBuffer);

        int maxBlades = Mathf.CeilToInt(triangleCount * grassProfile.maxBladesPerTriangle);
        grassPositionBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.Append,
            maxBlades,
            sizeof(float) * 9
        );
        grassPositionBuffer.SetCounterValue(0);
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassPositionsBuffer1", grassPositionBuffer);

        grassPositionComputeShader.Dispatch(grassPositionKernel, Mathf.CeilToInt(triangleCount / 64f), 1, 1);

        grassTriangleBuffer.Release();

        argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, 5 * sizeof(uint));
        uint[] args = new uint[5] { grassProfile.grassMesh.GetIndexCount(0), 0, 0, 0, 0 };
        argsBuffer.SetData(args);
        GraphicsBuffer.CopyCount(grassPositionBuffer, argsBuffer, sizeof(uint));
        
        grassProfile.grassMaterial.enableInstancing = true;
        grassProfile.grassMaterial.SetFloat("_MinHeight", ChunkGenNetwork.Instance.terrainDensityData.waterLevel);
        rp = new RenderParams(grassProfile.grassMaterial)
        {
            matProps = new MaterialPropertyBlock(),
            worldBounds = bounds,
            layer = 0
        };
        rp.matProps.SetBuffer("_Positions", grassPositionBuffer);
    }
    public void UpdateGrass()
    {

        rp.matProps.SetBuffer("_Positions", grassPositionBuffer);
    }

    void OnDisable()
    {
        if (grassPositionBuffer != null)
        {
            grassPositionBuffer.Release();
        }
        if (argsBuffer != null)
        {
            argsBuffer.Release();
        }
    }

    void Update()
    {
        if (underwater && ChunkGenNetwork.Instance.viewerPos.y > ChunkGenNetwork.Instance.terrainDensityData.waterLevel)
            return ;
        Graphics.RenderMeshIndirect(rp, grassProfile.grassMesh, argsBuffer, 1, 0);
    }
}
