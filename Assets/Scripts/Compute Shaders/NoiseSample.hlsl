#include "/FastNoiseLite.hlsl"

struct NoiseSettingsGPU
{
    // Noise and Fractal Values
    float noiseScale;
    int noiseDimension;
    int noiseType;
    int noiseFractalType;
    int rotationType3D;
    int noiseSeed;
    int noiseFractalOctaves;
    float noiseFractalLacunarity;
    float noiseFractalGain;
    float fractalWeightedStrength;
    float noiseFrequency;
    // Domain Warp Values
    int domainWarpToggle;
    int domainWarpType;
    int domainWarpFractalType;
    float domainWarpAmplitude;
    int domainWarpSeed;
    int domainWarpFractalOctaves;
    float domainWarpFractalLacunarity;
    float domainWarpFractalGain;
    float domainWarpFrequency;
    // Cellular(Voronoi) Values
    int cellularDistanceFunction;
    int cellularReturnType;
    float cellularJitter;
};
float SampleNoise(NoiseSettingsGPU noiseSettings, float3 id, float3 chunkPos) {
    float x = (id.x + chunkPos.x) * noiseSettings.noiseScale;
    float y = (id.y + chunkPos.y) * noiseSettings.noiseScale;
    float z = (id.z + chunkPos.z) * noiseSettings.noiseScale;
    fnl_state noiseGenerator = fnlCreateState();
    // Domain Warp Values
    if(noiseSettings.domainWarpToggle == 1) {
        noiseGenerator.domain_warp_type = noiseSettings.domainWarpType;
        noiseGenerator.fractal_type = noiseSettings.domainWarpFractalType;
        noiseGenerator.domain_warp_amp = noiseSettings.domainWarpAmplitude;
        noiseGenerator.seed = noiseSettings.domainWarpSeed;
        noiseGenerator.octaves = noiseSettings.domainWarpFractalOctaves;
        noiseGenerator.lacunarity = noiseSettings.domainWarpFractalLacunarity;
        noiseGenerator.gain = noiseSettings.domainWarpFractalGain;
        noiseGenerator.frequency = noiseSettings.domainWarpFrequency;
        if(noiseSettings.noiseDimension == 0) 
        {
            fnlDomainWarp2D(noiseGenerator, x, z);
        }
        else
        {
            fnlDomainWarp3D(noiseGenerator, x, y, z);
        }
    }
    // Noise Values
    noiseGenerator.noise_type = noiseSettings.noiseType;
    noiseGenerator.fractal_type = noiseSettings.noiseFractalType;
    noiseGenerator.rotation_type_3d = noiseSettings.rotationType3D;
    noiseGenerator.seed = noiseSettings.noiseSeed;
    noiseGenerator.octaves = noiseSettings.noiseFractalOctaves;
    noiseGenerator.lacunarity = noiseSettings.noiseFractalLacunarity;
    noiseGenerator.gain = noiseSettings.noiseFractalGain;
    noiseGenerator.weighted_strength = noiseSettings.fractalWeightedStrength;
    noiseGenerator.frequency = noiseSettings.noiseFrequency;
    noiseGenerator.cellular_distance_func = noiseSettings.cellularDistanceFunction;
    noiseGenerator.cellular_return_type = noiseSettings.cellularReturnType;
    noiseGenerator.cellular_jitter_mod = noiseSettings.cellularJitter;

    // Get the noise value
    float noise;
    if(noiseSettings.noiseDimension == 0) 
    {
        noise = fnlGetNoise2D(noiseGenerator, x, z);
    }
    else 
    {
        noise = fnlGetNoise3D(noiseGenerator, x, y, z);
    }
    return noise;
}
// float SampleNoise2D(NoiseSettingsGPU noiseSettings, float3 id, float3 chunkPos) {
//     float x = (id.x + chunkPos.x) * noiseSettings.noiseScale;
//     float y = (id.y + chunkPos.y) * noiseSettings.noiseScale;
//     float z = (id.z + chunkPos.z) * noiseSettings.noiseScale;
//     fnl_state noiseGenerator = fnlCreateState();
//     // Noise Values
//     noiseGenerator.noise_type = noiseSettings.noiseType;
//     noiseGenerator.fractal_type = noiseSettings.noiseFractalType;
//     noiseGenerator.rotation_type_3d = noiseSettings.rotationType3D;
//     noiseGenerator.seed = noiseSettings.noiseSeed;
//     noiseGenerator.octaves = noiseSettings.noiseFractalOctaves;
//     noiseGenerator.lacunarity = noiseSettings.noiseFractalLacunarity;
//     noiseGenerator.gain = noiseSettings.noiseFractalGain;
//     noiseGenerator.weighted_strength = noiseSettings.fractalWeightedStrength;
//     noiseGenerator.frequency = noiseSettings.noiseFrequency;
//     noiseGenerator.cellular_distance_func = noiseSettings.cellularDistanceFunction;
//     noiseGenerator.cellular_return_type = noiseSettings.cellularReturnType;
//     noiseGenerator.cellular_jitter_mod = noiseSettings.cellularJitter;

