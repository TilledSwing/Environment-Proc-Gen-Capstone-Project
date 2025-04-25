using System;
using System.Collections.Generic;
using FishNet.Demo.Benchmarks.NetworkTransforms;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.Scripting;

public class AssetSpawner : MonoBehaviour
{
    public List<SpawnableAsset> assets = new List<SpawnableAsset>();
    // public List<GameObject> assets = new List<GameObject>();
    private List<GameObject> spawnedAssets = new List<GameObject>();
    private List<List<Vector3>> spawnPoints;
    private List<List<Vector3>> spawnPointsNormals;
    public TerrainDensityData terrainDensityData;
    // public int assetsToSpawn = 25;
    private MeshFilter mf;
    private Mesh mesh; 
    private Vector3[] localVertices;
    private Vector3[] localNormals;
    private Vector3[] worldVertices;
    private Vector3[] worldNormals;

    public void SpawnAssets()
    {
        terrainDensityData = Resources.Load<TerrainDensityData>("TerrainDensityData");
        GetTerrainVerticesWorldPosition();
        CreateSpawnPoints();
        AssetSpawnHandler();
    }

    private void GetTerrainVerticesWorldPosition()
    {
        spawnPoints = new List<List<Vector3>>(assets.Count);
        spawnPointsNormals = new List<List<Vector3>>(assets.Count);
        for (int i = 0; i < assets.Count; i++) {
            spawnPoints.Add(new List<Vector3>());
            spawnPointsNormals.Add(new List<Vector3>());
        }
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
        for(int i = 0; i < assets.Count; i++) {
            int breakCounter = 0;
            while(spawnPoints[i].Count < assets[i].assetsToSpawn) {
                if(breakCounter >= 500) break;
                int random = UnityEngine.Random.Range(0, worldVertices.Length);
                Vector3 spawnPoint = worldVertices[random];
                Vector3 spawnPointNormal = worldNormals[random];
                if(!assets[i].rotateToFaceNormal) {
                    spawnPoint.y -= 1f;
                }
                else {
                    spawnPoint.y -= 0.1f;
                }
                float height = spawnPoint.y;
                float slope = Vector3.Angle(worldNormals[random], Vector3.up);
                if((assets[i].useMinSlope ? slope > assets[i].minSlope : true) && 
                   (assets[i].useMaxSlope ? slope < assets[i].maxSlope : true) && 
                   (assets[i].useMinHeight ? height > assets[i].minHeight : true) && 
                   (assets[i].useMaxHeight ? height < assets[i].maxHeight : true) && 
                   (assets[i].underwaterAsset ? height < terrainDensityData.waterLevel-3f : height > terrainDensityData.waterLevel)) {
                    spawnPoints[i].Add(spawnPoint);
                    spawnPointsNormals[i].Add(spawnPointNormal);
                }
                // if(spawnPoint.y > terrainDensityData.waterLevel && slope < 25) {
                //     spawnPoints[i].Add(spawnPoint);
                // }
                breakCounter++;
            }
        }
    }

    private void AssetSpawnHandler() {
        for(int i = 0; i < assets.Count; i++) {
            for(int j = 0; j < spawnPoints[i].Count; j++) {
                float randomRotationDeg = UnityEngine.Random.Range(0f, 360f);
                Quaternion randomYRotation = Quaternion.Euler(0f, randomRotationDeg, 0f);
                GameObject assetToSpawn = Instantiate(assets[i].asset, spawnPoints[i][j], randomYRotation);
                assetToSpawn.transform.SetParent(gameObject.transform);
                if(assets[i].rotateToFaceNormal) {
                    assetToSpawn.transform.rotation = Quaternion.FromToRotation(Vector3.up, spawnPointsNormals[i][j]);
                }
                spawnedAssets.Add(assetToSpawn);
            }
        }
    }

    public void ClearAssets() {
        if(spawnedAssets != null) {
            foreach(GameObject asset in spawnedAssets) {
                Destroy(asset);
            }
        }
    }

    [Serializable]
    public class SpawnableAsset {
        public GameObject asset;
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
}
