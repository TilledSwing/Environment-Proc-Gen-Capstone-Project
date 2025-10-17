using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "TerrainTextureData", menuName = "Scriptable Objects/TerrainTextureData")]
public class TerrainTextureData : ScriptableObject
{
    public BiomeTextureConfigs[] biomeTextureConfigs;
    public TextureFormat textureFormat;
    public int maxTextureSize;
}

[Serializable]
public class BiomeTextureConfigs
{
    public string biomeName;
    public float textureScale;
    public int MAX_TEXTURE_LAYERS;
    public List<TextureConfig> biomeTextures;
}

[Serializable]
public class TextureConfig
{
    public Texture2D texture;
    public bool useHeightRange;
    public HeightRange heightRange;
    public bool useSlopeRange;
    public SlopeRange slopeRange;
}

[Serializable]
public struct HeightRange
{
    public float heightStart;
    public float heightEnd;
    public HeightRange(float heightStart, float heightEnd)
    {
        this.heightStart = heightStart;
        this.heightEnd = heightEnd;
    }
}

[Serializable]
public struct SlopeRange
{
    public float slopeStart;
    public float slopeEnd;
    public SlopeRange(float slopeStart, float slopeEnd)
    {
        this.slopeStart = slopeStart;
        this.slopeEnd = slopeEnd;
    }
}
