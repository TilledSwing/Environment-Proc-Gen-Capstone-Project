using System;
using System.Collections.Generic;
using UnityEngine;

public class PriorityQueue<T>
{
    private List<(T item, float priority)> bstHeap = new();
    public int Count => bstHeap.Count;

    public void Enqueue(T item, float priority)
    {
        bstHeap.Add((item, priority));
        BSTHeapUp(bstHeap.Count - 1);
    }
    public T Dequeue()
    {
        if (bstHeap.Count == 0) throw new InvalidOperationException("Empty Queue");

        T resultItem = bstHeap[0].item;
        bstHeap[0] = bstHeap[^1];
        bstHeap.RemoveAt(bstHeap.Count - 1);
        BSTHeapDown(0);
        return resultItem;
    }
    public bool Contains(T item)
    {
        return bstHeap.Exists(heap => EqualityComparer<T>.Default.Equals(heap.item, item));
    }
    public void Clear()
    {
        bstHeap.Clear();
    }
    private void BSTHeapUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            if (bstHeap[index].priority >= bstHeap[parentIndex].priority) break;
            (bstHeap[index], bstHeap[parentIndex]) = (bstHeap[parentIndex], bstHeap[index]);
            index = parentIndex;
        }
    }
    private void BSTHeapDown(int index)
    {
        int lastIndex = bstHeap.Count - 1;

        while (true)
        {
            int smallestIndex = index;
            int leftIndex = 2 * index + 1;
            int rightIndex = 2 * index + 2;

            if (leftIndex <= lastIndex && bstHeap[leftIndex].priority < bstHeap[smallestIndex].priority) {
                smallestIndex = leftIndex;
            }
            if (rightIndex <= lastIndex && bstHeap[rightIndex].priority < bstHeap[smallestIndex].priority) {
                smallestIndex = rightIndex;
            }
            if (smallestIndex == index) break;
            (bstHeap[index], bstHeap[smallestIndex]) = (bstHeap[smallestIndex], bstHeap[index]);
            index = smallestIndex;
        }
    }
}
