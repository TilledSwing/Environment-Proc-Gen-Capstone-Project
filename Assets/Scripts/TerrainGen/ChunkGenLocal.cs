using System;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class ChunkGenLocal : MonoBehaviour
{
    public static float maxViewDst = 100;
    public Transform viewer;
    public static Vector3 viewerPos;
    public int chunkSize;
    public int chunksVisible;
    // public bool setMapSize = true;
    // public int mapSize = 30;
    public TerrainDensityData terrainDensityData;
    public AssetSpawnData assetSpawnData;
    public Dictionary<Vector3, TerrainChunk> chunkDictionary = new Dictionary<Vector3, TerrainChunk>();
    public Dictionary<Vector3Int, List<SpawnableAsset>> assets = new Dictionary<Vector3Int, List<SpawnableAsset>>();
    public List<TerrainChunk> chunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start()
    {
        terrainDensityData = Resources.Load<TerrainDensityData>("TerrainDensityData");
        assetSpawnData = Resources.Load<AssetSpawnData>("AssetSpawnData");
        chunkSize = terrainDensityData.width;
        chunksVisible = Mathf.RoundToInt(maxViewDst/chunkSize);
        terrainDensityData.noiseSeed = UnityEngine.Random.Range(0, 10000);
        terrainDensityData.domainWarpSeed = UnityEngine.Random.Range(0, 10000);
    }

    void Update()
    {
        viewerPos = new Vector3(viewer.position.x,viewer.position.y,viewer.position.z);
        UpdateVisibleChunks();
    }

    public void UpdateVisibleChunks() {
        for(int i = 0; i < chunksVisibleLastUpdate.Count; i++) {
            chunksVisibleLastUpdate[i].SetVisible(false);
        }
        chunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPos.x/chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPos.y/chunkSize);
        int currentChunkCoordZ = Mathf.RoundToInt(viewerPos.z/chunkSize);

        for(int xOffset = -chunksVisible; xOffset <= chunksVisible; xOffset++) {
            for(int yOffset = -chunksVisible; yOffset <= chunksVisible; yOffset++) {
                for(int zOffset = -chunksVisible; zOffset <= chunksVisible; zOffset++) {
                    Vector3Int viewedChunkCoord = new Vector3Int(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset, currentChunkCoordZ + zOffset);

                    if(viewedChunkCoord.y < 0) break;

                    if(chunkDictionary.ContainsKey(viewedChunkCoord)) {
                        chunkDictionary[viewedChunkCoord].UpdateChunk();
                        if(chunkDictionary[viewedChunkCoord].IsVisible()) {
                            chunksVisibleLastUpdate.Add(chunkDictionary[viewedChunkCoord]);
                        }
                    }
                    else {
                        chunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, transform));
                    }
                }
            }
        }
    }

    public class TerrainChunk {
        GameObject chunk;
        MarchingCubes marchingCubes;
        AssetSpawner assetSpawner;
        Vector3Int chunkPos;
        Bounds bounds;
        public TerrainChunk(Vector3Int chunkCoord, int chunkSize, Transform parent) {
            chunkPos = chunkCoord * chunkSize;
            bounds = new Bounds(chunkPos, Vector3.one * chunkSize);
            chunk = new GameObject("Chunk");
            assetSpawner = chunk.AddComponent<AssetSpawner>();
            assetSpawner.chunkPos = chunkPos;
            marchingCubes = chunk.AddComponent<MarchingCubes>();
            marchingCubes.chunkPos = chunkPos;
            chunk.transform.SetParent(parent);
            SetVisible(false);
        }

        public void UpdateChunk() {
            float viewerDstFromBound = Mathf.Sqrt(bounds.SqrDistance(viewerPos));
            bool visible = viewerDstFromBound <= maxViewDst;
            SetVisible(visible);
        }

        public void SetVisible(bool visible) {
            chunk.SetActive(visible);
            // assetSpawner.SetAssetsActive(visible);
        }

        public bool IsVisible(){
            return chunk.activeSelf;
        }
    }
}
