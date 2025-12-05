using System.Collections;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class AssetSettingsTabController : MonoBehaviour
{
    public AssetSpawnData assetSpawnData;
    // Header Settings
    public RawImage assetPreview;
    public TextMeshProUGUI assetName;
    public Toggle rotateToFaceNormalToggle;
    // Spawn Probability Settings
    public TMP_InputField spawnProbInput;
    public Slider spawnProbSlider;
    // Max Per Chunk Settings
    public TMP_InputField maxPerChunkInput;
    public Slider maxPerChunkSlider;
    // Min Height Settings
    public Toggle useMinHeightToggle;
    public TMP_InputField minHeightInput;
    public Slider minHeightSlider;
    // Max Height Settings
    public Toggle useMaxHeightToggle;
    public TMP_InputField maxHeightInput;
    public Slider maxHeightSlider;
    // Min Slope Settings
    public Toggle useMinSlopeToggle;
    public TMP_InputField minSlopeInput;
    public Slider minSlopeSlider;
    // Max Slope Settings
    public Toggle useMaxSlopeToggle;
    public TMP_InputField maxSlopeInput;
    public Slider maxSlopeSlider;
    // Underwater Settings
    public Toggle underwaterToggle;
    public TMP_InputField minDepthInput;
    public Slider minDepthSlider;
    // Underground Settings
    public Toggle undergroundToggle;
    public TMP_InputField minDensityInput;
    public Slider minDensitySlider;
    // Valuable Settings
    public Toggle valueableToggle;
    public TMP_InputField minValueInput;
    public TMP_InputField maxValueInput;
    public MinMaxSlider valueRangeSlider;

    public int assetIndex;
    public bool initialized = false;
    public CanvasGroup canvasGroup;
    public AssetIndexUpdater assetIndexUpdater;

    void Start()
    {
        // assetIndex = transform.GetSiblingIndex();
        
        assetIndexUpdater = transform.parent.GetComponent<AssetIndexUpdater>();
    }

    public void UpdateFaceAlignmentToggle()
    {
        if (!initialized) return;
        assetSpawnData.spawnableAssets[assetIndex].rotateToFaceNormal = rotateToFaceNormalToggle.isOn;
        RespawnAssets();
    }

    // Spawn Probability
    public void UpdateSpawnProbabilityInput()
    {
        if (!initialized) return;
        spawnProbInput.text = float.Parse(spawnProbInput.text).ToString("F2");
        spawnProbSlider.value = float.Parse(spawnProbInput.text);
        assetSpawnData.spawnableAssets[assetIndex].spawnProbability = float.Parse(spawnProbInput.text);
        RespawnAssets();
    }

    public void UpdateSpawnProbabilitySlider()
    {
        if (!initialized) return;
        assetSpawnData.spawnableAssets[assetIndex].spawnProbability = spawnProbSlider.value;
        RespawnAssets();
    }

    // Max Per Chunk
    public void UpdateMaxPerChunkInput()
    {
        if (!initialized) return;
        maxPerChunkSlider.value = int.Parse(maxPerChunkInput.text);
        assetSpawnData.spawnableAssets[assetIndex].maxPerChunk = int.Parse(maxPerChunkInput.text);
        RespawnAssets();
    }

    public void UpdateMaxPerChunkSlider()
    {
        if (!initialized) return;
        assetSpawnData.spawnableAssets[assetIndex].maxPerChunk = (int)maxPerChunkSlider.value;
        RespawnAssets();
    }

    // Min Height
    public void UpdateUseMinHeightToggle()
    {
        if (!initialized) return;
        assetSpawnData.spawnableAssets[assetIndex].useMinHeight = useMinHeightToggle.isOn;
        RespawnAssets();
    }

    public void UpdateMinHeightInput()
    {
        if (!initialized) return;
        minHeightSlider.value = int.Parse(minHeightInput.text);
        assetSpawnData.spawnableAssets[assetIndex].minHeight = int.Parse(minHeightInput.text);

        if (!useMinHeightToggle.isOn) return;
        RespawnAssets();
    }

    public void UpdateMinHeightSlider()
    {
        if (!initialized) return;
        assetSpawnData.spawnableAssets[assetIndex].minHeight = (int)minHeightSlider.value;

        if (!useMinHeightToggle.isOn) return;
        RespawnAssets();
    }

    // Max Height
    public void UpdateUseMaxHeightToggle()
    {
        if (!initialized) return;
        assetSpawnData.spawnableAssets[assetIndex].useMaxHeight = useMaxHeightToggle.isOn;
        RespawnAssets();
    }

    public void UpdateMaxHeightInput()
    {
        if (!initialized) return;
        maxHeightSlider.value = int.Parse(maxHeightInput.text);
        assetSpawnData.spawnableAssets[assetIndex].maxHeight = int.Parse(maxHeightInput.text);

        if (!useMaxHeightToggle.isOn) return;
        RespawnAssets();
    }

    public void UpdateMaxHeightSlider()
    {
        if (!initialized) return;
        assetSpawnData.spawnableAssets[assetIndex].maxHeight = (int)maxHeightSlider.value;

        if (!useMaxHeightToggle.isOn) return;
        RespawnAssets();
    }

    // Min Slope
    public void UpdateUseMinSlopeToggle()
    {
        if (!initialized) return;
        assetSpawnData.spawnableAssets[assetIndex].useMinSlope = useMinSlopeToggle.isOn;
        RespawnAssets();
    }

    public void UpdateMinSlopeInput()
    {
        if (!initialized) return;
        minSlopeSlider.value = int.Parse(minSlopeInput.text);
        assetSpawnData.spawnableAssets[assetIndex].minSlope = int.Parse(minSlopeInput.text);

        if (!useMinSlopeToggle.isOn) return;
        RespawnAssets();
    }

    public void UpdateMinSlopeSlider()
    {
        if (!initialized) return;
        assetSpawnData.spawnableAssets[assetIndex].minSlope = (int)minSlopeSlider.value;

        if (!useMinSlopeToggle.isOn) return;
        RespawnAssets();
    }

    // Max Slope
    public void UpdateUseMaxSlopeToggle()
    {
        if (!initialized) return;
        assetSpawnData.spawnableAssets[assetIndex].useMaxSlope = useMaxSlopeToggle.isOn;
        RespawnAssets();
    }

    public void UpdateMaxSlopeInput()
    {
        if (!initialized) return;
        maxSlopeSlider.value = int.Parse(maxSlopeInput.text);
        assetSpawnData.spawnableAssets[assetIndex].maxSlope = int.Parse(maxSlopeInput.text);

        if (!useMaxSlopeToggle.isOn) return;
        RespawnAssets();
    }

    public void UpdateMaxSlopeSlider()
    {
        if (!initialized) return;
        assetSpawnData.spawnableAssets[assetIndex].maxSlope = (int)maxSlopeSlider.value;

        if (!useMaxSlopeToggle.isOn) return;
        RespawnAssets();
    }

    // Underwater
    public void UpdateUnderwaterToggle()
    {
        if (!initialized) return;
        assetSpawnData.spawnableAssets[assetIndex].underwaterAsset = underwaterToggle.isOn;
        RespawnAssets();
    }

    public void UpdateMinDepthInput()
    {
        if (!initialized) return;
        minDepthInput.text = float.Parse(minDepthInput.text).ToString("F2");
        minDepthSlider.value = float.Parse(minDepthInput.text);
        assetSpawnData.spawnableAssets[assetIndex].minDepth = float.Parse(minDepthInput.text);

        if (!underwaterToggle.isOn) return;
        RespawnAssets();
    }

    public void UpdateMinDepthSlider()
    {
        if (!initialized) return;
        assetSpawnData.spawnableAssets[assetIndex].minDepth = minDepthSlider.value;

        if (!underwaterToggle.isOn) return;
        RespawnAssets();
    }

    // Underground
    public void UpdateUndergroundToggle()
    {
        if (!initialized) return;
        assetSpawnData.spawnableAssets[assetIndex].undergroundAsset = undergroundToggle.isOn;
        RespawnAssets();
    }

    public void UpdateMinDensityInput()
    {
        if (!initialized) return;
        minDensityInput.text = float.Parse(minDensityInput.text).ToString("F2");
        minDensitySlider.value = float.Parse(minDensityInput.text);
        assetSpawnData.spawnableAssets[assetIndex].minDensity = float.Parse(minDensityInput.text);

        if (!undergroundToggle.isOn) return;
        RespawnAssets();
    }

    public void UpdateMinDensitySlider()
    {
        if (!initialized) return;
        assetSpawnData.spawnableAssets[assetIndex].minDensity = minDensitySlider.value;

        if (!undergroundToggle.isOn) return;
        RespawnAssets();
    }

    // Valueable
    public void UpdateValueableToggle()
    {
        if (!initialized) return;
        assetSpawnData.spawnableAssets[assetIndex].isValuable = valueableToggle.isOn;
        RespawnAssets();
    }

    public void UpdateMinValueInput()
    {
        if (!initialized) return;
        valueRangeSlider.SetValues(int.Parse(minValueInput.text), valueRangeSlider.Values.maxValue);
        assetSpawnData.spawnableAssets[assetIndex].minValue = int.Parse(minValueInput.text);

        if (!valueableToggle.isOn) return;
        RespawnAssets();
    }

    public void UpdateMaxValueInput()
    {
        if (!initialized) return;
        valueRangeSlider.SetValues(valueRangeSlider.Values.minValue, int.Parse(maxValueInput.text));
        assetSpawnData.spawnableAssets[assetIndex].maxValue = int.Parse(maxValueInput.text);

        if (!valueableToggle.isOn) return;
        RespawnAssets();
    }

    public void UpdateValueRangeSlider()
    {
        if (!initialized) return;
        assetSpawnData.spawnableAssets[assetIndex].minValue = (int)valueRangeSlider.Values.minValue;
        assetSpawnData.spawnableAssets[assetIndex].maxValue = (int)valueRangeSlider.Values.maxValue;

        if (!valueableToggle.isOn) return;
        RespawnAssets();
    }

    // Remove Asset from settings
    public void RemoveAsset()
    {
        if (!initialized) return;
        ClearAssets();
        assetSpawnData.spawnableAssets.RemoveAt(assetIndex);
        transform.SetParent(null);
        Destroy(gameObject);
        assetIndexUpdater.UpdateAllIndices();
        assetIndexUpdater.StartCoroutine(assetIndexUpdater.AssetRespawnCoroutine());
        // RespawnAssets();
    }

    public void BlockUI()
    {
        StartCoroutine(UICooldown());
    }

    // Block Updates until 
    IEnumerator UICooldown()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        yield return new WaitForSeconds(2.5f);

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
    
    // Clear assets and associated data
    public void ClearAssets()
    {
        assetSpawnData = ChunkGenNetwork.Instance.assetSpawnData;
        for (int i = 0; i < assetSpawnData.spawnableAssets.Count; i++)
        {
            foreach (Asset asset in assetSpawnData.spawnableAssets[i].spawnedAssets)
            {
                Destroy(asset.obj);
            }
        }
        assetSpawnData.ResetSpawnPoints();
    }

    // Regenerate the assets with the current asset settings
    public void RespawnAssets()
    {
        ClearAssets();
        assetIndexUpdater.StartCoroutine(assetIndexUpdater.AssetRespawnCoroutine());
    }
}
