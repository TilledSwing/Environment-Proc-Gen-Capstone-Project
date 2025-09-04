using System.Collections;
using System.Collections.Generic;
using GameKit.Dependencies.Utilities.ObjectPooling;
using UnityEngine;

public static class ListPoolManager<T>
{
    private static Stack<List<T>> listPool = new();

    public static List<T> Get()
    {
        return listPool.Count > 0 ? listPool.Pop() : new List<T>();
    }
    public static void Return(List<T> list)
    {
        list.Clear();
        listPool.Push(list);
    }
}
