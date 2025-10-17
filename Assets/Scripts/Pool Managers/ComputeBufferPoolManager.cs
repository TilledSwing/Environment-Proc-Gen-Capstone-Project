using System.Collections.Generic;
using UnityEngine;

public class ComputeBufferPoolManager : MonoBehaviour
{
    public static ComputeBufferPoolManager Instance;
    Dictionary<string, Queue<ComputeBuffer>> bufferPool = new();
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

    public ComputeBuffer GetComputeBuffer(string bufferKey, int count, int stride, ComputeBufferType type = ComputeBufferType.Default)
    {
        if (bufferPool.TryGetValue(bufferKey, out Queue<ComputeBuffer> bufferQueue) && bufferQueue.Count > 0)
        {
            return bufferQueue.Dequeue();
        }
        return new ComputeBuffer(count, stride, type);
    }

    public void ReturnComputeBuffer(string bufferKey, ComputeBuffer computeBuffer)
    {
        if (!bufferPool.ContainsKey(bufferKey))
        {
            bufferPool[bufferKey] = new Queue<ComputeBuffer>();
        }
        bufferPool[bufferKey].Enqueue(computeBuffer);
    }

    private void OnDisable()
    {
        ReleaseAllBuffers();
    }

    private void OnDestroy()
    {
        ReleaseAllBuffers();
    }

    private void ReleaseAllBuffers()
    {
        foreach (var keyValuePair in bufferPool)
        {
            foreach (ComputeBuffer computeBuffer in keyValuePair.Value)
            {
                if (computeBuffer != null && computeBuffer.IsValid()) {
                    computeBuffer.Release();
                }
            }
        }
        bufferPool.Clear();
    }
}
