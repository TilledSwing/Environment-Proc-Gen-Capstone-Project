using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class GrassRender : MonoBehaviour
{
    public bool renderGrass = true;
    public ComputeShader grassPositionComputeShader;
    public ComputeShader grassUpdateComputeShader;
    public GrassProfile grassProfile;
    public int minHeight;
    public int maxHeight;
    public Vector3Int chunkPos;
    public Bounds bounds;
    List<RenderParams> renderParams;
    public int triangleCount;
    public ComputeBuffer grassTriangleBuffer;
    List<GraphicsBuffer> positionsBuffers;
    List<GraphicsBuffer> tempPositionsBuffers;
    List<GraphicsBuffer> argsBuffers;
    GraphicsBuffer grassCountsBuffer;
    public bool underwater;
    public bool isTerraforming = false;
    int maxBlades;
    int grassUpdateKernel;
    int grassPositionKernel;
    List<Vector2> heightRangeList;
    ComputeBuffer heightRangeBuffer;
    List<Vector2> curveRangeList;
    ComputeBuffer curveRangeBuffer;
    List<float> spawnProbabilityUpperThresholdList;
    ComputeBuffer spawnProbabilityUpperThresholdBuffer;
    void Awake()
    {
        heightRangeList = new();
        curveRangeList = new();
        spawnProbabilityUpperThresholdList = new();

        positionsBuffers = new();
        tempPositionsBuffers = new();
        argsBuffers = new();
        renderParams = new();
    }
    public void InitializeGrassRenderer(Vector3Int chunkPos,
                                        GrassProfile grassProfile,
                                        int minHeight,
                                        int maxHeight,
                                        ComputeShader grassPositionComputeShader,
                                        ComputeShader grassUpdateComputeShader,
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
        this.grassUpdateComputeShader = grassUpdateComputeShader;
        if (grassPositionKernel == 0)
            grassPositionKernel = grassPositionComputeShader.FindKernel("GrassCompute");
        if(grassUpdateKernel == 0) 
            grassUpdateKernel = grassUpdateComputeShader.FindKernel("GrassTerraform");
        this.triangleCount = triangleCount;
        grassTriangleBuffer = new(triangleCount, sizeof(float) * 18);
        grassTriangleBuffer.SetData(triangleArray);
        this.bounds = bounds;
        this.underwater = underwater;
    }
    public void SetupGrass()
    {
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassTriangleBuffer", grassTriangleBuffer);
        grassPositionComputeShader.SetInt("TriangleCount", triangleCount);
        grassPositionComputeShader.SetInt("GrassDensity", grassProfile.grassDensity);
        grassPositionComputeShader.SetInt("MinHeight", minHeight);
        grassPositionComputeShader.SetInt("MaxHeight", maxHeight);
        grassPositionComputeShader.SetInt("MaxBladesPerTriangle", grassProfile.maxBladesPerTriangle);
        grassPositionComputeShader.SetFloat("MaxSlope", Mathf.Cos(grassProfile.maxGrassSlope * Mathf.Deg2Rad));
        maxBlades = Mathf.CeilToInt(triangleCount * grassProfile.maxBladesPerTriangle);

        heightRangeList?.Clear();
        curveRangeList?.Clear();
        spawnProbabilityUpperThresholdList?.Clear();

        positionsBuffers?.Clear();
        tempPositionsBuffers?.Clear();
        argsBuffers?.Clear();
        renderParams?.Clear();

        if (heightRangeBuffer == null || !heightRangeBuffer.IsValid())
            heightRangeBuffer = new ComputeBuffer(5, sizeof(float) * 2);
        if (curveRangeBuffer == null || !curveRangeBuffer.IsValid())
            curveRangeBuffer = new ComputeBuffer(5, sizeof(float) * 2);
        if (spawnProbabilityUpperThresholdBuffer == null || !spawnProbabilityUpperThresholdBuffer.IsValid())
            spawnProbabilityUpperThresholdBuffer = new ComputeBuffer(5, sizeof(float));

        for (int i = 0; i < grassProfile.foliageList.Count; i++)
        {
            GrassProfile.FoliageType foliageType = grassProfile.foliageList[i];
            heightRangeList.Add(foliageType.grassHeightRange);
            curveRangeList.Add(foliageType.grassCurveRange);
            spawnProbabilityUpperThresholdList.Add(foliageType.spawnProbabilityUpperThreshold);

            if (positionsBuffers.Count <= i)
            {
                int count = Mathf.CeilToInt(maxBlades * (i != 0 ? foliageType.spawnProbabilityUpperThreshold - grassProfile.foliageList[i-1].spawnProbabilityUpperThreshold : foliageType.spawnProbabilityUpperThreshold));
                positionsBuffers.Add(new GraphicsBuffer(
                                                        GraphicsBuffer.Target.Append,
                                                        count,
                                                        sizeof(float) * 9
                                                    ));
            }
            positionsBuffers[i].SetCounterValue(0);
            grassPositionComputeShader.SetBuffer(grassPositionKernel, $"GrassPositionsBuffer{i+1}", positionsBuffers[i]);
        }
        for (int i = grassProfile.foliageList.Count; i < 5; i++)
        {
            if (positionsBuffers.Count <= i)
                positionsBuffers.Add(new GraphicsBuffer(
                                                        GraphicsBuffer.Target.Append,
                                                        1,
                                                        sizeof(float) * 9
                                                    ));
            positionsBuffers[i].SetCounterValue(0);
            grassPositionComputeShader.SetBuffer(grassPositionKernel, $"GrassPositionsBuffer{i+1}", positionsBuffers[i]);
        }
        heightRangeBuffer.SetData(heightRangeList);
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassHeightRangeBuffer", heightRangeBuffer);

        curveRangeBuffer.SetData(curveRangeList);
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassCurveRangeBuffer", curveRangeBuffer);

        spawnProbabilityUpperThresholdBuffer.SetData(spawnProbabilityUpperThresholdList);
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassProbabilityBuffer", spawnProbabilityUpperThresholdBuffer);

        grassPositionComputeShader.Dispatch(grassPositionKernel, Mathf.CeilToInt(triangleCount / 64f), 1, 1);

        grassTriangleBuffer.Release();

        if (grassCountsBuffer == null)
            grassCountsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, 5, sizeof(uint));

        for (int i = 0; i < grassProfile.foliageList.Count; i++)
        {
            // Args buffers
            GrassProfile.FoliageType foliageType = grassProfile.foliageList[i];
            if (argsBuffers.Count <= i)
                argsBuffers.Add(new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, 5 * sizeof(uint)));
            uint[] args = new uint[5] { foliageType.grassMesh.GetIndexCount(0), 0, 0, 0, 0 };
            argsBuffers[i].SetData(args);
            GraphicsBuffer positionBuffer = positionsBuffers[i];
            GraphicsBuffer.CopyCount(positionBuffer, argsBuffers[i], sizeof(uint));
            GraphicsBuffer.CopyCount(positionBuffer, grassCountsBuffer, i * sizeof(uint));

            // Render Params
            Material material = foliageType.grassMaterial;
            material.enableInstancing = true;
            if (foliageType.useUniformScale)
                material.EnableKeyword("_UNIFORM_SCALE");
            else
                material.DisableKeyword("_UNIFORM_SCALE");
            
            if (renderParams.Count <= i)
                renderParams.Add(new RenderParams(material)
                                {
                                    matProps = new MaterialPropertyBlock(),
                                    worldBounds = bounds,
                                    layer = 0
                                });
            renderParams[i].matProps.SetBuffer("_Positions", positionBuffer);
        }
    }
    public void UpdateGrass(Vector3 terraformCenter, float terraformRadius)
    {
        isTerraforming = true;
        tempPositionsBuffers.Clear();
        grassUpdateComputeShader.SetVector("TerraformCenter", terraformCenter);
        grassUpdateComputeShader.SetFloat("TerraformRadius", terraformRadius);
        grassUpdateComputeShader.SetBuffer(grassUpdateKernel, "GrassCountsBuffer", grassCountsBuffer);
        for (int i = 0; i < grassProfile.foliageList.Count; i++)
        {
            grassUpdateComputeShader.SetBuffer(grassUpdateKernel, $"OldGrassPositionsBuffer{i+1}", positionsBuffers[i]);
            GrassProfile.FoliageType foliageType = grassProfile.foliageList[i];
            GraphicsBuffer newGraphicsBuffer = new GraphicsBuffer(
                                                                    GraphicsBuffer.Target.Append,
                                                                    Mathf.CeilToInt(maxBlades * (i != 0 ? foliageType.spawnProbabilityUpperThreshold - grassProfile.foliageList[i-1].spawnProbabilityUpperThreshold : foliageType.spawnProbabilityUpperThreshold)),
                                                                    sizeof(float) * 9
                                                                 );
            newGraphicsBuffer.SetCounterValue(0);
            tempPositionsBuffers.Add(newGraphicsBuffer);
            grassUpdateComputeShader.SetBuffer(grassUpdateKernel, $"NewGrassPositionsBuffer{i+1}", newGraphicsBuffer);
        }
        for (int i = grassProfile.foliageList.Count; i < 5; i++)
        {
            grassUpdateComputeShader.SetBuffer(grassUpdateKernel, $"OldGrassPositionsBuffer{i+1}", positionsBuffers[i]);
            GraphicsBuffer newGraphicsBuffer = new GraphicsBuffer(
                                                                    GraphicsBuffer.Target.Append,
                                                                    1,
                                                                    sizeof(float) * 9
                                                                 );
            newGraphicsBuffer.SetCounterValue(0);
            tempPositionsBuffers.Add(newGraphicsBuffer);
            grassUpdateComputeShader.SetBuffer(grassUpdateKernel, $"NewGrassPositionsBuffer{i+1}", newGraphicsBuffer);
        }

        grassUpdateComputeShader.Dispatch(grassUpdateKernel, Mathf.CeilToInt(maxBlades / 64f), 1, 1);

        for (int i = 0; i < 5; i++)
        {
            positionsBuffers[i].Release();
        }
        for (int i = 0; i < grassProfile.foliageList.Count; i++)
        {
            argsBuffers[i].Release();
        }
        positionsBuffers.Clear();
        argsBuffers.Clear();
        for (int i = 0; i < tempPositionsBuffers.Count; i++)
        {
            positionsBuffers.Add(tempPositionsBuffers[i]);
        }
        tempPositionsBuffers.Clear();
        
        for (int i = 0; i < grassProfile.foliageList.Count; i++)
        {
            // Args buffers
            argsBuffers.Add(new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, 5 * sizeof(uint)));
            uint[] args = new uint[5] { grassProfile.foliageList[i].grassMesh.GetIndexCount(0), 0, 0, 0, 0 };
            argsBuffers[i].SetData(args);
            GraphicsBuffer positionBuffer = positionsBuffers[i];
            GraphicsBuffer.CopyCount(positionBuffer, argsBuffers[i], sizeof(uint));

            // Render Params
            renderParams[i].matProps.SetBuffer("_Positions", positionBuffer);
        }
        isTerraforming = false;
    }
    public void DisposalReleaseHandler()
    {
        if (heightRangeBuffer != null && heightRangeBuffer.IsValid())
        {
            heightRangeBuffer.Release();
        }
        if (curveRangeBuffer != null && curveRangeBuffer.IsValid())
        {
            curveRangeBuffer.Release();
        }
        if (spawnProbabilityUpperThresholdBuffer != null && spawnProbabilityUpperThresholdBuffer.IsValid())
        {
            spawnProbabilityUpperThresholdBuffer.Release();
        }
        foreach (GraphicsBuffer positionBuffer in positionsBuffers)
        {
            if (positionBuffer != null && positionBuffer.IsValid())
            {
                positionBuffer.Release();
            }
        }

        foreach (GraphicsBuffer argsBuffer in argsBuffers)
        {
            if (argsBuffer != null && argsBuffer.IsValid())
            {
                argsBuffer.Release();
            }
        }
        if (grassCountsBuffer != null && grassCountsBuffer.IsValid())
        {
            grassCountsBuffer.Release();
        }
    }
    void OnDisable()
    {
        DisposalReleaseHandler();
    }
    void Update()
    {
        if (underwater && ChunkGenNetwork.Instance.viewerPos.y > ChunkGenNetwork.Instance.terrainDensityData.waterLevel || renderGrass == false)
            return ;
        for (int i = 0; i < grassProfile.foliageList.Count; i++)
        {
            if(renderParams.Count <= i || argsBuffers.Count <= i) 
                continue;
            if (argsBuffers[i] == null || !argsBuffers[i].IsValid())
                continue;
            Graphics.RenderMeshIndirect(renderParams[i], grassProfile.foliageList[i].grassMesh, argsBuffers[i], 1, 0);
        }
    }
}
