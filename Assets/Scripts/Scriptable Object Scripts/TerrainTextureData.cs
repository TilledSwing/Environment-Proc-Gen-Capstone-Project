using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "TerrainTextureData", menuName = "Scriptable Objects/TerrainTextureData")]
public class TerrainTextureData : ScriptableObject
{
    private List<BiomeTextureConfigs> biomeTextureConfigsBackup = new();
    public List<BiomeTextureConfigs> biomeTextureConfigs;
    public TextureFormat textureFormat;
    public int maxTextureSize;

    public void BackupOriginalState()
    {
        biomeTextureConfigsBackup = new();
        foreach (BiomeTextureConfigs biomeTextureConfig in biomeTextureConfigs)
        {
            biomeTextureConfigsBackup.Add(biomeTextureConfig.Clone());
        }
    }

    public void RestoreToOriginalState()
    {
        if(biomeTextureConfigsBackup.Count == 0)
        {
            return;
        }
        
        biomeTextureConfigs = new();

        foreach (BiomeTextureConfigs biomeTextureConfig in biomeTextureConfigsBackup)
        {
            biomeTextureConfigs.Add(biomeTextureConfig.Clone());
        }
    }
}

[Serializable]
public class BiomeTextureConfigs
{
    public string biomeName;
    public float textureScale;
    public int MAX_TEXTURE_LAYERS;
    public List<TextureConfig> biomeTextures;
    public BiomeTextureConfigs Clone()
    {
        var clone = new BiomeTextureConfigs {
            biomeName = biomeName,
            textureScale = textureScale,
            MAX_TEXTURE_LAYERS =  MAX_TEXTURE_LAYERS,
            biomeTextures = new()
        };

        if (this.biomeTextures != null)
        {
            foreach (TextureConfig textureConfig in this.biomeTextures)
            {
                clone.biomeTextures.Add(textureConfig.Clone());
            }
        }

        return clone;
    }
}

[Serializable]
public class TextureConfig
{
    public Texture2D texture;
    public bool useHeightRange;
    public HeightRange heightRange;
    public bool useSlopeRange;
    public SlopeRange slopeRange;

    public TextureConfig Clone()
    {
        return new TextureConfig {
            texture = texture,
            useHeightRange = useHeightRange,
            heightRange = heightRange,
            useSlopeRange = useSlopeRange,
            slopeRange = slopeRange
        };
    }
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
