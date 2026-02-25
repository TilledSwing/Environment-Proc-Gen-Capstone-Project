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
    ComputeBuffer grassPositionBuffer;
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
        grassPositionBuffer = new ComputeBuffer(
            maxBlades,
            sizeof(float) * 9,
            ComputeBufferType.Append
        );
        grassPositionBuffer.SetCounterValue(0);
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassPositionsBuffer", grassPositionBuffer);

        grassPositionComputeShader.Dispatch(grassPositionKernel, Mathf.CeilToInt(triangleCount / 64f), 1, 1);

        grassTriangleBuffer.Release();

        ComputeBuffer countBuffer = new(1, sizeof(int), ComputeBufferType.Raw);
        ComputeBuffer.CopyCount(grassPositionBuffer, countBuffer, 0);
        int[] countArray = { 0 };
        countBuffer.GetData(countArray);
        bladeCount = countArray[0];
        countBuffer.Release();
        
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

    void OnDisable()
    {
        if (grassPositionBuffer != null)
        {
            grassPositionBuffer.Release();
        }
    }

    void Update()
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        if (GeometryUtility.TestPlanesAABB(planes, bounds))
        {
            Graphics.RenderMeshPrimitives(rp, grassMesh, 0, bladeCount);
        }
    }
}
