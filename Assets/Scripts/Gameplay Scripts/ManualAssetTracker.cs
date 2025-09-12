using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.VisualScripting.FullSerializer;

public static class ManualAssetTracker
{
    static GameObject Glowball;
    public static Dictionary<ManualAssetId, Type> PlaceableAssets = new Dictionary<ManualAssetId, Type>()
    {
        {ManualAssetId.GlowBall, typeof(GlowballLogic)}
    };


    //Returns the game object for a specified key if it exists
    public static GameObject Create(ManualAssetId key)
    {
        if (!PlaceableAssets.TryGetValue(key, out Type t))
            return null;

        GameObject obj = new GameObject(key.ToString());
        obj.AddComponent(t);
        return obj;
    }
}

public  enum ManualAssetId
{
    GlowBall
}

public class ManualAssetIdentification
{
    public ManualAssetId Id;
    public float xCord;
    public float yCord;
    public float zCord;
    public static List<ManualAssetIdentification> PlacedAssets = new List<ManualAssetIdentification>();

    public ManualAssetIdentification(ManualAssetId id, float xcord, float ycord, float zcord)
    {
        Id = id;
        xCord = xcord;
        yCord = ycord;
        zCord = zcord;

        PlacedAssets.Add(this);
        Debug.Log("There are this many manual Placed assets: " + PlacedAssets.Count);
    }

}


    