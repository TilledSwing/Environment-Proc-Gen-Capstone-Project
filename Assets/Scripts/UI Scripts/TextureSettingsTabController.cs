
using System;
using System.Collections.Generic;
using System.IO;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class TextureSettingsTabController : MonoBehaviour
{
    public int textureIndex;
    public Toggle heightToggle;
    public MinMaxSlider heightSlider;
    public TMP_InputField heightMinInput;
    public TMP_InputField heightMaxInput;
    public Toggle slopeToggle;
    public MinMaxSlider slopeSlider;
    public TMP_InputField slopeMinInput;
    public TMP_InputField slopeMaxInput;
    public GameObject textureList;
    public RawImage texturePreview;
    bool added = false;
    bool initialized = false;
    public TextureIndexUpdater textureIndexUpdater;
    void Start()
    {
        // textureIndex = transform.GetSiblingIndex();

        textureIndexUpdater = transform.parent.GetComponent<TextureIndexUpdater>();

        initialized = true;
    }
    public void AddTexture()
    {
        var extensions = new[] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg" ),
        };
        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);

        if (paths.Length > 0)
        {
            string path = paths[0];
            Texture2D texture = ResizeTexture(ProcessTextureFile(path), ChunkGenNetwork.Instance.terrainTextureData.maxTextureSize, ChunkGenNetwork.Instance.terrainTextureData.maxTextureSize, ChunkGenNetwork.Instance.terrainTextureData.textureFormat);
            // Texture2D texture = ProcessTextureFile(path);

            TextureSettingsTabController texSettingsTab = Instantiate(ChunkGenNetwork.Instance.textureSettingsTab, ChunkGenNetwork.Instance.textureWindow.transform).GetComponent<TextureSettingsTabController>();
            texSettingsTab.textureIndex = texSettingsTab.transform.GetSiblingIndex();
            initialized = false;

            texSettingsTab.texturePreview.texture = texture;

            texSettingsTab.heightSlider.SetValues(-ChunkGenNetwork.Instance.maxWorldYChunks * ChunkGenNetwork.Instance.terrainDensityData.width, ChunkGenNetwork.Instance.maxWorldYChunks * ChunkGenNetwork.Instance.terrainDensityData.width, -ChunkGenNetwork.Instance.maxWorldYChunks * ChunkGenNetwork.Instance.terrainDensityData.width, ChunkGenNetwork.Instance.maxWorldYChunks * ChunkGenNetwork.Instance.terrainDensityData.width);
            texSettingsTab.slopeSlider.SetValues(0, 360, 0, 360);

            texSettingsTab.heightToggle.isOn = false;
            texSettingsTab.slopeToggle.isOn = false;

            TextureConfig textureConfig = new TextureConfig();
            textureConfig.texture = texture;
            textureConfig.useHeightRange = false;
            textureConfig.useSlopeRange = false;
            textureConfig.heightRange = new HeightRange(texSettingsTab.heightSlider.Values.minValue, texSettingsTab.heightSlider.Values.maxValue);
            textureConfig.slopeRange = new SlopeRange(texSettingsTab.slopeSlider.Values.minValue, texSettingsTab.slopeSlider.Values.maxValue);
            ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures.Add(textureConfig);

            foreach (BiomeTextureConfigs biomeTextureConfig in ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs)
            {
                int textureWidth = biomeTextureConfig.biomeTextures[0].texture.width;
                int textureHeight = biomeTextureConfig.biomeTextures[0].texture.height;
                int textureCount = biomeTextureConfig.MAX_TEXTURE_LAYERS;
                TextureFormat textureFormat = biomeTextureConfig.biomeTextures[0].texture.format;
                Texture2DArray textureArray = new(textureWidth, textureHeight, textureCount, textureFormat, true, false);
                textureArray.wrapMode = TextureWrapMode.Repeat;
                textureArray.filterMode = FilterMode.Bilinear;

                float lowestStartHeight = float.MaxValue;
                float greatestEndHeight = float.MinValue;

                int textureListCount = biomeTextureConfig.biomeTextures.Count;
                for (int i = 0; i < textureListCount; i++)
                {
                    Graphics.CopyTexture(biomeTextureConfig.biomeTextures[i].texture, 0, textureArray, i);
                    ChunkGenNetwork.Instance.useHeights[i] = biomeTextureConfig.biomeTextures[i].useHeightRange ? 1 : 0;
                    ChunkGenNetwork.Instance.heightStarts[i] = biomeTextureConfig.biomeTextures[i].heightRange.heightStart;
                    ChunkGenNetwork.Instance.heightEnds[i] = biomeTextureConfig.biomeTextures[i].heightRange.heightEnd;
                    ChunkGenNetwork.Instance.useSlopes[i] = biomeTextureConfig.biomeTextures[i].useSlopeRange ? 1 : 0;
                    ChunkGenNetwork.Instance.slopeStarts[i] = biomeTextureConfig.biomeTextures[i].slopeRange.slopeStart;
                    ChunkGenNetwork.Instance.slopeEnds[i] = biomeTextureConfig.biomeTextures[i].slopeRange.slopeEnd;

                    if (ChunkGenNetwork.Instance.heightStarts[i] < lowestStartHeight)
                        lowestStartHeight = ChunkGenNetwork.Instance.heightStarts[i] + 1;

                    if (ChunkGenNetwork.Instance.heightEnds[i] > greatestEndHeight)
                        greatestEndHeight = ChunkGenNetwork.Instance.heightEnds[i] - 1;
                }
                ChunkGenNetwork.Instance.textureArray = textureArray;
                ChunkGenNetwork.Instance.terrainMaterial.SetTexture("_TextureArray", ChunkGenNetwork.Instance.textureArray);
                ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_UseHeightsArray", ChunkGenNetwork.Instance.useHeights);
                ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_HeightStartsArray", ChunkGenNetwork.Instance.heightStarts);
                ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_HeightEndsArray", ChunkGenNetwork.Instance.heightEnds);
                ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_UseSlopesArray", ChunkGenNetwork.Instance.useSlopes);
                ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_SlopeStartsArray", ChunkGenNetwork.Instance.slopeStarts);
                ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_SlopeEndsArray", ChunkGenNetwork.Instance.slopeEnds);
                ChunkGenNetwork.Instance.terrainMaterial.SetInt("_LayerCount", textureListCount);
                ChunkGenNetwork.Instance.terrainMaterial.SetFloat("_LowestStartHeight", lowestStartHeight);
                ChunkGenNetwork.Instance.terrainMaterial.SetFloat("_GreatestEndHeight", greatestEndHeight);
            }

            added = true;
            initialized = true;
            // ChunkGenNetwork.Instance.TextureSetup();
        }
    }
    public void UpdateHeightMinMaxTexture()
    {
        if (!initialized) return;
        ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures[textureIndex].heightRange = new HeightRange(heightSlider.Values.minValue, heightSlider.Values.maxValue);

        ChunkGenNetwork.Instance.heightStarts[textureIndex] = heightSlider.Values.minValue;
        ChunkGenNetwork.Instance.heightEnds[textureIndex] = heightSlider.Values.maxValue;
        ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_HeightStartsArray", ChunkGenNetwork.Instance.heightStarts);
        ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_HeightEndsArray", ChunkGenNetwork.Instance.heightEnds);
    }
    public void UpdateHeightMinInputTexture()
    {
        if (!initialized) return;
        ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures[textureIndex].heightRange = new HeightRange(heightSlider.Values.minValue, heightSlider.Values.maxValue);

        heightSlider.SetValues(int.Parse(heightMinInput.text), heightSlider.Values.maxValue);
        ChunkGenNetwork.Instance.heightStarts[textureIndex] = int.Parse(heightMinInput.text);
        ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_HeightStartsArray", ChunkGenNetwork.Instance.heightStarts);
    }
    public void UpdateHeightMaxInputTexture()
    {
        if (!initialized) return;
        ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures[textureIndex].heightRange = new HeightRange(heightSlider.Values.minValue, heightSlider.Values.maxValue);

        heightSlider.SetValues(heightSlider.Values.minValue, int.Parse(heightMaxInput.text));
        ChunkGenNetwork.Instance.heightEnds[textureIndex] = int.Parse(heightMaxInput.text);
        ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_HeightEndsArray", ChunkGenNetwork.Instance.heightEnds);
    }
    public void UpdateSlopeMinMaxTexture()
    {
        if (!initialized) return;
        ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures[textureIndex].slopeRange = new SlopeRange(slopeSlider.Values.minValue, slopeSlider.Values.maxValue);

        ChunkGenNetwork.Instance.slopeStarts[textureIndex] = slopeSlider.Values.minValue;
        ChunkGenNetwork.Instance.slopeEnds[textureIndex] = slopeSlider.Values.maxValue;
        ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_SlopeStartsArray", ChunkGenNetwork.Instance.slopeStarts);
        ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_SlopeEndsArray", ChunkGenNetwork.Instance.slopeEnds);
    }
    public void UpdateSlopeMinInputTexture()
    {
        if (!initialized) return;
        ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures[textureIndex].heightRange = new HeightRange(heightSlider.Values.minValue, heightSlider.Values.maxValue);

        slopeSlider.SetValues(int.Parse(slopeMinInput.text), slopeSlider.Values.maxValue);
        ChunkGenNetwork.Instance.slopeStarts[textureIndex] = int.Parse(slopeMinInput.text);
        ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_SlopeStartsArray", ChunkGenNetwork.Instance.slopeStarts);
    }
    public void UpdateSlopeMaxInputTexture()
    {
        if (!initialized) return;
        ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures[textureIndex].heightRange = new HeightRange(heightSlider.Values.minValue, heightSlider.Values.maxValue);

        slopeSlider.SetValues(slopeSlider.Values.minValue, int.Parse(slopeMaxInput.text));
        ChunkGenNetwork.Instance.slopeEnds[textureIndex] = int.Parse(slopeMaxInput.text);
        ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_SlopeEndsArray", ChunkGenNetwork.Instance.slopeEnds);
    }
    public void UpdateUseHeightTexture()
    {
        if (!initialized) return;
        ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures[textureIndex].useHeightRange = heightToggle.isOn;

        ChunkGenNetwork.Instance.useHeights[textureIndex] = heightToggle.isOn ? 1 : 0;
        ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_UseHeightsArray", ChunkGenNetwork.Instance.useHeights);
    }
    public void UpdateUseSlopeTexture()
    {
        if (!initialized) return;
        ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures[textureIndex].useSlopeRange = slopeToggle.isOn;

        ChunkGenNetwork.Instance.useSlopes[textureIndex] = slopeToggle.isOn ? 1 : 0;
        ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_UseSlopesArray",  ChunkGenNetwork.Instance.useSlopes);
    }
    public void SetTexture()
    {
        var extensions = new[] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg" ),
        };
        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);

        if (paths.Length > 0)
        {
            string path = paths[0];
            Texture2D texture = ProcessTextureFile(path);
            RawImage texturePreview = gameObject.GetComponentInChildren<RawImage>();

            texturePreview.texture = texture;
            Texture2DArray textureArray = UpdateTextureArray(texture);
            ChunkGenNetwork.Instance.terrainMaterial.SetTexture("_TextureArray", textureArray);
        }
    }
    private Texture2DArray UpdateTextureArray(Texture2D texture)
    {
        Texture2DArray textureArray = ChunkGenNetwork.Instance.textureArray;
        Texture2D resizedTexture = ResizeTexture(texture, ChunkGenNetwork.Instance.terrainTextureData.maxTextureSize, ChunkGenNetwork.Instance.terrainTextureData.maxTextureSize, ChunkGenNetwork.Instance.terrainTextureData.textureFormat);
        for (int i = 0; i < resizedTexture.mipmapCount; i++)
        {
            Graphics.CopyTexture(resizedTexture, 0, i, textureArray, textureIndex, i);
        }
        return textureArray;
    }
    private Texture2D ResizeTexture(Texture2D sourceTexture, int width, int height, TextureFormat textureFormat)
    {
        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(sourceTexture, renderTexture);
        RenderTexture.active = renderTexture;

        Texture2D result = new Texture2D(width, height, textureFormat, true);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexture);
        return result;
    }
    public void RemoveTexture()
    {
        ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures.RemoveAt(textureIndex);
        foreach (BiomeTextureConfigs biomeTextureConfig in ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs)
        {
            int textureWidth = biomeTextureConfig.biomeTextures[0].texture.width;
            int textureHeight = biomeTextureConfig.biomeTextures[0].texture.height;
            int textureCount = biomeTextureConfig.MAX_TEXTURE_LAYERS;
            TextureFormat textureFormat = biomeTextureConfig.biomeTextures[0].texture.format;
            Texture2DArray textureArray = new(textureWidth, textureHeight, textureCount, textureFormat, true, false);
            textureArray.wrapMode = TextureWrapMode.Repeat;
            textureArray.filterMode = FilterMode.Bilinear;
            ChunkGenNetwork.Instance.useHeights = new float[biomeTextureConfig.MAX_TEXTURE_LAYERS];
            ChunkGenNetwork.Instance.heightStarts = new float[biomeTextureConfig.MAX_TEXTURE_LAYERS];
            ChunkGenNetwork.Instance.heightEnds = new float[biomeTextureConfig.MAX_TEXTURE_LAYERS];
            ChunkGenNetwork.Instance.useSlopes = new float[biomeTextureConfig.MAX_TEXTURE_LAYERS];
            ChunkGenNetwork.Instance.slopeStarts = new float[biomeTextureConfig.MAX_TEXTURE_LAYERS];
            ChunkGenNetwork.Instance.slopeEnds = new float[biomeTextureConfig.MAX_TEXTURE_LAYERS];

            float lowestStartHeight = float.MaxValue;
            float greatestEndHeight = float.MinValue;

            int textureListCount = biomeTextureConfig.biomeTextures.Count;
            for (int i = 0; i < textureListCount; i++)
            {
                Graphics.CopyTexture(biomeTextureConfig.biomeTextures[i].texture, 0, textureArray, i);
                ChunkGenNetwork.Instance.useHeights[i] = biomeTextureConfig.biomeTextures[i].useHeightRange ? 1 : 0;
                ChunkGenNetwork.Instance.heightStarts[i] = biomeTextureConfig.biomeTextures[i].heightRange.heightStart;
                ChunkGenNetwork.Instance.heightEnds[i] = biomeTextureConfig.biomeTextures[i].heightRange.heightEnd;
                ChunkGenNetwork.Instance.useSlopes[i] = biomeTextureConfig.biomeTextures[i].useSlopeRange ? 1 : 0;
                ChunkGenNetwork.Instance.slopeStarts[i] = biomeTextureConfig.biomeTextures[i].slopeRange.slopeStart;
                ChunkGenNetwork.Instance.slopeEnds[i] = biomeTextureConfig.biomeTextures[i].slopeRange.slopeEnd;

                if (ChunkGenNetwork.Instance.heightStarts[i] < lowestStartHeight)
                    lowestStartHeight = ChunkGenNetwork.Instance.heightStarts[i] + 1;

                if (ChunkGenNetwork.Instance.heightEnds[i] > greatestEndHeight)
                    greatestEndHeight = ChunkGenNetwork.Instance.heightEnds[i] - 1;
            }
            ChunkGenNetwork.Instance.textureArray = textureArray;
            ChunkGenNetwork.Instance.terrainMaterial.SetTexture("_TextureArray", ChunkGenNetwork.Instance.textureArray);
            ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_UseHeightsArray", ChunkGenNetwork.Instance.useHeights);
            ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_HeightStartsArray", ChunkGenNetwork.Instance.heightStarts);
            ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_HeightEndsArray", ChunkGenNetwork.Instance.heightEnds);
            ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_UseSlopesArray", ChunkGenNetwork.Instance.useSlopes);
            ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_SlopeStartsArray", ChunkGenNetwork.Instance.slopeStarts);
            ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_SlopeEndsArray", ChunkGenNetwork.Instance.slopeEnds);
            ChunkGenNetwork.Instance.terrainMaterial.SetInt("_LayerCount", textureListCount);
            ChunkGenNetwork.Instance.terrainMaterial.SetFloat("_LowestStartHeight", lowestStartHeight);
            ChunkGenNetwork.Instance.terrainMaterial.SetFloat("_GreatestEndHeight", greatestEndHeight);
        }
        transform.SetParent(null);
        Destroy(gameObject);
        textureIndexUpdater.UpdateAllIndices();
    }
    public Texture2D ProcessTextureFile(string path)
    {
        byte[] fileData = File.ReadAllBytes(path);

        Texture2D texture = new Texture2D(1, 1);
        texture.LoadImage(fileData);

        return texture;
    }
}
