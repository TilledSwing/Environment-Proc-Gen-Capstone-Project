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
    public int height = 80;
    public float noiseScale = 0.6f;
    public float isolevel = 0.5f;
    public bool lerp = true;



    // Default Values
        // Noise and Fractal Settings
    private NoiseDimension defaultNoiseDimension = NoiseDimension._3D;
    private FastNoiseLite.NoiseType defaultNoiseType = FastNoiseLite.NoiseType.OpenSimplex2;
    private FastNoiseLite.FractalType defaultNoiseFractalType = FastNoiseLite.FractalType.FBm;
    private int defaultNoiseSeed;
    private int defaultNoiseFractalOctaves = 5;
    private float defaultNoiseFractalLacunarity = 2;
    private float defaultNoiseFractalGain = 0.5f;
    private float defaultFractalWeightedStrength = 0;
    private float defaultNoiseFrequency = 0.01f;
    // Domain Warp Values
    private bool defaultDomainWarpToggle = false;
    private FastNoiseLite.DomainWarpType defaultDomainWarpType = FastNoiseLite.DomainWarpType.OpenSimplex2;
    private FastNoiseLite.FractalType defaultDomainWarpFractalType = FastNoiseLite.FractalType.None;
    private float defaultDomainWarpAmplitude = 1;
    private int defaultDomainWarpSeed;
    private int defaultDomainWarpFractalOctaves = 5;
    private float defaultDomainWarpFractalLacunarity = 2;
    private float defaultDomainWarpFractalGain = 0.5f;
    private float defaultDomainWarpFrequency = 0.01f;
    // Cellular(Voronoi) Values
    private FastNoiseLite.CellularDistanceFunction defaultCellularDistanceFunction = FastNoiseLite.CellularDistanceFunction.EuclideanSq;
    private FastNoiseLite.CellularReturnType defaultCellularReturnType = FastNoiseLite.CellularReturnType.Distance;
    private float defaultCellularJitter = 1;
    // Terrain Values
    private int defaultWidth = 200;
    private int defaultHeight = 80;
    private float defaultNoiseScale = 0.6f;
    private float defaultIsolevel = 0.5f;
    private bool defaultLerp = true;

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
