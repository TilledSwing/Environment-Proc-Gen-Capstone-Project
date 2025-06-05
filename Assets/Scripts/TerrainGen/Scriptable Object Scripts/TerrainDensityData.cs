using UnityEngine;

[CreateAssetMenu(fileName = "TerrainDensityData", menuName = "Scriptable Objects/TerrainDensityData")]
public class TerrainDensityData : ScriptableObject
{
    public NoiseGenerator[] noiseGenerators;
    // // Noise and Fractal Settings
    // public int selectedNoiseDimension = 1;
    // public fnl_noise_dimension noiseDimension = fnl_noise_dimension._3D;
    // public int selectedNoiseType = 0;
    // public fnl_noise_type noiseType = fnl_noise_type.OpenSimplex2;
    // public int selectedNoiseFractalType = 1;
    // public fnl_fractal_type noiseFractalType =fnl_fractal_type.FBm;
    // public int selectedRotationType3D = 0;
    // public fnl_rotation_type_3d rotationType3D = fnl_rotation_type_3d.None;
    // public int noiseSeed;
    // public int noiseFractalOctaves = 5;
    // public float noiseFractalLacunarity = 2;
    // public float noiseFractalGain = 0.5f;
    // public float fractalWeightedStrength = 0;
    // public float noiseFrequency = 0.01f;
    //     // Domain Warp Values
    //     public bool domainWarpToggle = false;
    //     public int selectedDomainWarpType = 0;
    //     public fnl_domain_warp_type domainWarpType = fnl_domain_warp_type.OpenSimplex2;
    //     public int selectedDomainWarpFractalType = 0;
    //     public fnl_domain_warp_fractal_type domainWarpFractalType = fnl_domain_warp_fractal_type.None;
    //     public float domainWarpAmplitude = 1;
    //     public int domainWarpSeed;
    //     public int domainWarpFractalOctaves = 5;
    //     public float domainWarpFractalLacunarity = 2;
    //     public float domainWarpFractalGain = 0.5f;
    //     public float domainWarpFrequency = 0.01f;
    //     // Cellular(Voronoi) Values
    //     public int selectedCellularDistanceFunction = 1;
    //     public fnl_cellular_distance_func cellularDistanceFunction = fnl_cellular_distance_func.EuclideanSq;
    //     public int selectedCellularReturnType = 1;
    //     public fnl_cellular_return_type cellularReturnType = fnl_cellular_return_type.Distance;
    //     public float cellularJitter = 1;
    //     // Terrain Values
    //     public float noiseScale = 0.6f;
    // // Cave Noise and Fractal Settings
    // public int selectedCaveNoiseDimension = 1;
    // public fnl_noise_dimension caveNoiseDimension = fnl_noise_dimension._3D;
    // public int selectedCaveNoiseType = 0;
    // public fnl_noise_type caveNoiseType = fnl_noise_type.OpenSimplex2;
    // public int selectedCaveNoiseFractalType = 0;
    // public fnl_fractal_type caveNoiseFractalType =fnl_fractal_type.None;
    // public int selectedCaveRotationType3D = 0;
    // public fnl_rotation_type_3d caveRotationType3D = fnl_rotation_type_3d.None;
    // public int caveNoiseSeed;
    // public int caveNoiseFractalOctaves = 5;
    // public float caveNoiseFractalLacunarity = 2;
    // public float caveNoiseFractalGain = 0.5f;
    // public float caveFractalWeightedStrength = 0;
    // public float caveNoiseFrequency = 0.01f;
    //     // Domain Warp Values
    //     public bool caveDomainWarpToggle = false;
    //     public int caveSelectedDomainWarpType = 0;
    //     public fnl_domain_warp_type caveDomainWarpType = fnl_domain_warp_type.OpenSimplex2;
    //     public int caveSelectedDomainWarpFractalType = 0;
    //     public fnl_domain_warp_fractal_type caveDomainWarpFractalType = fnl_domain_warp_fractal_type.None;
    //     public float caveDomainWarpAmplitude = 1;
    //     public int caveDomainWarpSeed;
    //     public int caveDomainWarpFractalOctaves = 5;
    //     public float caveDomainWarpFractalLacunarity = 2;
    //     public float caveDomainWarpFractalGain = 0.5f;
    //     public float caveDomainWarpFrequency = 0.01f;
    //     // Cellular(Voronoi) Values
    //     public int caveSelectedCellularDistanceFunction = 1;
    //     public fnl_cellular_distance_func caveCellularDistanceFunction = fnl_cellular_distance_func.EuclideanSq;
    //     public int caveSelectedCellularReturnType = 1;
    //     public fnl_cellular_return_type caveCellularReturnType = fnl_cellular_return_type.Distance;
    //     public float caveCellularJitter = 1;
    //     // Terrain Values
    //     public float caveNoiseScale = 0.6f;
    // Terrain Values
    public int width = 24;
    public int height = 100;
    public float isolevel = 0.5f;
    public int waterLevel = 35;
    public bool lerp = true;
    public bool terracing = false;
    public int terraceHeight = 2;
    // public enum fnl_noise_dimension {
    //     _2D = 0,
    //     _3D = 1
    // }
    // public enum fnl_noise_type {
    //     OpenSimplex2 = 0,
    //     OpenSimplex2S = 1,
    //     Cellular = 2,
    //     Perlin = 3,
    //     ValueCubic = 4,
    //     Value = 5
    // };
    // public enum fnl_rotation_type_3d {
    //     None = 0,
    //     ImproveXYPlanes = 1,
    //     ImproveXZPlanes = 2
    // };
    // public enum fnl_fractal_type {
    //     None = 0,
    //     FBm = 1,
    //     Ridged = 2,
    //     PingPong = 3
    // };
    // // Domain Warp Values
    // public enum fnl_domain_warp_type {
    //     OpenSimplex2 = 0,
    //     OpenSimplex2Reduced = 1,
    //     BasicGrid = 2
    // };
    // public enum fnl_domain_warp_fractal_type {
    //     None = 0,
    //     DomainWarpProgressive = 4,
    //     DomainWarpIndependent = 5
    // };
    // // Cellular Values
    // public enum fnl_cellular_distance_func {
    //     Euclidean = 0,
    //     EuclideanSq = 1,
    //     Manhattan = 2,
    //     Hybrid = 3
    // };
    // public enum fnl_cellular_return_type {
    //     CellValue = 0,
    //     Distance = 1,
    //     Distance2 = 2,
    //     Distance2Add = 3,
    //     Distance2Sub = 4,
    //     Distance2Mul = 5,
    //     Distance2Div = 6
    // };
}
