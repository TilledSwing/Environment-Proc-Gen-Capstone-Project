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
    int foliageCount;
    int maxBlades;
    int grassUpdateKernel;
    int grassPositionKernel;
    List<Vector2> heightRangeList;
    ComputeBuffer heightRangeBuffer;
    List<Vector2> curveRangeList;
    ComputeBuffer curveRangeBuffer;
    List<float> spawnProbabilityUpperThresholdList;
    ComputeBuffer spawnProbabilityUpperThresholdBuffer;
    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    int grassBladeSize = sizeof(float) * 9;
    bool pingPong = false;
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
        foliageCount = grassProfile.foliageList.Count;
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

        for (int i = 0; i < foliageCount; i++)
        {
            GrassProfile.FoliageType foliageType = grassProfile.foliageList[i];
            heightRangeList.Add(foliageType.grassHeightRange);
            curveRangeList.Add(foliageType.grassCurveRange);
            spawnProbabilityUpperThresholdList.Add(foliageType.spawnProbabilityUpperThreshold);
            int count = Mathf.CeilToInt(maxBlades * (i != 0 ? foliageType.spawnProbabilityUpperThreshold - grassProfile.foliageList[i-1].spawnProbabilityUpperThreshold : foliageType.spawnProbabilityUpperThreshold));

            GraphicsBuffer positionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Append, count, grassBladeSize);
            GraphicsBuffer tempPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Append, count, grassBladeSize);

            positionsBuffers.Add(positionBuffer);
            positionBuffer.SetCounterValue(0);

            tempPositionsBuffers.Add(tempPositionBuffer);
            tempPositionBuffer.SetCounterValue(0);

            grassPositionComputeShader.SetBuffer(grassPositionKernel, $"GrassPositionsBuffer{i+1}", positionBuffer);
        }
        for (int i = foliageCount; i < 5; i++)
        {
            GraphicsBuffer positionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Append, 1, grassBladeSize);
            GraphicsBuffer tempPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Append, 1, grassBladeSize);

            positionsBuffers.Add(positionBuffer);
            positionBuffer.SetCounterValue(0);

            tempPositionsBuffers.Add(tempPositionBuffer);
            tempPositionBuffer.SetCounterValue(0);

            grassPositionComputeShader.SetBuffer(grassPositionKernel, $"GrassPositionsBuffer{i+1}", positionBuffer);
        }
        heightRangeBuffer.SetData(heightRangeList);
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassHeightRangeBuffer", heightRangeBuffer);

        curveRangeBuffer.SetData(curveRangeList);
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassCurveRangeBuffer", curveRangeBuffer);

        spawnProbabilityUpperThresholdBuffer.SetData(spawnProbabilityUpperThresholdList);
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassProbabilityBuffer", spawnProbabilityUpperThresholdBuffer);

        grassPositionComputeShader.Dispatch(grassPositionKernel, Mathf.CeilToInt(triangleCount / 256f), 1, 1);

        grassTriangleBuffer.Release();

        if (grassCountsBuffer == null || !grassCountsBuffer.IsValid())
            grassCountsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, 5, sizeof(uint));

        for (int i = 0; i < foliageCount; i++)
        {
            // Args buffers
            GrassProfile.FoliageType foliageType = grassProfile.foliageList[i];
            if (argsBuffers.Count <= i)
                argsBuffers.Add(new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, 5 * sizeof(uint)));
            GraphicsBuffer argsBuffer = argsBuffers[i];
            args[0] = foliageType.grassMesh.GetIndexCount(0);
            argsBuffer.SetData(args);
            GraphicsBuffer positionBuffer = positionsBuffers[i];
            GraphicsBuffer.CopyCount(positionBuffer, argsBuffer, sizeof(uint));
            GraphicsBuffer.CopyCount(positionBuffer, grassCountsBuffer, i * sizeof(uint));

            // Render Params
            Material material = foliageType.grassMaterial;
            material.enableInstancing = true;
            if (foliageType.useUniformScale)
                material.EnableKeyword("_UNIFORM_SCALE");
            else
                material.DisableKeyword("_UNIFORM_SCALE");
            
            RenderParams renderParam;
            if (renderParams.Count <= i)
            {
                renderParam = new RenderParams(material) { matProps = new MaterialPropertyBlock(), worldBounds = bounds, };
                renderParams.Add(renderParam);
            }
            else
            {
                renderParam = renderParams[i];
            }
            renderParam.material = material;
            renderParam.worldBounds = bounds;
            renderParam.matProps.SetBuffer("_Positions", positionBuffer);
        }
    }
    public void UpdateGrass(Vector3 terraformCenter, float terraformRadius)
    {
        isTerraforming = true;
        grassUpdateComputeShader.SetVector("TerraformCenter", terraformCenter);
        grassUpdateComputeShader.SetFloat("TerraformRadius", terraformRadius);
        grassUpdateComputeShader.SetBuffer(grassUpdateKernel, "GrassCountsBuffer", grassCountsBuffer);
        for (int i = 0; i < positionsBuffers.Count; i++)
        {
            GraphicsBuffer readBuffer = pingPong ? tempPositionsBuffers[i] : positionsBuffers[i];
            GraphicsBuffer writeBuffer = pingPong ? positionsBuffers[i] : tempPositionsBuffers[i];
            writeBuffer.SetCounterValue(0);

            grassUpdateComputeShader.SetBuffer(grassUpdateKernel, $"OldGrassPositionsBuffer{i+1}", readBuffer);
            grassUpdateComputeShader.SetBuffer(grassUpdateKernel, $"NewGrassPositionsBuffer{i+1}", writeBuffer);
        }

        grassUpdateComputeShader.Dispatch(grassUpdateKernel, Mathf.CeilToInt(maxBlades / 256f), 1, 1);
        
        for (int i = 0; i < foliageCount; i++)
        {
            // Args buffers
            GraphicsBuffer argsBuffer = argsBuffers[i];
            args[0] = grassProfile.foliageList[i].grassMesh.GetIndexCount(0);
            argsBuffer.SetData(args);
            GraphicsBuffer positionBuffer = pingPong ? positionsBuffers[i] : tempPositionsBuffers[i];
            GraphicsBuffer.CopyCount(positionBuffer, argsBuffer, sizeof(uint));

            // Render Params
            renderParams[i].matProps.SetBuffer("_Positions", positionBuffer);
        }
        pingPong = !pingPong;
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
        ResetPositionBuffers();
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
    public void ResetPositionBuffers()
    {
        foreach (GraphicsBuffer positionBuffer in positionsBuffers)
        {
            if (positionBuffer != null && positionBuffer.IsValid())
            {
                positionBuffer.Release();
            }
        }
        foreach (GraphicsBuffer tempPositionBuffer in tempPositionsBuffers)
        {
            if (tempPositionBuffer != null && tempPositionBuffer.IsValid())
            {
                tempPositionBuffer.Release();
            }
        }
    }
    void OnDisable()
    {
        DisposalReleaseHandler();
    }
    void Update()
    {
        if (renderGrass == false || (underwater && ChunkGenNetwork.Instance.viewerPos.y > ChunkGenNetwork.Instance.terrainDensityData.waterLevel))
            return ;
        for (int i = 0; i < foliageCount; i++)
        {
            Graphics.RenderMeshIndirect(renderParams[i], grassProfile.foliageList[i].grassMesh, argsBuffers[i], 1, 0);
        }
    }
}