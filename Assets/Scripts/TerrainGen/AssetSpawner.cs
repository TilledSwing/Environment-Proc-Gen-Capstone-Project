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
    public Vector3Int chunkPos;
    public LayerMask assetLayer;
    public int assetSpacing = 8;
    public int maxAttempts = 8;
    public bool assetsSet = false;
    void Start() {
        assetLayer = LayerMask.GetMask("Asset Layer");
    }
    /// <summary>
    /// Initiate asset spawning for a given chunk
    /// </summary>
    public void SpawnAssets()
    {
        vertexBufferLength = chunkVertices.Length;
        vertices = chunkVertices.ToList<ComputeMarchingCubes.Vertex>();
        vertices.Sort();
        // Debug.Log(vertexBufferLength);
        if (!assetSpawnData.assets.ContainsKey(chunkPos) && vertexBufferLength > 0)
        {
            uint seed = Hash(chunkPos.x, chunkPos.y, chunkPos.z, terrainDensityData.noiseGenerators[0].noiseSeed); ;
            Unity.Mathematics.Random rng = new(seed);
            InitializeData();
            CreateSpawnPointsJobHandler(rng);
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
    public void CreateSpawnPointsJobHandler(Unity.Mathematics.Random rng)
    {
        int totalIterations = assetSpawnData.spawnableAssets.Count * maxAttempts;
        List<AssetSpawnFilters> assetSpawnFilters = new(assetSpawnData.spawnableAssets.Count);
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            assetSpawnFilters.Add(new AssetSpawnFilters(assetSpawnData.spawnableAssets[i].rotateToFaceNormal, assetSpawnData.spawnableAssets[i].spawnProbability, assetSpawnData.spawnableAssets[i].useMinSlope,
                                                         assetSpawnData.spawnableAssets[i].minSlope, assetSpawnData.spawnableAssets[i].useMaxSlope, assetSpawnData.spawnableAssets[i].maxSlope,
                                                         assetSpawnData.spawnableAssets[i].useMinHeight, assetSpawnData.spawnableAssets[i].minHeight, assetSpawnData.spawnableAssets[i].useMaxHeight,
                                                         assetSpawnData.spawnableAssets[i].maxHeight, assetSpawnData.spawnableAssets[i].underwaterAsset));
        }
        List<ComputeMarchingCubes.Vertex> flatSpawnPoints = new(totalIterations);
        for (int i = 0; i < totalIterations; i++)
        {
            var vert = new ComputeMarchingCubes.Vertex();
            vert.position = float3.zero;
            vert.normal = float3.zero;
            flatSpawnPoints.Add(vert);
        }

        // uint seed = Hash(chunkPos.x, chunkPos.y, chunkPos.z, terrainDensityData.noiseGenerators[0].noiseSeed); ;
        // Unity.Mathematics.Random rng = new(seed);

        for (int i = 0; i < totalIterations; i++)
        {
            int spawnableAssetIndex = i / maxAttempts;
            int assetAttemptIndex = i % maxAttempts;

            float roll = rng.NextFloat();

            if (assetSpawnFilters[spawnableAssetIndex].spawnProbability < roll) continue;

            int randomIndex = rng.NextInt(0, chunkVertices.Length);
            float3 spawnPoint = vertices[randomIndex].position;
            float3 spawnPointNormal = vertices[randomIndex].normal;

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

            if (assetSpawnFilters[spawnableAssetIndex].useMinSlope && slope < assetSpawnFilters[spawnableAssetIndex].minSlope - 0.001f) continue;
            if (assetSpawnFilters[spawnableAssetIndex].useMaxSlope && slope > assetSpawnFilters[spawnableAssetIndex].maxSlope + 0.001f) continue;
            if (assetSpawnFilters[spawnableAssetIndex].useMinHeight && height < assetSpawnFilters[spawnableAssetIndex].minHeight - 0.001f) continue;
            if (assetSpawnFilters[spawnableAssetIndex].useMaxHeight && height > assetSpawnFilters[spawnableAssetIndex].maxHeight + 0.001f) continue;
            if (assetSpawnFilters[spawnableAssetIndex].underwaterAsset && height > terrainDensityData.waterLevel - 3) continue;
            if (!assetSpawnFilters[spawnableAssetIndex].underwaterAsset && height < terrainDensityData.waterLevel) continue;
            // Debug.Log(randomIndex);
            ComputeMarchingCubes.Vertex vert;
            vert.position = spawnPoint;
            vert.normal = spawnPointNormal;
            flatSpawnPoints[(spawnableAssetIndex * maxAttempts) + assetAttemptIndex] = vert;
        }

        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            List<ComputeMarchingCubes.Vertex> assetPoints = new();

            int start = i * maxAttempts;
            int end = start + maxAttempts;

            for (int j = start; j < end; j++)
            {
                var vert = flatSpawnPoints[j];
                if (vert.position.Equals(float3.zero) || vert.normal.Equals(float3.zero)) continue;
                assetPoints.Add(vert);
            }

            spawnPoints[i] = assetPoints;
        }

        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            float spacingSquared = assetSpacing * assetSpacing;
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
                    // Collider[] colliders = Physics.OverlapSphere(spawnPoints[i][j].position, assetSpacing, assetLayer);
                    // if (colliders.Length > 0)
                    // {
                    //     tooClose = true;
                    //     break;
                    // }
                }

                if (!tooClose)
                {
                    tempAccepted.Add(spawnPoints[i][j]);
                    // Debug.Log(spawnPoints[i][j]);
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
        public AssetSpawnFilters(bool rotateToFaceNormal, float spawnProbability, bool useMinSlope, int minSlope, bool useMaxSlope, int maxSlope, bool useMinHeight, int minHeight, bool useMaxHeight, int maxHeight, bool underwaterAsset)
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
        float randomRotationDeg = rng.NextFloat(0f,360f);
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
        assetSpawnData.assets[chunkPos][i].spawnedAssets.Add(new Asset(assetToSpawn, assetToSpawn.GetComponent<MeshRenderer>(), assetToSpawn.GetComponent<MeshCollider>()));
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
    public SpawnableAsset(GameObject asset, float spawnProbability, bool useMinSlope, int minSlope, bool useMaxSlope, int maxSlope, bool useMinHeight, int minHeight, bool useMaxHeight, int maxHeight, bool underwaterAsset)
    {
        this.asset = asset;
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