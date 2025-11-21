using System.Diagnostics;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UsableAssetListController : MonoBehaviour
{
    public UseableAssetData assetData;
    public GameObject assetPreview;
    public GameObject assetSettingsTab;
    public GameObject assetList;
    public GameObject assetGrid;
    private bool instantiated = false;
    void OnEnable()
    {
        if(instantiated) return;

        foreach(AssetData asset in assetData.useableAssets)
        {
            GameObject preview = Instantiate(assetPreview, gameObject.transform);
            preview.GetComponentInChildren<RawImage>().texture = asset.icon;
            preview.GetComponentInChildren<TextMeshProUGUI>().text = asset.name;

            UseableAssetController useableAssetController = preview.GetComponent<UseableAssetController>();
            useableAssetController.assetList = assetList;
            useableAssetController.useableAssetList = gameObject;
            useableAssetController.assetGrid = assetGrid;

        }

        instantiated = true;
    }
}
