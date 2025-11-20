using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class AssetSettingsTabController : MonoBehaviour
{
    AssetSpawnData assetSpawnData;
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
    public Toggle undegroundToggle;
    public TMP_InputField minDensityInput;
    public Slider minDensitySlider;
    // Valuable Settings
    public Toggle valueableToggle;
    public MinMaxSlider valueRangeSlider;

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

    public void RespawnAssets()
    {
        ClearAssets();
        StartCoroutine(AssetRespawnCoroutine());
    }

    IEnumerator AssetRespawnCoroutine()
    {
        int count = 0;
        foreach(var terrainChunk in ChunkGenNetwork.Instance.chunksVisibleLastUpdate)
        {
            terrainChunk.marchingCubes.assetSpawner.SpawnAssets();

            if(++count % 10 == 0) 
                yield return null;
        }
    }
}
