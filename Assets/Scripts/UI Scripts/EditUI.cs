using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class EditUI : MonoBehaviour
{
    public MarchingCubes mc;
    public TerrainDensityDataOld tdd;
    public Slider slider;
    public Toggle toggle;
    public TMP_InputField input;

    /// <summary>
    /// Sets the sliders, toggles, and entry boxes to the values of the Terrain Density Data (only if the scene was built/rebuilt)
    /// </summary>
    void Start()
    {
        UpdateSettings();

    }

    void Update()
    {
        // setting inputs on update to make sure they're always correct
        switch(input.name)
        {
            case "NoiseSeedInput":
                input.text = tdd.noiseSeed.ToString();
                break;
            case "DWSeedInput":
                input.text = tdd.domainWarpSeed.ToString();
                break;     
        }
    }

    /// <summary>
    /// Updates the mesh only when a UI slider is released
    /// </summary>
    public void OnDeselect()
    {
        Debug.Log("deselected slider");
        mc.UpdateMesh();
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
                toggle.isOn = tdd.domainWarpToggle;
                break;
            case "VisualizeToggle":
                toggle.isOn = tdd.polygonizationVisualization;
                break;
        }

        // setting sliders
        switch(slider.name)
        {
            case "HeightSlider":
                slider.value = tdd.height;
                break;
            case "FrequencySlider":
                slider.value = tdd.noiseFrequency;
                break;
            case "NoiseScaleSlider":
                slider.value = tdd.noiseScale;
                break;
            case "IsoSlider":
                slider.value = tdd.isolevel;
                break;
            case "JitterSlider":
                slider.value = tdd.cellularJitter;
                break;
            case "WaterSlider":
                slider.value = tdd.waterLevel;
                break;
            case "FOctavesSlider":
                slider.value = tdd.noiseFractalOctaves;
                break;
            case "FLacunaritySlider":
                slider.value = tdd.noiseFractalLacunarity;
                break;
            case "FGainSlider":
                slider.value = tdd.noiseFractalGain;
                break;
            case "FStrengthSlider":
                slider.value = tdd.fractalWeightedStrength;
                break;
            case "TerraceSlider":
                slider.value = tdd.terraceHeight;
                break;
            case "DWOctavesSlider":
                slider.value = tdd.domainWarpFractalOctaves;
                break;
            case "DWLacunaritySlider":
                slider.value = tdd.domainWarpFractalLacunarity;
                break;
            case "DWGainSlider":
                slider.value = tdd.domainWarpFractalGain;
                break;
            case "DWFrequencySlider":
                slider.value = tdd.domainWarpFrequency;
                break;
            case "DWAmpSlider":
                slider.value = tdd.domainWarpAmplitude;
                break;
        }
    }

    public void ResetButton()
    {
        tdd.ResetToDefault();
        mc.GenerateTerrainData();
    }

    /// <summary>
    /// Methods to change the different parameters of the TDD with the UI toggles
    /// </summary>
    /// <param name="marked">Whether the box is checked or not</param>
    public void OnLERPToggleChanged(bool marked)
    {
        tdd.lerp = marked;
        Debug.Log("toggle changed");
        mc.UpdateMesh();
    }
    public void OnTerraceToggleChanged(bool marked)
    {
        tdd.terracing = marked;
        Debug.Log("toggle changed");
        mc.UpdateMesh();
    }
    public void OnDWToggleChanged(bool marked)
    {
        tdd.domainWarpToggle = marked;
        Debug.Log("toggle changed");
        mc.UpdateMesh();
    }
    public void OnVisualizeChanged(bool marked)
    {
        tdd.polygonizationVisualization = marked;
        Debug.Log("toggle changed");
        mc.UpdateMesh();
    }

    /// <summary>
    /// Methods to change the seeds of the TDD with the UI input field
    /// </summary>
    /// <param name="seed">The int seed entered</param>
    public void OnNoiseSeedChanged(string seed)
    {
        tdd.noiseSeed = System.Convert.ToInt32(seed);
        Debug.Log("seed changed");
        mc.UpdateMesh();
    }
    public void OnDWSeedChanged(string seed)
    {
        tdd.domainWarpSeed = System.Convert.ToInt32(seed);
        Debug.Log("seed changed");
        mc.UpdateMesh();
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
        tdd.noiseFrequency = value;
    }
    public void OnNScaleChanged(float value) 
    {
        tdd.noiseScale = value;
    }
    public void OnIsoChanged(float value) 
    {
        tdd.isolevel = value;
    }
    public void OnJitterChanged(float value) 
    {
        tdd.cellularJitter = value;
    }
    public void OnWaterChanged(float value) 
    {
        tdd.waterLevel = (int)value;
    }
    public void OnFOctavesChanged(float value) 
    {
        tdd.noiseFractalOctaves = (int)value;
    }
    public void OnFLacunarityChanged(float value) 
    {
        tdd.noiseFractalLacunarity = value;
    }
    public void OnFGainChanged(float value) 
    {
        tdd.noiseFractalGain = value;
    }
    public void OnFStrengthChanged(float value) 
    {
        tdd.fractalWeightedStrength = value;
    }
    public void OnTerraceChanged(float value) 
    {
        tdd.terraceHeight = (int)value;
    }
    public void OnDWOctavesChanged(float value) 
    {
        tdd.domainWarpFractalOctaves = (int)value;
    }
    public void OnDWLacunarityChanged(float value) 
    {
        tdd.domainWarpFractalLacunarity = value;
    }
    public void OnDWGainChanged(float value) 
    {
        tdd.domainWarpFractalGain = value;
    }
    public void OnDWFrequencyChanged(float value) 
    {
        tdd.domainWarpFrequency = value;
    }
    public void OnDWAmplitudeChanged(float value) 
    {
        tdd.domainWarpAmplitude = value;
    }
}
