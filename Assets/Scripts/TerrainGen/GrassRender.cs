using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
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
        this.triangleCount = triangleCount;
        grassTriangleBuffer = new(triangleCount, sizeof(float) * 18);
        grassTriangleBuffer.SetData(triangleArray);
        this.bounds = bounds;
        this.underwater = underwater;
    }
    public void SetupGrass()
    {
        int grassPositionKernel = grassPositionComputeShader.FindKernel("GrassCompute");
        grassPositionComputeShader.SetBuffer(grassPositionKernel, "GrassTriangleBuffer", grassTriangleBuffer);
        grassPositionComputeShader.SetInt("TriangleCount", triangleCount);
        grassPositionComputeShader.SetInt("GrassDensity", grassProfile.grassDensity);
        grassPositionComputeShader.SetInt("MinHeight", minHeight);
        grassPositionComputeShader.SetInt("MaxHeight", maxHeight);
        grassPositionComputeShader.SetInt("MaxBladesPerTriangle", grassProfile.maxBladesPerTriangle);
        grassPositionComputeShader.SetFloat("MaxSlope", Mathf.Cos(grassProfile.maxGrassSlope * Mathf.Deg2Rad));
        maxBlades = Mathf.CeilToInt(triangleCount * grassProfile.maxBladesPerTriangle);

        List<Vector2> heightRangeList = new();
        ComputeBuffer heightRangeBuffer = new ComputeBuffer(5, sizeof(float) * 2);
        List<Vector2> curveRangeList = new();
        ComputeBuffer curveRangeBuffer = new ComputeBuffer(5, sizeof(float) * 2);
        List<float> spawnProbabilityUpperThresholdList = new();
        ComputeBuffer spawnProbabilityUpperThresholdBuffer = new ComputeBuffer(5, sizeof(float));

        positionsBuffers = new();
        tempPositionsBuffers = new();
        argsBuffers = new();
        renderParams = new();

        for (int i = 0; i < grassProfile.foliageList.Count; i++)
        {
            GrassProfile.FoliageType foliageType = grassProfile.foliageList[i];
            heightRangeList.Add(foliageType.grassHeightRange);
            curveRangeList.Add(foliageType.grassCurveRange);
            spawnProbabilityUpperThresholdList.Add(foliageType.spawnProbabilityUpperThreshold);

            positionsBuffers.Add(new GraphicsBuffer(
                                                    GraphicsBuffer.Target.Append,
                                                    Mathf.CeilToInt(maxBlades * (i != 0 ? foliageType.spawnProbabilityUpperThreshold - grassProfile.foliageList[i-1].spawnProbabilityUpperThreshold : foliageType.spawnProbabilityUpperThreshold)),
                                                    sizeof(float) * 9
                                                   ));
            positionsBuffers[i].SetCounterValue(0);
            grassPositionComputeShader.SetBuffer(grassPositionKernel, $"GrassPositionsBuffer{i+1}", positionsBuffers[i]);
        }
        for (int i = grassProfile.foliageList.Count; i < 5; i++)
        {
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
        heightRangeBuffer.Release();
        curveRangeBuffer.Release();
        spawnProbabilityUpperThresholdBuffer.Release();

        grassCountsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, 5, sizeof(uint));

        for (int i = 0; i < grassProfile.foliageList.Count; i++)
        {
            // Args buffers
            argsBuffers.Add(new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, 5 * sizeof(uint)));
            uint[] args = new uint[5] { grassProfile.foliageList[i].grassMesh.GetIndexCount(0), 0, 0, 0, 0 };
            argsBuffers[i].SetData(args);
            GraphicsBuffer positionBuffer = positionsBuffers[i];
            GraphicsBuffer.CopyCount(positionBuffer, argsBuffers[i], sizeof(uint));
            GraphicsBuffer.CopyCount(positionBuffer, grassCountsBuffer, i * sizeof(uint));

            // Render Params
            Material material = grassProfile.foliageList[i].grassMaterial;
            material.enableInstancing = true;
            if (grassProfile.foliageList[i].useUniformScale)
                material.EnableKeyword("_UNIFORM_SCALE");
            else
                material.DisableKeyword("_UNIFORM_SCALE");
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
        int grassUpdateKernel = grassUpdateComputeShader.FindKernel("GrassTerraform");
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

        for (int i = 0; i < grassProfile.foliageList.Count; i++)
        {
            positionsBuffers[i].Release();
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
    void OnDisable()
    {
        foreach (GraphicsBuffer positionBuffer in positionsBuffers)
        {
            if (positionBuffer != null)
            {
                positionBuffer.Release();
            }
        }
        positionsBuffers.Clear();

        foreach (GraphicsBuffer argsBuffer in argsBuffers)
        {
            if (argsBuffer != null)
            {
                argsBuffer.Release();
            }
        }
        argsBuffers.Clear();
        if (grassCountsBuffer != null)
            {
                grassCountsBuffer.Release();
            }
    }
    void Update()
    {
        if (underwater && ChunkGenNetwork.Instance.viewerPos.y > ChunkGenNetwork.Instance.terrainDensityData.waterLevel || renderGrass == false)
            return ;
        for (int i = 0; i < grassProfile.foliageList.Count; i++)
        {
            Graphics.RenderMeshIndirect(renderParams[i], grassProfile.foliageList[i].grassMesh, argsBuffers[i], 1, 0);
        }
    }
}
