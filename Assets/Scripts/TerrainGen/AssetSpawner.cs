using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using FishNet.Demo.Benchmarks.NetworkTransforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

public class AssetSpawner : MonoBehaviour
{
    public int vertexBufferLength;
    public List<List<Asset>> spawnedAssets;
    public TerrainDensityData terrainDensityData;
    public AssetSpawnData assetSpawnData;
    public List<List<ComputeMarchingCubes.Vertex>> spawnPoints;
    public List<List<ComputeMarchingCubes.Vertex>> acceptedSpawnPoints;
    public NativeArray<ComputeMarchingCubes.Vertex> chunkVertices;
    public List<ComputeMarchingCubes.Vertex> vertices;
    public float[] heightsArray;
    float3[] minDepthPoints;
    public Vector3Int chunkPos;
    public LayerMask assetLayer;
    public LayerMask interactLayer;
    public int assetSpacing = 8;
    public bool assetsSet = false;
    public bool emptyChunk = false;
    void Start() {
        assetLayer = LayerMask.GetMask("Asset Layer");
        interactLayer = LayerMask.GetMask("Interact Layer");
    }
    /// <summary>
    /// Initiate asset spawning for a given chunk
    /// </summary>
    public void SpawnAssets()
    {
        vertexBufferLength = chunkVertices.Length;
        if (vertexBufferLength <= 0) emptyChunk = true;
        if (!assetSpawnData.assets.ContainsKey(chunkPos))
        {
            vertices = chunkVertices.ToList();
            vertices.Sort();
            uint seed = Hash(chunkPos.x, chunkPos.y, chunkPos.z, terrainDensityData.noiseGenerators[0].noiseSeed);
            Unity.Mathematics.Random rng = new(seed);
            InitializeData();
            CreateSpawnPointsJobHandler(ref rng);
            SetSpawnPoints();
            AssetSpawnHandler(rng);
        }
    }
    private void OnDisable()
    {
        if (chunkVertices != null) {
            chunkVertices.Dispose();
        }
    }
    /// <summary>
    /// Initizalize all the data structures
    /// </summary>
    private void InitializeData()
    {
        spawnPoints?.Clear();
        acceptedSpawnPoints?.Clear();
        spawnedAssets?.Clear();
        spawnPoints = new List<List<ComputeMarchingCubes.Vertex>>(assetSpawnData.spawnableAssets.Count);
        acceptedSpawnPoints = new List<List<ComputeMarchingCubes.Vertex>>(assetSpawnData.spawnableAssets.Count);
        spawnedAssets = new List<List<Asset>>(assetSpawnData.spawnableAssets.Count);
        assetSpawnData.assets.Add(chunkPos, assetSpawnData.spawnableAssets);
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            spawnPoints.Add(new List<ComputeMarchingCubes.Vertex>());
            acceptedSpawnPoints.Add(new List<ComputeMarchingCubes.Vertex>());
            spawnedAssets.Add(new List<Asset>());
        }
    }
    public void CreateSpawnPointsJobHandler(ref Unity.Mathematics.Random rng)
    {
        List<AssetSpawnFilters> assetSpawnFilters = new(assetSpawnData.spawnableAssets.Count);
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            // if (emptyChunk && !assetSpawnData.spawnableAssets[i].undergroundAsset) continue;
            assetSpawnFilters.Add(new AssetSpawnFilters(assetSpawnData.spawnableAssets[i].rotateToFaceNormal, assetSpawnData.spawnableAssets[i].spawnProbability, assetSpawnData.spawnableAssets[i].useMinSlope,
                                                         assetSpawnData.spawnableAssets[i].minSlope, assetSpawnData.spawnableAssets[i].useMaxSlope, assetSpawnData.spawnableAssets[i].maxSlope,
                                                         assetSpawnData.spawnableAssets[i].useMinHeight, assetSpawnData.spawnableAssets[i].minHeight, assetSpawnData.spawnableAssets[i].useMaxHeight,
                                                         assetSpawnData.spawnableAssets[i].maxHeight, assetSpawnData.spawnableAssets[i].underwaterAsset, assetSpawnData.spawnableAssets[i].undergroundAsset));
        }
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            // if (emptyChunk && !assetSpawnData.spawnableAssets[i].undergroundAsset) continue;
            if (assetSpawnFilters[i].undergroundAsset)
            {
                minDepthPoints = GetMinDepthChunkPoints(assetSpawnData.spawnableAssets[i].minDepth, heightsArray);
                if (minDepthPoints == null || minDepthPoints.Length == 0) continue;
            }
            for (int j = 0; j < assetSpawnData.spawnableAssets[i].maxPerChunk; j++)
            {
                float roll = rng.NextFloat();

                if (assetSpawnFilters[i].spawnProbability < roll) continue;

                int randomIndex;
                float3 spawnPoint;
                float3 spawnPointNormal;

                if (assetSpawnFilters[i].undergroundAsset)
                {
                    randomIndex = rng.NextInt(0, minDepthPoints.Length);
                    spawnPoint = minDepthPoints[randomIndex];
                    spawnPointNormal = new float3(rng.NextFloat(0f, 360f), rng.NextFloat(0f, 360f), rng.NextFloat(0f, 360f));
                }
                else
                {
                    if (emptyChunk) continue;
                    randomIndex = rng.NextInt(0, vertices.Count);
                    spawnPoint = vertices[randomIndex].position;
                    spawnPointNormal = vertices[randomIndex].normal;
                }

                if (!assetSpawnFilters[i].rotateToFaceNormal)
                {
                    spawnPoint.y -= 0.75f;
                }
                else
                {
                    spawnPoint.y -= 0.1f;
                }

                float height = spawnPoint.y;
                float slope = math.round(math.degrees(math.acos(math.clamp(math.dot(math.normalize(spawnPointNormal), math.up()), -1f, 1f))) * 100f) / 100f;

                if (assetSpawnFilters[i].useMinSlope && slope < assetSpawnFilters[i].minSlope - 0.01f) continue;
                if (assetSpawnFilters[i].useMaxSlope && slope > assetSpawnFilters[i].maxSlope + 0.01f) continue;
                if (assetSpawnFilters[i].useMinHeight && height < assetSpawnFilters[i].minHeight - 0.01f) continue;
                if (assetSpawnFilters[i].useMaxHeight && height > assetSpawnFilters[i].maxHeight + 0.01f) continue;
                if (assetSpawnFilters[i].underwaterAsset && height > terrainDensityData.waterLevel - 3) continue;
                if (!assetSpawnFilters[i].underwaterAsset && height < terrainDensityData.waterLevel && !assetSpawnFilters[i].undergroundAsset) continue;
                ComputeMarchingCubes.Vertex vert;
                vert.position = spawnPoint;
                vert.normal = spawnPointNormal;
                if (vert.position.Equals(float3.zero) || vert.normal.Equals(float3.zero)) continue;
                spawnPoints[i].Add(vert);
            }
        }

        float spacingSquared = assetSpacing * assetSpacing;
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            List<ComputeMarchingCubes.Vertex> tempAccepted = new();
            if (spawnPoints[i].Count == 0) continue;
            tempAccepted.Add(spawnPoints[i][0]);

            for (int j = 1; j < spawnPoints[i].Count; j++)
            {
                bool tooClose = false;
                foreach (var accepted in tempAccepted)
                {
                    if (math.lengthsq(spawnPoints[i][j].position - accepted.position) <= spacingSquared)
                    {
                        tooClose = true;
                        break;
                    }
                    Collider[] colliders = Physics.OverlapSphere(spawnPoints[i][j].position, assetSpacing, assetLayer | interactLayer);
                    if (colliders.Length > 0)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    tempAccepted.Add(spawnPoints[i][j]);
                }
            }

            acceptedSpawnPoints[i].AddRange(tempAccepted);
        }
    }
    public static uint Hash(int x, int y, int z, int baseSeed)
    {
        uint hash = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(z * 83492791) ^ (uint)baseSeed;
        return hash;
    }
