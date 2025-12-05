using System.Collections;
using UnityEngine;

public class AssetIndexUpdater : MonoBehaviour
{
    public void UpdateAllIndices()
    {
        int count = 0;
        foreach(Transform child in transform)
        {
            child.GetComponent<AssetSettingsTabController>().assetIndex = child.GetSiblingIndex();
            count++;
        }
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