using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GenerationConfiguration", menuName = "Scriptable Objects/GenerationConfiguration")]
public class GenerationConfiguration : ScriptableObject
{
    public List<TerrainConfig> terrainConfigs;
}
[Serializable]
public class TerrainConfig
{
    public TerrainDensityData terrainDensityData;
    public TerrainTextureData terrainTextureData;
    public AssetSpawnData assetSpawnData;
}