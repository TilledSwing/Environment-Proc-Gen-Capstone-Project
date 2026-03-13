using FishNet.Object;
using System.Collections;
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
    void OnCollisionEnter(Collision collision)
    {
        hit = true;
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
                Vector3Int hitChunkPos = new Vector3Int(Mathf.FloorToInt(terraformCenter.x / terrainDensityData.chunkSize), Mathf.FloorToInt(terraformCenter.y / terrainDensityData.chunkSize), Mathf.FloorToInt(terraformCenter.z / terrainDensityData.chunkSize)) * terrainDensityData.chunkSize;
                BombTerraformServer(terraformCenter, hitChunkPos);
            }
            yield return null;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void BombTerraformServer(Vector3 terraformCenter, Vector3Int hitChunkPos)
    {
        if (math.abs(terraformCenter.y - explosionRadius) >= terrainDensityData.chunkSize * ChunkGenNetwork.Instance.maxWorldYChunks)
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
    /// <summary>
    /// Handles bomb logic
    /// </summary>
    /// <param name="terraformCenter">Center point of the terraformation</param>
    /// <param name="hitChunkPos">>The position of the chunk that terraform center is</param>
    public void BombTerraformLocal(Vector3 terraformCenter, Vector3Int hitChunkPos)
    {
        Debug.LogWarning("BombTerraform called");
        ChunkGenNetwork.TerrainChunk[] chunkAndNeighbors = ChunkGenNetwork.Instance.GetChunkAndNeighbors(new Vector3Int(Mathf.CeilToInt(hitChunkPos.x / terrainDensityData.chunkSize), Mathf.CeilToInt(hitChunkPos.y / terrainDensityData.chunkSize), Mathf.CeilToInt(hitChunkPos.z / terrainDensityData.chunkSize)));
        foreach (ChunkGenNetwork.TerrainChunk terrainChunk in chunkAndNeighbors)
        {
            if (terrainChunk == null) continue;
            Bounds bounds = new Bounds(terrainChunk.chunkPos + (new Vector3(0.5f, 0.5f, 0.5f) * terrainDensityData.chunkSize), Vector3.one * terrainDensityData.chunkSize);
            if(bounds.SqrDistance(terraformCenter) <= explosionRadius * explosionRadius)
            {
                ComputeMarchingCubes marchingCubes = terrainChunk.marchingCubes;
                Vector3Int chunkPos = terrainChunk.chunkPos;
                Vector3Int radius = new Vector3Int(Mathf.CeilToInt(explosionRadius), Mathf.CeilToInt(explosionRadius), Mathf.CeilToInt(explosionRadius));
                Vector3Int start = Vector3Int.Max(Vector3Int.RoundToInt(terraformCenter) - radius - chunkPos, Vector3Int.zero);
                Vector3Int end = Vector3Int.Min(Vector3Int.RoundToInt(terraformCenter) + radius - chunkPos, new Vector3Int(Mathf.CeilToInt(terrainDensityData.chunkSize), Mathf.CeilToInt(terrainDensityData.chunkSize), Mathf.CeilToInt(terrainDensityData.chunkSize)));

                int threadSizeX = Mathf.CeilToInt((end.x - start.x) + 1f);
                int threadSizeY = Mathf.CeilToInt((end.y - start.y) + 1f);
                int threadSizeZ = Mathf.CeilToInt((end.z - start.z) + 1f);

                Terraforming.TerraformJob terraformJob = new Terraforming.TerraformJob
                {
                    heightsArray = marchingCubes.heightsArray,
                    xSize = threadSizeX,
                    ySize = threadSizeY,
                    TerraformCenter = terraformCenter,
                    TerraformOffset = (Vector3)start,
                    TerraformRadius = explosionRadius,
                    TerraformStrength = terraformStrength,
                    chunkSize = terrainDensityData.chunkSize,
                    chunkPos = (Vector3)chunkPos,
                    terraformMode = true,
                };

                JobHandle terraformHandler = terraformJob.Schedule(threadSizeX * threadSizeY * threadSizeZ, 16, terrainChunk.marchingCubes.marchingCubesJobHandler);
                terraformHandler.Complete();

                marchingCubes.MarchingCubesJobHandler(true);
                if (marchingCubes.grass != null && !marchingCubes.assetSpawner.emptyChunk)
                {
                    if (!marchingCubes.grass.isTerraforming)
                        marchingCubes.grass.UpdateGrass(terraformCenter, explosionRadius);
                }
            }
        }

        Collider[] colliders = Physics.OverlapSphere(terraformCenter, explosionRadius, assetLayer);
        foreach (Collider collider in colliders)
        {
            Destroy(collider.gameObject);
        }
    }
}