//     // Get the noise value
//     float noise = fnlGetNoise2D(noiseGenerator, x, z);
//     return noise;
// }
// float SampleNoise3D(NoiseSettingsGPU noiseSettings, float3 id, float3 chunkPos) {
//     float x = (id.x + chunkPos.x) * noiseSettings.noiseScale;
//     float y = (id.y + chunkPos.y) * noiseSettings.noiseScale;
//     float z = (id.z + chunkPos.z) * noiseSettings.noiseScale;
//     fnl_state noiseGenerator = fnlCreateState();
//     // Noise Values
//     noiseGenerator.noise_type = noiseSettings.noiseType;
//     noiseGenerator.fractal_type = noiseSettings.noiseFractalType;
//     noiseGenerator.rotation_type_3d = noiseSettings.rotationType3D;
//     noiseGenerator.seed = noiseSettings.noiseSeed;
//     noiseGenerator.octaves = noiseSettings.noiseFractalOctaves;
//     noiseGenerator.lacunarity = noiseSettings.noiseFractalLacunarity;
//     noiseGenerator.gain = noiseSettings.noiseFractalGain;
//     noiseGenerator.weighted_strength = noiseSettings.fractalWeightedStrength;
//     noiseGenerator.frequency = noiseSettings.noiseFrequency;
//     noiseGenerator.cellular_distance_func = noiseSettings.cellularDistanceFunction;
//     noiseGenerator.cellular_return_type = noiseSettings.cellularReturnType;
//     noiseGenerator.cellular_jitter_mod = noiseSettings.cellularJitter;

//     // Get the noise value
//     float noise = fnlGetNoise3D(noiseGenerator, x, y, z);
//     return noise;
// }
// float SampleNoiseDW2D(NoiseSettingsGPU noiseSettings, float3 id, float3 chunkPos) {
//     float x = (id.x + chunkPos.x) * noiseSettings.noiseScale;
//     float y = (id.y + chunkPos.y) * noiseSettings.noiseScale;
//     float z = (id.z + chunkPos.z) * noiseSettings.noiseScale;
//     fnl_state noiseGenerator = fnlCreateState();
//     // Domain Warp Values
//     noiseGenerator.domain_warp_type = noiseSettings.domainWarpType;
//     noiseGenerator.fractal_type = noiseSettings.domainWarpFractalType;
//     noiseGenerator.domain_warp_amp = noiseSettings.domainWarpAmplitude;
//     noiseGenerator.seed = noiseSettings.domainWarpSeed;
//     noiseGenerator.octaves = noiseSettings.domainWarpFractalOctaves;
//     noiseGenerator.lacunarity = noiseSettings.domainWarpFractalLacunarity;
//     noiseGenerator.gain = noiseSettings.domainWarpFractalGain;
//     noiseGenerator.frequency = noiseSettings.domainWarpFrequency;
//     fnlDomainWarp2D(noiseGenerator, x, z);
//     // Noise Values
//     noiseGenerator.noise_type = noiseSettings.noiseType;
//     noiseGenerator.fractal_type = noiseSettings.noiseFractalType;
//     noiseGenerator.rotation_type_3d = noiseSettings.rotationType3D;
//     noiseGenerator.seed = noiseSettings.noiseSeed;
//     noiseGenerator.octaves = noiseSettings.noiseFractalOctaves;
//     noiseGenerator.lacunarity = noiseSettings.noiseFractalLacunarity;
//     noiseGenerator.gain = noiseSettings.noiseFractalGain;
//     noiseGenerator.weighted_strength = noiseSettings.fractalWeightedStrength;
//     noiseGenerator.frequency = noiseSettings.noiseFrequency;
//     noiseGenerator.cellular_distance_func = noiseSettings.cellularDistanceFunction;
//     noiseGenerator.cellular_return_type = noiseSettings.cellularReturnType;
//     noiseGenerator.cellular_jitter_mod = noiseSettings.cellularJitter;

//     // Get the noise value
//     float noise = fnlGetNoise2D(noiseGenerator, x, z);
//     return noise;
// }
// float SampleNoiseDW3D(NoiseSettingsGPU noiseSettings, float3 id, float3 chunkPos) {
//     float x = (id.x + chunkPos.x) * noiseSettings.noiseScale;
//     float y = (id.y + chunkPos.y) * noiseSettings.noiseScale;
//     float z = (id.z + chunkPos.z) * noiseSettings.noiseScale;
//     fnl_state noiseGenerator = fnlCreateState();
//     // Domain Warp Values
//     noiseGenerator.domain_warp_type = noiseSettings.domainWarpType;
//     noiseGenerator.fractal_type = noiseSettings.domainWarpFractalType;
//     noiseGenerator.domain_warp_amp = noiseSettings.domainWarpAmplitude;
//     noiseGenerator.seed = noiseSettings.domainWarpSeed;
//     noiseGenerator.octaves = noiseSettings.domainWarpFractalOctaves;
//     noiseGenerator.lacunarity = noiseSettings.domainWarpFractalLacunarity;
//     noiseGenerator.gain = noiseSettings.domainWarpFractalGain;
//     noiseGenerator.frequency = noiseSettings.domainWarpFrequency;
//     fnlDomainWarp3D(noiseGenerator, x, y, z);
//     // Noise Values
//     noiseGenerator.noise_type = noiseSettings.noiseType;
//     noiseGenerator.fractal_type = noiseSettings.noiseFractalType;
//     noiseGenerator.rotation_type_3d = noiseSettings.rotationType3D;
//     noiseGenerator.seed = noiseSettings.noiseSeed;
//     noiseGenerator.octaves = noiseSettings.noiseFractalOctaves;
//     noiseGenerator.lacunarity = noiseSettings.noiseFractalLacunarity;
//     noiseGenerator.gain = noiseSettings.noiseFractalGain;
//     noiseGenerator.weighted_strength = noiseSettings.fractalWeightedStrength;
//     noiseGenerator.frequency = noiseSettings.noiseFrequency;
//     noiseGenerator.cellular_distance_func = noiseSettings.cellularDistanceFunction;
//     noiseGenerator.cellular_return_type = noiseSettings.cellularReturnType;
//     noiseGenerator.cellular_jitter_mod = noiseSettings.cellularJitter;

//     // Get the noise value
//     float noise = fnlGetNoise3D(noiseGenerator, x, y, z);
//     return noise;
// }