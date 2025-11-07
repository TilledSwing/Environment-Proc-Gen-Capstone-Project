using FishNet;
using FishNet.Connection;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static LobbyBroadcast;

public class NetworkManager : NetworkBehaviour
{
    private float explosionRadius = 10f;
    private float terraformStrength = 5f;
    public LayerMask assetLayer;

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
        // ChunkGenNetwork.Instance.lightChange.intensity = 0.8f;

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
        while (!ChunkGenNetwork.Instance.initialLoadComplete || ChunkGenNetwork.Instance.hasPendingMeshInits || ChunkGenNetwork.Instance.isLoadingMeshes || ChunkGenNetwork.Instance.hasPendingAssetInstantiations ||
                ChunkGenNetwork.Instance.isLoadingAssetInstantiations || ChunkGenNetwork.Instance.hasPendingReadbacks || ChunkGenNetwork.Instance.isLoadingReadbacks || ChunkGenNetwork.Instance.isLoadingChunks || PlayerController.instance == null)
        { 
            yield return new WaitForSeconds(0.5f);
        }

        GameObject player = PlayerController.instance.gameObject;

        //while (!player.GetComponent<BombLogic>().IsClientInitialized || !player.GetComponent<Terraforming>().IsClientInitialized)
        while(!player.GetComponent<Terraforming>().IsClientInitialized)
        {
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(2f);

        Debug.LogWarning("Through Wait");
        for (int i = 0; i < terraformCenters.Count; i++)
        {
            // Debug.LogWarning("Inside Terraform Apply");
            if (terraformTypes[i] == 0)
                //player.GetComponent<BombThrow>().GetComponent<BombLogic>().BombTerraformLocal(terraformCenters[i], hitChunkPositions[i])
                StartCoroutine(ApplyPreviousBombTerraform(terraformCenters[i], hitChunkPositions[i]));
            if (terraformTypes[i] == 1)
                player.GetComponent<Terraforming>().TerraformClientLocal(terraformCenters[i], hitChunkPositions[i], false);
            else
                player.GetComponent<Terraforming>().TerraformClientLocal(terraformCenters[i], hitChunkPositions[i], true);
        }
    }

    public IEnumerator ApplyPreviousBombTerraform(Vector3 terraformCenter, Vector3Int hitChunkPos)
    {
        Collider[] colliders = Physics.OverlapSphere(terraformCenter, explosionRadius, assetLayer);
        foreach (Collider collider in colliders)
        {
            Destroy(collider.gameObject);
        }

        TerrainDensityData terrainDensityData = ChunkGenNetwork.Instance.terrainDensityData;
        Debug.LogWarning("BombTerraform called");
        ChunkGenNetwork.TerrainChunk[] chunkAndNeighbors = ChunkGenNetwork.Instance.GetChunkAndNeighbors(hitChunkPos);
        foreach (ChunkGenNetwork.TerrainChunk terrainChunk in chunkAndNeighbors)
        {
            if (terrainChunk == null) continue;
            if (Mathf.Sqrt(terrainChunk.bounds.SqrDistance(terraformCenter)) <= explosionRadius)
            {
                ComputeMarchingCubes marchingCubes = terrainChunk.marchingCubes;
                Vector3Int chunkPos = terrainChunk.chunkPos;
                Vector3Int radius = new Vector3Int(Mathf.CeilToInt(explosionRadius), Mathf.CeilToInt(explosionRadius), Mathf.CeilToInt(explosionRadius));
                Vector3Int start = Vector3Int.Max(Vector3Int.RoundToInt(terraformCenter) - radius - chunkPos, Vector3Int.zero);
                Vector3Int end = Vector3Int.Min(Vector3Int.RoundToInt(terraformCenter) + radius - chunkPos, new Vector3Int(Mathf.CeilToInt(terrainDensityData.width), Mathf.CeilToInt(terrainDensityData.width), Mathf.CeilToInt(terrainDensityData.width)));

                int threadSizeX = Mathf.CeilToInt((end.x - start.x) + 1f);
                int threadSizeY = Mathf.CeilToInt((end.y - start.y) + 1f);
                int threadSizeZ = Mathf.CeilToInt((end.z - start.z) + 1f);

                while (marchingCubes.heightsBuffer == null)
                    yield return new WaitForSeconds(0.5f);

                int terraformKernel = marchingCubes.terraformComputeShader.FindKernel("Terraform");
                marchingCubes.terraformComputeShader.SetBuffer(terraformKernel, "HeightsBuffer", marchingCubes.heightsBuffer);
                marchingCubes.terraformComputeShader.SetInt("ChunkSize", terrainDensityData.width);
                marchingCubes.terraformComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);
                marchingCubes.terraformComputeShader.SetVector("TerraformOffset", (Vector3)start);
                marchingCubes.terraformComputeShader.SetVector("TerraformCenter", terraformCenter);
                marchingCubes.terraformComputeShader.SetFloat("TerraformRadius", explosionRadius);
                marchingCubes.terraformComputeShader.SetFloat("TerraformStrength", terraformStrength);
                marchingCubes.terraformComputeShader.SetBool("TerraformMode", true);
                marchingCubes.terraformComputeShader.SetInt("MaxWorldYChunks", ChunkGenNetwork.Instance.maxWorldYChunks);

                marchingCubes.terraformComputeShader.Dispatch(terraformKernel, threadSizeX, threadSizeY, threadSizeZ);

                int size = (terrainDensityData.width + 1) * (terrainDensityData.width + 1) * (terrainDensityData.width + 1);

                marchingCubes.heightsBuffer.GetData(marchingCubes.heightsArray, 0, 0, size);

                marchingCubes.MarchingCubesJobHandler(marchingCubes.heightsArray, true);
            }
        }
    }
}
