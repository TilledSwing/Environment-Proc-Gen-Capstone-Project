using System;
using JetBrains.Annotations;
using UnityEngine;

public class Terraforming : MonoBehaviour
{
    Camera playerCamera;
    public float terraformMaxDst = 20f;
    public float terraformRadius = 5f;
    public float terraformStrength = 5f;
    bool mode = true;
    LayerMask terrainLayer;
    public TerrainDensityData1 terrainDensityData;
    void Start()
    {
        playerCamera = Camera.main;
        terrainLayer = LayerMask.GetMask("Terrain Layer");
        // terrainDensityData = Resources.Load<TerrainDensityData1>("TerrainDensityData1");
        // chunkGen = FindFirstObjectByType<ChunkGenNetwork>();
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            mode = true;
            Terraform(mode);
        }
        if (Input.GetMouseButton(1))
        {
            mode = false;
            Terraform(mode);
        }
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
            if (hit.distance <= 1.1f || Mathf.Abs(hit.point.y) >= (terrainDensityData.width * 10 - 5))
            {
                return;
            }
            Vector3 terraformCenter = hit.point;
            GameObject hitChunk = hit.collider.gameObject;
            ComputeMarchingCubes hitMarchingCubes = hitChunk.GetComponent<ComputeMarchingCubes>();
            Vector3Int hitChunkPos = hitMarchingCubes.chunkPos;
            ChunkGenNetwork.TerrainChunk[] chunkAndNeighbors = ChunkGenNetwork.Instance.GetChunkAndNeighbors(new Vector3Int(Mathf.CeilToInt(hitChunkPos.x / terrainDensityData.width), Mathf.CeilToInt(hitChunkPos.y / terrainDensityData.width), Mathf.CeilToInt(hitChunkPos.z / terrainDensityData.width)));
            foreach (ChunkGenNetwork.TerrainChunk terrainChunk in chunkAndNeighbors)
            {
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
                    marchingCubes.terraformComputeShader.SetBool("TerraformMode", mode);

                    marchingCubes.terraformComputeShader.Dispatch(terraformKernel, threadSizeX, threadSizeY, threadSizeZ);

                    marchingCubes.SyncMarchingCubes(marchingCubes.heightsBuffer, true);
                }
            }
        }
    }
}
