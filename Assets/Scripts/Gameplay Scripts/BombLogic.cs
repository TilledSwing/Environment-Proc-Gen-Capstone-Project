using FishNet.Connection;
using FishNet.Example.ColliderRollbacks;
using FishNet.Object;
using System.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class BombLogic : NetworkBehaviour
{
    public float maxAirTime = 6f;
    public float explosionDelay = 3f;
    public float explosionRadius = 10f;
    public TerrainDensityData terrainDensityData;
    public float terraformStrength = 5f;
    public LayerMask assetLayer;
    private float creationTime;
    private bool hit = false;
    void Start()
    {
        creationTime = Time.time;
        StartCoroutine(DelayedExplosion(explosionDelay, explosionRadius));
    }

    //public override void OnStartClient()
    //{
    //    base.OnStartClient();
    //    if (!base.IsOwner)
    //        this.enabled = false;
    //}

    void Update()
    {
        if (!hit)
        {
            CheckTime();
        }
    }
    void CheckTime()
    {
        float timeExisted = Time.time - creationTime;
        if (timeExisted >= maxAirTime)
        {
            Destroy(gameObject);
        }
    }
    /// <summary>
    /// Have the glow ball "stick" in place when it collides with something
    /// </summary>
    /// <param name="collision"></param>
    void OnCollisionEnter(Collision collision)
    {
        hit = true;
        // StartCoroutine(DelayedExplosion(explosionDelay, explosionRadius));
    }
    IEnumerator DelayedExplosion(float explosionDelay, float explosionRadius)
    {
        float t = 0;
        while (t < explosionDelay)
        {
            t += Time.deltaTime;
            float currentTime = Mathf.Clamp01(t / explosionDelay);
            if (currentTime >= 1)
            {
                Vector3 terraformCenter = gameObject.transform.position;
                Vector3Int hitChunkPos = new Vector3Int(Mathf.FloorToInt(terraformCenter.x / terrainDensityData.width), Mathf.FloorToInt(terraformCenter.y / terrainDensityData.width), Mathf.FloorToInt(terraformCenter.z / terrainDensityData.width)) * terrainDensityData.width;
                BombTerraformServer(terraformCenter, hitChunkPos);

                Collider[] colliders = Physics.OverlapSphere(gameObject.transform.position, explosionRadius, assetLayer);
                foreach (Collider collider in colliders) {
                    Destroy(collider.gameObject);
                }
            }
            yield return null;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void BombTerraformServer(Vector3 terraformCenter, Vector3Int hitChunkPos)
    {
        if (math.abs(terraformCenter.y - explosionRadius) >= terrainDensityData.width * ChunkGenNetwork.Instance.maxWorldYChunks)
        {
            ServerManager.Despawn(gameObject);
            return;
        }

        PlayerController.instance.terraformCenters.Add(terraformCenter);
        PlayerController.instance.hitChunkPositions.Add(hitChunkPos);
        PlayerController.instance.terraformTypes.Add(0);

        BombTerraform(terraformCenter, hitChunkPos);
        ServerManager.Despawn(gameObject);
    }

    [ObserversRpc]
    public void BombTerraform(Vector3 terraformCenter, Vector3Int hitChunkPos)
    {                                          
        SoundManager.Instance.PlaySFXAtPoint("BombExplosion", terraformCenter);
        BombTerraformLocal(terraformCenter, hitChunkPos);
    }

    public void BombTerraformLocal(Vector3 terraformCenter, Vector3Int hitChunkPos)
    {
        Debug.LogWarning("BombTerraform called");
        ChunkGenNetwork.TerrainChunk[] chunkAndNeighbors = ChunkGenNetwork.Instance.GetChunkAndNeighbors(new Vector3Int(Mathf.CeilToInt(hitChunkPos.x / terrainDensityData.width), Mathf.CeilToInt(hitChunkPos.y / terrainDensityData.width), Mathf.CeilToInt(hitChunkPos.z / terrainDensityData.width)));
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

                // int terraformKernel = marchingCubes.terraformComputeShader.FindKernel("Terraform");
                // marchingCubes.terraformComputeShader.SetBuffer(terraformKernel, "HeightsBuffer", marchingCubes.heightsBuffer);
                // marchingCubes.terraformComputeShader.SetInt("ChunkSize", terrainDensityData.width);
                // marchingCubes.terraformComputeShader.SetVector("ChunkPos", (Vector3)chunkPos);
                // marchingCubes.terraformComputeShader.SetVector("TerraformOffset", (Vector3)start);
                // marchingCubes.terraformComputeShader.SetVector("TerraformCenter", terraformCenter);
                // marchingCubes.terraformComputeShader.SetFloat("TerraformRadius", explosionRadius);
                // marchingCubes.terraformComputeShader.SetFloat("TerraformStrength", terraformStrength);
                // marchingCubes.terraformComputeShader.SetBool("TerraformMode", true);
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
                    TerraformRadius = explosionRadius,
                    TerraformStrength = terraformStrength,
                    chunkSize = terrainDensityData.width,
                    chunkPos = (Vector3)chunkPos,
                    terraformMode = true,
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
