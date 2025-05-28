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
            ComputeBuffer computeBuffer = bufferQueue.Dequeue();
            // if (type == ComputeBufferType.Append || type == ComputeBufferType.Counter)
            // {
            //     computeBuffer.SetCounterValue(0);
            // }
            return computeBuffer;
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

    private void OnDestroy()
    {
        foreach (var keyValuePair in bufferPool)
        {
            foreach (ComputeBuffer computeBuffer in keyValuePair.Value)
            {
                computeBuffer.Release();
            }
        }
        bufferPool.Clear();
    }
}
