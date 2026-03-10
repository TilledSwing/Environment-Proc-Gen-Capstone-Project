using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Struct of the TerrainDensityData Object class so it can be serialized over the network.
/// </summary>
[System.Serializable]
public struct TerrainSettings
{
    // Terrain Values
    public int width;
    public int height;
    public float isolevel;
    public int waterLevel;
    public bool lerp;
    public bool water;
}

/// <summary>
/// Struct of the NoiseGenerator Object class so it can be serialized over the network.
/// </summary>
[System.Serializable]
public struct NoiseGeneratorSettings
{
    public float[] remoteTexture;
    public int noiseGeneratorType;
    // Noise and Fractal Settings
    public int selectedNoiseDimension;
    public int noiseDimension;
    public int selectedNoiseType;
    public int noiseType;
    public int selectedNoiseFractalType;
    public int noiseFractalType;
    public int selectedRotationType3D;
    public int rotationType3D;
    public int noiseSeed;
    public int noiseFractalOctaves;
    public float noiseFractalLacunarity;
    public float noiseFractalGain;
    public float fractalWeightedStrength;
    public float noiseFrequency;
    // Domain Warp Values
    public bool domainWarpToggle;
    public int selectedDomainWarpType;
    public int domainWarpType;
    public int selectedDomainWarpFractalType;
    public int domainWarpFractalType;
    public float domainWarpAmplitude;
    public int domainWarpSeed;
    public int domainWarpFractalOctaves;
    public float domainWarpFractalLacunarity;
    public float domainWarpFractalGain;
    public float domainWarpFrequency;
    // Cellular(Voronoi) Values
    public int selectedCellularDistanceFunction;
    public int cellularDistanceFunction;
    public int selectedCellularReturnType;
    public int cellularReturnType;
    public float cellularJitter;
    // Terrain Values
    public float noiseScale;
}

/// <summary>
/// Struct of the NoiseGenerator Object class so it can be serialized over the network.
/// </summary>
[System.Serializable]
public struct AssetSpawnSettings
{
    public int maxPerChunk;
    public bool rotateToFaceNormal;
    public float spawnProbability;
    public bool useMinSlope;
    public int minSlope;
    public bool useMaxSlope;
    public int maxSlope;
    public bool useMinHeight;
    public int minHeight;
    public bool useMaxHeight;
    public int maxHeight;
    public bool underwaterAsset;
    public float minDepth;
    public bool undergroundAsset;
    public float minDensity;
    public bool isValuable;
    public int minValue;
    public int maxValue;
}

public static class SeedSerializer
{
    public static TerrainSettings SerializeTerrainDensity(TerrainDensityData settings)
    {
        return new TerrainSettings
        {
            // Terrain Values
            width = settings.chunkSize,
            isolevel = settings.isolevel,
            waterLevel = settings.waterLevel,
            lerp = settings.lerp,
            water = settings.water
        };
    }

    public static AssetSpawnSettings[] SerializeAssetData(AssetSpawnData assetData)
    {
        AssetSpawnSettings[] spawnSettings = new AssetSpawnSettings[assetData.spawnableAssets.Count];

        for (int i = 0; i < spawnSettings.Length; i++)
        {
            var scriptableAssetData = assetData.spawnableAssets[i];
            AssetSpawnSettings serializedSettings = new AssetSpawnSettings
            {
                maxPerChunk = scriptableAssetData.maxPerChunk,
                rotateToFaceNormal = scriptableAssetData.rotateToFaceNormal,
                spawnProbability = scriptableAssetData.spawnProbability,
                useMinSlope = scriptableAssetData.useMinSlope,
                minSlope = scriptableAssetData.minSlope,
                useMaxSlope = scriptableAssetData.useMaxSlope,
                maxSlope = scriptableAssetData.maxSlope,
                useMinHeight = scriptableAssetData.useMinHeight,
                minHeight = scriptableAssetData.minHeight,
                useMaxHeight = scriptableAssetData.useMaxHeight,
                maxHeight = scriptableAssetData.maxHeight,
                underwaterAsset = scriptableAssetData.underwaterAsset,
                minDepth = scriptableAssetData.minDepth,
                undergroundAsset = scriptableAssetData.undergroundAsset,
                minDensity = scriptableAssetData.minDensity,
                isValuable = scriptableAssetData.isValuable,
                minValue = scriptableAssetData.minValue,
                maxValue = scriptableAssetData.maxValue
            };

            spawnSettings[i] = serializedSettings;
        }

        return spawnSettings;
    }

    public static TerrainDensityData DeserializeTerrainDensity(TerrainSettings settings)
    {
        var deserializedDensity = ScriptableObject.CreateInstance<TerrainDensityData>();

        // Terrain Values
        deserializedDensity.chunkSize = settings.width;
        deserializedDensity.isolevel = settings.isolevel;
        deserializedDensity.waterLevel = settings.waterLevel;
        deserializedDensity.lerp = settings.lerp;
        deserializedDensity.water = settings.water;

        return deserializedDensity;
    }

    public static void DeserializeAndUpdateAssetData(AssetSpawnData assetData, AssetSpawnSettings[] settings)
    {
        for (int i = 0; i < settings.Length; i++)
        {
            assetData.spawnableAssets[i].maxPerChunk = settings[i].maxPerChunk;
            assetData.spawnableAssets[i].rotateToFaceNormal = settings[i].rotateToFaceNormal;
            assetData.spawnableAssets[i].spawnProbability = settings[i].spawnProbability;
            assetData.spawnableAssets[i].useMinSlope = settings[i].useMinSlope;
            assetData.spawnableAssets[i].minSlope = settings[i].minSlope;
            assetData.spawnableAssets[i].useMaxSlope = settings[i].useMaxSlope;
            assetData.spawnableAssets[i].maxSlope = settings[i].maxSlope;
            assetData.spawnableAssets[i].useMinHeight = settings[i].useMinHeight;
            assetData.spawnableAssets[i].minHeight = settings[i].minHeight;
            assetData.spawnableAssets[i].useMaxHeight = settings[i].useMaxHeight;
            assetData.spawnableAssets[i].maxHeight = settings[i].maxHeight;
            assetData.spawnableAssets[i].underwaterAsset = settings[i].underwaterAsset;
            assetData.spawnableAssets[i].minDepth = settings[i].minDepth;
            assetData.spawnableAssets[i].undergroundAsset = settings[i].undergroundAsset;
            assetData.spawnableAssets[i].minDensity = settings[i].minDensity;
            assetData.spawnableAssets[i].isValuable = settings[i].isValuable;
            assetData.spawnableAssets[i].minValue = settings[i].minValue;
            assetData.spawnableAssets[i].maxValue = settings[i].maxValue;
        }
    }
}
