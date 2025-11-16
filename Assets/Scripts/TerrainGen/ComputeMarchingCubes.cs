using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using System.Threading;
using UnityEngine.UI;

public class ComputeMarchingCubes : MonoBehaviour
{
    public ComputeShader marchingCubesComputeShader;
    public ComputeShader terrainDensityComputeShader;
    public ComputeShader terrainNoiseComputeShader;
    public ComputeShader terraformComputeShader;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    // public List<Vector3> vertices = new List<Vector3>();
    // public List<Vertex> verticesNormals = new List<Vertex>();
    // private List<int> triangles = new List<int>();
    public TerrainDensityData terrainDensityData;
    public WaterPlaneGenerator waterGen;
    public AssetSpawner assetSpawner;
    public Vector3Int chunkPos;
    public Bounds bounds;
    public ComputeBuffer heightsBuffer;
    public ComputeBuffer vertexBuffer;
    public float[] heightsArray;
    public bool initialLoadComplete = false;
    public bool rendering = false;
    public ChunkGenNetwork.LOD currentLOD;
    public Mesh lod1Mesh;
    public Mesh lod2Mesh;
    public Mesh lod3Mesh;
    public Mesh lod6Mesh;
    public event Action<Mesh> OnMeshGenerated;
    private Mesh generatedMesh;

    public struct Vertex : IComparable<Vertex>
    {
        public float3 position;
        public float3 normal;
        public int CompareTo(Vertex other)
        {
            int cmp = position.x.CompareTo(other.position.x);
            if (cmp != 0) return cmp;

            cmp = position.y.CompareTo(other.position.y);
            if (cmp != 0) return cmp;

            cmp = position.z.CompareTo(other.position.z);
            if (cmp != 0) return cmp;

            cmp = normal.x.CompareTo(other.normal.x);
            if (cmp != 0) return cmp;

            cmp = normal.y.CompareTo(other.normal.y);
            if (cmp != 0) return cmp;

            return normal.z.CompareTo(other.normal.z);
        }
    }

    public struct Triangle
    {
        public Vertex v1;
        public Vertex v2;
        public Vertex v3;
    }

