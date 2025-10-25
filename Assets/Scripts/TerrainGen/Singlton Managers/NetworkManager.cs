using FishNet;
using FishNet.Connection;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static LobbyBroadcast;

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
        ChunkGenNetwork.Instance.hudCanvas.SetActive(true);
        ChunkGenNetwork.Instance.chatContainer.SetActive(true);
        ChunkGenNetwork.Instance.lobbyContainer.SetActive(true);
        ChunkGenNetwork.Instance.lightChange.intensity = 0.8f;

        //ChunkGenNetwork.Instance.flashlight.SetActive(true);
        PlayerController.instance.waterLevel = ChunkGenNetwork.Instance.terrainDensityData.waterLevel;

        GameObject.Find("NetworkManager/NetworkHudCanvas/Logo").SetActive(false);
        GameObject.Find("NetworkManager/NetworkHudCanvas/RemoteJoinTextBox").SetActive(false);
        GameObject.Find("NetworkManager/NetworkHudCanvas/NameTextBox").SetActive(false);
    }

    [ServerRpc(RequireOwnership = false)]
    void ClientReady(NetworkConnection target)
    {
        UpdateClientMesh(target,
                        SeedSerializer.SerializeTerrainDensity(ChunkGenNetwork.Instance.terrainDensityData),
                        PlayerController.instance.terraformCenters,
                        PlayerController.instance.hitChunkPositions,
                        PlayerController.instance.terraformTypes);
    }


    [TargetRpc]
    void UpdateClientMesh(NetworkConnection conn, TerrainSettings settings, List<Vector3> terraformCenters, List<Vector3Int> hitChunkPositions, List<int> terraformTypes)
    {
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

        ChunkGenNetwork.Instance.assetSpawnData.ResetSpawnPoints();
        ChunkGenNetwork.Instance.initialLoadComplete = false;
        ChunkGenNetwork.Instance.UpdateVisibleChunks();

        //Debug.LogWarning(terraformCenters.Count);

        if (terraformCenters.Count > 0)
            StartCoroutine(ApplyTerraforms(terraformCenters, hitChunkPositions, terraformTypes));
    }

    private IEnumerator ApplyTerraforms(List<Vector3> terraformCenters, List<Vector3Int> hitChunkPositions, List<int> terraformTypes)
    {
        yield return new WaitForSeconds(0.5f);
        GameObject player = PlayerController.instance.gameObject;
        for (int i = 0; i < terraformCenters.Count; i++)
        {
            Debug.LogWarning("Inside Terraform Apply");
            if (terraformTypes[i] == 0)
                player.GetComponent<BombLogic>().BombTerraformLocal(terraformCenters[i], hitChunkPositions[i]);
            else if (terraformTypes[i] == 1)
                player.GetComponent<Terraforming>().TerraformClientLocal(terraformCenters[i], hitChunkPositions[i], false);
            else
                player.GetComponent<Terraforming>().TerraformClientLocal(terraformCenters[i], hitChunkPositions[i], true);
        }
    }
}
