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
    public ChunkGenNetwork.TerrainChunk owner;
    public int vertexBufferLength;
    public List<Asset> spawnedAssets;
    public TerrainDensityData terrainDensityData;
    public AssetSpawnData assetSpawnData;
    public List<List<ComputeMarchingCubes.Vertex>> spawnPoints;
    public List<List<ComputeMarchingCubes.Vertex>> acceptedSpawnPoints;
    public NativeArray<ComputeMarchingCubes.Vertex> chunkVertices;
    public NativeArray<float> heightsArray;
    public NativeList<float3> minDepthPoints;
    public Vector3Int chunkPos;
    public LayerMask assetLayer;
    public LayerMask interactLayer;
    public int assetSpacing = 8;
    public bool assetsSet = false;
    public bool assetsHiddenEarly = false;
    public bool emptyChunk = false;
    public bool minDepthPointsCalculated = false;
    public bool spawnPointsKilled;
    public JobHandle minDepthPointJobHandler;
    Unity.Mathematics.Random rng;
    void Start() {
        assetLayer = LayerMask.GetMask("Asset Layer");
        interactLayer = LayerMask.GetMask("Interact Layer");

        spawnPoints = new List<List<ComputeMarchingCubes.Vertex>>(assetSpawnData.spawnableAssets.Count);
        acceptedSpawnPoints = new List<List<ComputeMarchingCubes.Vertex>>(assetSpawnData.spawnableAssets.Count);
        spawnedAssets = new List<Asset>();
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            spawnPoints.Add(new List<ComputeMarchingCubes.Vertex>());
            acceptedSpawnPoints.Add(new List<ComputeMarchingCubes.Vertex>());
        }
    }
    /// <summary>
    /// Initiate asset spawning for a given chunk
    /// </summary>
    public void SpawnAssets()
    {
        if (chunkVertices == null)
            return ;
        vertexBufferLength = chunkVertices.Length;
        if (vertexBufferLength <= 0) emptyChunk = true;
        
        if (!assetSpawnData.assets.ContainsKey(chunkPos))
        {
            uint seed = Hash(chunkPos.x, chunkPos.y, chunkPos.z, ChunkGenNetwork.Instance.seed);
            rng = new(seed);
            InitializeData();
            if (!heightsArray.IsCreated)
                return ;
            CreateSpawnPoints(ref rng);
            if(spawnPointsKilled) 
                return ;
            AssetSpawnHandler(rng);
        }
    }
    public void DisposalReleaseHandler() 
    {
        if (chunkVertices.IsCreated) {
            chunkVertices.Dispose();
        }
        if (heightsArray.IsCreated) {
            heightsArray.Dispose();
        }
        if (minDepthPoints.IsCreated) {
            minDepthPoints.Dispose();
        }
    }
    private void OnDisable()
    {
        DisposalReleaseHandler();
    }
    private void OnApplicationQuit()
    {
        DisposalReleaseHandler();
    }
    /// <summary>
    /// Initizalize all the data structures
    /// </summary>
    private void InitializeData()
    {
        assetSpawnData.assets.Add(chunkPos, new List<ComputeMarchingCubes.Vertex>());
 
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            spawnPoints[i].Clear();
            acceptedSpawnPoints[i].Clear();
        }
    }
    public void CreateSpawnPoints(ref Unity.Mathematics.Random rng)
    {
        List<AssetSpawnFilters> assetSpawnFilters = new(assetSpawnData.spawnableAssets.Count);
        foreach (SpawnableAsset asset in  assetSpawnData.spawnableAssets)
        {
            assetSpawnFilters.Add(new AssetSpawnFilters(asset.rotateToFaceNormal, asset.spawnProbability, asset.useMinSlope,
                                                        asset.minSlope, asset.useMaxSlope, asset.maxSlope,
                                                        asset.useMinHeight, asset.minHeight, asset.useMaxHeight,
                                                        asset.maxHeight, asset.underwaterAsset, asset.minDepth,
                                                        asset.undergroundAsset, asset.minDensity));
        }
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            if (emptyChunk && !assetSpawnData.spawnableAssets[i].undergroundAsset) continue;
            AssetSpawnFilters currentAssetSpawnFilters = assetSpawnFilters[i];
            if (currentAssetSpawnFilters.underwaterAsset && chunkPos.y > terrainDensityData.waterLevel && terrainDensityData.water) continue;
    
            if (currentAssetSpawnFilters.undergroundAsset && !minDepthPointsCalculated)
            {
                int iterations = Mathf.CeilToInt((terrainDensityData.chunkSize + 1) / ChunkGenNetwork.Instance.resolution) * Mathf.CeilToInt((terrainDensityData.chunkSize + 1) / ChunkGenNetwork.Instance.resolution) * Mathf.CeilToInt((terrainDensityData.chunkSize + 1) / ChunkGenNetwork.Instance.resolution);
                if (minDepthPoints.IsCreated)
                    minDepthPoints.Dispose();
                minDepthPoints = new(iterations, Allocator.Persistent);

                MinDepthPointsJob minDepthJob = new MinDepthPointsJob
                {
                    depthResult = minDepthPoints.AsParallelWriter(),
                    depth = currentAssetSpawnFilters.minDensity,
                    heightsArray = heightsArray,
                    chunkSize = terrainDensityData.chunkSize,
                    chunkPos = new int3(chunkPos.x, chunkPos.y, chunkPos.z),
                    resolution = ChunkGenNetwork.Instance.resolution,
                };
                
                minDepthPointJobHandler = minDepthJob.Schedule();
                minDepthPointJobHandler.Complete();

                if (minDepthPoints.Length == 0)
                {
                    minDepthPoints.Dispose();
                    continue;
                }

                minDepthPointsCalculated = true;
            }
            for (int j = 0; j < assetSpawnData.spawnableAssets[i].maxPerChunk; j++)
            {
                float roll = rng.NextFloat();

                if (currentAssetSpawnFilters.spawnProbability < roll) continue;

                int randomIndex;
                float3 spawnPoint;
                float3 spawnPointNormal;

                if (currentAssetSpawnFilters.undergroundAsset)
                {
                    if (minDepthPoints.Length == 0 || minDepthPoints.IsEmpty) continue;
                    randomIndex = rng.NextInt(0, minDepthPoints.Length);
                    spawnPoint = minDepthPoints[randomIndex];
                    spawnPointNormal = new float3(rng.NextFloat(0f, 360f), rng.NextFloat(0f, 360f), rng.NextFloat(0f, 360f));
                }
                else
                {
                    if (emptyChunk) continue;
                    if (chunkVertices.IsCreated)
                    {
                        randomIndex = rng.NextInt(0, chunkVertices.Length);
                        spawnPoint = chunkVertices[randomIndex].position;
                        spawnPointNormal = chunkVertices[randomIndex].normal;
                    }
                    else
                    {
                        spawnPointsKilled = true;
                        return ;
                    }
                }

                if (!currentAssetSpawnFilters.rotateToFaceNormal)
                {
                    spawnPoint.y -= 0.75f;
                }

                float height = spawnPoint.y;
                float slope = math.round(math.degrees(math.acos(math.clamp(math.dot(math.normalize(spawnPointNormal), math.up()), -1f, 1f))) * 100f) / 100f;

                if (currentAssetSpawnFilters.useMinSlope && slope < currentAssetSpawnFilters.minSlope - 0.01f) continue;
                if (currentAssetSpawnFilters.useMaxSlope && slope > currentAssetSpawnFilters.maxSlope + 0.01f) continue;
                if (currentAssetSpawnFilters.useMinHeight && height < currentAssetSpawnFilters.minHeight - 0.01f) continue;
                if (currentAssetSpawnFilters.useMaxHeight && height > currentAssetSpawnFilters.maxHeight + 0.01f) continue;
                if (currentAssetSpawnFilters.underwaterAsset && height > terrainDensityData.waterLevel - currentAssetSpawnFilters.minDepth && terrainDensityData.water) continue;
                if (!currentAssetSpawnFilters.underwaterAsset && height < terrainDensityData.waterLevel && !currentAssetSpawnFilters.undergroundAsset) continue;
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
                }

                if (!tooClose)
                {
                    tempAccepted.Add(spawnPoints[i][j]);
                }
            }

            acceptedSpawnPoints[i].AddRange(tempAccepted);
            assetSpawnData.assets[chunkPos].AddRange(tempAccepted);
        }
        if (minDepthPoints.IsCreated)
            minDepthPoints.Dispose();
        minDepthPointsCalculated = false;
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
    /// Use the spawn points from the compute shader to instantiate their respective game objects
    /// </summary>
    private void AssetSpawnHandler(Unity.Mathematics.Random rng)
    {
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            ComputeMarchingCubes.Vertex[] points = acceptedSpawnPoints[i].ToArray();
            if (points == null || points.Length == 0) continue;
            for (int j = 0; j < points.Length; j++)
            {
                int indexI = i;
                int indexJ = j;
                uint seed = rng.NextUInt();
                ChunkGenNetwork.Instance.pendingAssetInstantiations.Enqueue(new ChunkGenNetwork.AssetInstantiation(owner, indexI, indexJ, seed, owner.chunkID));
            }
        }
        assetsSet = true;
    }
    public void AssetInstantiation(int i, int j, uint seed)
    {
        if (gameObject == null) return;
        Unity.Mathematics.Random rng = new(seed);
        Quaternion randomYRotation = Quaternion.Euler(0f, rng.NextFloat(0f, 360f), 0f);

        Vector3 n = acceptedSpawnPoints[i][j].normal;
        if (!math.all(math.isfinite(n)) || math.lengthsq(n) < 0.0001f)
            n = Vector3.up;
        Quaternion normal = Quaternion.FromToRotation(Vector3.up, n);
        SpawnableAsset spawnableAsset = assetSpawnData.spawnableAssets[i];
        bool rotateToFaceNormal = spawnableAsset.rotateToFaceNormal;
        
        GameObject assetToSpawn = Instantiate(spawnableAsset.asset, acceptedSpawnPoints[i][j].position, rotateToFaceNormal ? normal * randomYRotation : randomYRotation);
        assetToSpawn.transform.SetParent(owner.assetParent.transform);
        
        if (spawnableAsset.isValuable)
        {
            assetToSpawn.layer = LayerMask.NameToLayer("Interact Layer");
            ValuableProperties properties = assetToSpawn.AddComponent<ValuableProperties>();
            properties.value = rng.NextInt(spawnableAsset.minValue, spawnableAsset.maxValue);
            assetToSpawn.AddComponent<ScanObject>();
            assetToSpawn.transform.rotation = Quaternion.Euler(rng.NextFloat(0f, 360f), assetToSpawn.transform.rotation.y, rng.NextFloat(0f, 360f));
        }
        Asset asset = new Asset(assetToSpawn, assetToSpawn.GetComponent<MeshRenderer>(), assetToSpawn.GetComponent<MeshCollider>(), spawnableAsset.name);
        spawnedAssets.Add(asset);
        spawnableAsset.spawnedAssets.Add(asset);
    }
    /// <summary>
    /// Destroy all the assets
    /// </summary>
    public void ClearAssets()
    {
        foreach (Asset asset in spawnedAssets)
        {
            ChunkGenNetwork.Instance.assetPool.ReturnAsset(asset);
        }
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

                        if(heightsArray[(z * size * size) + (y * size) + x] < depth)
                        {
                            depthResult.AddNoResize(new int3(chunkPos.x + x, chunkPos.y + y, chunkPos.z + z));
                        }
                    }
                }
            }
        }
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
    public string name;
    public Asset(GameObject obj, MeshRenderer meshRenderer, MeshCollider meshCollider, string name)
    {
        this.obj = obj;
        this.meshRenderer = meshRenderer;
        this.meshCollider = meshCollider;
        this.name = name;
    }
}