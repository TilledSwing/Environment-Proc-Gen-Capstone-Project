using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PresetDropdownController : MonoBehaviour
{
    void Start()
    {
        List<TerrainConfig> terrainConfigs = ChunkGenNetwork.Instance.generationConfiguration.terrainConfigs;
        List<string> terrainConfigNames = new();

        foreach (TerrainConfig terrainConfig in terrainConfigs)
        {
            terrainConfigNames.Add(terrainConfig.name);
        }

        TMP_Dropdown dropdown = gameObject.GetComponent<TMP_Dropdown>();
        dropdown.ClearOptions();
        dropdown.AddOptions(terrainConfigNames);
    }
}
