using UnityEngine;

public class GrassRender : MonoBehaviour
{
    public ComputeShader grassPositionComputeShader;
    public Mesh grassMesh;
    public Material grassMaterial;
    public int grassDensity;
    public Vector3Int chunkPos;
    public int bladeCount;
    public Bounds bounds;
    public RenderParams rp;
    public ComputeMarchingCubes.Triangle[] grassTriangles;
    ComputeBuffer grassTriangleBuffer;
    ComputeBuffer grassPositionBuffer;
    public void SetupGrass()
    {
        int grassPositionKernel = grassPositionComputeShader.FindKernel("GrassCompute");
        grassPositionComputeShader.SetInt("TriangleCount", grassTriangles.Length);
        grassPositionComputeShader.SetInt("GrassDensity", grassDensity);

        grassPositionComputeShader.SetInt("MinHeight", ChunkGenNetwork.Instance.terrainDensityData.waterLevel);
        grassPositionComputeShader.SetInt("MaxHeight", 52);
        grassPositionComputeShader.SetFloat("MaxSlope", Mathf.Cos(ChunkGenNetwork.Instance.maxGrassSlope * Mathf.Deg2Rad));

        grassTriangleBuffer = new(grassTriangles.Length, sizeof(float) * 18);
        grassTriangleBuffer.SetData(grassTriangles);
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassTriangleBuffer", grassTriangleBuffer);

        int maxBlades = Mathf.CeilToInt(grassTriangles.Length * 15);
        grassPositionBuffer = new ComputeBuffer(
            maxBlades,
            sizeof(float) * 9,
            ComputeBufferType.Append
        );
        grassPositionBuffer.SetCounterValue(0);
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassPositionsBuffer", grassPositionBuffer);

        grassPositionComputeShader.Dispatch(grassPositionKernel, Mathf.CeilToInt(grassTriangles.Length / 64f), 1, 1);

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
