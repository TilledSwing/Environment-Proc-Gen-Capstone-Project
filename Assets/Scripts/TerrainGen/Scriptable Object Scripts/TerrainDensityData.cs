using UnityEngine;

[CreateAssetMenu(fileName = "TerrainDensityData", menuName = "Scriptable Objects/TerrainDensityData")]
public class TerrainDensityData : ScriptableObject
{
    public NoiseGenerator[] noiseGenerators;
    // Terrain Values
    public int width = 24;
    public int height = 100;
    public float isolevel = 0.5f;
    public int waterLevel = 35;
    public bool water = true;
    public bool lerp = true;
    public bool terracing = false;
    public int terraceHeight = 2;
}