using System;
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

public class AssetSpawner : MonoBehaviour
{
    public List<SpawnableAsset> assets = new List<SpawnableAsset>();
    private List<List<GameObject>> spawnedAssets = new List<List<GameObject>>();
    public TerrainDensityData1 terrainDensityData;
    public AssetSpawnData assetSpawnData;
    public List<List<Vector3>> spawnPoints;
    public List<List<Vector3>> spawnPointsNormals;
    // public NativeArray<float3> spawnPoints;
    // public NativeArray<float3> spawnPointsNormals;
    private MeshFilter mf;
    private Mesh mesh; 
    private Vector3[] localVertices;
    private Vector3[] localNormals;
    private Vector3[] worldVertices;
    private Vector3[] worldNormals;
    public Vector3Int chunkPos;
    public LayerMask assetLayer;
    public int assetSpacing = 12;

    public void SpawnAssets()
    {
        assetLayer = LayerMask.GetMask("Asset Layer");
        terrainDensityData = Resources.Load<TerrainDensityData1>("TerrainDensityData1");
        assetSpawnData = Resources.Load<AssetSpawnData>("AssetSpawnData");
        if(!assetSpawnData.assets.ContainsKey(chunkPos)) {
            GetTerrainVerticesWorldPosition();
            InitializeData();
            CreateSpawnPoints();
            SetSpawnPoints();
            AssetSpawnHandler();
        }
    }

