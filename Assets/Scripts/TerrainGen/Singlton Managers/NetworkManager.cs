using FishNet.Object;
using FishNet.Connection;
using UnityEngine;
using FishNet;
using System.Collections;

public class NetworkManager : NetworkBehaviour
{
    /// <summary>
    /// Sets the player to the new viewer for chunk generation and disables the local chunk generator
    /// </summary>
    public override void OnStartClient()
    {
        base.OnStartClient();

        // Only need to load in new data if not the server host.
        if (!base.IsServerStarted)
        {
            ClientReady(LocalConnection);
        }

        ChunkGenNetwork.Instance.viewer = GameObject.Find("Player(Clone)").transform;
        ChunkGenNetwork.Instance.SetFogActive(true);
        ChunkGenNetwork.Instance.objectiveCanvas.SetActive(true);
        ChunkGenNetwork.Instance.chatContainer.SetActive(true);
        ChunkGenNetwork.Instance.lobbyContainer.SetActive(true);
        PlayerController.instance.waterLevel = ChunkGenNetwork.Instance.terrainDensityData.waterLevel;
    }

    [ServerRpc(RequireOwnership = false)]
    void ClientReady(NetworkConnection target)
    {
        UpdateClientMesh(target, SeedSerializer.SerializeTerrainDensity(ChunkGenNetwork.Instance.terrainDensityData));
    }


    [TargetRpc]
    void UpdateClientMesh(NetworkConnection conn, TerrainSettings settings)
    {
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
        ChunkGenNetwork.Instance.initialLoadComplete = false;
        // Action Queues
        ChunkGenNetwork.Instance.hasPendingMeshInits = false;
        ChunkGenNetwork.Instance.pendingMeshInits = new();
        ChunkGenNetwork.Instance.isLoadingMeshes = false;
        ChunkGenNetwork.Instance.hasPendingReadbacks = false;
        ChunkGenNetwork.Instance.pendingReadbacks = new();
        ChunkGenNetwork.Instance.isLoadingReadbacks = false;
        ChunkGenNetwork.Instance.hasPendingAssetInstantiations = false;
        ChunkGenNetwork.Instance.pendingAssetInstantiations = new();
        ChunkGenNetwork.Instance.isLoadingAssetInstantiations = false;

        ChunkGenNetwork.Instance.chunkSize = ChunkGenNetwork.Instance.terrainDensityData.width;
        ChunkGenNetwork.Instance.chunksVisible = Mathf.RoundToInt(ChunkGenNetwork.Instance.maxViewDst / ChunkGenNetwork.Instance.chunkSize);

        PlayerController.instance.waterLevel = terrainDensityDataNew.waterLevel;
    }
}
