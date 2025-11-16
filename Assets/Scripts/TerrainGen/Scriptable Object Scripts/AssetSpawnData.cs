using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AssetSpawnData", menuName = "Scriptable Objects/AssetSpawnData")]
public class AssetSpawnData : ScriptableObject
{
    public List<SpawnableAsset> spawnableAssets = new();
    public Dictionary<Vector3Int, List<ComputeMarchingCubes.Vertex>> assets = new();

    public void ResetSpawnPoints() {
        for (int i = 0; i < spawnableAssets.Count; i++)
        {
            spawnableAssets[i].spawnedAssets.Clear();
            assets.Clear();
        }
    }
}
