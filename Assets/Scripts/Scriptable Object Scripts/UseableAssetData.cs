using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UseableAssetData", menuName = "Scriptable Objects/UseableAssetData")]
public class UseableAssetData : ScriptableObject
{
    public List<AssetData> useableAssets;
}

[Serializable]
public class AssetData
{
    public GameObject obj;
    public Texture icon;
    public string name;
}