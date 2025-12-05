using UnityEngine;

public class UseableAssetController : MonoBehaviour
{
    public UseableAssetData assetData;
    public GameObject assetSettingsTab;
    public GameObject assetList;
    public GameObject useableAssetList;
    public GameObject assetGrid;
    int assetIndex;

    void Start()
    {
        assetIndex = transform.GetSiblingIndex();
    }

    public void AddAsset()
    {
        AssetSettingsTabController assetTab = Instantiate(assetSettingsTab, assetList.transform).GetComponent<AssetSettingsTabController>();
        assetTab.assetSpawnData = ChunkGenNetwork.Instance.assetSpawnData;
        assetTab.canvasGroup = assetList.GetComponent<CanvasGroup>();

        Texture icon = assetData.useableAssets[assetIndex].icon;
        string name = assetData.useableAssets[assetIndex].name;
        GameObject obj = assetData.useableAssets[assetIndex].obj;
        assetTab.assetPreview.texture = icon;
        assetTab.assetName.text = name;
        SpawnableAsset spawnableAsset = new SpawnableAsset(obj, 1, false, 1, false, 0, false, 360, false, 0, false, 512, false, 5, false, 1, false, 1, 50);
        spawnableAsset.icon = icon;
        spawnableAsset.name = name;

        ChunkGenNetwork.Instance.assetSpawnData.spawnableAssets.Add(spawnableAsset);
        assetTab.assetIndex = assetTab.transform.GetSiblingIndex();

        assetGrid.SetActive(false);
        assetTab.initialized = true;
    }
}
