using System;
using System.Collections.Generic;
using System.Drawing;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class AssetSpawner : MonoBehaviour
{
    public int vertexBufferLength;
    public List<List<Asset>> spawnedAssets;
    public TerrainDensityData terrainDensityData;
    public AssetSpawnData assetSpawnData;
    public List<List<ComputeMarchingCubes.Vertex>> spawnPoints;
    public List<List<ComputeMarchingCubes.Vertex>> acceptedSpawnPoints;
    public NativeArray<ComputeMarchingCubes.Vertex> chunkVertices;
    // public float[] heightsArray;
    public NativeArray<float> heightsArray;
    public NativeList<float3> minDepthPoints;
    public Vector3Int chunkPos;
    public LayerMask assetLayer;
    public LayerMask interactLayer;
    public int assetSpacing = 8;
    public bool assetsSet = false;
    public bool emptyChunk = false;
    public bool minDepthPointsCalculated = false;
    Unity.Mathematics.Random rng;
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
            uint seed = Hash(chunkPos.x, chunkPos.y, chunkPos.z, terrainDensityData.noiseGenerators[0].noiseSeed);
            rng = new(seed);
            InitializeData();
            CreateSpawnPoints(ref rng);
            SetSpawnPoints();
            AssetSpawnHandler(rng);
        }
    }
    /// <summary>
    /// Release associated buffers
    /// </summary>
    private void OnDisable()
    {
        if (chunkVertices.IsCreated) {
            chunkVertices.Dispose();
        }
        if (heightsArray.IsCreated)
        {
            heightsArray.Dispose();
        }
    }
    /// <summary>
    /// Release associated buffers
    /// </summary>
    private void OnApplicationQuit()
    {
        if (chunkVertices.IsCreated) {
            chunkVertices.Dispose();
        }
        if (heightsArray.IsCreated)
        {
            heightsArray.Dispose();
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
        assetSpawnData.assets.Add(chunkPos, new List<ComputeMarchingCubes.Vertex>());
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            spawnPoints.Add(new List<ComputeMarchingCubes.Vertex>());
            acceptedSpawnPoints.Add(new List<ComputeMarchingCubes.Vertex>());
            spawnedAssets.Add(new List<Asset>());
        }
    }
    public void CreateSpawnPoints(ref Unity.Mathematics.Random rng)
    {
        List<AssetSpawnFilters> assetSpawnFilters = new(assetSpawnData.spawnableAssets.Count);
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            assetSpawnFilters.Add(new AssetSpawnFilters(assetSpawnData.spawnableAssets[i].rotateToFaceNormal, assetSpawnData.spawnableAssets[i].spawnProbability, assetSpawnData.spawnableAssets[i].useMinSlope,
                                                         assetSpawnData.spawnableAssets[i].minSlope, assetSpawnData.spawnableAssets[i].useMaxSlope, assetSpawnData.spawnableAssets[i].maxSlope,
                                                         assetSpawnData.spawnableAssets[i].useMinHeight, assetSpawnData.spawnableAssets[i].minHeight, assetSpawnData.spawnableAssets[i].useMaxHeight,
                                                         assetSpawnData.spawnableAssets[i].maxHeight, assetSpawnData.spawnableAssets[i].underwaterAsset, assetSpawnData.spawnableAssets[i].minDepth,
                                                         assetSpawnData.spawnableAssets[i].undergroundAsset, assetSpawnData.spawnableAssets[i].minDensity));
        }
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            if (emptyChunk && !assetSpawnData.spawnableAssets[i].undergroundAsset) continue;
            if (assetSpawnFilters[i].underwaterAsset && chunkPos.y > terrainDensityData.waterLevel && terrainDensityData.water) continue;
    
            if (assetSpawnFilters[i].undergroundAsset && !minDepthPointsCalculated)
            {
                int iterations = Mathf.CeilToInt((terrainDensityData.width + 1) / ChunkGenNetwork.Instance.resolution) * Mathf.CeilToInt((terrainDensityData.width + 1) / ChunkGenNetwork.Instance.resolution) * Mathf.CeilToInt((terrainDensityData.width + 1) / ChunkGenNetwork.Instance.resolution);
                minDepthPoints = new(iterations, Allocator.Persistent);
                
                NativeList<float3> depthResult = new(iterations, Allocator.Persistent);
                NativeArray<float> heightsNativeArray = new(heightsArray, Allocator.Persistent);

                MinDepthPointsJob minDepthJob = new MinDepthPointsJob
                {
                    depthResult = depthResult.AsParallelWriter(),
                    depth = assetSpawnFilters[i].minDepth,
                    heightsArray = heightsNativeArray,
                    chunkSize = terrainDensityData.width,
                    chunkPos = new int3(chunkPos.x, chunkPos.y, chunkPos.z),
                    resolution = ChunkGenNetwork.Instance.resolution,
                };

                minDepthJob.Run();

                // minDepthPoints = GetMinDepthChunkPoints(assetSpawnFilters[i].minDensity, heightsArray);
                // if (minDepthPoints == null || minDepthPoints.Count == 0) continue;
                if (depthResult.Length == 0)
                {
                    depthResult.Dispose();
                    heightsNativeArray.Dispose();
                    continue;
                }

                minDepthPoints.Clear();
                minDepthPoints.AddRange(depthResult.AsArray());

                minDepthPointsCalculated = true;

                depthResult.Dispose();
                heightsNativeArray.Dispose();
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
                    if (minDepthPoints.Length == 0 || minDepthPoints.IsEmpty) continue;
                    randomIndex = rng.NextInt(0, minDepthPoints.Length);
                    spawnPoint = minDepthPoints[randomIndex];
                    spawnPointNormal = new float3(rng.NextFloat(0f, 360f), rng.NextFloat(0f, 360f), rng.NextFloat(0f, 360f));
                }
                else
                {
                    if (emptyChunk) continue;
                    randomIndex = rng.NextInt(0, chunkVertices.Length);
                    spawnPoint = chunkVertices[randomIndex].position;
                    spawnPointNormal = chunkVertices[randomIndex].normal;
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
                if (assetSpawnFilters[i].underwaterAsset && height > terrainDensityData.waterLevel - assetSpawnFilters[i].minDepth && terrainDensityData.water) continue;
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
                    /* OLD ASSET PROXIMITY SPAWN CHECK REPLACE WITH POISSON DISK SAMPLING */
                    // ChunkGenNetwork.TerrainChunk[] chunkAndNeighbors = ChunkGenNetwork.Instance.GetChunkAndNeighbors(new Vector3Int(Mathf.CeilToInt(chunkPos.x / terrainDensityData.width), Mathf.CeilToInt(chunkPos.y / terrainDensityData.width), Mathf.CeilToInt(chunkPos.z / terrainDensityData.width)));
                    // foreach (ChunkGenNetwork.TerrainChunk terrainChunk in chunkAndNeighbors)
                    // {
                    //     if (terrainChunk == null) continue;
                    //     if (!assetSpawnData.assets.TryGetValue(terrainChunk.chunkPos, out List<ComputeMarchingCubes.Vertex> neighborSpawnPoints)) continue;
                    //     for (int k = 0; k < neighborSpawnPoints.Count; k++)
                    //     {
                    //         if (math.lengthsq(assetSpawnData.assets[terrainChunk.chunkPos][k].position - spawnPoints[i][j].position) <= spacingSquared)
                    //         {
                    //             tooClose = true;
                    //             break;
                    //         }
                    //     }
                    //     if (tooClose) break;
                    // }
                    // Collider[] colliders = Physics.OverlapSphere(spawnPoints[i][j].position, assetSpacing, assetLayer | interactLayer);
                    // if (colliders.Length > 0)
                    // {
                    //     tooClose = true;
                    //     break;
                    // }
                }

                if (!tooClose)
                {
                    tempAccepted.Add(spawnPoints[i][j]);
                }
            }

            acceptedSpawnPoints[i].AddRange(tempAccepted);
        }
        minDepthPoints.Dispose();
    }
    /// <summary>
    /// Simple hashing function
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="baseSeed"></param>
    /// <returns></returns>
    public static uint Hash(int x, int y, int z, int baseSeed)
    {
        uint hash = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(z * 83492791) ^ (uint)baseSeed;
        return hash;
    }
    /// <summary>
    /// Struct for parameterizing asset spawns
    /// </summary>
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
        public float minDepth;
        public bool undergroundAsset;
        public float minDensity;
        public AssetSpawnFilters(bool rotateToFaceNormal, float spawnProbability, bool useMinSlope, int minSlope, bool useMaxSlope, int maxSlope, bool useMinHeight, int minHeight, bool useMaxHeight, int maxHeight, bool underwaterAsset, float minDepth, bool undergroundAsset, float minDensity)
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
            this.minDepth = minDepth;
            this.undergroundAsset = undergroundAsset;
            this.minDensity = minDensity;
        }
    }
    /// <summary>
    /// Add this chunks spawn points and game objects to a centralized scriptable object
    /// </summary>
    private void SetSpawnPoints()
    {
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            assetSpawnData.assets[chunkPos].AddRange(acceptedSpawnPoints[i]);
        }
    }
    /// <summary>
    /// Use the spawn points from the compute shader to instantiate their respective game objects
    /// </summary>
    private void AssetSpawnHandler(Unity.Mathematics.Random rng)
    {
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            ComputeMarchingCubes.Vertex[] points = acceptedSpawnPoints[i].ToArray();
            if (points == null || points.Length == 0) continue;
            for (int j = 0; j < acceptedSpawnPoints[i].Count; j++)
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
        if (assetSpawnData.spawnableAssets[i].rotateToFaceNormal)
        {
            Quaternion normal = Quaternion.FromToRotation(Vector3.up, acceptedSpawnPoints[i][j].normal);
            assetToSpawn = Instantiate(assetSpawnData.spawnableAssets[i].asset, acceptedSpawnPoints[i][j].position, normal * randomYRotation);
            assetToSpawn.transform.SetParent(gameObject.transform);
            spawnedAssets[i].Add(new Asset(assetToSpawn, assetToSpawn.GetComponent<MeshRenderer>(), assetToSpawn.GetComponent<MeshCollider>()));
        }
        else
        {
            assetToSpawn = Instantiate(assetSpawnData.spawnableAssets[i].asset, acceptedSpawnPoints[i][j].position, randomYRotation);
            assetToSpawn.transform.SetParent(gameObject.transform);
            spawnedAssets[i].Add(new Asset(assetToSpawn, assetToSpawn.GetComponent<MeshRenderer>(), assetToSpawn.GetComponent<MeshCollider>()));
        }
        if (assetSpawnData.spawnableAssets[i].isValuable)
        {
            assetToSpawn.layer = LayerMask.NameToLayer("Interact Layer");
            ValuableProperties properties = assetToSpawn.AddComponent<ValuableProperties>();
            properties.value = rng.NextInt(assetSpawnData.spawnableAssets[i].minValue, assetSpawnData.spawnableAssets[i].maxValue);
            assetToSpawn.AddComponent<ScanObject>();
            assetToSpawn.transform.rotation = Quaternion.Euler(rng.NextFloat(0f, 360f), assetToSpawn.transform.rotation.y, rng.NextFloat(0f, 360f));
        }
        assetSpawnData.spawnableAssets[i].spawnedAssets.Add(new Asset(assetToSpawn, assetToSpawn.GetComponent<MeshRenderer>(), assetToSpawn.GetComponent<MeshCollider>()));
    }
    public List<float3> GetMinDepthChunkPoints(float minDepth, NativeArray<float> heightsArray)
    {
        List<float3> depthResult = new();
        int size = terrainDensityData.width + 1;

        for (int z = 0; z < size; z++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if(heightsArray[(z * size * size) + (y * size) + x] > minDepth)
                    {
                        depthResult.Add(new float3(chunkPos.x + x, chunkPos.y + y, chunkPos.z + z));
                    }
                }
            }
        }

        return depthResult;
    }
    [BurstCompile]
    private struct MinDepthPointsJob: IJob
    {
        public float depth;
        public NativeList<float3>.ParallelWriter depthResult;
        public NativeArray<float> heightsArray;
        public int3 chunkPos;
        public int chunkSize;
        public int resolution;
        public void Execute()
        {
            int size = chunkSize + 1;
            for (int z = 0; z < size; z++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {

                        if(heightsArray[(z * size * size) + (y * size) + x] > depth)
                        {
                            depthResult.AddNoResize(new int3(chunkPos.x + x, chunkPos.y + y, chunkPos.z + z));
                        }
                    }
                }
            }
        }
    }
    /// <summary>
    /// Destroy all the assets
    /// </summary>
    public void ClearAssets()
    {
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            if (acceptedSpawnPoints[i].ToArray() != null)
            {
                foreach (Asset asset in assetSpawnData.spawnableAssets[i].spawnedAssets)
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
    public string name;
    public Texture icon;
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
    public float minDepth;
    public bool undergroundAsset;
    public float minDensity;
    public bool isValuable;
    public int minValue;
    public int maxValue;
    public SpawnableAsset()
    {
        
    }
    public SpawnableAsset(GameObject asset, int maxPerChunk, bool rotateToFaceNormal, float spawnProbability, bool useMinSlope, int minSlope, bool useMaxSlope, int maxSlope, bool useMinHeight, int minHeight, bool useMaxHeight, int maxHeight, bool underwaterAsset, float minDepth, bool undergroundAsset, float minDensity, bool isValuable, int minValue, int maxValue)
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
        this.undergroundAsset = undergroundAsset;
        this.minDensity = minDensity;
        this.isValuable = isValuable;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }
    public SpawnableAsset Clone()
    {
        return new SpawnableAsset {
            asset = asset,
            name = name,
            icon = icon,
            spawnedAssets = new(),
            maxPerChunk = maxPerChunk,
            rotateToFaceNormal = rotateToFaceNormal,
            spawnProbability = spawnProbability,
            useMinSlope = useMinSlope,
            minSlope = minSlope,
            useMaxSlope = useMaxSlope,
            maxSlope = maxSlope,
            useMinHeight = useMinHeight,
            minHeight = minHeight,
            useMaxHeight = useMaxHeight,
            maxHeight = maxHeight,
            underwaterAsset = underwaterAsset,
            minDepth = minDepth,
            undergroundAsset = undergroundAsset,
            minDensity = minDensity,
            isValuable = isValuable,
            minValue = minValue,
            maxValue = maxValue
        };
    }
}
/// <summary>
/// Simple asset data class
/// </summary>
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