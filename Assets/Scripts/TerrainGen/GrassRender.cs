using UnityEngine;

public class GrassRender : MonoBehaviour
{
    public ComputeShader grassPositionComputeShader;
    public Mesh grassMesh;
    public Material grassMaterial;
    public int grassDensity;
    public int maxBladesPerTriangle;
    public int minHeight;
    public int maxHeight;
    public float maxGrassSlope;
    public Vector2 grassHeightRange;
    public Vector3Int chunkPos;
    public int bladeCount;
    public Bounds bounds;
    public RenderParams rp;
    public int triangleCount;
    public ComputeBuffer grassTriangleBuffer;
    GraphicsBuffer grassPositionBuffer;
    GraphicsBuffer argsBuffer;
    public bool underwater;
    public void SetupGrass()
    {
        int grassPositionKernel = grassPositionComputeShader.FindKernel("GrassCompute");
        grassPositionComputeShader.SetInt("TriangleCount", triangleCount);
        grassPositionComputeShader.SetInt("GrassDensity", grassDensity);
        grassPositionComputeShader.SetInt("MinHeight", minHeight);
        grassPositionComputeShader.SetInt("MaxHeight", maxHeight);
        grassPositionComputeShader.SetInt("MaxBladesPerTriangle", maxBladesPerTriangle);
        grassPositionComputeShader.SetVector("GrassHeightRange", grassHeightRange);
        grassPositionComputeShader.SetFloat("MaxSlope", Mathf.Cos(maxGrassSlope * Mathf.Deg2Rad));

        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassTriangleBuffer", grassTriangleBuffer);

        int maxBlades = Mathf.CeilToInt(triangleCount * maxBladesPerTriangle);
        grassPositionBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.Append,
            maxBlades,
            sizeof(float) * 9
        );
        grassPositionBuffer.SetCounterValue(0);
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassPositionsBuffer", grassPositionBuffer);

        grassPositionComputeShader.Dispatch(grassPositionKernel, Mathf.CeilToInt(triangleCount / 64f), 1, 1);

        grassTriangleBuffer.Release();

        argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, 5 * sizeof(uint));
        uint[] args = new uint[5] { grassMesh.GetIndexCount(0), 0, 0, 0, 0 };
        argsBuffer.SetData(args);
        GraphicsBuffer.CopyCount(grassPositionBuffer, argsBuffer, sizeof(uint));
        
        grassMaterial.enableInstancing = true;
        grassMaterial.SetFloat("_MinHeight", ChunkGenNetwork.Instance.terrainDensityData.waterLevel);
        rp = new RenderParams(grassMaterial)
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
        Graphics.RenderMeshIndirect(rp, grassMesh, argsBuffer, 1, 0);
    }
}
