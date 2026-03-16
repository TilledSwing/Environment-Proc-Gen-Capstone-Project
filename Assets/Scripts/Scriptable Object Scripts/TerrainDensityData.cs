using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TerrainDensityData", menuName = "Scriptable Objects/TerrainDensityData")]
public class TerrainDensityData : ScriptableObject
{
    // Terrain Values
    public string encodedNodeTreeString;
    public int chunkSize;
    public float isolevel;
    public int waterLevel;
    public bool water = true;
    public bool lerp = true;
    public bool hasFoliage;
    public List<GrassProfile> grassProfiles;
}