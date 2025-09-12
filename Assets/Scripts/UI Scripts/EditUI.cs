using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class EditUI : MonoBehaviour
{
    public ChunkGenNetwork cgn;
    public ChunkGenNetwork.TerrainChunk mc;
    public TerrainDensityData tdd;
    public NoiseGenerator ng;
    public ChunkGenNetwork.LOD lod;
    public Slider slider;
    public Toggle toggle;
    public TMP_InputField input;

    /// <summary>
    /// Sets the sliders, toggles, and entry boxes to the values of the Terrain Density Data (only if the scene was built/rebuilt)
    /// </summary>
    void Start()
    {
        tdd = cgn.terrainDensityData;
        UpdateSettings();
    }

    void Update()
    {
        // setting inputs on update to make sure they're always correct
        switch(input.name)
        {
            case "NoiseSeedInput":
                input.text = ng.noiseSeed.ToString();
                break;
            case "DWSeedInput":
                input.text = ng.domainWarpSeed.ToString();
                break;     
        }
    }

    /// <summary>
    /// Updates the mesh only when a UI slider is released
    /// </summary>
    public void OnDeselect()
    {
        Debug.Log("deselected slider");
        mc.marchingCubes.Regen();
    }

    /// <summary>
    /// Method updates the settings when the terrain is generated/regenerated to reflect the proper values.
    /// </summary>
    public void UpdateSettings()
    {

        // setting toggles
        switch(toggle.name)
        {
            case "LERPToggle":
                toggle.isOn = tdd.lerp;
                break;
            case "TerraceToggle":
                toggle.isOn = tdd.terracing;
                break;
            case "DWToggle":
                toggle.isOn = ng.domainWarpToggle;
                break;
        }

        // setting sliders
        switch(slider.name)
        {
            case "HeightSlider":
                slider.value = tdd.height;
                break;
            case "FrequencySlider":
                slider.value = ng.noiseFrequency;
                break;
            case "NoiseScaleSlider":
                slider.value = ng.noiseScale;
                break;
            case "IsoSlider":
                slider.value = tdd.isolevel;
                break;
            case "JitterSlider":
                slider.value = ng.cellularJitter;
                break;
            case "WaterSlider":
                slider.value = tdd.waterLevel;
                break;
            case "FOctavesSlider":
                slider.value = ng.noiseFractalOctaves;
                break;
            case "FLacunaritySlider":
                slider.value = ng.noiseFractalLacunarity;
                break;
            case "FGainSlider":
                slider.value = ng.noiseFractalGain;
                break;
            case "FStrengthSlider":
                slider.value = ng.fractalWeightedStrength;
                break;
            case "TerraceSlider":
                slider.value = tdd.terraceHeight;
                break;
            case "DWOctavesSlider":
                slider.value = ng.domainWarpFractalOctaves;
                break;
            case "DWLacunaritySlider":
                slider.value = ng.domainWarpFractalLacunarity;
                break;
            case "DWGainSlider":
                slider.value = ng.domainWarpFractalGain;
                break;
            case "DWFrequencySlider":
                slider.value = ng.domainWarpFrequency;
                break;
            case "DWAmpSlider":
                slider.value = ng.domainWarpAmplitude;
                break;
        }
    }

    public void ResetButton()
    {
        // Noise and Fractal Settings
        ng.noiseDimension = NoiseGenerator.fnl_noise_dimension._3D;
        ng.noiseType = NoiseGenerator.fnl_noise_type.OpenSimplex2;
        ng.noiseFractalType = NoiseGenerator.fnl_fractal_type.FBm;
        ng.noiseFractalOctaves = 5;
        ng.noiseFractalLacunarity = 2;
        ng.noiseFractalGain = 0.5f;
        ng.fractalWeightedStrength = 0;
        ng.noiseFrequency = 0.01f;
        // Domain Warp Values
        ng.domainWarpToggle = false;
        ng.domainWarpType = NoiseGenerator.fnl_domain_warp_type.OpenSimplex2;
        ng.domainWarpFractalType = NoiseGenerator.fnl_domain_warp_fractal_type.None;
        ng.domainWarpAmplitude = 1;
        ng.domainWarpFractalOctaves = 5;
        ng.domainWarpFractalLacunarity = 2;
        ng.domainWarpFractalGain = 0.5f;
        ng.domainWarpFrequency = 0.01f;
        // Cellular(Voronoi) Values
        ng.cellularDistanceFunction = NoiseGenerator.fnl_cellular_distance_func.EuclideanSq;
        ng.cellularReturnType = NoiseGenerator.fnl_cellular_return_type.Distance;
        ng.cellularJitter = 1;
        // Terrain Values
        ng.width = 24;
        tdd.height = 100;
        ng.noiseScale = 0.6f;
        tdd.isolevel = 0.5f;
        tdd.waterLevel = 35;
        tdd.lerp = true;
        tdd.terracing = false;
        tdd.terraceHeight = 2;
        mc.marchingCubes.Regen();
    }

    /// <summary>
    /// Methods to change the different parameters of the TDD with the UI toggles
    /// </summary>
    /// <param name="marked">Whether the box is checked or not</param>
    public void OnLERPToggleChanged(bool marked)
    {
        tdd.lerp = marked;
        Debug.Log("toggle changed");
        mc.marchingCubes.Regen();
    }
    public void OnTerraceToggleChanged(bool marked)
    {
        tdd.terracing = marked;
        Debug.Log("toggle changed");
        mc.marchingCubes.Regen();
    }
    public void OnDWToggleChanged(bool marked)
    {
        ng.domainWarpToggle = marked;
        Debug.Log("toggle changed");
        mc.marchingCubes.Regen();
    }

    /// <summary>
    /// Methods to change the seeds of the TDD with the UI input field
    /// </summary>
    /// <param name="seed">The int seed entered</param>
    public void OnNoiseSeedChanged(string seed)
    {
        ng.noiseSeed = System.Convert.ToInt32(seed);
        Debug.Log("seed changed");
        mc.marchingCubes.Regen();
    }
    public void OnDWSeedChanged(string seed)
    {
        ng.domainWarpSeed = System.Convert.ToInt32(seed);
        Debug.Log("seed changed");
        mc.marchingCubes.Regen();
    }

    /// <summary>
    /// Methods to change the different parameters of the TDD with the UI sliders
    /// </summary>
    /// <param name="value">Reading from OnValueChanged()</param>
    public void OnHeightChanged(float value) 
    {
        tdd.height = (int)value;
    }
    public void OnNFreqChanged(float value) 
    {
        ng.noiseFrequency = value;
    }
    public void OnNScaleChanged(float value) 
    {
        ng.noiseScale = value;
    }
    public void OnIsoChanged(float value) 
    {
        tdd.isolevel = value;
    }
    public void OnJitterChanged(float value) 
    {
        ng.cellularJitter = value;
    }
    public void OnWaterChanged(float value) 
    {
        tdd.waterLevel = (int)value;
    }
    public void OnFOctavesChanged(float value) 
    {
        ng.noiseFractalOctaves = (int)value;
    }
    public void OnFLacunarityChanged(float value) 
    {
        ng.noiseFractalLacunarity = value;
    }
    public void OnFGainChanged(float value) 
    {
        ng.noiseFractalGain = value;
    }
    public void OnFStrengthChanged(float value) 
    {
        ng.fractalWeightedStrength = value;
    }
    public void OnTerraceChanged(float value) 
    {
        tdd.terraceHeight = (int)value;
    }
    public void OnDWOctavesChanged(float value) 
    {
        ng.domainWarpFractalOctaves = (int)value;
    }
    public void OnDWLacunarityChanged(float value) 
    {
        ng.domainWarpFractalLacunarity = value;
    }
    public void OnDWGainChanged(float value) 
    {
        ng.domainWarpFractalGain = value;
    }
    public void OnDWFrequencyChanged(float value) 
    {
        ng.domainWarpFrequency = value;
    }
    public void OnDWAmplitudeChanged(float value) 
    {
        ng.domainWarpAmplitude = value;
    }
}
