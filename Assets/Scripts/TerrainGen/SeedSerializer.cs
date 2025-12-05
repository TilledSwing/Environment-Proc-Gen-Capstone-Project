using System;
using Unity.VisualScripting;
using UnityEngine;
using static NoiseGenerator;

/// <summary>
/// Struct of the TerrainDensityData Object class so it can be serialized over the network.
/// </summary>
[System.Serializable]
public struct TerrainSettings
{
    public NoiseGeneratorSettings[] noiseSettings;
    // Terrain Values
    public int width;
    public int height;
    public float isolevel;
    public int waterLevel;
    public bool lerp;
    public bool terracing;
    public int terraceHeight;
    public bool water;
}

/// <summary>
/// Struct of the NoiseGenerator Object class so it can be serialized over the network.
/// </summary>
[System.Serializable]
public struct NoiseGeneratorSettings
{
    public bool activated;
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
        var noiseGenSettings = new NoiseGeneratorSettings[settings.noiseGenerators.Length];
        for (int i = 0; i < noiseGenSettings.Length; i++)
        {
            noiseGenSettings[i] = SerializeNoiseDensity(settings.noiseGenerators[i]);
        }

        return new TerrainSettings
        {
            noiseSettings = noiseGenSettings,
            // Terrain Values
            width = settings.width,
            height = settings.height,
            isolevel = settings.isolevel,
            waterLevel = settings.waterLevel,
            lerp = settings.lerp,
            terracing = settings.terracing,
            terraceHeight = settings.terraceHeight,
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

    private static NoiseGeneratorSettings SerializeNoiseDensity(NoiseGenerator settings)
    {
        float[] textur = new float[0];
         try
        {
            textur = SplineCurveFunctions.CurveToArray(settings.valueCurve);
        }
        catch (Exception e)
        {
            textur = new float[0];
        }
        return new NoiseGeneratorSettings
        {
            activated = settings.activated,
           
            remoteTexture = textur,
            noiseGeneratorType = (int)settings.noiseGeneratorType,
            // Noise and Fractal Settings
            selectedNoiseDimension = settings.selectedNoiseDimension,
            noiseDimension = (int)settings.noiseDimension,
            selectedNoiseType = settings.selectedNoiseType,
            noiseType = (int)settings.noiseType,
            selectedNoiseFractalType = settings.selectedNoiseFractalType,
            noiseFractalType = (int)settings.noiseFractalType,
            selectedRotationType3D = settings.selectedRotationType3D,
            rotationType3D = (int)settings.rotationType3D,
            noiseSeed = settings.noiseSeed,
            noiseFractalOctaves = settings.noiseFractalOctaves,
            noiseFractalLacunarity = settings.noiseFractalLacunarity,
            noiseFractalGain = settings.noiseFractalGain,
            fractalWeightedStrength = settings.fractalWeightedStrength,
            noiseFrequency = settings.noiseFrequency,
            // Domain Warp Values
            domainWarpToggle = settings.domainWarpToggle,
            selectedDomainWarpType = settings.selectedDomainWarpType,
            domainWarpType = (int)settings.domainWarpType,
            selectedDomainWarpFractalType = settings.selectedDomainWarpFractalType,
            domainWarpFractalType = (int)settings.domainWarpFractalType,
            domainWarpAmplitude = settings.domainWarpAmplitude,
            domainWarpSeed = settings.domainWarpSeed,
            domainWarpFractalOctaves = settings.domainWarpFractalOctaves,
            domainWarpFractalLacunarity = settings.domainWarpFractalLacunarity,
            domainWarpFractalGain = settings.domainWarpFractalGain,
            domainWarpFrequency = settings.domainWarpFrequency,
            // Cellular(Voronoi) Values
            selectedCellularDistanceFunction = settings.selectedCellularDistanceFunction,
            cellularDistanceFunction = (int)settings.cellularDistanceFunction,
            selectedCellularReturnType = settings.selectedCellularReturnType,
            cellularReturnType = (int)settings.cellularReturnType,
            cellularJitter = settings.cellularJitter,
            // Terrain Values
            noiseScale = settings.noiseScale,
        };
    }

    public static TerrainDensityData DeserializeTerrainDensity(TerrainSettings settings)
    {
        var deserializedDensity = ScriptableObject.CreateInstance<TerrainDensityData>();

        var noiseGenerators = new NoiseGenerator[settings.noiseSettings.Length];
        for (int i = 0; i < noiseGenerators.Length; i++)
        {
            noiseGenerators[i] = DeserializeNoiseDensity(settings.noiseSettings[i]);
        }

        deserializedDensity.noiseGenerators = noiseGenerators;

        // Terrain Values
        deserializedDensity.width = settings.width;
        deserializedDensity.height = settings.height;
        deserializedDensity.isolevel = settings.isolevel;
        deserializedDensity.waterLevel = settings.waterLevel;
        deserializedDensity.lerp = settings.lerp;
        deserializedDensity.terracing = settings.terracing;
        deserializedDensity.terraceHeight = settings.terraceHeight;
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

    private static NoiseGenerator DeserializeNoiseDensity(NoiseGeneratorSettings settings)
    {
        var deserializedNoise = ScriptableObject.CreateInstance<NoiseGenerator>();

        deserializedNoise.activated = settings.activated;
        deserializedNoise.remoteTexture = SplineCurveFunctions.ArrayToTexture(settings.remoteTexture);
        deserializedNoise.noiseGeneratorType = (NoiseGeneratorType)settings.noiseGeneratorType;
        // Noise and Fractal Settings
        deserializedNoise.selectedNoiseDimension = settings.selectedNoiseDimension;
        deserializedNoise.noiseDimension = (fnl_noise_dimension)settings.noiseDimension;
        deserializedNoise.selectedNoiseType = settings.selectedNoiseType;
        deserializedNoise.noiseType = (fnl_noise_type)settings.noiseType;
        deserializedNoise.selectedNoiseFractalType = settings.selectedNoiseFractalType;
        deserializedNoise.noiseFractalType = (fnl_fractal_type)settings.noiseFractalType;
        deserializedNoise.selectedRotationType3D = settings.selectedRotationType3D;
        deserializedNoise.rotationType3D = (fnl_rotation_type_3d)settings.rotationType3D;
        deserializedNoise.noiseSeed = settings.noiseSeed;
        deserializedNoise.noiseFractalOctaves = settings.noiseFractalOctaves;
        deserializedNoise.noiseFractalLacunarity = settings.noiseFractalLacunarity;
        deserializedNoise.noiseFractalGain = settings.noiseFractalGain;
        deserializedNoise.fractalWeightedStrength = settings.fractalWeightedStrength;
        deserializedNoise.noiseFrequency = settings.noiseFrequency;
        // Domain Warp Values
        deserializedNoise.domainWarpToggle = settings.domainWarpToggle;
        deserializedNoise.selectedDomainWarpType = settings.selectedDomainWarpType;
        deserializedNoise.domainWarpType = (fnl_domain_warp_type)settings.domainWarpType;
        deserializedNoise.selectedDomainWarpFractalType = settings.selectedDomainWarpFractalType;
        deserializedNoise.domainWarpFractalType = (fnl_domain_warp_fractal_type)settings.domainWarpFractalType;
        deserializedNoise.domainWarpAmplitude = settings.domainWarpAmplitude;
        deserializedNoise.domainWarpSeed = settings.domainWarpSeed;
        deserializedNoise.domainWarpFractalOctaves = settings.domainWarpFractalOctaves;
        deserializedNoise.domainWarpFractalLacunarity = settings.domainWarpFractalLacunarity;
        deserializedNoise.domainWarpFractalGain = settings.domainWarpFractalGain;
        deserializedNoise.domainWarpFrequency = settings.domainWarpFrequency;
        // Cellular(Voronoi) Values
        deserializedNoise.selectedCellularDistanceFunction = settings.selectedCellularDistanceFunction;
        deserializedNoise.cellularDistanceFunction = (fnl_cellular_distance_func)settings.cellularDistanceFunction;
        deserializedNoise.selectedCellularReturnType = settings.selectedCellularReturnType;
        deserializedNoise.cellularReturnType = (fnl_cellular_return_type)settings.cellularReturnType;
        deserializedNoise.cellularJitter = settings.cellularJitter;
        // Terrain Values
        deserializedNoise.noiseScale = settings.noiseScale;

        return deserializedNoise;
    }
}
