using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class VertexNativeArrayPoolManager : MonoBehaviour
{
    public static VertexNativeArrayPoolManager Instance;
    Dictionary<string, Queue<NativeArray<ComputeMarchingCubes.Vertex>>> arrayPool = new();
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

    public NativeArray<ComputeMarchingCubes.Vertex> GetNativeArray(string arrayKey, int count)
    {
        if (arrayPool.TryGetValue(arrayKey, out Queue<NativeArray<ComputeMarchingCubes.Vertex>> arrayQueue) && arrayQueue.Count > 0)
        {
            return arrayQueue.Dequeue();
        }
        return new NativeArray<ComputeMarchingCubes.Vertex>(count, Allocator.Persistent);
    }

    public void ReturnNativeArray(string arrayKey, NativeArray<ComputeMarchingCubes.Vertex> nativeArray)
    {
        for (int i = 0; i < nativeArray.Length; i++)
        {
            nativeArray[i] = default;
        }
        if (!arrayPool.ContainsKey(arrayKey))
        {
            arrayPool[arrayKey] = new Queue<NativeArray<ComputeMarchingCubes.Vertex>>();
        }
        arrayPool[arrayKey].Enqueue(nativeArray);
    }

    private void OnDestroy()
    {
        ReleaseAllArrays();
    }
    
    private void OnDisable()
    {
        ReleaseAllArrays();
    }

    private void ReleaseAllArrays()
    {
        foreach (var keyValuePair in arrayPool)
        {
            foreach (NativeArray<ComputeMarchingCubes.Vertex> nativeArray in keyValuePair.Value)
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