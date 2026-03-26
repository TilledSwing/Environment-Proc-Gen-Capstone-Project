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
    public float minHeight;
    public float maxHeight;
    public Vector3Int chunkPos;
    public Bounds bounds;
    List<RenderParams> renderParams;
    public int triangleCount;
    public ComputeBuffer grassTriangleBuffer;
    GraphicsBuffer positionsBuffer;
    GraphicsBuffer tempPositionsBuffer;
    GraphicsBuffer argsBuffers;
    public bool underwater;
    public bool isTerraforming = false;
    int foliageCount;
    int maxBlades;
    int grassUpdateKernel;
    int grassPositionKernel;
    List<Vector2> heightRangeList;
    GraphicsBuffer heightRangeBuffer;
    List<Vector2> curveRangeList;
    GraphicsBuffer curveRangeBuffer;
    List<float> spawnProbabilityUpperThresholdList;
    GraphicsBuffer spawnProbabilityUpperThresholdBuffer;
    List<uint> foliageOffsetsList;
    GraphicsBuffer foliageOffsetsBuffer;
    GraphicsBuffer.IndirectDrawIndexedArgs[] args;
    int grassBladeSize = sizeof(float) * 9;
    bool pingPong = false;
    public bool grassSet = false;
    uint currentOffset;
    void Awake()
    {
        heightRangeList = new();
        curveRangeList = new();
        spawnProbabilityUpperThresholdList = new();
        foliageOffsetsList = new();
        renderParams = new();
    }
    public void InitializeGrassRenderer(Vector3Int chunkPos,
                                        GrassProfile grassProfile,
                                        float minHeight,
                                        float maxHeight,
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
        args = new GraphicsBuffer.IndirectDrawIndexedArgs[foliageCount];
    }
    public void SetupGrass()
    {
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassTriangleBuffer", grassTriangleBuffer);
        grassPositionComputeShader.SetInt("TriangleCount", triangleCount);
        grassPositionComputeShader.SetInt("FoliageCount", foliageCount);
        grassPositionComputeShader.SetInt("GrassDensity", grassProfile.grassDensity);
        grassPositionComputeShader.SetFloat("MinHeight", minHeight);
        grassPositionComputeShader.SetFloat("MaxHeight", maxHeight);
        grassPositionComputeShader.SetInt("MaxBladesPerTriangle", grassProfile.maxBladesPerTriangle);
        grassPositionComputeShader.SetFloat("MaxSlope", Mathf.Cos(grassProfile.maxGrassSlope * Mathf.Deg2Rad));
        maxBlades = Mathf.CeilToInt(triangleCount * grassProfile.maxBladesPerTriangle);

        heightRangeList?.Clear();
        curveRangeList?.Clear();
        spawnProbabilityUpperThresholdList?.Clear();
        foliageOffsetsList?.Clear();
        renderParams?.Clear();

        if (heightRangeBuffer == null || !heightRangeBuffer.IsValid())
            heightRangeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, foliageCount, sizeof(float) * 2);
        if (curveRangeBuffer == null || !curveRangeBuffer.IsValid())
            curveRangeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, foliageCount, sizeof(float) * 2);
        if (spawnProbabilityUpperThresholdBuffer == null || !spawnProbabilityUpperThresholdBuffer.IsValid())
            spawnProbabilityUpperThresholdBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, foliageCount, sizeof(float));

        currentOffset = 0;
        for (int i = 0; i < foliageCount; i++)
        {
            GrassProfile.FoliageType foliageType = grassProfile.foliageList[i];
            heightRangeList.Add(foliageType.grassHeightRange);
            curveRangeList.Add(foliageType.grassCurveRange);
            spawnProbabilityUpperThresholdList.Add(foliageType.spawnProbabilityUpperThreshold);
            int count = Mathf.CeilToInt(maxBlades * (i != 0 ? foliageType.spawnProbabilityUpperThreshold - grassProfile.foliageList[i-1].spawnProbabilityUpperThreshold : foliageType.spawnProbabilityUpperThreshold));

            foliageOffsetsList.Add(currentOffset);
            currentOffset += (uint)count;

            args[i].indexCountPerInstance = foliageType.grassMesh.GetIndexCount(0);
            args[i].instanceCount = 0;
            args[i].startIndex = 0;
            args[i].baseVertexIndex = 0;
            args[i].startInstance = 0;
        }
        positionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)currentOffset, grassBladeSize);
        tempPositionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)currentOffset, grassBladeSize);
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassPositionsBuffer", positionsBuffer);

        foliageOffsetsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, foliageCount, sizeof(uint));
        foliageOffsetsBuffer.SetData(foliageOffsetsList);
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "OffsetsBuffer", foliageOffsetsBuffer);

        argsBuffers = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, foliageCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        argsBuffers.SetData(args);
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "ArgsBuffers", argsBuffers);

        heightRangeBuffer.SetData(heightRangeList);
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassHeightRangeBuffer", heightRangeBuffer);

        curveRangeBuffer.SetData(curveRangeList);
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassCurveRangeBuffer", curveRangeBuffer);

        spawnProbabilityUpperThresholdBuffer.SetData(spawnProbabilityUpperThresholdList);
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassProbabilityBuffer", spawnProbabilityUpperThresholdBuffer);

        grassPositionComputeShader.Dispatch(grassPositionKernel, Mathf.CeilToInt(triangleCount / 128f), 1, 1);

        for (int i = 0; i < foliageCount; i++)
        {
            GrassProfile.FoliageType foliageType = grassProfile.foliageList[i];

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
                renderParam.worldBounds = bounds;
            }
            renderParam.material = material;
            renderParam.matProps.SetBuffer("_Positions", positionsBuffer);
            renderParam.matProps.SetInt("_Offset", (int)foliageOffsetsList[i]);
        }
        grassSet = true;
    }
    public void UpdateGrass(Vector3 terraformCenter, float terraformRadius)
    {
        isTerraforming = true;
        grassUpdateComputeShader.SetVector("TerraformCenter", terraformCenter);
        grassUpdateComputeShader.SetFloat("TerraformRadius", terraformRadius);
        grassUpdateComputeShader.SetInt("FoliageCount", foliageCount);
        grassUpdateComputeShader.SetInt("GrassCount", (int)currentOffset);
        
        for (int i = 0; i < foliageCount; i++)
        {
            GrassProfile.FoliageType foliageType = grassProfile.foliageList[i];
            args[i].indexCountPerInstance = foliageType.grassMesh.GetIndexCount(0);
            args[i].instanceCount = 0;
            args[i].startIndex = 0;
            args[i].baseVertexIndex = 0;
            args[i].startInstance = 0;
        }
        argsBuffers.SetData(args);
        grassUpdateComputeShader.SetBuffer(grassUpdateKernel, "ArgsBuffers", argsBuffers);
        grassUpdateComputeShader.SetBuffer(grassUpdateKernel, "OffsetsBuffer", foliageOffsetsBuffer);

        GraphicsBuffer readBuffer = pingPong ? tempPositionsBuffer : positionsBuffer;
        GraphicsBuffer writeBuffer = pingPong ? positionsBuffer : tempPositionsBuffer;

        grassUpdateComputeShader.SetBuffer(grassUpdateKernel, "OldGrassPositionsBuffer", readBuffer);
        grassUpdateComputeShader.SetBuffer(grassUpdateKernel, "NewGrassPositionsBuffer", writeBuffer);

        grassUpdateComputeShader.Dispatch(grassUpdateKernel, Mathf.CeilToInt((int)currentOffset / 256f), 1, 1);
        
        for (int i = 0; i < foliageCount; i++)
        {
            GraphicsBuffer positionBuffer = pingPong ? positionsBuffer : tempPositionsBuffer;

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
        if (foliageOffsetsBuffer != null && foliageOffsetsBuffer.IsValid())
        {
            foliageOffsetsBuffer.Release();
        }
        if (argsBuffers != null && argsBuffers.IsValid())
        {
            argsBuffers.Release();
        }
        if (grassTriangleBuffer != null && grassTriangleBuffer.IsValid())
        {
            grassTriangleBuffer.Release();
        }
        ResetPositionBuffers();
    }
    public void ResetPositionBuffers()
    {
        if (positionsBuffer != null && positionsBuffer.IsValid())
        {
            positionsBuffer.Release();
        }
        if (tempPositionsBuffer != null && tempPositionsBuffer.IsValid())
        {
            tempPositionsBuffer.Release();
        }
    }
    void OnDisable()
    {
        DisposalReleaseHandler();
        grassSet = false;
    }
    void Update()
    {
        if (renderGrass == false || (underwater && ChunkGenNetwork.Instance.viewerPos.y > ChunkGenNetwork.Instance.terrainDensityData.waterLevel) || !grassSet)
            return ;

        for (int i = 0; i < foliageCount; i++)
        {
            Graphics.RenderMeshIndirect(renderParams[i], grassProfile.foliageList[i].grassMesh, argsBuffers, 1, i);
        }
    }
}