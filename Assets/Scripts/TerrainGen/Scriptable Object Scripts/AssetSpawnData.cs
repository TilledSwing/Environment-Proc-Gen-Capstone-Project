using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AssetSpawnData", menuName = "Scriptable Objects/AssetSpawnData")]
public class AssetSpawnData : ScriptableObject
{
    public List<SpawnableAsset> spawnableAssets = new List<SpawnableAsset>();
    public Dictionary<Vector3Int, List<SpawnableAsset>> assets = new Dictionary<Vector3Int, List<SpawnableAsset>>();

    public void ResetSpawnPoints() {
        for(int i = 0; i < spawnableAssets.Count; i++) {
            spawnableAssets[i].spawnedAssets.Clear();
        }
    }
}
