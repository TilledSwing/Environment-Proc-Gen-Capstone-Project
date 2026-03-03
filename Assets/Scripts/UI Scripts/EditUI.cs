using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class EditUI : MonoBehaviour
{
    public TerrainDensityData tdd;
    public AssetSpawnData asd;
    public Slider slider;
    public Toggle toggle;
    public TMP_InputField input;
    public GameObject loadScreen;

    /// <summary>
    /// Sets the sliders, toggles, and entry boxes to the values of the Terrain Density Data (only if the scene was built/rebuilt)
    /// </summary>
    void Start()
    {
        // UpdateSettings();
        // tdd = ChunkGenNetwork.Instance.generationConfiguration.terrainConfigs[2].terrainDensityData;
        // asd = ChunkGenNetwork.Instance.generationConfiguration.terrainConfigs[2].assetSpawnData;
    }

    void Update()
    {
    }

    public void SetNewData()
    {
        tdd = ChunkGenNetwork.Instance.generationConfiguration.terrainConfigs[ChunkGenNetwork.Instance.presetDropdown.value].terrainDensityData;
        asd = ChunkGenNetwork.Instance.generationConfiguration.terrainConfigs[ChunkGenNetwork.Instance.presetDropdown.value].assetSpawnData;
    }

    /// <summary>
    /// Reloads the terrain and the assets.
    /// </summary>
    public IEnumerator Reload()
    {
        yield return null;

        ChunkGenNetwork.Instance.InitializeGenerator();

        UpdateSettings();
        loadScreen.SetActive(false);
    }

    /// <summary>
    /// Updates the mesh only when a UI slider is released
    /// </summary>
    public void OnDeselect()
    {
        loadScreen.SetActive(true);
        Debug.Log("deselected slider");
        StartCoroutine(Reload());
    }

    /// <summary>
    /// Method updates the settings when the terrain is generated/regenerated to reflect the proper
    /// values.
    /// </summary>
    public void UpdateSettings()
    {

        // setting toggles
        switch (toggle.name)
        {
            case "LERPToggle":
                toggle.isOn = tdd.lerp;
                break;
            case "WaterToggle":
                toggle.isOn = tdd.water;
                break;
        }

        // setting sliders
        switch (slider.name)
        {
            case "HeightSlider":
                slider.value = tdd.height;
                break;
            case "IsoSlider":
                slider.value = tdd.isolevel;
                break;
            case "WaterSlider":
                slider.value = tdd.waterLevel;
                break;
        }
    }

    /// <summary>
    /// Resets the terrain modifiers to default
    /// </summary>
    public void ResetButton()
    {
        loadScreen.SetActive(true);
        // Terrain Values
        tdd.height = 250;
        tdd.isolevel = 0.5f;
        tdd.waterLevel = 0;
        tdd.water = true;
        tdd.lerp = true;

        StartCoroutine(Reload());
    }

    /// <summary>
    /// Reloads the scene to get a new seed
    /// </summary>
    public void RegenerateButton()
    {
        loadScreen.SetActive(true);
        ResetButton();
        
    }
    /// <summary>
    /// Methods to change the different parameters of the TDD with the UI toggles
    /// </summary>
    /// <param name="marked">Whether the box is checked or not</param>
    public void OnLERPToggleChanged(bool marked)
    {
        loadScreen.SetActive(true);
        tdd.lerp = marked;
        Debug.Log("toggle changed");
        StartCoroutine(Reload());
    }
    public void OnWaterToggleChanged(bool marked)
    {
        loadScreen.SetActive(true);
        tdd.water = marked;
        Debug.Log("toggle changed");
        StartCoroutine(Reload());
    }

    /// <summary>
    /// Methods to change the different parameters of the TDD with the UI sliders
    /// </summary>
    /// <param name="value">Reading from OnValueChanged()</param>
    public void OnHeightChanged(float value)
    {
        tdd.height = (int)value;
    }
    public void OnIsoChanged(float value)
    {
        tdd.isolevel = value;
    }
    public void OnWaterChanged(float value)
    {
        tdd.waterLevel = (int)value;
    }
}
