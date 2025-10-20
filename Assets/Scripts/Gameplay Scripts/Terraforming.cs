using System;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using FishNet.Object;
using FishNet.Connection;

public class Terraforming : NetworkBehaviour
{
    Camera playerCamera;
    public float terraformMaxDst = 20f;
    public float terraformRadius = 5f;
    public float terraformStrength = 5f;
    bool mode = true;
    LayerMask terrainLayer;
    public TerrainDensityData terrainDensityData;
    public float terraformUpdateTic = 0.2f;
    float time = 0f;
    //void Start()
    //{
    //    playerCamera = Camera.main;
    //    terrainLayer = LayerMask.GetMask("Terrain Layer");
    //    // terrainDensityData = Resources.Load<TerrainDensityData1>("TerrainDensityData1");
    //    // chunkGen = FindFirstObjectByType<ChunkGenNetwork>();
    //}

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!base.IsOwner)
            this.enabled = false;
        else
        {
            playerCamera = Camera.main;
            terrainLayer = LayerMask.GetMask("Terrain Layer");
        }
    }

    void Update()
    {
        // Block input if in a chat message block. Ensures that typing words with certain letters or numbers won't trigger input events.
        if (EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null)
        {
            return;
        }

        if (Input.GetMouseButton(0) && time >= terraformUpdateTic)
        {
            time = 0f;
            mode = true;
            Terraform(mode);
        }
        if (Input.GetMouseButton(1))
        {
            time = 0f;
            mode = false;
            Terraform(mode);
        }
        time += Time.deltaTime;
    }
    /// <summary>
    /// Terraform where the player is looking when they use a terraform interaction key
    /// </summary>
    /// <param name="mode">Whether to add or subtract terrain</param>
    public void Terraform(bool mode)
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, terraformMaxDst, terrainLayer))
        {
            if (hit.distance <= 1f || math.abs(hit.point.y - terraformRadius) >= terrainDensityData.width * (mode ? ChunkGenNetwork.Instance.maxWorldYChunks : ChunkGenNetwork.Instance.maxWorldYChunks + 1))
                return;
            Vector3 terraformCenter = hit.point;
            GameObject hitChunk = hit.collider.gameObject;
            ComputeMarchingCubes hitMarchingCubes = hitChunk.GetComponent<ComputeMarchingCubes>();
            Vector3Int hitChunkPos = hitMarchingCubes.chunkPos;
            TerraformServer(terraformCenter, hitChunkPos, mode);
            ChunkGenNetwork cg = FindFirstObjectByType<ChunkGenNetwork>();
            cg.navMeshNeedsUpdate = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TerraformServer(Vector3 terraformCenter, Vector3Int hitChunkPos, bool terraformMode)
    {
        PlayerController.instance.terraformCenters.Add(terraformCenter);
        PlayerController.instance.hitChunkPositions.Add(hitChunkPos);
        int terraformType = terraformMode == false ? 1 : 2;
        PlayerController.instance.terraformTypes.Add(terraformType);

        // Debug.LogWarning(PlayerController.instance.terraformCenters.Count);
        TerraformClient(terraformCenter, hitChunkPos, terraformMode);
    }

    [ObserversRpc]
    public void TerraformClient(Vector3 terraformCenter, Vector3Int hitChunkPos, bool terraformMode)
    {
        TerraformClientLocal(terraformCenter, hitChunkPos, terraformMode);  
    }

    public void TerraformClientLocal(Vector3 terraformCenter, Vector3Int hitChunkPos, bool terraformMode)
    {
        ChunkGenNetwork.TerrainChunk[] chunkAndNeighbors = ChunkGenNetwork.Instance.GetChunkAndNeighbors(new Vector3Int(Mathf.CeilToInt(hitChunkPos.x / terrainDensityData.width), Mathf.CeilToInt(hitChunkPos.y / terrainDensityData.width), Mathf.CeilToInt(hitChunkPos.z / terrainDensityData.width)));
        foreach (ChunkGenNetwork.TerrainChunk terrainChunk in chunkAndNeighbors)
        {
            if (terrainChunk == null) continue;
            if (Mathf.Sqrt(terrainChunk.bounds.SqrDistance(terraformCenter)) <= terraformRadius)
            {
                ComputeMarchingCubes marchingCubes = terrainChunk.marchingCubes;
                Vector3Int chunkPos = terrainChunk.chunkPos;
                Vector3Int radius = new Vector3Int(Mathf.CeilToInt(terraformRadius), Mathf.CeilToInt(terraformRadius), Mathf.CeilToInt(terraformRadius));
                Vector3Int start = Vector3Int.Max(Vector3Int.RoundToInt(terraformCenter) - radius - chunkPos, Vector3Int.zero);
                Vector3Int end = Vector3Int.Min(Vector3Int.RoundToInt(terraformCenter) + radius - chunkPos, new Vector3Int(Mathf.CeilToInt(terrainDensityData.width), Mathf.CeilToInt(terrainDensityData.width), Mathf.CeilToInt(terrainDensityData.width)));

                int threadSizeX = Mathf.CeilToInt((end.x - start.x) + 1f);
                int threadSizeY = Mathf.CeilToInt((end.y - start.y) + 1f);
                int threadSizeZ = Mathf.CeilToInt((end.z - start.z) + 1f);

                int terraformKernel = marchingCubes.terraformComputeShader.FindKernel("Terraform");
                marchingCubes.terraformComputeShader.SetBuffer(terraformKernel, "HeightsBuffer", marchingCubes.heightsBuffer);
                marchingCubes.terraformComputeShader.SetInt("ChunkSize", terrainDensityData.width);
                marchingCubes.terraformComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);
                marchingCubes.terraformComputeShader.SetVector("TerraformOffset", (Vector3)start);
                marchingCubes.terraformComputeShader.SetVector("TerraformCenter", terraformCenter);
                marchingCubes.terraformComputeShader.SetFloat("TerraformRadius", terraformRadius);
                marchingCubes.terraformComputeShader.SetFloat("TerraformStrength", terraformStrength);
                marchingCubes.terraformComputeShader.SetBool("TerraformMode", terraformMode);
                marchingCubes.terraformComputeShader.SetInt("MaxWorldYChunks", ChunkGenNetwork.Instance.maxWorldYChunks);

                marchingCubes.terraformComputeShader.Dispatch(terraformKernel, threadSizeX, threadSizeY, threadSizeZ);

                int size = (terrainDensityData.width + 1) * (terrainDensityData.width + 1) * (terrainDensityData.width + 1);

                marchingCubes.heightsBuffer.GetData(marchingCubes.heightsArray, 0, 0, size);

                marchingCubes.MarchingCubesJobHandler(marchingCubes.heightsArray, true);
            }
        }
    }
}
