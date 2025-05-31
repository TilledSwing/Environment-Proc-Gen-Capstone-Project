using System;
using UnityEngine;

[CreateAssetMenu(fileName = "TerrainTextureData", menuName = "Scriptable Objects/TerrainTextureData")]
public class TerrainTextureData : ScriptableObject
{
    public BiomeTextureConfigs[] biomeTextureConfigs;
    [Serializable]
    public struct BiomeTextureConfigs
    {
        public string biomeName;
        public float textureScale;
        public int MAX_TEXTURE_LAYERS;
        public TextureConfig[] biomeTextures;
    }
    [Serializable]
    public struct TextureConfig
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
    }
    [Serializable]
    public struct SlopeRange
    {
        public float slopeStart;
        public float slopeEnd;
    }
}
