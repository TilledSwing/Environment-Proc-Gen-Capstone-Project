using System.Collections.Generic;
using UnityEngine;

public class AssetObjectPoolManager : MonoBehaviour
{
    public AssetObjectPoolManager Instance;
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    Dictionary<string, Queue<Asset>> assetPool = new();
    public GameObject GetAsset(string assetType, GameObject assetToSpawn, Vector3 position, Quaternion rotation)
    {
        GameObject asset;
        if (!assetPool.TryGetValue(assetType, out Queue<Asset> assetQueue))
        {
            assetQueue = new();
            assetPool[assetType] = assetQueue;
        }
        if (assetQueue.Count == 0)
        {
            asset = Instantiate(assetToSpawn, position, rotation);
        }
        else
        {
            Asset assetData = assetQueue.Dequeue();
            asset = assetData.obj;
            asset.transform.position = position;
            asset.transform.rotation = rotation;

            if (assetData.meshRenderer != null)
                assetData.meshRenderer.enabled = true;
            else
                asset.SetActive(true);
            if (assetData.meshCollider != null)
                assetData.meshCollider.enabled = true;
        }
        
        return asset;
    }
    public void ReturnAsset(Asset asset)
    {
        if (asset.meshRenderer != null)
            asset.meshRenderer.enabled = false;
        else
            asset.obj.SetActive(false);
        if (asset.meshCollider != null)
            asset.meshCollider.enabled = false;
            
        assetPool.TryGetValue(asset.name, out Queue<Asset> assetQueue);
        assetQueue.Enqueue(asset);
    }
}