// public void CreateSpawnPointsJobHandler()
    // {
    //     int totalIterations = assetSpawnData.spawnableAssets.Count * maxAttempts;
    //     NativeArray<AssetSpawnFilters> assetSpawnFilters = AssetSpawnFiltersNativeArrayPoolManager.Instance.GetNativeArray("AssetSpawnFiltersArray", assetSpawnData.spawnableAssets.Count);
    //     for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
    //     {
    //         assetSpawnFilters[i] = new AssetSpawnFilters(assetSpawnData.spawnableAssets[i].rotateToFaceNormal, assetSpawnData.spawnableAssets[i].spawnProbability, assetSpawnData.spawnableAssets[i].useMinSlope,
    //                                                      assetSpawnData.spawnableAssets[i].minSlope, assetSpawnData.spawnableAssets[i].useMaxSlope, assetSpawnData.spawnableAssets[i].maxSlope,
    //                                                      assetSpawnData.spawnableAssets[i].useMinHeight, assetSpawnData.spawnableAssets[i].minHeight, assetSpawnData.spawnableAssets[i].useMaxHeight,
    //                                                      assetSpawnData.spawnableAssets[i].maxHeight, assetSpawnData.spawnableAssets[i].underwaterAsset);
    //     }
    //     NativeArray<ComputeMarchingCubes.Vertex> flatSpawnPoints = VertexNativeArrayPoolManager.Instance.GetNativeArray("VertexArray", totalIterations);
    //     var spawnPointsJob = new CreateSpawnPointsJob
    //     {
    //         vertexArray = chunkVertices,
    //         assetSpawnFilters = assetSpawnFilters,
    //         spawnPoints = flatSpawnPoints,
    //         baseSeed = terrainDensityData.noiseGenerators[0].noiseSeed, // Update Later
    //         chunkPos = new int3(chunkPos.x, chunkPos.y, chunkPos.z),
    //         waterLevel = terrainDensityData.waterLevel,
    //         maxAttempts = maxAttempts
    //     };

    //     JobHandle spawnPointsHandler = spawnPointsJob.Schedule(totalIterations, terrainDensityData.width);
    //     spawnPointsHandler.Complete();

    //     for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
    //     {
    //         List<ComputeMarchingCubes.Vertex> assetPoints = new();

    //         int start = i * maxAttempts;
    //         int end = start + maxAttempts;

    //         for (int j = start; j < end; j++)
    //         {
    //             var vert = flatSpawnPoints[j];
    //             if (vert.position.Equals(float3.zero) || vert.normal.Equals(float3.zero)) continue;
    //             assetPoints.Add(vert);
    //         }

    //         spawnPoints[i] = assetPoints;
    //     }

    //     AssetSpawnFiltersNativeArrayPoolManager.Instance.ReturnNativeArray("AssetSpawnFiltersArray", assetSpawnFilters);
    //     VertexNativeArrayPoolManager.Instance.ReturnNativeArray("VertexArray", flatSpawnPoints);
    //     // chunkVertices.Dispose();

    //     for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
    //     {
    //         float spacingSquared = assetSpacing * assetSpacing;
    //         List<ComputeMarchingCubes.Vertex> tempAccepted = new();
    //         if (spawnPoints[i].Count == 0) continue;
    //         tempAccepted.Add(spawnPoints[i][0]);

    //         for (int j = 1; j < spawnPoints[i].Count; j++)
    //         {
    //             bool tooClose = false;
    //             foreach (var accepted in tempAccepted)
    //             {
    //                 if (math.lengthsq(spawnPoints[i][j].position - accepted.position) <= spacingSquared)
    //                 {
    //                     tooClose = true;
    //                     break;
    //                 }
    //                 Collider[] colliders = Physics.OverlapSphere(spawnPoints[i][j].position, assetSpacing, assetLayer);
    //                 if (colliders.Length > 0)
    //                 {
    //                     tooClose = true;
    //                     break;
    //                 }
    //             }

    //             if (!tooClose)
    //             {
    //                 tempAccepted.Add(spawnPoints[i][j]);
    //             }
    //         }

    //         acceptedSpawnPoints[i].AddRange(tempAccepted);
    //     }
    // }
    [BurstCompile]
    private struct CreateSpawnPointsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<ComputeMarchingCubes.Vertex> vertexArray;
        [ReadOnly] public NativeArray<AssetSpawnFilters> assetSpawnFilters;
        public NativeArray<ComputeMarchingCubes.Vertex> spawnPoints;
        public int baseSeed;
        public int3 chunkPos;
        public int waterLevel;
        public int maxAttempts;
        public void Execute(int index)
        {
            int spawnableAssetIndex = index / maxAttempts;
            int assetAttemptIndex = index % maxAttempts;

            uint seed = Hash(chunkPos.x, chunkPos.y, chunkPos.z, spawnableAssetIndex, baseSeed);
            Unity.Mathematics.Random rng = new(seed + (uint)assetAttemptIndex);
            float roll = rng.NextFloat();

            if (assetSpawnFilters[spawnableAssetIndex].spawnProbability < roll) return;

            int randomIndex = rng.NextInt(0, vertexArray.Length);
            float3 spawnPoint = vertexArray[randomIndex].position;
            float3 spawnPointNormal = vertexArray[randomIndex].normal;

            if (!assetSpawnFilters[spawnableAssetIndex].rotateToFaceNormal)
            {
                spawnPoint.y -= 0.75f;
            }
            else
            {
                spawnPoint.y -= 0.1f;
            }

            float height = spawnPoint.y;
            float slope = math.degrees(math.acos(math.clamp(math.dot(math.normalize(spawnPointNormal), math.up()), -1f, 1f)));

            if (assetSpawnFilters[spawnableAssetIndex].useMinSlope && slope < assetSpawnFilters[spawnableAssetIndex].minSlope) return;
            if (assetSpawnFilters[spawnableAssetIndex].useMaxSlope && slope > assetSpawnFilters[spawnableAssetIndex].maxSlope) return;
            if (assetSpawnFilters[spawnableAssetIndex].useMinHeight && height < assetSpawnFilters[spawnableAssetIndex].minHeight) return;
            if (assetSpawnFilters[spawnableAssetIndex].useMaxHeight && height > assetSpawnFilters[spawnableAssetIndex].maxHeight) return;
            if (assetSpawnFilters[spawnableAssetIndex].underwaterAsset && height > waterLevel - 3) return;
            if (!assetSpawnFilters[spawnableAssetIndex].underwaterAsset && height < waterLevel) return;
            ComputeMarchingCubes.Vertex vert;
            vert.position = spawnPoint;
            vert.normal = spawnPointNormal;
            spawnPoints[(spawnableAssetIndex * maxAttempts) + assetAttemptIndex] = vert;
        }
        public static uint Hash(int x, int y, int z, int spawnableAssetIndex, int baseSeed)
        {
            uint hash = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(z * 83492791) ^ (uint)(spawnableAssetIndex * 1013904223) ^ (uint)baseSeed;
            return hash;
        }
    }
    public struct AssetSpawnFilters
    {
        public bool rotateToFaceNormal;
        public float spawnProbability;
        public bool useMinSlope;
        public int minSlope;
        public bool useMaxSlope;
        public int maxSlope;
        public bool useMinHeight;
        public int minHeight;
        public bool useMaxHeight;
        public int maxHeight;
        public bool underwaterAsset;
        public bool undergroundAsset;
        public AssetSpawnFilters(bool rotateToFaceNormal, float spawnProbability, bool useMinSlope, int minSlope, bool useMaxSlope, int maxSlope, bool useMinHeight, int minHeight, bool useMaxHeight, int maxHeight, bool underwaterAsset, bool undergroundAsset)
        {
            this.rotateToFaceNormal = rotateToFaceNormal;
            this.spawnProbability = spawnProbability;
            this.useMinSlope = useMinSlope;
            this.minSlope = minSlope;
            this.useMaxSlope = useMaxSlope;
            this.maxSlope = maxSlope;
            this.useMinHeight = useMinHeight;
            this.minHeight = minHeight;
            this.useMaxHeight = useMaxHeight;
            this.maxHeight = maxHeight;
            this.underwaterAsset = underwaterAsset;
            this.undergroundAsset = undergroundAsset;
        }
    }
    /// <summary>
    /// Add this chunks spawn points and game objects to a centralized scriptable object
    /// </summary>
    private void SetSpawnPoints()
    {
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            assetSpawnData.assets[chunkPos][i].spawnPoints = acceptedSpawnPoints[i].ToArray();
        }
    }
    /// <summary>
    /// Use the spawn points from the compute shader to instantiate their respective game objects
    /// </summary>
    private void AssetSpawnHandler(Unity.Mathematics.Random rng)
    {
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            ComputeMarchingCubes.Vertex[] points = assetSpawnData.assets[chunkPos][i].spawnPoints;
            if (points == null || points.Length == 0) continue;
            for (int j = 0; j < assetSpawnData.assets[chunkPos][i].spawnPoints.Length; j++)
            {
                int indexI = i;
                int indexJ = j;
                ChunkGenNetwork.Instance.pendingAssetInstantiations.Enqueue(() =>
                    AssetInstantiation(indexI, indexJ, rng)
                );
            }
        }
        assetsSet = true;
    }
    public void AssetInstantiation(int i, int j, Unity.Mathematics.Random rng)
    {
        float randomRotationDeg = rng.NextFloat(0f, 360f);
        Quaternion randomYRotation = Quaternion.Euler(0f, randomRotationDeg, 0f);
        GameObject assetToSpawn;
        if (assetSpawnData.assets[chunkPos][i].rotateToFaceNormal)
        {
            Quaternion normal = Quaternion.FromToRotation(Vector3.up, acceptedSpawnPoints[i][j].normal);
            assetToSpawn = Instantiate(assetSpawnData.assets[chunkPos][i].asset, acceptedSpawnPoints[i][j].position, normal * randomYRotation);
            assetToSpawn.transform.SetParent(gameObject.transform);
            spawnedAssets[i].Add(new Asset(assetToSpawn, assetToSpawn.GetComponent<MeshRenderer>(), assetToSpawn.GetComponent<MeshCollider>()));
        }
        else
        {
            assetToSpawn = Instantiate(assetSpawnData.assets[chunkPos][i].asset, acceptedSpawnPoints[i][j].position, randomYRotation);
            assetToSpawn.transform.SetParent(gameObject.transform);
            spawnedAssets[i].Add(new Asset(assetToSpawn, assetToSpawn.GetComponent<MeshRenderer>(), assetToSpawn.GetComponent<MeshCollider>()));
        }
        if (assetSpawnData.spawnableAssets[i].isValuable)
        {
            assetToSpawn.layer = LayerMask.NameToLayer("Interact Layer");
            ValuableProperties properties = assetToSpawn.AddComponent<ValuableProperties>();
            properties.value = rng.NextInt(assetSpawnData.spawnableAssets[i].minValue, assetSpawnData.spawnableAssets[i].maxValue);
        }
        assetSpawnData.assets[chunkPos][i].spawnedAssets.Add(new Asset(assetToSpawn, assetToSpawn.GetComponent<MeshRenderer>(), assetToSpawn.GetComponent<MeshCollider>()));
    }
    public float3[] GetMinDepthChunkPoints(float depth, float[] heightsArray)
    {
        List<float3> depthResult = new();
        int size = terrainDensityData.width + 1;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    if(heightsArray[(x * size * size) + (y * size) + z] > depth)
                    {
                        // Debug.Log(heightsArray[(x * terrainDensityData.width * terrainDensityData.width) + (y * terrainDensityData.width) + z]);
                        depthResult.Add(new float3(chunkPos.x + x, chunkPos.y + y, chunkPos.z + z));
                    }
                }
            }
        }

        return depthResult.ToArray();
    }
    /// <summary>
    /// Destroy all the assets
    /// </summary>
    public void ClearAssets()
    {
        for (int i = 0; i < assetSpawnData.assets[chunkPos].Count; i++)
        {
            if (assetSpawnData.assets[chunkPos][i].spawnedAssets != null)
            {
                foreach (Asset asset in assetSpawnData.assets[chunkPos][i].spawnedAssets)
                {
                    Destroy(asset.obj);
                }
            }
        }
    }
    /// <summary>
    /// Clear asset data
    /// </summary>
    public void ClearData() {
        assetSpawnData.ResetSpawnPoints();
    }
}
/// <summary>
/// Custom class to store provided spawnable assets and their relevant information and data
/// </summary>
[Serializable]
public class SpawnableAsset
{
    public GameObject asset;
    public ComputeMarchingCubes.Vertex[] spawnPoints;
    public List<Asset> spawnedAssets = new();
    public int maxPerChunk;
    public bool rotateToFaceNormal;
    public float spawnProbability;
    public bool useMinSlope;
    public int minSlope;
    public bool useMaxSlope;
    public int maxSlope;
    public bool useMinHeight;
    public int minHeight;
    public bool useMaxHeight;
    public int maxHeight;
    public bool underwaterAsset;
    public bool undergroundAsset;
    public float minDepth;
    public bool isValuable;
    public int minValue;
    public int maxValue;
    public SpawnableAsset(GameObject asset, int maxPerChunk, bool rotateToFaceNormal, float spawnProbability, bool useMinSlope, int minSlope, bool useMaxSlope, int maxSlope, bool useMinHeight, int minHeight, bool useMaxHeight, int maxHeight, bool underwaterAsset, float minDepth, bool isValuable, int minValue, int maxValue)
    {
        this.asset = asset;
        this.maxPerChunk = maxPerChunk;
        this.rotateToFaceNormal = rotateToFaceNormal;
        this.spawnProbability = spawnProbability;
        this.useMinSlope = useMinSlope;
        this.minSlope = minSlope;
        this.useMaxSlope = useMaxSlope;
        this.maxSlope = maxSlope;
        this.useMinHeight = useMinHeight;
        this.minHeight = minHeight;
        this.useMaxHeight = useMaxHeight;
        this.maxHeight = maxHeight;
        this.underwaterAsset = underwaterAsset;
        this.minDepth = minDepth;
        this.isValuable = isValuable;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }
}
[Serializable]
public class Asset
{
    public GameObject obj;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;
    public Asset(GameObject obj, MeshRenderer meshRenderer, MeshCollider meshCollider)
    {
        this.obj = obj;
        this.meshRenderer = meshRenderer;
        this.meshCollider = meshCollider;
    }
}