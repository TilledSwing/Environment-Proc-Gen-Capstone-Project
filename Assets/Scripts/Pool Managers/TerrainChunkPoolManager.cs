using System.Collections.Generic;
using UnityEngine;
using static ChunkGenNetwork;

public class TerrainChunkPoolManager : MonoBehaviour
{
    public TerrainChunkPoolManager Instance;
    Queue<TerrainChunk> nonWaterChunkPool = new();
    Queue<TerrainChunk> waterChunkPool = new();
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    public TerrainChunk GetChunk(long packedCoord, Vector3Int chunkCoord, Vector3Int chunkPos, bool waterChunk)
    {
        TerrainChunk chunk;
        if (waterChunk)
        {
            if (waterChunkPool.Count == 0)
            {
                chunk = new();
                chunk.StartChunk(packedCoord, chunkCoord, chunkPos, waterChunk);
            }
            else
            {
                chunk = waterChunkPool.Dequeue();
                chunk.RenewChunk(packedCoord, chunkCoord, chunkPos, waterChunk);
            }
        }
        else
        {
            if (nonWaterChunkPool.Count == 0)
            {
                chunk = new();
                chunk.StartChunk(packedCoord, chunkCoord, chunkPos, waterChunk);
            }
            else
            {
                chunk = nonWaterChunkPool.Dequeue();
                chunk.RenewChunk(packedCoord, chunkCoord, chunkPos, waterChunk);
            }
        }
        return chunk;
    }
    public void ReturnChunk(TerrainChunk chunk, bool waterChunk)
    {
        chunk.ClearChunk();
        if (waterChunk)
            waterChunkPool.Enqueue(chunk);
        else
            nonWaterChunkPool.Enqueue(chunk);
    }
}
