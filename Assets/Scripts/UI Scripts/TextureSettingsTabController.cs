
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
    private int textureIndex;
    private Toggle heightToggle;
    private MinMaxSlider heightSlider;
    private TMP_InputField heightMinInput;
    private TMP_InputField heightMaxInput;
    private Toggle slopeToggle;
    private MinMaxSlider slopeSlider;
    private TMP_InputField slopeMinInput;
    private TMP_InputField slopeMaxInput;
    public GameObject textureList;
    RawImage texturePreview;
    bool added = false;
    bool initialized = false;
    public List<TextureConfig> biomeTextures;
    void Start()
    {
        textureIndex = transform.GetSiblingIndex();

        MinMaxSlider[] sliders = gameObject.GetComponentsInChildren<MinMaxSlider>();
        heightSlider = sliders[0];
        slopeSlider = sliders[1];

        Toggle[] toggles = gameObject.GetComponentsInChildren<Toggle>();
        heightToggle = toggles[0];
        slopeToggle = toggles[1];
        initialized = true;

        TMP_InputField[] heightInputs = heightSlider.GetComponentsInChildren<TMP_InputField>();
        heightMinInput = heightInputs[0];
        heightMaxInput = heightInputs[1];
        TMP_InputField[] slopeInputs = slopeSlider.GetComponentsInChildren<TMP_InputField>();
        slopeMinInput = slopeInputs[0];
        slopeMaxInput = slopeInputs[1];

        biomeTextures = ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures;
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
            Texture2D texture = ProcessTextureFile(path);

            GameObject texSettingsTab = Instantiate(ChunkGenNetwork.Instance.textureSettingsTab, ChunkGenNetwork.Instance.textureWindow.transform);
            initialized = false;
            RawImage texturePreview = texSettingsTab.GetComponentInChildren<RawImage>();

            texturePreview.texture = texture;

            MinMaxSlider[] sliders = texSettingsTab.GetComponentsInChildren<MinMaxSlider>();
            sliders[0].SetValues(-ChunkGenNetwork.Instance.maxWorldYChunks * ChunkGenNetwork.Instance.terrainDensityData.width, ChunkGenNetwork.Instance.maxWorldYChunks * ChunkGenNetwork.Instance.terrainDensityData.width, -ChunkGenNetwork.Instance.maxWorldYChunks * ChunkGenNetwork.Instance.terrainDensityData.width, ChunkGenNetwork.Instance.maxWorldYChunks * ChunkGenNetwork.Instance.terrainDensityData.width);
            sliders[1].SetValues(0, 360, 0, 360);

            Toggle[] toggles = texSettingsTab.GetComponentsInChildren<Toggle>();
            toggles[0].isOn = false;
            toggles[1].isOn = false;

            TextureConfig textureConfig = new TextureConfig();
            textureConfig.texture = texture;
            textureConfig.useHeightRange = false;
            textureConfig.useSlopeRange = false;
            textureConfig.heightRange = new HeightRange(sliders[0].Values.minValue, sliders[0].Values.maxValue);
            textureConfig.slopeRange = new SlopeRange(sliders[1].Values.minValue, sliders[1].Values.maxValue);
            ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures.Add(textureConfig);
            added = true;
            initialized = true;
        }
    }
    public void UpdateHeightMinMaxTexture()
    {
        if (!initialized) return;
        // ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures[textureIndex].heightRange = new HeightRange(heightSlider.Values.minValue, heightSlider.Values.maxValue);

        ChunkGenNetwork.Instance.heightStarts[textureIndex] = heightSlider.Values.minValue;
        ChunkGenNetwork.Instance.heightEnds[textureIndex] = heightSlider.Values.maxValue;
        ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_HeightStartsArray", ChunkGenNetwork.Instance.heightStarts);
        ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_HeightEndsArray", ChunkGenNetwork.Instance.heightEnds);
    }
    public void UpdateHeightMinInputTexture()
    {
        if (!initialized) return;
        // ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures[textureIndex].heightRange = new HeightRange(heightSlider.Values.minValue, heightSlider.Values.maxValue);

        heightSlider.SetValues(int.Parse(heightMinInput.text), heightSlider.Values.maxValue);
        ChunkGenNetwork.Instance.heightStarts[textureIndex] = int.Parse(heightMinInput.text);
        ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_HeightStartsArray", ChunkGenNetwork.Instance.heightStarts);
    }
    public void UpdateHeightMaxInputTexture()
    {
        if (!initialized) return;
        // ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures[textureIndex].heightRange = new HeightRange(heightSlider.Values.minValue, heightSlider.Values.maxValue);

        heightSlider.SetValues(heightSlider.Values.minValue, int.Parse(heightMaxInput.text));
        ChunkGenNetwork.Instance.heightEnds[textureIndex] = int.Parse(heightMaxInput.text);
        ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_HeightEndsArray", ChunkGenNetwork.Instance.heightEnds);
    }
    public void UpdateSlopeMinMaxTexture()
    {
        if (!initialized) return;
        // ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures[textureIndex].slopeRange = new SlopeRange(slopeSlider.Values.minValue, slopeSlider.Values.maxValue);

        ChunkGenNetwork.Instance.slopeStarts[textureIndex] = slopeSlider.Values.minValue;
        ChunkGenNetwork.Instance.slopeEnds[textureIndex] = slopeSlider.Values.maxValue;
        ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_SlopeStartsArray", ChunkGenNetwork.Instance.slopeStarts);
        ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_SlopeEndsArray", ChunkGenNetwork.Instance.slopeEnds);
    }
    public void UpdateSlopeMinInputTexture()
    {
        if (!initialized) return;
        // ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures[textureIndex].heightRange = new HeightRange(heightSlider.Values.minValue, heightSlider.Values.maxValue);

        slopeSlider.SetValues(int.Parse(slopeMinInput.text), slopeSlider.Values.maxValue);
        ChunkGenNetwork.Instance.slopeStarts[textureIndex] = int.Parse(slopeMinInput.text);
        ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_SlopeStartsArray", ChunkGenNetwork.Instance.slopeStarts);
    }
    public void UpdateSlopeMaxInputTexture()
    {
        if (!initialized) return;
        // ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures[textureIndex].heightRange = new HeightRange(heightSlider.Values.minValue, heightSlider.Values.maxValue);

        slopeSlider.SetValues(slopeSlider.Values.minValue, int.Parse(slopeMaxInput.text));
        ChunkGenNetwork.Instance.slopeEnds[textureIndex] = int.Parse(slopeMaxInput.text);
        ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_SlopeEndsArray", ChunkGenNetwork.Instance.slopeEnds);
    }
    public void UpdateUseHeightTexture()
    {
        if (!initialized) return;
        // ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures[textureIndex].useHeightRange = heightToggle.isOn;

        ChunkGenNetwork.Instance.useHeights[textureIndex] = heightToggle.isOn ? 1 : 0;
        ChunkGenNetwork.Instance.terrainMaterial.SetFloatArray("_UseHeightsArray", ChunkGenNetwork.Instance.useHeights);
    }
    public void UpdateUseSlopeTexture()
    {
        if (!initialized) return;
        // ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures[textureIndex].useSlopeRange = slopeToggle.isOn;

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
            UpdateTextureArray(texture);
        }
    }
    private void UpdateTextureArray(Texture2D texture)
    {
        Texture2DArray textureArray = ChunkGenNetwork.Instance.textureArray;
        Texture2D resizedTexture = ResizeTexture(texture, ChunkGenNetwork.Instance.terrainTextureData.maxTextureSize, ChunkGenNetwork.Instance.terrainTextureData.maxTextureSize, ChunkGenNetwork.Instance.terrainTextureData.textureFormat);
        for (int i = 0; i < resizedTexture.mipmapCount; i++)
        {
            Graphics.CopyTexture(resizedTexture, 0, i, textureArray, textureIndex, i);
        }
        ChunkGenNetwork.Instance.terrainMaterial.SetTexture("_TextureArray", textureArray);
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
        UpdateTextures(textureIndex);
        Destroy(gameObject);
    }
    public void UpdateTextures(int startIndex)
    {
        foreach (BiomeTextureConfigs biomeTextureConfig in ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs)
        {
            float lowestStartHeight = float.MaxValue;
            float greatestEndHeight = float.MinValue;

            int textureListCount = textureList.GetComponentCount();
            for (int i = startIndex; i < textureListCount; i++)
            {
                Graphics.CopyTexture(biomeTextureConfig.biomeTextures[i+1].texture, 0, ChunkGenNetwork.Instance.textureArray, i);
                ChunkGenNetwork.Instance.useHeights[i] = biomeTextureConfig.biomeTextures[i+1].useHeightRange ? 1 : 0;
                ChunkGenNetwork.Instance.heightStarts[i] = biomeTextureConfig.biomeTextures[i+1].heightRange.heightStart;
                ChunkGenNetwork.Instance.heightEnds[i] = biomeTextureConfig.biomeTextures[i+1].heightRange.heightEnd;
                ChunkGenNetwork.Instance.useSlopes[i] = biomeTextureConfig.biomeTextures[i+1].useSlopeRange ? 1 : 0;
                ChunkGenNetwork.Instance.slopeStarts[i] = biomeTextureConfig.biomeTextures[i+1].slopeRange.slopeStart;
                ChunkGenNetwork.Instance.slopeEnds[i] = biomeTextureConfig.biomeTextures[i+1].slopeRange.slopeEnd;

                if (ChunkGenNetwork.Instance.heightStarts[i] < lowestStartHeight)
                    lowestStartHeight = ChunkGenNetwork.Instance.heightStarts[i] + 1;

                if (ChunkGenNetwork.Instance.heightEnds[i] > greatestEndHeight)
                    greatestEndHeight = ChunkGenNetwork.Instance.heightEnds[i] - 1;
            }
            // textureArray.Apply(false);
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
    }
    public Texture2D ProcessTextureFile(string path)
    {
        byte[] fileData = File.ReadAllBytes(path);

        Texture2D texture = new Texture2D(1, 1);
        texture.LoadImage(fileData);

        return texture;
    }
    void OnApplicationQuit()
    {
        ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures = biomeTextures;
        // if (added) {
        //     ChunkGenNetwork.Instance.terrainTextureData.biomeTextureConfigs[0].biomeTextures.RemoveAt(textureIndex);
        // }
    }
}
