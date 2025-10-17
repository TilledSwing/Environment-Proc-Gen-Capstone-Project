using System.Collections.Generic;
using System.Xml.Serialization;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class AssetSpawnFiltersNativeArrayPoolManager : MonoBehaviour
{
    public static AssetSpawnFiltersNativeArrayPoolManager Instance;
    Dictionary<string, Queue<NativeArray<AssetSpawner.AssetSpawnFilters>>> arrayPool = new();
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public NativeArray<AssetSpawner.AssetSpawnFilters> GetNativeArray(string arrayKey, int count)
    {
        if (arrayPool.TryGetValue(arrayKey, out Queue<NativeArray<AssetSpawner.AssetSpawnFilters>> arrayQueue) && arrayQueue.Count > 0)
        {
            return arrayQueue.Dequeue();
        }
        return new NativeArray<AssetSpawner.AssetSpawnFilters>(count, Allocator.Persistent);
    }

    public void ReturnNativeArray(string arrayKey, NativeArray<AssetSpawner.AssetSpawnFilters> nativeArray)
    {
        for (int i = 0; i < nativeArray.Length; i++)
        {
            nativeArray[i] = default;
        }
        if (!arrayPool.ContainsKey(arrayKey))
        {
            arrayPool[arrayKey] = new Queue<NativeArray<AssetSpawner.AssetSpawnFilters>>();
        }
        arrayPool[arrayKey].Enqueue(nativeArray);
    }

    private void OnDestroy()
    {
        ReleaseAllArrays();
    }

    private void OnDisbale()
    {
        ReleaseAllArrays();
    }

    private void ReleaseAllArrays()
    {
        foreach (var keyValuePair in arrayPool)
        {
            foreach (NativeArray<AssetSpawner.AssetSpawnFilters> nativeArray in keyValuePair.Value)
            {
                if (nativeArray != null)
                {
                    nativeArray.Dispose();
                }
            }
        }
        arrayPool.Clear();
    }
}