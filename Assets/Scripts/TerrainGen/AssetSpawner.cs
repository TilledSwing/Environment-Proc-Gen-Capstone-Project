using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public ComputeShader spawnPointsComputeShader;
    public int vertexBufferLength;
    public List<List<Asset>> spawnedAssets;
    public TerrainDensityData1 terrainDensityData;
    public AssetSpawnData assetSpawnData;
    public List<ComputeMarchingCubes.Vertex[]> spawnPoints;
    public List<List<ComputeMarchingCubes.Vertex>> acceptedSpawnPoints;
    private MeshFilter mf;
    private Mesh mesh;
    public ComputeMarchingCubes.Vertex[] worldVertices;
    ChunkGenNetwork chunkGenNetwork;
    public Vector3Int chunkPos;
    public LayerMask assetLayer;
    public int assetSpacing = 8;
    public int maxAttempts = 8;
    public bool assetsSet = false;
    /// <summary>
    /// Initiate asset spawning for a given chunk
    /// </summary>
    public void SpawnAssets()
    {
        assetLayer = LayerMask.GetMask("Asset Layer");
        vertexBufferLength = worldVertices.Length;
        if (!assetSpawnData.assets.ContainsKey(chunkPos) && vertexBufferLength > 0)
        {
            InitializeData();
            CreateSpawnPoints();
            SetSpawnPoints();
            AssetSpawnHandler();
        }
    }
    /// <summary>
    /// Initizalize all the data structures
    /// </summary>
    private void InitializeData() {
        spawnPoints = new List<ComputeMarchingCubes.Vertex[]>(assetSpawnData.spawnableAssets.Count);
        acceptedSpawnPoints = new List<List<ComputeMarchingCubes.Vertex>>(assetSpawnData.spawnableAssets.Count);
        spawnedAssets = new List<List<Asset>>(assetSpawnData.spawnableAssets.Count);
        assetSpawnData.assets.Add(chunkPos, assetSpawnData.spawnableAssets);
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++) {
            spawnPoints.Add(new ComputeMarchingCubes.Vertex[maxAttempts]);
            acceptedSpawnPoints.Add(new List<ComputeMarchingCubes.Vertex>());
            spawnedAssets.Add(new List<Asset>());
        }
    }
    /// <summary>
    /// Create all the spawn points for the given chunk using a compute shader
    /// </summary>
    private void CreateSpawnPoints() {
        int spawnPointsKernel = spawnPointsComputeShader.FindKernel("SpawnPoints");
        ComputeBuffer vertexBuffer = new ComputeBuffer(vertexBufferLength, sizeof(float) * 6);
        vertexBuffer.SetData(worldVertices);
        spawnPointsComputeShader.SetBuffer(spawnPointsKernel, "VertexBuffer", vertexBuffer);
        for(int i = 0; i < assetSpawnData.spawnableAssets.Count; i++) {
            int chunkSeed = terrainDensityData.noiseSeed + chunkPos.x * 73856093 + chunkPos.y * 19349663 + chunkPos.z * 41793205 + i * 83492791;
            spawnPointsComputeShader.SetInt("chunkSeed", chunkSeed);
            spawnPointsComputeShader.SetInt("maxAttempts", maxAttempts);
            spawnPointsComputeShader.SetBool("rotateToFaceNormal", assetSpawnData.spawnableAssets[i].rotateToFaceNormal);
            spawnPointsComputeShader.SetFloat("spawnProbability", assetSpawnData.spawnableAssets[i].spawnProbability);
            spawnPointsComputeShader.SetBool("useMinSlope", assetSpawnData.spawnableAssets[i].useMinSlope);
            spawnPointsComputeShader.SetInt("minSlope", assetSpawnData.spawnableAssets[i].minSlope);
            spawnPointsComputeShader.SetBool("useMaxSlope", assetSpawnData.spawnableAssets[i].useMaxSlope);
            spawnPointsComputeShader.SetInt("maxSlope", assetSpawnData.spawnableAssets[i].maxSlope);
            spawnPointsComputeShader.SetBool("useMinHeight", assetSpawnData.spawnableAssets[i].useMinHeight);
            spawnPointsComputeShader.SetInt("minHeight", assetSpawnData.spawnableAssets[i].minHeight);
            spawnPointsComputeShader.SetBool("useMaxHeight", assetSpawnData.spawnableAssets[i].useMaxHeight);
            spawnPointsComputeShader.SetInt("maxHeight", assetSpawnData.spawnableAssets[i].maxHeight);
            spawnPointsComputeShader.SetBool("underwaterAsset", assetSpawnData.spawnableAssets[i].underwaterAsset);
            spawnPointsComputeShader.SetInt("waterLevel", terrainDensityData.waterLevel);
            spawnPointsComputeShader.SetInt("VertexBufferLength", vertexBufferLength);
            ComputeBuffer spawnPointsBuffer = new ComputeBuffer(maxAttempts, sizeof(float) * 6, ComputeBufferType.Append);
            spawnPointsComputeShader.SetBuffer(spawnPointsKernel, "SpawnPointBuffer", spawnPointsBuffer);
            spawnPointsBuffer.SetCounterValue(0);

            spawnPointsComputeShader.Dispatch(spawnPointsKernel, Mathf.CeilToInt(maxAttempts / 8.0f), 1, 1);

            ComputeBuffer spawnPointsCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
            ComputeBuffer.CopyCount(spawnPointsBuffer, spawnPointsCountBuffer, 0);

            int[] spawnPointsCountArray = { 0 };
            spawnPointsCountBuffer.GetData(spawnPointsCountArray);
            spawnPointsCountBuffer.Release();
            int spawnPointsCount = spawnPointsCountArray[0];

            if(spawnPointsCount > 0) {
                spawnPoints[i] = new ComputeMarchingCubes.Vertex[spawnPointsCount];
                spawnPointsBuffer.GetData(spawnPoints[i], 0, 0, spawnPointsCount);

                float spacingSquared = assetSpacing * assetSpacing;
                List<ComputeMarchingCubes.Vertex> tempAccepted = new();
                tempAccepted.Add(spawnPoints[i][0]);

                for (int j = 1; j < spawnPoints[i].Length; j++) {
                    bool tooClose = false;
                    foreach (var accepted in tempAccepted) {
                        if ((spawnPoints[i][j].position - accepted.position).sqrMagnitude <= spacingSquared) {
                            tooClose = true;
                            break;
                        }
                        Collider[] colliders = Physics.OverlapSphere(spawnPoints[i][j].position, assetSpacing, assetLayer);
                        if(colliders.Length > 0) {
                            tooClose = true;
                        }
                    }

                    if (!tooClose) {
                        tempAccepted.Add(spawnPoints[i][j]);
                    }
                }

                acceptedSpawnPoints[i].AddRange(tempAccepted);
            }
            else {
                spawnPoints[i] = Array.Empty<ComputeMarchingCubes.Vertex>();
            }
            spawnPointsBuffer.Release();
        }
        vertexBuffer.Release();
    }
    /// <summary>
    /// Add this chunks spawn points and game objects to a centralized scriptable object
    /// </summary>
    private void SetSpawnPoints() {
        for(int i = 0; i < assetSpawnData.spawnableAssets.Count; i++) {
            assetSpawnData.assets[chunkPos][i].spawnPoints = acceptedSpawnPoints[i].ToArray();
            // assetSpawnData.assets[chunkPos][i].spawnedAssets = spawnedAssets[i];
        }
    }
    /// <summary>
    /// Use the spawn points from the compute shader to instantiate their respective game objects
    /// </summary>
    private void AssetSpawnHandler()
    {
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            ComputeMarchingCubes.Vertex[] points = assetSpawnData.assets[chunkPos][i].spawnPoints;
            if (points == null || points.Length == 0)
            {
                continue;
            }
            for (int j = 0; j < assetSpawnData.assets[chunkPos][i].spawnPoints.Length; j++)
            {
                float randomRotationDeg = UnityEngine.Random.Range(0f, 360f);
                Quaternion randomYRotation = Quaternion.Euler(0f, randomRotationDeg, 0f);
                GameObject assetToSpawn;
                if (assetSpawnData.assets[chunkPos][i].rotateToFaceNormal)
                {
                    Quaternion normal = Quaternion.FromToRotation(Vector3.up, assetSpawnData.assets[chunkPos][i].spawnPoints[j].normal);
                    assetToSpawn = Instantiate(assetSpawnData.assets[chunkPos][i].asset, assetSpawnData.assets[chunkPos][i].spawnPoints[j].position, normal * randomYRotation);
                    assetToSpawn.transform.SetParent(gameObject.transform);
                    spawnedAssets[i].Add(new Asset(assetToSpawn, assetToSpawn.GetComponent<MeshRenderer>(), assetToSpawn.GetComponent<MeshCollider>()));
                }
                else
                {
                    assetToSpawn = Instantiate(assetSpawnData.assets[chunkPos][i].asset, assetSpawnData.assets[chunkPos][i].spawnPoints[j].position, randomYRotation);
                    assetToSpawn.transform.SetParent(gameObject.transform);
                    spawnedAssets[i].Add(new Asset(assetToSpawn, assetToSpawn.GetComponent<MeshRenderer>(), assetToSpawn.GetComponent<MeshCollider>()));
                }
            }
            assetSpawnData.assets[chunkPos][i].spawnedAssets = spawnedAssets[i];
        }
        assetsSet = true;
    }
    /// <summary>
    /// Destroy all the assets
    /// </summary>
    public void ClearAssets() {
        for(int i = 0; i < assetSpawnData.assets[chunkPos].Count; i++) {
            if(assetSpawnData.assets[chunkPos][i].spawnedAssets != null) {
                foreach(Asset asset in assetSpawnData.assets[chunkPos][i].spawnedAssets) {
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
    public List<Asset> spawnedAssets = new List<Asset>();
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
        // Debug.Log(meshRenderer);
    }
}
