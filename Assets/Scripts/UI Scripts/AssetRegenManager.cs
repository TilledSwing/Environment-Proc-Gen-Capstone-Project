using System.Collections;
using UnityEngine;

public class AssetRegenManager : MonoBehaviour
{
    AssetSpawnData assetSpawnData;
    void Start()
    {
        assetSpawnData = ChunkGenNetwork.Instance.assetSpawnData;
    }

    // Regenerate the assets with the current asset settings
    public void RespawnAssets()
    {
        StartCoroutine(AssetRespawnCoroutine());
    }

    public IEnumerator AssetRespawnCoroutine()
    {
        int count = 0;
        foreach(var terrainChunk in ChunkGenNetwork.Instance.chunksVisibleLastUpdate)
        {
            terrainChunk.marchingCubes.assetSpawner.SpawnAssets();

            if(++count % 12 == 0) 
                yield return null;
        }
    }
}
