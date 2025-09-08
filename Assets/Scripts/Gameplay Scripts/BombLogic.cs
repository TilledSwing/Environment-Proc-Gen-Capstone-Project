using System.Collections;
using UnityEngine;

public class BombLogic : MonoBehaviour
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
    /// <summary>
    /// Have the glow ball "stick" in place when it collides with something
    /// </summary>
    /// <param name="collision"></param>
    void OnCollisionEnter(Collision collision)
    {
        hit = true;
        StartCoroutine(DelayedExplosion(explosionDelay, explosionRadius));
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
                BombTerraform();
                Collider[] colliders = Physics.OverlapSphere(gameObject.transform.position, explosionRadius, assetLayer);
                foreach (Collider collider in colliders) {
                    Destroy(collider.gameObject);
                }
                Destroy(gameObject);
            }
            yield return null;
        }
    }
    void BombTerraform()
    {
        Vector3 terraformCenter = gameObject.transform.position;
        Vector3Int hitChunkPos = new Vector3Int(Mathf.FloorToInt(terraformCenter.x / terrainDensityData.width), Mathf.FloorToInt(terraformCenter.y / terrainDensityData.width), Mathf.FloorToInt(terraformCenter.z / terrainDensityData.width));
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

                marchingCubes.SyncMarchingCubes(marchingCubes.heightsBuffer, true);
            }
        }
    }
}
