using System.Collections.Generic;
using FishNet.Demo.Benchmarks.NetworkTransforms;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.Scripting;

public class AssetSpawner : MonoBehaviour
{
    public List<GameObject> assets = new List<GameObject>();
    private List<List<Vector3>> spawnPoints;
    private List<GameObject> spawnedAssets = new List<GameObject>();
    public TerrainDensityData terrainDensityData;
    public int assetsToSpawn = 25;
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
        for (int i = 0; i < assets.Count; i++) {
            spawnPoints.Add(new List<Vector3>());
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
            while(spawnPoints[i].Count < assetsToSpawn) {
                int random = Random.Range(0, worldVertices.Length);
                Vector3 spawnPoint = worldVertices[random];
                spawnPoint.y -= 1;
                float slope = Vector3.Angle(worldNormals[random], Vector3.up);
                if(spawnPoint.y > terrainDensityData.waterLevel && slope < 25) {
                    spawnPoints[i].Add(spawnPoint);
                }
            }
        }
    }

    private void AssetSpawnHandler() {
        for(int i = 0; i < assets.Count; i++) {
            for(int j = 0; j < spawnPoints[i].Count; j++) {
                spawnedAssets.Add(Instantiate(assets[i], spawnPoints[i][j], Quaternion.identity));
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
}
