using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AssetSpawnData", menuName = "Scriptable Objects/AssetSpawnData")]
public class AssetSpawnData : ScriptableObject
{
    private List<SpawnableAsset> spawnableAssetsBackup = new();
    public List<SpawnableAsset> spawnableAssets;
    public Dictionary<Vector3Int, List<ComputeMarchingCubes.Vertex>> assets = new();

    public void ResetSpawnPoints() {
        for (int i = 0; i < spawnableAssets.Count; i++)
        {
            spawnableAssets[i].spawnedAssets.Clear();
            assets.Clear();
        }
    }

    public void BackupOriginalState()
    {
        spawnableAssetsBackup = new();
        foreach(SpawnableAsset spawnableAsset in spawnableAssets)
        {
            spawnableAssetsBackup.Add(spawnableAsset.Clone());
        }
    }

    public void RestoreToOriginalState()
    {
        if (spawnableAssetsBackup.Count == 0)
        {
            return;
        }
        
        spawnableAssets = new();

        foreach (SpawnableAsset spawnableAsset in spawnableAssetsBackup)
        {
            spawnableAssets.Add(spawnableAsset.Clone());
        }
        spawnableAssetsBackup.Clear();
    }
}