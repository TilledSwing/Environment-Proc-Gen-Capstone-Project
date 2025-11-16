using System;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using FishNet.Object;
using FishNet.Connection;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

public class Terraforming : NetworkBehaviour
{
    Camera playerCamera;
    public float terraformMinDst = 1.2f;
    public float terraformMaxDst = 20f;
    public float terraformRadius = 5f;
    public float terraformStrength = 5f;
    bool mode = true;
    LayerMask terrainLayer;
    public TerrainDensityData terrainDensityData;
    public float terraformUpdateTic = 0.2f;
    float time = 0f;
    bool firstClick = true;
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
        if (Input.GetMouseButton(1) && time >= terraformUpdateTic)
        {
            time = 0f;
            mode = false;
            Terraform(mode);
        }
        if(Input.GetMouseButtonUp(1))
        {
            firstClick = true;
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
            if (hit.distance <= terraformMinDst || math.abs(hit.point.y - terraformRadius) >= terrainDensityData.width * (mode ? ChunkGenNetwork.Instance.maxWorldYChunks : ChunkGenNetwork.Instance.maxWorldYChunks + 1))
                return;
            // if (Vector3.Dot(playerCamera.transform.forward.normalized, Vector3.up) < -Mathf.Sin(45f * Mathf.Deg2Rad) && !mode)
            // {
            // Trying to avoid player clipping
            if (hit.distance < 1.5f && Vector3.Dot(playerCamera.transform.forward.normalized, Vector3.up) < -Mathf.Sin(0f * Mathf.Deg2Rad) && !mode)
            {
                if (firstClick)
                {
                    PlayerController.instance.moveDirection.y = 0;
                    PlayerController.instance.characterController.Move(Vector3.up * 2.5f);
                    firstClick = false;
                }
                else
                {
                    // PlayerController.instance.moveDirection.y = 0;
                    PlayerController.instance.characterController.Move(-ray.direction * 0.23f);
                }
            }
            Vector3 terraformCenter = hit.point;
            GameObject hitChunk = hit.collider.gameObject;
            ComputeMarchingCubes hitMarchingCubes = hitChunk.GetComponent<ComputeMarchingCubes>();
            Vector3Int hitChunkPos = hitMarchingCubes.chunkPos;
            TerraformServer(terraformCenter, hitChunkPos, mode);
            
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
            if (terrainChunk.bounds.SqrDistance(terraformCenter) <= terraformRadius * terraformRadius)
            {
                ComputeMarchingCubes marchingCubes = terrainChunk.marchingCubes;
                Vector3Int chunkPos = terrainChunk.chunkPos;
                Vector3Int radius = new Vector3Int(Mathf.CeilToInt(terraformRadius), Mathf.CeilToInt(terraformRadius), Mathf.CeilToInt(terraformRadius));
                Vector3Int start = Vector3Int.Max(Vector3Int.RoundToInt(terraformCenter) - radius - chunkPos, Vector3Int.zero);
                Vector3Int end = Vector3Int.Min(Vector3Int.RoundToInt(terraformCenter) + radius - chunkPos, new Vector3Int(Mathf.CeilToInt(terrainDensityData.width), Mathf.CeilToInt(terrainDensityData.width), Mathf.CeilToInt(terrainDensityData.width)));

                int threadSizeX = Mathf.CeilToInt((end.x - start.x) + 1f);
                int threadSizeY = Mathf.CeilToInt((end.y - start.y) + 1f);
                int threadSizeZ = Mathf.CeilToInt((end.z - start.z) + 1f);

                // int terraformKernel = marchingCubes.terraformComputeShader.FindKernel("Terraform");
                // marchingCubes.terraformComputeShader.SetBuffer(terraformKernel, "HeightsBuffer", marchingCubes.heightsBuffer);
                // marchingCubes.terraformComputeShader.SetInt("ChunkSize", terrainDensityData.width);
                // marchingCubes.terraformComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);
                // marchingCubes.terraformComputeShader.SetVector("TerraformOffset", (Vector3)start);
                // marchingCubes.terraformComputeShader.SetVector("TerraformCenter", terraformCenter);
                // marchingCubes.terraformComputeShader.SetFloat("TerraformRadius", terraformRadius);
                // marchingCubes.terraformComputeShader.SetFloat("TerraformStrength", terraformStrength);
                // marchingCubes.terraformComputeShader.SetBool("TerraformMode", terraformMode);
                // marchingCubes.terraformComputeShader.SetInt("MaxWorldYChunks", ChunkGenNetwork.Instance.maxWorldYChunks);

                // marchingCubes.terraformComputeShader.Dispatch(terraformKernel, threadSizeX, threadSizeY, threadSizeZ);

                // int size = (terrainDensityData.width + 1) * (terrainDensityData.width + 1) * (terrainDensityData.width + 1);

                // marchingCubes.heightsBuffer.GetData(marchingCubes.heightsArray, 0, 0, size);

                NativeArray<float> heightsArray = new(marchingCubes.heightsArray, Allocator.Persistent);

                TerraformJob terraformJob = new TerraformJob
                {
                    heightsArray = heightsArray,
                    xSize = threadSizeX,
                    ySize = threadSizeY,
                    TerraformCenter = terraformCenter,
                    TerraformOffset = (Vector3)start,
                    TerraformRadius = terraformRadius,
                    TerraformStrength = terraformStrength,
                    chunkSize = terrainDensityData.width,
                    chunkPos = (Vector3)chunkPos,
                    terraformMode = terraformMode,
                };

                JobHandle terraformHandler = terraformJob.Schedule(threadSizeX * threadSizeY * threadSizeZ, 16);
                terraformHandler.Complete();

                marchingCubes.heightsArray = heightsArray.ToArray();
                heightsArray.Dispose();

                marchingCubes.MarchingCubesJobHandler(marchingCubes.heightsArray, true);
            }
        }
    }

    [BurstCompile]
    private struct TerraformJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<float> heightsArray;
        public int xSize;
        public int ySize;
        public float3 TerraformCenter;
        public float3 TerraformOffset;
        public float TerraformRadius;
        public float TerraformStrength;
        public int chunkSize;
        public float3 chunkPos;
        public bool terraformMode;
        public void Execute(int index)
        {
            int x = index % xSize;
            int y = index / xSize % ySize;
            int z = index / (xSize * ySize);
            float3 id = new float3(x, y, z);

            float3 localVoxelPos = TerraformOffset + id;

            if (localVoxelPos.x >= chunkSize + 1 || localVoxelPos.y >= chunkSize + 1 || localVoxelPos.z >= chunkSize + 1)
                return;
                
            float3 worldVoxelPos = localVoxelPos + chunkPos;
            float dstToCenter = math.length(worldVoxelPos - TerraformCenter);

            float density = heightsArray[FlattenIndex(localVoxelPos, chunkSize)];
            float strength = TerraformStrength * (float)(1.0 + math.abs(density / 3.0));
            float falloff = (float)(1.0 - (dstToCenter / TerraformRadius));

            if(dstToCenter < TerraformRadius) {
                if(terraformMode) {
                    heightsArray[FlattenIndex(localVoxelPos, chunkSize)] -= strength * falloff;
                }
                else if(!terraformMode) {
                    heightsArray[FlattenIndex(localVoxelPos, chunkSize)] += strength * falloff;
                }
            }
        }

        int FlattenIndex(float3 id, int size)
        {
            return (int)(id.z * (size + 1) * (size + 1) + id.y * (size + 1) + id.x);
        }
    }
}