    void Start()
    {
        SetTerrainSettings();
        GenerateMesh();
        initialLoadComplete = true;
        // StartCoroutine(GenerateMesh());
    }
    // void Update()
    // {
    //     if(rendering)
    //     {
    //         Graphics.DrawProceduralIndirect(ChunkGenNetwork.Instance.terrainMaterial, new Bounds(chunkPos + (new Vector3(0.5f, 0.5f, 0.5f) * terrainDensityData.width), Vector3.one * terrainDensityData.width), MeshTopology.Triangles, vertexBuffer);
    //     }
    // }
    public void Regen()
    {
        SetTerrainSettings();
        GenerateMesh();
        OnMeshGenerated?.Invoke(generatedMesh);

    }
    private void SetTerrainSettings()
    {
        // Terrain Values
        terrainDensityComputeShader.SetInt("height", terrainDensityData.height);
        terrainDensityComputeShader.SetBool("terracing", terrainDensityData.terracing);
        terrainDensityComputeShader.SetInt("terraceHeight", terrainDensityData.terraceHeight);
        terrainDensityComputeShader.SetInt("ChunkSize", terrainDensityData.width);
        terrainDensityComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);
        terrainDensityComputeShader.SetFloat("isolevel", terrainDensityData.isolevel);
        terrainDensityComputeShader.SetInt("MaxWorldYChunks", ChunkGenNetwork.Instance.maxWorldYChunks);
    }
    // Set all the noise settings from the TerrainDensityData scriptable object
    private void SetNoiseSettings(NoiseGenerator noiseGenerator)
    {
        // Noise and Fractal Values
        terrainNoiseComputeShader.SetInt("noiseDimension", (int)noiseGenerator.noiseDimension);
        terrainNoiseComputeShader.SetInt("noiseType", (int)noiseGenerator.noiseType);
        terrainNoiseComputeShader.SetInt("noiseFractalType", (int)noiseGenerator.noiseFractalType);
        terrainNoiseComputeShader.SetInt("rotationType3D", (int)noiseGenerator.rotationType3D);
        terrainNoiseComputeShader.SetInt("noiseSeed", noiseGenerator.noiseSeed);
        terrainNoiseComputeShader.SetInt("noiseFractalOctaves", noiseGenerator.noiseFractalOctaves);
        terrainNoiseComputeShader.SetFloat("noiseFractalLacunarity", noiseGenerator.noiseFractalLacunarity);
        terrainNoiseComputeShader.SetFloat("noiseFractalGain", noiseGenerator.noiseFractalGain);
        terrainNoiseComputeShader.SetFloat("fractalWeightedStrength", noiseGenerator.fractalWeightedStrength);
        terrainNoiseComputeShader.SetFloat("noiseFrequency", noiseGenerator.noiseFrequency);
        // Domain Warp Values
        terrainNoiseComputeShader.SetBool("domainWarpToggle", noiseGenerator.domainWarpToggle);
        terrainNoiseComputeShader.SetInt("domainWarpType", (int)noiseGenerator.domainWarpType);
        terrainNoiseComputeShader.SetInt("domainWarpFractalType", (int)noiseGenerator.domainWarpFractalType);
        terrainNoiseComputeShader.SetFloat("domainWarpAmplitude", noiseGenerator.domainWarpAmplitude);
        terrainNoiseComputeShader.SetInt("domainWarpSeed", noiseGenerator.domainWarpSeed);
        terrainNoiseComputeShader.SetInt("domainWarpFractalOctaves", noiseGenerator.domainWarpFractalOctaves);
        terrainNoiseComputeShader.SetFloat("domainWarpFractalLacunarity", noiseGenerator.domainWarpFractalLacunarity);
        terrainNoiseComputeShader.SetFloat("domainWarpFractalGain", noiseGenerator.domainWarpFractalGain);
        terrainNoiseComputeShader.SetFloat("domainWarpFrequency", noiseGenerator.domainWarpFrequency);
        // Cellular(Voronoi) Values
        terrainNoiseComputeShader.SetInt("cellularDistanceFunction", (int)noiseGenerator.cellularDistanceFunction);
        terrainNoiseComputeShader.SetInt("cellularReturnType", (int)noiseGenerator.cellularReturnType);
        terrainNoiseComputeShader.SetFloat("cellularJitter", noiseGenerator.cellularJitter);
        // Terrain Values
        terrainNoiseComputeShader.SetFloat("noiseScale", noiseGenerator.noiseScale);
        terrainNoiseComputeShader.SetInt("ChunkSize", terrainDensityData.width);
        terrainNoiseComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);
    }
    /// <summary>
    /// Set density and generate terrain mesh
    /// </summary>
    // public void GenerateMesh()
    // {
    //     heightsBuffer = SetHeights();

    //     // Wait for heights buffer to be set
    //     // float[] sync = new float[1];
    //     // heightsBuffer.GetData(sync);
    //     // yield return null;

    //     if (!initialLoadComplete)
    //     {
    //         // foreach(ChunkGenNetwork.LODData lodData in ChunkGenNetwork.Instance.lodData) {
    //         //     SyncMarchingCubes(heightsBuffer, false, lodData);
    //         // }
    //         SyncMarchingCubes(heightsBuffer, false);
    //     }
    //     else
    //     {
    //         // foreach(ChunkGenNetwork.LODData lodData in ChunkGenNetwork.Instance.lodData) {
    //         //     AsyncMarchingCubes(heightsBuffer, false, lodData);
    //         // }
    //         AsyncMarchingCubes(heightsBuffer, false);
    //     }
    // }
    public void GenerateMesh()
    {
        SetHeights();
        // SyncMarchingCubes(heightsBuffer, false);
    }
    public void UpdateMesh(ChunkGenNetwork.LOD lod)
    {
        if (currentLOD == lod) return;
        if (lod == ChunkGenNetwork.LOD.LOD1 && lod1Mesh != null) meshFilter.mesh = lod1Mesh;
        if (lod == ChunkGenNetwork.LOD.LOD2 && lod2Mesh != null) meshFilter.mesh = lod2Mesh;
        if (lod == ChunkGenNetwork.LOD.LOD3 && lod3Mesh != null) meshFilter.mesh = lod3Mesh;
        if (lod == ChunkGenNetwork.LOD.LOD6 && lod6Mesh != null) meshFilter.mesh = lod6Mesh;
        currentLOD = lod;
    }
    /// <summary>
    /// Set up the density values for the chunk using compute shaders
    /// </summary>
    /// <returns>The buffer the density values are stored in</returns>
    public void SetHeights()
    {
        int terrainNoiseKernel = terrainNoiseComputeShader.FindKernel("TerrainNoise");
        int densityKernel = terrainDensityComputeShader.FindKernel("TerrainDensity");

        List<ComputeBuffer> noiseBuffers = new();

        foreach (NoiseGenerator noiseGenerator in terrainDensityData.noiseGenerators)
        {
            SetNoiseSettings(noiseGenerator);
            ComputeBuffer noiseBuffer = ComputeBufferPoolManager.Instance.GetComputeBuffer("NoiseBuffer", (terrainDensityData.width + 1) * (terrainDensityData.width + 1) * (terrainDensityData.width + 1), sizeof(float));
            terrainNoiseComputeShader.SetBuffer(terrainNoiseKernel, "TerrainNoiseBuffer", noiseBuffer);
            terrainNoiseComputeShader.Dispatch(terrainNoiseKernel, Mathf.CeilToInt(terrainDensityData.width / 4f) + 1, Mathf.CeilToInt(terrainDensityData.width / 4f) + 1, Mathf.CeilToInt(terrainDensityData.width / 4f) + 1);
            if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.BaseGenerator)
            {
                terrainDensityComputeShader.SetBool("BaseNoiseActivated", noiseGenerator.activated);
                terrainDensityComputeShader.SetBuffer(densityKernel, "BaseNoiseBuffer", noiseBuffer);
                if (noiseGenerator.remoteTexture == null)
                    terrainDensityComputeShader.SetTexture(densityKernel, "BaseCurveTexture", SplineCurveFunctions.ArrayToTexture(SplineCurveFunctions.CurveToArray(noiseGenerator.valueCurve)));
                else
                    terrainDensityComputeShader.SetTexture(densityKernel, "BaseCurveTexture", noiseGenerator.remoteTexture);
                // terrainDensityComputeShader.SetBuffer(densityKernel, "BaseCurveArray", noiseCurveBuffer);
            }
            if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.LargeCaveGenerator)
            {
                terrainDensityComputeShader.SetBool("LargeCaveNoiseActivated", noiseGenerator.activated);
                terrainDensityComputeShader.SetBuffer(densityKernel, "LargeCaveNoiseBuffer", noiseBuffer);
                if (noiseGenerator.remoteTexture == null)
                    terrainDensityComputeShader.SetTexture(densityKernel, "LargeCaveCurveTexture", SplineCurveFunctions.ArrayToTexture(SplineCurveFunctions.CurveToArray(noiseGenerator.valueCurve)));
                else
                    terrainDensityComputeShader.SetTexture(densityKernel, "LargeCaveCurveTexture", noiseGenerator.remoteTexture);
                // terrainDensityComputeShader.SetBuffer(densityKernel, "LargeCaveCurveArray", noiseCurveBuffer);
            }
            if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.CaveDetail1Generator)
            {
                terrainDensityComputeShader.SetBool("CaveDetail1NoiseActivated", noiseGenerator.activated);
                terrainDensityComputeShader.SetBuffer(densityKernel, "CaveDetail1NoiseBuffer", noiseBuffer);
                if (noiseGenerator.remoteTexture == null)
                    terrainDensityComputeShader.SetTexture(densityKernel, "CaveDetail1CurveTexture", SplineCurveFunctions.ArrayToTexture(SplineCurveFunctions.CurveToArray(noiseGenerator.valueCurve)));
                else
                    terrainDensityComputeShader.SetTexture(densityKernel, "CaveDetail1CurveTexture", noiseGenerator.remoteTexture);
                // terrainDensityComputeShader.SetBuffer(densityKernel, "CaveDetail1CurveArray", noiseCurveBuffer);
            }
            if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.CaveDetail2Generator)
            {
                terrainDensityComputeShader.SetBool("CaveDetail2NoiseActivated", noiseGenerator.activated);
                terrainDensityComputeShader.SetBuffer(densityKernel, "CaveDetail2NoiseBuffer", noiseBuffer);
                if (noiseGenerator.remoteTexture == null)
                    terrainDensityComputeShader.SetTexture(densityKernel, "CaveDetail2CurveTexture", SplineCurveFunctions.ArrayToTexture(SplineCurveFunctions.CurveToArray(noiseGenerator.valueCurve)));
                else
                    terrainDensityComputeShader.SetTexture(densityKernel, "CaveDetail2CurveTexture", noiseGenerator.remoteTexture);
                // terrainDensityComputeShader.SetBuffer(densityKernel, "CaveDetail2CurveArray", noiseCurveBuffer);
            }
            if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.ContinentalnessGenerator)
            {
                terrainDensityComputeShader.SetBool("ContinentalnessNoiseActivated", noiseGenerator.activated);
                terrainDensityComputeShader.SetBuffer(densityKernel, "ContinentalnessNoiseBuffer", noiseBuffer);
                if (noiseGenerator.remoteTexture == null)
                    terrainDensityComputeShader.SetTexture(densityKernel, "ContinentalnessCurveTexture", SplineCurveFunctions.ArrayToTexture(SplineCurveFunctions.CurveToArray(noiseGenerator.valueCurve)));
                else
                    terrainDensityComputeShader.SetTexture(densityKernel, "ContinentalnessCurveTexture", noiseGenerator.remoteTexture);
                // terrainDensityComputeShader.SetBuffer(densityKernel, "ContinentalnessCurveArray", noiseCurveBuffer);
            }
            if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.TemperatureMapGenerator)
            {
                terrainDensityComputeShader.SetBool("TemperatureNoiseActivated", noiseGenerator.activated);
                terrainDensityComputeShader.SetBuffer(densityKernel, "TemperatureNoiseBuffer", noiseBuffer);
                if (noiseGenerator.remoteTexture == null)
                    terrainDensityComputeShader.SetTexture(densityKernel, "TemperatureCurveTexture", SplineCurveFunctions.ArrayToTexture(SplineCurveFunctions.CurveToArray(noiseGenerator.valueCurve)));
                else
                    terrainDensityComputeShader.SetTexture(densityKernel, "TemperatureCurveTexture", noiseGenerator.remoteTexture);
                // terrainDensityComputeShader.SetBuffer(densityKernel, "TemperatureCurveArray", noiseCurveBuffer);
            }
            if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.HumidityMapGenerator)
            {
                terrainDensityComputeShader.SetBool("HumidityNoiseActivated", noiseGenerator.activated);
                terrainDensityComputeShader.SetBuffer(densityKernel, "HumidityNoiseBuffer", noiseBuffer);
                if (noiseGenerator.remoteTexture == null)
                    terrainDensityComputeShader.SetTexture(densityKernel, "HumidityCurveTexture", SplineCurveFunctions.ArrayToTexture(SplineCurveFunctions.CurveToArray(noiseGenerator.valueCurve)));
                else
                    terrainDensityComputeShader.SetTexture(densityKernel, "HumidityCurveTexture", noiseGenerator.remoteTexture);
                // terrainDensityComputeShader.SetBuffer(densityKernel, "HumidityCurveArray", noiseCurveBuffer);
            }
            if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.PeaksAndValleysMapGenerator)
            {
                terrainDensityComputeShader.SetBool("PeaksAndValleysNoiseActivated", noiseGenerator.activated);
                terrainDensityComputeShader.SetBuffer(densityKernel, "PeaksAndValleysNoiseBuffer", noiseBuffer);
                if (noiseGenerator.remoteTexture == null)
                    terrainDensityComputeShader.SetTexture(densityKernel, "PeaksAndValleysCurveTexture", SplineCurveFunctions.ArrayToTexture(SplineCurveFunctions.CurveToArray(noiseGenerator.valueCurve)));
                else
                    terrainDensityComputeShader.SetTexture(densityKernel, "PeaksAndValleysCurveTexture", noiseGenerator.remoteTexture);
                // terrainDensityComputeShader.SetBuffer(densityKernel, "PeaksAndValleysCurveArray", noiseCurveBuffer);
            }
            if (noiseGenerator.noiseGeneratorType == NoiseGenerator.NoiseGeneratorType.ErosionMapGenerator)
            {
                terrainDensityComputeShader.SetBool("ErosionNoiseActivated", noiseGenerator.activated);
                terrainDensityComputeShader.SetBuffer(densityKernel, "ErosionNoiseBuffer", noiseBuffer);
                if (noiseGenerator.remoteTexture == null)
                    terrainDensityComputeShader.SetTexture(densityKernel, "ErosionCurveTexture", SplineCurveFunctions.ArrayToTexture(SplineCurveFunctions.CurveToArray(noiseGenerator.valueCurve)));
                else
                    terrainDensityComputeShader.SetTexture(densityKernel, "ErosionCurveTexture", noiseGenerator.remoteTexture);
                // terrainDensityComputeShader.SetBuffer(densityKernel, "ErosionCurveArray", noiseCurveBuffer);
            }
            noiseBuffers.Add(noiseBuffer);
        }

        heightsBuffer = new ComputeBuffer((terrainDensityData.width + 1) * (terrainDensityData.width + 1) * (terrainDensityData.width + 1), sizeof(float));

        terrainDensityComputeShader.SetBuffer(densityKernel, "HeightsBuffer", heightsBuffer);
        terrainDensityComputeShader.Dispatch(densityKernel, Mathf.CeilToInt(terrainDensityData.width / 8f) + 1, Mathf.CeilToInt(terrainDensityData.width / 8f) + 1, Mathf.CeilToInt(terrainDensityData.width / 8f) + 1);

        foreach (ComputeBuffer noiseBuffer in noiseBuffers)
        {
            ComputeBufferPoolManager.Instance.ReturnComputeBuffer("NoiseBuffer", noiseBuffer);
        }

        int size = (terrainDensityData.width + 1) * (terrainDensityData.width + 1) * (terrainDensityData.width + 1);

        if (!initialLoadComplete)
        {
            heightsArray = new float[size];
            heightsBuffer.GetData(heightsArray, 0, 0, size);

            // heightsBuffer.Dispose();

            MarchingCubesJobHandler(heightsArray, false);
        }
        else
        {
            ChunkGenNetwork.Instance.pendingReadbacks.Enqueue(new ChunkGenNetwork.ReadbackRequest(bounds, heightsBuffer, (AsyncGPUReadbackRequest dataRequest) =>
            {
                if (dataRequest.hasError)
                {
                    Debug.LogError("Failed to read heights buffer.");
                    return;
                }

                heightsArray = new float[(terrainDensityData.width + 1) * (terrainDensityData.width + 1) * (terrainDensityData.width + 1)];
                NativeArray<float> rawData = dataRequest.GetData<float>();
                // heightsBuffer.Dispose();

                for (int i = 0; i < size; i++)
                {
                    heightsArray[i] = rawData[i];
                }

                MarchingCubesJobHandler(heightsArray, false);
            }), bounds.SqrDistance(ChunkGenNetwork.Instance.viewerPos));
        }

        // return heightsBuffer;
    }
    /// <summary>
    /// Sets up a mesh given a vertex array and count using lower level api for better performance
    /// </summary>
    /// <param name="vertexCount">The amount of items in the vertex array</param>
    /// <param name="vertexArray">An array of vertices given by marching cubes</param>
    /// <param name="terraforming">Whether the user is terraforming</param>
    public void SetMeshValuesPerformant(int vertexCount, Triangle[] vertexArray, bool terraforming)
    {
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        List<Triangle> validTriangles = new();
        for(int i = 0; i < vertexCount; i++)
        {
            Triangle t = vertexArray[i];
            if (!(math.all(t.v1.position == float3.zero) && math.all(t.v2.position == float3.zero) && math.all(t.v3.position == float3.zero))) 
                validTriangles.Add(t);
        }

        meshData.SetVertexBufferParams(validTriangles.Count * 3,
        new VertexAttributeDescriptor(VertexAttribute.Position),
        new VertexAttributeDescriptor(VertexAttribute.Normal));

        var vertexBuffer = meshData.GetVertexData<Vertex>(0);

        meshData.SetIndexBufferParams(validTriangles.Count * 3, IndexFormat.UInt32);
        var indexBuffer = meshData.GetIndexData<int>();
        
        for (int i = 0; i < validTriangles.Count; i++)
        {
            int start = i * 3;
            Triangle t = validTriangles[i];
            
            if (math.all(t.v1.position == float3.zero) && math.all(t.v2.position == float3.zero) && math.all(t.v3.position == float3.zero)) continue;

            vertexBuffer[start] = t.v1;
            vertexBuffer[start + 1] = t.v2;
            vertexBuffer[start + 2] = t.v3;

            indexBuffer[start] = start;
            indexBuffer[start + 1] = start + 1;
            indexBuffer[start + 2] = start + 2;
        }

        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, validTriangles.Count * 3, MeshTopology.Triangles));

        if (!terraforming)
        {
            assetSpawner.chunkVertices = new NativeArray<Vertex>(vertexBuffer.Length, Allocator.Persistent);
            assetSpawner.chunkVertices.CopyFrom(vertexBuffer);
            assetSpawner.heightsArray = heightsArray;
        }

        Mesh mesh = new Mesh();
        // Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh, MeshUpdateFlags.Default);
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
        // mesh.bounds = new Bounds(chunkPos + (new Vector3(0.5f, 0.5f, 0.5f) * terrainDensityData.width), Vector3.one * terrainDensityData.width);

        // if (lodData.lod == ChunkGenNetwork.LOD.LOD1)
        // {
        //     lod1Mesh = mesh;
        //     meshCollider.sharedMesh = mesh;
        // }
        // if (lodData.lod == ChunkGenNetwork.LOD.LOD2) lod2Mesh = mesh;
        // if (lodData.lod == ChunkGenNetwork.LOD.LOD3) lod3Mesh = mesh;
        // if (lodData.lod == ChunkGenNetwork.LOD.LOD6) lod6Mesh = mesh;

        // if (lodData.lod == currentLOD)
        // {
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
        OnMeshGenerated?.Invoke(generatedMesh);

        mesh.bounds = new Bounds(chunkPos + (new Vector3(0.5f, 0.5f, 0.5f) * terrainDensityData.width), Vector3.one * terrainDensityData.width);

        if (!terraforming)
        {
            assetSpawner.SpawnAssets();
        }
        // }
    }

    public void MarchingCubesJobHandler(float[] heights, bool terraforming)
    {
        int iterations = Mathf.CeilToInt((terrainDensityData.width + 1) / ChunkGenNetwork.Instance.resolution) * Mathf.CeilToInt((terrainDensityData.width + 1) / ChunkGenNetwork.Instance.resolution) * Mathf.CeilToInt((terrainDensityData.width + 1) / ChunkGenNetwork.Instance.resolution);

        NativeList<Triangle> triangleArray = new(iterations, Allocator.Persistent);
        NativeArray<float> heightsArray = new(heights, Allocator.Persistent);
        NativeArray<float3> vertexOffsetTable = new(MarchingCubesTables.vertexOffsetTable, Allocator.Persistent);
        NativeArray<int> edgeIndexTable = new(MarchingCubesTables.edgeIndexTable, Allocator.Persistent);
        NativeArray<int> triangleTable = new(MarchingCubesTables.triangleTable, Allocator.Persistent);

        MarchingCubesJob marchingCubesJob = new MarchingCubesJob
        {
            triangleArray = triangleArray.AsParallelWriter(),
            heightsArray = heightsArray,
            vertexOffsetTable = vertexOffsetTable,
            edgeIndexTable = edgeIndexTable,
            triangleTable = triangleTable,
            chunkSize = terrainDensityData.width,
            chunkPos = new int3(chunkPos.x, chunkPos.y, chunkPos.z),
            isolevel = terrainDensityData.isolevel,
            lerpToggle = terrainDensityData.lerp,
            resolution = ChunkGenNetwork.Instance.resolution,
        };

        JobHandle marchingCubesHandler = marchingCubesJob.Schedule(iterations, 16);
        marchingCubesHandler.Complete();

        if (terrainDensityData.waterLevel > chunkPos.y && terrainDensityData.waterLevel < Mathf.RoundToInt(chunkPos.y + terrainDensityData.width))
        {
            waterGen.UpdateMesh();
        }

        NativeArray<Triangle> triangleArrayCopy = triangleArray.AsArray();
        SetMeshValuesPerformant(triangleArray.Length, triangleArrayCopy.ToArray(), terraforming);
        triangleArray.Dispose();
        heightsArray.Dispose();
        vertexOffsetTable.Dispose();
        edgeIndexTable.Dispose();
        triangleTable.Dispose();
    }
    [BurstCompile]
    private struct MarchingCubesJob : IJobParallelFor
    {
        public NativeList<Triangle>.ParallelWriter triangleArray;
        [ReadOnly] public NativeArray<float> heightsArray;
        [ReadOnly] public NativeArray<float3> vertexOffsetTable;
        [ReadOnly] public NativeArray<int> edgeIndexTable;
        [ReadOnly] public NativeArray<int> triangleTable;
        public int chunkSize;
        public int3 chunkPos;
        public float isolevel;
        public bool lerpToggle;
        public int resolution;
        public void Execute(int index)
        {
            int x = index / ((chunkSize + 1) * (chunkSize + 1));
            int y = index / (chunkSize + 1) % (chunkSize + 1);
            int z = index % (chunkSize + 1);
            uint3 id = new((uint)x, (uint)y, (uint)z);

            if (id.x >= chunkSize || id.y >= chunkSize || id.z >= chunkSize)
                return;

            if (id.x % resolution != 0 || id.y % resolution != 0 || id.z % resolution != 0)
                return;

            CubeVertices cubeVertices;
            float adjustedIdx = id.x * resolution;
            float adjustedIdy = id.y * resolution;
            float adjustedIdz = id.z * resolution;
            float3 adjustedPos = new float3(adjustedIdx, adjustedIdy, adjustedIdz);
            cubeVertices.v0 = heightsArray[FlattenIndex(adjustedPos + (vertexOffsetTable[0] * resolution), chunkSize)];
            cubeVertices.v1 = heightsArray[FlattenIndex(adjustedPos + (vertexOffsetTable[1] * resolution), chunkSize)];
            cubeVertices.v2 = heightsArray[FlattenIndex(adjustedPos + (vertexOffsetTable[2] * resolution), chunkSize)];
            cubeVertices.v3 = heightsArray[FlattenIndex(adjustedPos + (vertexOffsetTable[3] * resolution), chunkSize)];
            cubeVertices.v4 = heightsArray[FlattenIndex(adjustedPos + (vertexOffsetTable[4] * resolution), chunkSize)];
            cubeVertices.v5 = heightsArray[FlattenIndex(adjustedPos + (vertexOffsetTable[5] * resolution), chunkSize)];
            cubeVertices.v6 = heightsArray[FlattenIndex(adjustedPos + (vertexOffsetTable[6] * resolution), chunkSize)];
            cubeVertices.v7 = heightsArray[FlattenIndex(adjustedPos + (vertexOffsetTable[7] * resolution), chunkSize)];

            float3 cubePosition = new float3(adjustedIdx + chunkPos.x, adjustedIdy + chunkPos.y, adjustedIdz + chunkPos.z);

            int configurationIndex = 0;

            if (cubeVertices.v0 < isolevel) configurationIndex |= 1;
            if (cubeVertices.v1 < isolevel) configurationIndex |= 2;
            if (cubeVertices.v2 < isolevel) configurationIndex |= 4;
            if (cubeVertices.v3 < isolevel) configurationIndex |= 8;
            if (cubeVertices.v4 < isolevel) configurationIndex |= 16;
            if (cubeVertices.v5 < isolevel) configurationIndex |= 32;
            if (cubeVertices.v6 < isolevel) configurationIndex |= 64;
            if (cubeVertices.v7 < isolevel) configurationIndex |= 128;

            if (configurationIndex == 0 || configurationIndex == 255)
            {
                return;
            }

            int edgeIndex = 0;
            int flattenedConfigurationIndex = configurationIndex << 4;

            for (int t = 0; t < 5; t++)
            {
                int edge1Value = triangleTable[flattenedConfigurationIndex + edgeIndex];
                int edge2Value = triangleTable[flattenedConfigurationIndex + edgeIndex + 1];
                int edge3Value = triangleTable[flattenedConfigurationIndex + edgeIndex + 2];

                if (edge1Value == -1 || edge2Value == -1 || edge3Value == -1)
                {
                    return;
                }

                int flattenedEdge1Value = edge1Value << 1;
                float3 edge1V1 = cubePosition + (vertexOffsetTable[edgeIndexTable[flattenedEdge1Value]] * resolution);
                float3 edge1V2 = cubePosition + (vertexOffsetTable[edgeIndexTable[flattenedEdge1Value + 1]] * resolution);
                
                int flattenedEdge2Value = edge2Value << 1;
                float3 edge2V1 = cubePosition + (vertexOffsetTable[edgeIndexTable[flattenedEdge2Value]] * resolution);
                float3 edge2V2 = cubePosition + (vertexOffsetTable[edgeIndexTable[flattenedEdge2Value + 1]] * resolution);

                int flattenedEdge3Value = edge3Value << 1;
                float3 edge3V1 = cubePosition + (vertexOffsetTable[edgeIndexTable[flattenedEdge3Value]] * resolution);
                float3 edge3V2 = cubePosition + (vertexOffsetTable[edgeIndexTable[flattenedEdge3Value + 1]] * resolution);

                float3 vertex1;
                float3 vertex2;
                float3 vertex3;

                if (lerpToggle)
                {
                    vertex1 = math.lerp(edge1V1, edge1V2, (isolevel - cubeVertices.GetCubeVertex(edgeIndexTable[flattenedEdge1Value])) / (cubeVertices.GetCubeVertex(edgeIndexTable[flattenedEdge1Value + 1]) - cubeVertices.GetCubeVertex(edgeIndexTable[flattenedEdge1Value])));
                    vertex2 = math.lerp(edge2V1, edge2V2, (isolevel - cubeVertices.GetCubeVertex(edgeIndexTable[flattenedEdge2Value])) / (cubeVertices.GetCubeVertex(edgeIndexTable[flattenedEdge2Value + 1]) - cubeVertices.GetCubeVertex(edgeIndexTable[flattenedEdge2Value])));
                    vertex3 = math.lerp(edge3V1, edge3V2, (isolevel - cubeVertices.GetCubeVertex(edgeIndexTable[flattenedEdge3Value])) / (cubeVertices.GetCubeVertex(edgeIndexTable[flattenedEdge3Value + 1]) - cubeVertices.GetCubeVertex(edgeIndexTable[flattenedEdge3Value])));
                }
                else
                {
                    vertex1 = (edge1V1 + edge1V2) / 2;
                    vertex2 = (edge2V1 + edge2V2) / 2;
                    vertex3 = (edge3V1 + edge3V2) / 2;
                }

                float3 normal = math.normalize(math.cross(vertex2 - vertex1, vertex3 - vertex1));

                Triangle tri;
                tri.v1.position = vertex1;
                tri.v2.position = vertex2;
                tri.v3.position = vertex3;

                tri.v1.normal = normal;
                tri.v2.normal = normal;
                tri.v3.normal = normal;
                triangleArray.AddNoResize(tri);


                edgeIndex += 3;
            }
        }

        int FlattenIndex(float3 id, int size)
        {
            return (int)(id.x * (size + 1) * (size + 1) + id.y * (size + 1) + id.z);
        }

        struct CubeVertices
        {
            public float v0, v1, v2, v3, v4, v5, v6, v7;

            public float GetCubeVertex(int cubeVertIndex)
            {
                switch (cubeVertIndex)
                {
                    case 0: return v0;
                    case 1: return v1;
                    case 2: return v2;
                    case 3: return v3;
                    case 4: return v4;
                    case 5: return v5;
                    case 6: return v6;
                    case 7: return v7;
                    default: return 0f;
                }
            }
        }
    }
    // Releases height buffers when the application is closed/stopped
    void OnDestroy()
    {
        if (heightsBuffer != null && heightsBuffer.IsValid())
        {
            heightsBuffer.Release();
        }
    }
    void OnDisable()
    {
        if (heightsBuffer != null && heightsBuffer.IsValid())
        {
            heightsBuffer.Release();
        }
    }
    /// <summary>
    /// Draws wireframe cubes to visualize chunks
    /// </summary>
    void OnDrawGizmos()
    {
        if (terrainDensityData == null || gameObject.GetComponent<MeshRenderer>().enabled == false) return; // still not found
        Gizmos.DrawWireCube(chunkPos + (new Vector3(0.5f, 0.5f, 0.5f) * terrainDensityData.width), Vector3.one * terrainDensityData.width);
    }
    /// <summary>
    /// Perform marching cubes in a compute shader and trigger mesh generation and asset spawning
    /// </summary>
    /// <param name="heightsBuffer">The buffer containing the chunks density field</param>
    /// <param name="terraforming">Whether the user is terraforming</param>
    public void AsyncMarchingCubes(ComputeBuffer heightsBuffer, bool terraforming)
    {
        int marchingKernel = marchingCubesComputeShader.FindKernel("MarchingCubes");

        marchingCubesComputeShader.SetBuffer(marchingKernel, "HeightsBuffer", heightsBuffer);
        ComputeBuffer vertexBuffer = ComputeBufferPoolManager.Instance.GetComputeBuffer("VertexBuffer", terrainDensityData.width * terrainDensityData.width * terrainDensityData.width, sizeof(float) * 18, ComputeBufferType.Append);
        marchingCubesComputeShader.SetBuffer(marchingKernel, "VertexBuffer", vertexBuffer);

        marchingCubesComputeShader.SetInt("ChunkSize", terrainDensityData.width);
        marchingCubesComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);
        marchingCubesComputeShader.SetFloat("isolevel", terrainDensityData.isolevel);
        marchingCubesComputeShader.SetBool("lerpToggle", terrainDensityData.lerp);
        marchingCubesComputeShader.SetInt("Resolution", ChunkGenNetwork.Instance.resolution);

        vertexBuffer.SetCounterValue(0);
        marchingCubesComputeShader.Dispatch(marchingKernel, Mathf.CeilToInt(terrainDensityData.width / ChunkGenNetwork.Instance.resolution / 4f), Mathf.CeilToInt(terrainDensityData.width / ChunkGenNetwork.Instance.resolution / 4f), Mathf.CeilToInt(terrainDensityData.width / ChunkGenNetwork.Instance.resolution / 4f));

        ComputeBuffer vertexCountBuffer = ComputeBufferPoolManager.Instance.GetComputeBuffer("VertexCountBuffer", 1, sizeof(int), ComputeBufferType.Raw);
        ComputeBuffer.CopyCount(vertexBuffer, vertexCountBuffer, 0);

        ChunkGenNetwork.Instance.pendingReadbacks.Enqueue(new ChunkGenNetwork.ReadbackRequest(bounds, vertexCountBuffer, (AsyncGPUReadbackRequest countRequest) =>
        {
            if (countRequest.hasError)
            {
                Debug.LogError("Failed to read vertex count.");
                return;
            }

            int vertexCount = countRequest.GetData<int>()[0];
            ComputeBufferPoolManager.Instance.ReturnComputeBuffer("VertexCountBuffer", vertexCountBuffer);

            ChunkGenNetwork.Instance.pendingReadbacks.Enqueue(new ChunkGenNetwork.ReadbackRequest(bounds, vertexBuffer, (AsyncGPUReadbackRequest dataRequest) =>
            {
                if (dataRequest.hasError)
                {
                    Debug.LogError("Failed to read vertex buffer.");
                    return;
                }

                Triangle[] vertexArray = new Triangle[vertexCount];
                NativeArray<Triangle> rawData = dataRequest.GetData<Triangle>();

                for (int i = 0; i < vertexCount; i++)
                {
                    vertexArray[i] = rawData[i];
                }

                ComputeBufferPoolManager.Instance.ReturnComputeBuffer("VertexBuffer", vertexBuffer);

                if (terrainDensityData.waterLevel > chunkPos.y && terrainDensityData.waterLevel < Mathf.RoundToInt(chunkPos.y + terrainDensityData.width))
                {
                    waterGen.UpdateMesh();
                }

                if (vertexCount > 0)
                {
                    SetMeshValuesPerformant(vertexCount, vertexArray, terraforming);
                    // ChunkGenNetwork.Instance.pendingMeshInits.Enqueue(() =>
                    //     SetMeshValuesPerformant(vertexCount, vertexArray, terraforming)
                    // );
                }
            }), bounds.SqrDistance(ChunkGenNetwork.Instance.viewerPos));
        }), bounds.SqrDistance(ChunkGenNetwork.Instance.viewerPos));
    }
    /// <summary>
    /// Perform marching cubes in a compute shader and trigger mesh generation and asset spawning
    /// </summary>
    /// <param name="heightsBuffer">The buffer containing the chunks density field</param>
    /// <param name="terraforming">Whether the user is terraforming</param>
    public void SyncMarchingCubes(ComputeBuffer heightsBuffer, bool terraforming)
    {
        int marchingKernel = marchingCubesComputeShader.FindKernel("MarchingCubes");

        marchingCubesComputeShader.SetBuffer(marchingKernel, "HeightsBuffer", heightsBuffer);
        vertexBuffer = ComputeBufferPoolManager.Instance.GetComputeBuffer("VertexBuffer", terrainDensityData.width * terrainDensityData.width * terrainDensityData.width, sizeof(float) * 18, ComputeBufferType.Append);
        marchingCubesComputeShader.SetBuffer(marchingKernel, "VertexBuffer", vertexBuffer);

        marchingCubesComputeShader.SetInt("ChunkSize", terrainDensityData.width);
        marchingCubesComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);
        marchingCubesComputeShader.SetFloat("isolevel", terrainDensityData.isolevel);
        marchingCubesComputeShader.SetBool("lerpToggle", terrainDensityData.lerp);
        marchingCubesComputeShader.SetInt("Resolution", ChunkGenNetwork.Instance.resolution);

        vertexBuffer.SetCounterValue(0);
        marchingCubesComputeShader.Dispatch(marchingKernel, Mathf.CeilToInt(terrainDensityData.width / ChunkGenNetwork.Instance.resolution / 4f), Mathf.CeilToInt(terrainDensityData.width / ChunkGenNetwork.Instance.resolution / 4f), Mathf.CeilToInt(terrainDensityData.width / ChunkGenNetwork.Instance.resolution / 4f));

        // ChunkGenNetwork.Instance.terrainMaterial.SetBuffer("_Triangles", vertexBuffer);
        // Graphics.DrawProceduralIndirect(ChunkGenNetwork.Instance.terrainMaterial, new Bounds(chunkPos + (new Vector3(0.5f, 0.5f, 0.5f) * terrainDensityData.width), Vector3.one * terrainDensityData.width), MeshTopology.Triangles, vertexBuffer);
        // rendering = true;
        ComputeBuffer vertexCountBuffer = ComputeBufferPoolManager.Instance.GetComputeBuffer("VertexCountBuffer", 1, sizeof(int), ComputeBufferType.Raw);
        ComputeBuffer.CopyCount(vertexBuffer, vertexCountBuffer, 0);

        int[] vertexCountArray = { 0 };
        vertexCountBuffer.GetData(vertexCountArray);

        ComputeBufferPoolManager.Instance.ReturnComputeBuffer("VertexCountBuffer", vertexCountBuffer);

        int vertexCount = vertexCountArray[0];

        Triangle[] vertexArray = new Triangle[vertexCount];
        vertexBuffer.GetData(vertexArray, 0, 0, vertexCount);

        ComputeBufferPoolManager.Instance.ReturnComputeBuffer("VertexBuffer", vertexBuffer);

        if (terrainDensityData.waterLevel > chunkPos.y && terrainDensityData.waterLevel < Mathf.RoundToInt(chunkPos.y + terrainDensityData.width))
        {
            waterGen.UpdateMesh();
        }

        SetMeshValuesPerformant(vertexCount, vertexArray, terraforming);
        // ChunkGenNetwork.Instance.pendingMeshInits.Enqueue(() =>
        //     SetMeshValuesPerformant(vertexCount, vertexArray, terraforming)
        // );
    }
    // Old mesh setup code saved for reference

    // public void SetMeshValues(int vertexCount, Triangle[] vertexArray, bool terraforming)
    // {
    //     vertices.Clear();
    //     triangles.Clear();
    //     verticesNormals.Clear();
    //     vertices.Capacity = vertexCount * 3;
    //     verticesNormals.Capacity = vertexCount * 3;
    //     triangles.Capacity = vertexCount * 3;
    //     for (int i = 0; i < vertexCount; i++)
    //     {
    //         Triangle t = vertexArray[i];
    //         vertices.Add(t.v1.position);
    //         vertices.Add(t.v2.position);
    //         vertices.Add(t.v3.position);

    //         Vertex v1;
    //         v1.position = t.v1.position;
    //         v1.normal = t.v1.normal;
    //         verticesNormals.Add(v1);
    //         Vertex v2;
    //         v2.position = t.v2.position;
    //         v2.normal = t.v2.normal;
    //         verticesNormals.Add(v2);
    //         Vertex v3;
    //         v3.position = t.v3.position;
    //         v3.normal = t.v3.normal;
    //         verticesNormals.Add(v3);

    //         triangles.Add(i * 3);
    //         triangles.Add(i * 3 + 1);
    //         triangles.Add(i * 3 + 2);
    //     }
    //     SetupMesh(terraforming);
    // }

    // public void SetupMesh(bool terraforming)
    // {
    //     Mesh mesh = new Mesh();
    //     mesh.indexFormat = IndexFormat.UInt32;
    //     mesh.SetVertices(vertices);
    //     mesh.SetTriangles(triangles, 0);
    //     List<Vector3> normals = new List<Vector3>(verticesNormals.Count);
    //     for (int i = 0; i < verticesNormals.Count; i++)
    //     {
    //         normals.Add(verticesNormals[i].normal);
    //     }
    //     mesh.SetNormals(normals);
    //     mesh.RecalculateBounds();


    //     meshFilter.mesh = mesh;
    //     meshCollider.sharedMesh = mesh;

    //     assetSpawner.worldVertices = verticesNormals.ToArray();
    //     if (!terraforming)
    //     {
    //         assetSpawner.SpawnAssets();
    //     }

    //     if (Mathf.RoundToInt(chunkPos.y / terrainDensityData.width) == 0)
    //     {
    //         waterGen.UpdateMesh();
    //     }

    //     vertices.Clear();
    //     triangles.Clear();
    //     verticesNormals.Clear();
    // }
}