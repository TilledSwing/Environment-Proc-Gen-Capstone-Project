using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class ChunkGenLocal : MonoBehaviour
{
    public float maxViewDst = 100;
    public Transform viewer;
    public static Vector3 viewerPos;
    public int chunkSize;
    public int chunksVisible;
    public TerrainDensityData1 terrainDensityData;
    public ComputeShader marchingCubesComputeShader;
    public ComputeShader terrainDensityComputeShader;
    public ComputeShader terrainNoiseComputeShader;
    public ComputeShader caveNoiseComputeShader;
    public ComputeShader terraformComputeShader;
    public ComputeShader spawnPointsComputeShader;
    public Material terrainMaterial;
    public Material waterMaterial;
    public AssetSpawnData assetSpawnData;
    public Dictionary<Vector3, TerrainChunk> chunkDictionary = new Dictionary<Vector3, TerrainChunk>();
    public Dictionary<Vector3Int, List<SpawnableAsset>> assets = new Dictionary<Vector3Int, List<SpawnableAsset>>();
    public List<TerrainChunk> chunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start()
    {
        chunkSize = terrainDensityData.width;
        chunksVisible = Mathf.RoundToInt(maxViewDst / chunkSize);
        terrainDensityData.noiseSeed = UnityEngine.Random.Range(0, 100000);
        terrainDensityData.caveNoiseSeed = UnityEngine.Random.Range(0, 100000);
        terrainDensityData.domainWarpSeed = UnityEngine.Random.Range(0, 100000);
        terrainDensityData.caveDomainWarpSeed = UnityEngine.Random.Range(0, 100000);
    }

    void Update()
    {
        viewerPos = new Vector3(viewer.position.x, viewer.position.y, viewer.position.z);
        UpdateVisibleChunks();
    }

    public void UpdateVisibleChunks()
    {
        for (int i = 0; i < chunksVisibleLastUpdate.Count; i++)
        {
            chunksVisibleLastUpdate[i].SetVisible(false);
        }
        chunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPos.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPos.y / chunkSize);
        int currentChunkCoordZ = Mathf.RoundToInt(viewerPos.z / chunkSize);

        for (int xOffset = -chunksVisible; xOffset <= chunksVisible; xOffset++)
        {
            for (int yOffset = -chunksVisible; yOffset <= chunksVisible; yOffset++)
            {
                for (int zOffset = -chunksVisible; zOffset <= chunksVisible; zOffset++)
                {
                    Vector3Int viewedChunkCoord = new Vector3Int(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset, currentChunkCoordZ + zOffset);

                    // if (viewedChunkCoord.y < 0) break;

                    if (chunkDictionary.ContainsKey(viewedChunkCoord))
                    {
                        chunkDictionary[viewedChunkCoord].UpdateChunk(maxViewDst, chunkSize);
                        if (chunkDictionary[viewedChunkCoord].IsVisible())
                        {
                            chunksVisibleLastUpdate.Add(chunkDictionary[viewedChunkCoord]);
                        }
                    }
                    else
                    {
                        TerrainChunk chunk = new TerrainChunk(viewedChunkCoord, chunkSize, transform, terrainDensityData, assetSpawnData,
                                                         marchingCubesComputeShader, terrainDensityComputeShader, terrainNoiseComputeShader,
                                                         caveNoiseComputeShader, terraformComputeShader, spawnPointsComputeShader,
                                                         terrainMaterial, waterMaterial);
                        chunkDictionary.Add(viewedChunkCoord, chunk);
                        chunk.UpdateChunk(maxViewDst, chunkSize);

                        if (chunk.IsVisible())
                        {
                            chunksVisibleLastUpdate.Add(chunk);
                        }
                    }
                }
            }
        }
    }

    public TerrainChunk[] GetChunkAndNeighbors(Vector3Int chunkCoord)
    {
        TerrainChunk[] chunkAndNeighbors = new TerrainChunk[] {
            chunkDictionary[new Vector3Int(chunkCoord.x, chunkCoord.y, chunkCoord.z)],       // 1
            chunkDictionary[new Vector3Int(chunkCoord.x, chunkCoord.y+1, chunkCoord.z)],     // 2
            chunkDictionary[new Vector3Int(chunkCoord.x, chunkCoord.y, chunkCoord.z+1)],     // 3
            chunkDictionary[new Vector3Int(chunkCoord.x, chunkCoord.y+1, chunkCoord.z+1)],   // 4
            chunkDictionary[new Vector3Int(chunkCoord.x, chunkCoord.y-1, chunkCoord.z)],     // 5
            chunkDictionary[new Vector3Int(chunkCoord.x, chunkCoord.y, chunkCoord.z-1)],     // 6
            chunkDictionary[new Vector3Int(chunkCoord.x, chunkCoord.y-1, chunkCoord.z-1)],   // 7
            chunkDictionary[new Vector3Int(chunkCoord.x, chunkCoord.y+1, chunkCoord.z-1)],   // 8
            chunkDictionary[new Vector3Int(chunkCoord.x, chunkCoord.y-1, chunkCoord.z+1)],   // 9
            chunkDictionary[new Vector3Int(chunkCoord.x+1, chunkCoord.y, chunkCoord.z)],     // 10
            chunkDictionary[new Vector3Int(chunkCoord.x+1, chunkCoord.y+1, chunkCoord.z)],   // 11
            chunkDictionary[new Vector3Int(chunkCoord.x+1, chunkCoord.y, chunkCoord.z+1)],   // 12
            chunkDictionary[new Vector3Int(chunkCoord.x+1, chunkCoord.y+1, chunkCoord.z+1)], // 13
            chunkDictionary[new Vector3Int(chunkCoord.x+1, chunkCoord.y-1, chunkCoord.z)],   // 14
            chunkDictionary[new Vector3Int(chunkCoord.x+1, chunkCoord.y, chunkCoord.z-1)],   // 15
            chunkDictionary[new Vector3Int(chunkCoord.x+1, chunkCoord.y-1, chunkCoord.z-1)], // 16
            chunkDictionary[new Vector3Int(chunkCoord.x+1, chunkCoord.y+1, chunkCoord.z-1)], // 17
            chunkDictionary[new Vector3Int(chunkCoord.x+1, chunkCoord.y-1, chunkCoord.z+1)], // 18
            chunkDictionary[new Vector3Int(chunkCoord.x-1, chunkCoord.y, chunkCoord.z)],     // 19
            chunkDictionary[new Vector3Int(chunkCoord.x-1, chunkCoord.y+1, chunkCoord.z)],   // 20
            chunkDictionary[new Vector3Int(chunkCoord.x-1, chunkCoord.y, chunkCoord.z+1)],   // 21
            chunkDictionary[new Vector3Int(chunkCoord.x-1, chunkCoord.y+1, chunkCoord.z+1)], // 22
            chunkDictionary[new Vector3Int(chunkCoord.x-1, chunkCoord.y-1, chunkCoord.z)],   // 23
            chunkDictionary[new Vector3Int(chunkCoord.x-1, chunkCoord.y, chunkCoord.z-1)],   // 24
            chunkDictionary[new Vector3Int(chunkCoord.x-1, chunkCoord.y-1, chunkCoord.z-1)], // 25
            chunkDictionary[new Vector3Int(chunkCoord.x-1, chunkCoord.y+1, chunkCoord.z-1)], // 26
            chunkDictionary[new Vector3Int(chunkCoord.x-1, chunkCoord.y-1, chunkCoord.z+1)], // 27
        };
        return chunkAndNeighbors;
    }

    void OnApplicationQuit()
    {
        assetSpawnData.assets.Clear();
        chunkDictionary.Clear();
        assets.Clear();
    }

    public class TerrainChunk
    {
        public GameObject chunk;
        public ComputeMarchingCubes marchingCubes;
        public AssetSpawner assetSpawner;
        public Vector3Int chunkPos;
        public Bounds bounds;
        public MeshCollider meshCollider;
        public TerrainChunk(Vector3Int chunkCoord, int chunkSize, Transform parent, TerrainDensityData1 terrainDensityData, AssetSpawnData assetSpawnData, ComputeShader marchingCubesComputeShader, ComputeShader terrainDensityComputeShader, ComputeShader terrainNoiseComputeShader, ComputeShader caveNoiseComputeShader, ComputeShader terraformComputeShader, ComputeShader spawnPointsComputeShader, Material terrainMaterial, Material waterMaterial)
        {
            chunkPos = chunkCoord * chunkSize;
            bounds = new Bounds(chunkPos + (new Vector3(0.5f, 0.5f, 0.5f) * chunkSize), Vector3.one * chunkSize);
            chunk = new GameObject("Chunk");
            chunk.layer = 3;
            meshCollider = chunk.AddComponent<MeshCollider>();
            chunk.AddComponent<MeshFilter>();
            MeshRenderer mr = chunk.AddComponent<MeshRenderer>();
            mr.material = terrainMaterial;
            mr.material.SetFloat("_UnderwaterTexHeightEnd", terrainDensityData.waterLevel - 15f);
            mr.material.SetFloat("_Tex1HeightStart", terrainDensityData.waterLevel - 18f);
            assetSpawner = chunk.AddComponent<AssetSpawner>();
            assetSpawner.chunkPos = chunkPos;
            assetSpawner.terrainDensityData = terrainDensityData;
            assetSpawner.assetSpawnData = assetSpawnData;
            assetSpawner.spawnPointsComputeShader = spawnPointsComputeShader;
            marchingCubes = chunk.AddComponent<ComputeMarchingCubes>();
            marchingCubes.chunkPos = chunkPos;
            marchingCubes.marchingCubesComputeShader = marchingCubesComputeShader;
            marchingCubes.terrainDensityComputeShader = terrainDensityComputeShader;
            marchingCubes.terrainNoiseComputeShader = terrainNoiseComputeShader;
            marchingCubes.caveNoiseComputeShader = caveNoiseComputeShader;
            marchingCubes.terraformComputeShader = terraformComputeShader;
            marchingCubes.terrainDensityData = terrainDensityData;
            marchingCubes.terrainMaterial = terrainMaterial;
            marchingCubes.waterMaterial = waterMaterial;
            chunk.transform.SetParent(parent);
            SetVisible(false);
        }

        public void UpdateChunk(float maxViewDst, int chunkSize)
        {
            float viewerDstFromBound = Mathf.Sqrt(bounds.SqrDistance(viewerPos));
            bool colliderEnable = viewerDstFromBound <= chunkSize;
            bool visible = viewerDstFromBound <= maxViewDst;
            SetCollider(colliderEnable);
            SetVisible(visible);
        }

        public void SetVisible(bool visible)
        {
            if (chunk.activeSelf != visible)
            {
                chunk.SetActive(visible);
            }
        }

        public void SetCollider(bool colliderEnable)
        {
            meshCollider.enabled = colliderEnable;
            for (int i = 0; i < assetSpawner.spawnedAssets.Count; i++)
            {
                foreach (GameObject asset in assetSpawner.spawnedAssets[i])
                {
                    asset.GetComponent<MeshCollider>().enabled = colliderEnable;
                }
            }
        }

        public bool IsVisible()
        {
            return chunk.activeSelf;
        }
    }
}