    private void InitializeData() {
        spawnPoints = new List<List<Vector3>>(assetSpawnData.spawnableAssets.Count);
        spawnPointsNormals = new List<List<Vector3>>(assetSpawnData.spawnableAssets.Count);
        spawnedAssets = new List<List<GameObject>>(assetSpawnData.spawnableAssets.Count);
        assetSpawnData.assets.Add(chunkPos, assetSpawnData.spawnableAssets);
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++) {
            spawnPoints.Add(new List<Vector3>());
            spawnPointsNormals.Add(new List<Vector3>());
            spawnedAssets.Add(new List<GameObject>());
        }

    }

    private void GetTerrainVerticesWorldPosition()
    {
        mf = GetComponent<MeshFilter>();
        mesh = mf.mesh;
        localVertices = mesh.vertices;
        localNormals = mesh.normals;
        worldVertices = new Vector3[localVertices.Length];
        worldNormals = new Vector3[localNormals.Length];
        for (int i = 0; i < localVertices.Length; i++)
        {
            worldVertices[i] = mf.transform.TransformPoint(localVertices[i]);
            worldNormals[i] = mf.transform.TransformDirection(localNormals[i]);
        }
    }

    private void CreateSpawnPoints() {
        for(int i = 0; i < assetSpawnData.spawnableAssets.Count; i++) {
            int breakCounter = 0;
            int spawnThreshold = assetSpawnData.spawnableAssets[i].assetsToSpawn;
            int chunkSeed = terrainDensityData.noiseSeed + terrainDensityData.width * 73856093 + terrainDensityData.width * 19349663;
            System.Random rng = new System.Random(chunkSeed);

            while(spawnPoints[i].Count < spawnThreshold) {
                if(breakCounter >= 150) break;

                float highestVertexHeight = 0;
                float lowestVertexHeight = 0;
                foreach(Vector3 vertex in worldVertices) {
                    if(vertex.y > highestVertexHeight) highestVertexHeight = vertex.y;
                    if(vertex.y < lowestVertexHeight) lowestVertexHeight = vertex.y;
                }

                if(worldVertices.Length == 0) break;
                else if(assetSpawnData.spawnableAssets[i].useMinHeight && highestVertexHeight < assetSpawnData.spawnableAssets[i].minHeight) break;
                else if(assetSpawnData.spawnableAssets[i].useMaxHeight && lowestVertexHeight > assetSpawnData.spawnableAssets[i].maxHeight) break;
                else if(assetSpawnData.spawnableAssets[i].underwaterAsset && lowestVertexHeight > terrainDensityData.waterLevel-3f) break;
                else if(!assetSpawnData.spawnableAssets[i].underwaterAsset && highestVertexHeight < terrainDensityData.waterLevel) break;

                // int random = UnityEngine.Random.Range(0, worldVertices.Length);
                int random = rng.Next(0, worldVertices.Length);
                Vector3 spawnPoint = worldVertices[random];
                Vector3 spawnPointNormal = worldNormals[random];

                if(!assetSpawnData.spawnableAssets[i].rotateToFaceNormal) spawnPoint.y -= 0.75f;
                else spawnPoint.y -= 0.1f;

                float height = spawnPoint.y;
                float slope = Vector3.Angle(worldNormals[random], Vector3.up);

                float spacingSquared = assetSpacing * assetSpacing;
                bool invalidSpawnPoint = false;
                for(int j = 0; j < assetSpawnData.spawnableAssets.Count && !invalidSpawnPoint; j++) {
                    foreach(Vector3 point in spawnPoints[j]) {
                        if((spawnPoint - point).sqrMagnitude <= spacingSquared) {
                            invalidSpawnPoint = true;
                            break;
                        }
                    }
                }
                if(invalidSpawnPoint) {
                    breakCounter++;
                    continue;
                }

                Collider[] colliders = Physics.OverlapSphere(spawnPoint, assetSpacing, assetLayer);
                if(colliders.Length > 0) {
                    breakCounter++;
                    continue;
                }

                if((assetSpawnData.spawnableAssets[i].useMinSlope ? slope > assetSpawnData.spawnableAssets[i].minSlope : true) && 
                   (assetSpawnData.spawnableAssets[i].useMaxSlope ? slope < assetSpawnData.spawnableAssets[i].maxSlope : true) && 
                   (assetSpawnData.spawnableAssets[i].useMinHeight ? height > assetSpawnData.spawnableAssets[i].minHeight : true) && 
                   (assetSpawnData.spawnableAssets[i].useMaxHeight ? height < assetSpawnData.spawnableAssets[i].maxHeight : true) && 
                   (assetSpawnData.spawnableAssets[i].underwaterAsset ? height < terrainDensityData.waterLevel-3f : height > terrainDensityData.waterLevel)) {
                    spawnPoints[i].Add(spawnPoint);
                    spawnPointsNormals[i].Add(spawnPointNormal);
                }
                breakCounter++;
            }
        }
    }

    // private void CreateSpawnPoints() {
    //     spawnPoints = new NativeArray<float3>(assetsToSpawn, Allocator.TempJob);
    //     spawnPoints = new NativeArray<float3>(assetsToSpawn, Allocator.TempJob);

    //     SpawnJob spawnJob = new SpawnJob
    //     {
    //         seed = terrainDensityData.noiseSeed,
    //         spawnPoints = spawnPoints,
    //         spawnPointsNormals = spawnPointsNormals
    //     };

    //     JobHandle handle = spawnJob.Schedule(assetsToSpawn, 64);
    //     handle.Complete();



    //     spawnPoints.Dispose();
    //     spawnPointsNormals.Dispose();
    // }

    // [BurstCompile]
    // struct SpawnJob : IJobParallelFor
    // {
    //     public int seed;
    //     public NativeArray<float3> spawnPoints;
    //     public NativeArray<float3> spawnPointsNormals;

    //     public void Execute(int index)
    //     {
    //         var rng = new Unity.Mathematics.Random((uint)(seed + index));
    //         float3 pos = new float3(
    //             rng.NextFloat(0f, areaSize.x),
    //             rng.NextFloat(0f, areaSize.y),
    //             rng.NextFloat(0f, areaSize.z)
    //         );
    //         spawnPoints[index] = pos;
    //     }
    // }

    private void SetSpawnPoints() {
        for(int i = 0; i < assetSpawnData.spawnableAssets.Count; i++) {
            assetSpawnData.assets[chunkPos][i].spawnPoints = spawnPoints[i];
            assetSpawnData.assets[chunkPos][i].spawnPointsNormals = spawnPointsNormals[i];
            assetSpawnData.assets[chunkPos][i].spawnedAssets = spawnedAssets[i];
        }
    }

    private void AssetSpawnHandler() {
        for(int i = 0; i < assetSpawnData.spawnableAssets.Count; i++) {
            for(int j = 0; j < assetSpawnData.assets[chunkPos][i].spawnPoints.Count; j++) {
                float randomRotationDeg = UnityEngine.Random.Range(0f, 360f);
                Quaternion randomYRotation = Quaternion.Euler(0f, randomRotationDeg, 0f);
                GameObject assetToSpawn;
                if(assetSpawnData.assets[chunkPos][i].rotateToFaceNormal) {
                    Quaternion normal = Quaternion.FromToRotation(Vector3.up, assetSpawnData.assets[chunkPos][i].spawnPointsNormals[j]);
                    assetToSpawn = Instantiate(assetSpawnData.assets[chunkPos][i].asset, assetSpawnData.assets[chunkPos][i].spawnPoints[j], normal*randomYRotation);
                }
                else{
                    assetToSpawn = Instantiate(assetSpawnData.assets[chunkPos][i].asset, assetSpawnData.assets[chunkPos][i].spawnPoints[j], randomYRotation);
                }
                assetToSpawn.transform.SetParent(gameObject.transform);
                spawnedAssets[i].Add(assetToSpawn);
            }
        }
    }

    public void ClearAssets() {
        for(int i = 0; i < assetSpawnData.assets[chunkPos].Count; i++) {
            if(assetSpawnData.assets[chunkPos][i].spawnedAssets != null) {
                foreach(GameObject asset in assetSpawnData.assets[chunkPos][i].spawnedAssets) {
                    Destroy(asset);
                }
            }
        }
    }

    public void ClearData() {
        spawnPoints.Clear();
        spawnPointsNormals.Clear();
        assetSpawnData.ResetSpawnPoints();
    }

    public void SetAssetsActive(bool active) {
        for(int i = 0; i < assetSpawnData.assets[chunkPos].Count; i++) {
            foreach(GameObject asset in assetSpawnData.assets[chunkPos][i].spawnedAssets) {
                asset.SetActive(active);
            }
        }
    }
}

[Serializable]
public class SpawnableAsset {
    public GameObject asset;
    public List<Vector3> spawnPoints;
    public List<Vector3> spawnPointsNormals;
    public List<GameObject> spawnedAssets = new List<GameObject>();
    public bool rotateToFaceNormal;
    public int assetsToSpawn;
    public bool useMinSlope;
    public int minSlope;
    public bool useMaxSlope;
    public int maxSlope;
    public bool useMinHeight;
    public int minHeight;
    public bool useMaxHeight;
    public int maxHeight;
    public bool underwaterAsset;
    public SpawnableAsset(GameObject asset, int assetsToSpawn, bool useMinSlope, int minSlope, bool useMaxSlope, int maxSlope, bool useMinHeight, int minHeight, bool useMaxHeight, int maxHeight, bool underwaterAsset) {
        this.asset = asset;
        this.assetsToSpawn = assetsToSpawn;
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
