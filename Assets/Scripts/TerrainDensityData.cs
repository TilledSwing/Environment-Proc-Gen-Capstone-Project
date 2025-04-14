using UnityEngine;

[CreateAssetMenu(fileName = "TerrainDensityData", menuName = "Scriptable Objects/TerrainDensityData")]
public class TerrainDensityData : ScriptableObject
{
    public enum NoiseDimension {
        _2D,
        _3D
    }
    // Noise and Fractal Settings
    public NoiseDimension noiseDimension = NoiseDimension._3D;
    public FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.OpenSimplex2;
    public FastNoiseLite.FractalType noiseFractalType = FastNoiseLite.FractalType.FBm;
    public int noiseSeed;
    public int noiseFractalOctaves = 5;
    public float noiseFractalLacunarity = 2;
    public float noiseFractalGain = 0.5f;
    public float fractalWeightedStrength = 0;
    public float noiseFrequency = 0.01f;
    // Domain Warp Values
    public bool domainWarpToggle = false;
    public FastNoiseLite.DomainWarpType domainWarpType = FastNoiseLite.DomainWarpType.OpenSimplex2;
    public FastNoiseLite.FractalType domainWarpFractalType = FastNoiseLite.FractalType.None;
    public float domainWarpAmplitude = 1;
    public int domainWarpSeed;
    public int domainWarpFractalOctaves = 5;
    public float domainWarpFractalLacunarity = 2;
    public float domainWarpFractalGain = 0.5f;
    public float domainWarpFrequency = 0.01f;
    // Cellular(Voronoi) Values
    public FastNoiseLite.CellularDistanceFunction cellularDistanceFunction = FastNoiseLite.CellularDistanceFunction.EuclideanSq;
    public FastNoiseLite.CellularReturnType cellularReturnType = FastNoiseLite.CellularReturnType.Distance;
    public float cellularJitter = 1;
    // Terrain Values
    public int width = 200;
    public int height = 50;
    public float noiseScale = 0.6f;
    public float isolevel = 5f;
    public bool lerp = true;



    // Default Values
        // Noise and Fractal Settings
    public NoiseDimension defaultNoiseDimension = NoiseDimension._3D;
    public FastNoiseLite.NoiseType defaultNoiseType = FastNoiseLite.NoiseType.OpenSimplex2;
    public FastNoiseLite.FractalType defaultNoiseFractalType = FastNoiseLite.FractalType.FBm;
    public int defaultNoiseSeed;
    public int defaultNoiseFractalOctaves = 5;
    public float defaultNoiseFractalLacunarity = 2;
    public float defaultNoiseFractalGain = 0.5f;
    public float defaultFractalWeightedStrength = 0;
    public float defaultNoiseFrequency = 0.01f;
    // Domain Warp Values
    public bool defaultDomainWarpToggle = false;
    public FastNoiseLite.DomainWarpType defaultDomainWarpType = FastNoiseLite.DomainWarpType.OpenSimplex2;
    public FastNoiseLite.FractalType defaultDomainWarpFractalType = FastNoiseLite.FractalType.None;
    public float defaultDomainWarpAmplitude = 1;
    public int defaultDomainWarpSeed;
    public int defaultDomainWarpFractalOctaves = 5;
    public float defaultDomainWarpFractalLacunarity = 2;
    public float defaultDomainWarpFractalGain = 0.5f;
    public float defaultDomainWarpFrequency = 0.01f;
    // Cellular(Voronoi) Values
    public FastNoiseLite.CellularDistanceFunction defaultCellularDistanceFunction = FastNoiseLite.CellularDistanceFunction.EuclideanSq;
    public FastNoiseLite.CellularReturnType defaultCellularReturnType = FastNoiseLite.CellularReturnType.Distance;
    public float defaultCellularJitter = 1;
    // Terrain Values
    public int defaultWidth = 200;
    public int defaultHeight = 50;
    public float defaultNoiseScale = 0.6f;
    public float defaultIsolevel = 5f;
    public bool defaultLerp = true;

    public void ResetToDefault() {
        // Noise and Fractal Settings
        noiseDimension = defaultNoiseDimension;
        noiseType = defaultNoiseType;
        noiseFractalType = defaultNoiseFractalType;
        noiseSeed = defaultNoiseSeed;
        noiseFractalOctaves = defaultNoiseFractalOctaves;
        noiseFractalLacunarity = defaultNoiseFractalLacunarity;
        noiseFractalGain = defaultNoiseFractalGain;
        fractalWeightedStrength = defaultFractalWeightedStrength;
        noiseFrequency = defaultNoiseFrequency;
        // Domain Warp Values
        domainWarpToggle = defaultDomainWarpToggle;
        domainWarpType = defaultDomainWarpType;
        domainWarpFractalType = defaultDomainWarpFractalType;
        domainWarpAmplitude = defaultDomainWarpAmplitude;
        domainWarpSeed = defaultDomainWarpSeed;
        domainWarpFractalOctaves = defaultDomainWarpFractalOctaves;
        domainWarpFractalLacunarity = defaultDomainWarpFractalLacunarity;
        domainWarpFractalGain = defaultDomainWarpFractalGain;
        domainWarpFrequency = defaultDomainWarpFrequency;
        // Cellular(Voronoi) Values
        cellularDistanceFunction = defaultCellularDistanceFunction;
        cellularReturnType = defaultCellularReturnType;
        cellularJitter = defaultCellularJitter;
        // Terrain Values
        width = defaultWidth;
        height = defaultHeight;
        noiseScale = defaultNoiseScale;
        isolevel = defaultIsolevel;
        lerp = defaultLerp;
    }
}
