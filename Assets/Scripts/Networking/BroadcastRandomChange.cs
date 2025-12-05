using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class BroadcastRandomChange : NetworkBehaviour
{
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!base.IsOwner)
            this.enabled = false;
    }

    public void OnRandomTerrainGen()
    {
        GameObject chunk = GameObject.Find("ChunkParent");

        while (chunk.transform.childCount > 0)
        {
            DestroyImmediate(chunk.transform.GetChild(0).gameObject);
        }

        // Set random seeds
        foreach (NoiseGenerator noiseGenerator in ChunkGenNetwork.Instance.terrainDensityData.noiseGenerators)
        {
            noiseGenerator.noiseSeed = UnityEngine.Random.Range(0, 100000);
            noiseGenerator.domainWarpSeed = UnityEngine.Random.Range(0, 100000);
        }

        // Reset action and chunking to defaults (loading in from fresh)
        // Chunk Variables
        ChunkGenNetwork.Instance.chunkDictionary = new();
        ChunkGenNetwork.Instance.chunksVisibleLastUpdate = new();
        ChunkGenNetwork.Instance.chunkLoadQueue = new();
        ChunkGenNetwork.Instance.chunkLoadSet = new();
        ChunkGenNetwork.Instance.chunkHideQueue = new();
        ChunkGenNetwork.Instance.chunkShowQueue = new();
        ChunkGenNetwork.Instance.isLoadingChunkVisibility = false;
        // queueUpdateDistanceThreshold = 15f;
        ChunkGenNetwork.Instance.isLoadingChunks = false;
        // Action Queues
        ChunkGenNetwork.Instance.hasPendingReadbacks = false;
        ChunkGenNetwork.Instance.pendingReadbacks = new();
        ChunkGenNetwork.Instance.isLoadingReadbacks = false;
        ChunkGenNetwork.Instance.hasPendingAssetInstantiations = false;
        ChunkGenNetwork.Instance.pendingAssetInstantiations = new();
        ChunkGenNetwork.Instance.isLoadingAssetInstantiations = false;

        ChunkGenNetwork.Instance.chunkSize = ChunkGenNetwork.Instance.terrainDensityData.width;
        ChunkGenNetwork.Instance.chunksVisible = Mathf.RoundToInt(ChunkGenNetwork.Instance.maxViewDst / ChunkGenNetwork.Instance.chunkSize);

        PlayerController.instance.waterLevel = ChunkGenNetwork.Instance.terrainDensityData.waterLevel;

        ChunkGenNetwork.Instance.assetSpawnData.ResetSpawnPoints();
        ChunkGenNetwork.Instance.initialLoadComplete = false;
        ChunkGenNetwork.Instance.UpdateVisibleChunks();

        RandomTerrainGenServer();
    }

    [ServerRpc(RequireOwnership = false)]
    void RandomTerrainGenServer()
    {
        UpdateClientMeshObservers(SeedSerializer.SerializeTerrainDensity(ChunkGenNetwork.Instance.terrainDensityData), 
                                  SeedSerializer.SerializeAssetData(ChunkGenNetwork.Instance.assetSpawnData));
    }

    [ObserversRpc]
    void UpdateClientMeshObservers(TerrainSettings settings, AssetSpawnSettings[] assetSettings)
    {
        // The server already has the updated values
        if (base.IsServerStarted)
            return;

        GameObject chunk = GameObject.Find("ChunkParent");

        while (chunk.transform.childCount > 0)
        {
            DestroyImmediate(chunk.transform.GetChild(0).gameObject);
        }

        TerrainDensityData terrainDensityDataNew = SeedSerializer.DeserializeTerrainDensity(settings);
        ChunkGenNetwork.Instance.terrainDensityData = terrainDensityDataNew;

        // Reset action and chunking to defaults (loading in from fresh)
        // Chunk Variables
        ChunkGenNetwork.Instance.chunkDictionary = new();
        ChunkGenNetwork.Instance.chunksVisibleLastUpdate = new();
        ChunkGenNetwork.Instance.chunkLoadQueue = new();
        ChunkGenNetwork.Instance.chunkLoadSet = new();
        ChunkGenNetwork.Instance.chunkHideQueue = new();
        ChunkGenNetwork.Instance.chunkShowQueue = new();
        ChunkGenNetwork.Instance.isLoadingChunkVisibility = false;
        // queueUpdateDistanceThreshold = 15f;
        ChunkGenNetwork.Instance.isLoadingChunks = false;
        // Action Queues
        ChunkGenNetwork.Instance.hasPendingReadbacks = false;
        ChunkGenNetwork.Instance.pendingReadbacks = new();
        ChunkGenNetwork.Instance.isLoadingReadbacks = false;
        ChunkGenNetwork.Instance.hasPendingAssetInstantiations = false;
        ChunkGenNetwork.Instance.pendingAssetInstantiations = new();
        ChunkGenNetwork.Instance.isLoadingAssetInstantiations = false;

        ChunkGenNetwork.Instance.chunkSize = ChunkGenNetwork.Instance.terrainDensityData.width;
        ChunkGenNetwork.Instance.chunksVisible = Mathf.RoundToInt(ChunkGenNetwork.Instance.maxViewDst / ChunkGenNetwork.Instance.chunkSize);

        PlayerController.instance.waterLevel = terrainDensityDataNew.waterLevel;

        ChunkGenNetwork.Instance.assetSpawnData.ResetSpawnPoints();
        SeedSerializer.DeserializeAndUpdateAssetData(ChunkGenNetwork.Instance.assetSpawnData, assetSettings);
        ChunkGenNetwork.Instance.initialLoadComplete = false;
        ChunkGenNetwork.Instance.UpdateVisibleChunks();
    }
}
