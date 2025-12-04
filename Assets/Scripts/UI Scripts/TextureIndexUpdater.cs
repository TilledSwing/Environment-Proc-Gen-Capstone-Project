using UnityEngine;

public class TextureIndexUpdater : MonoBehaviour
{
    public void UpdateAllIndices()
    {
        int count = 0;
        foreach(Transform child in transform)
        {
            child.GetComponent<TextureSettingsTabController>().textureIndex = count;
            count++;
        }
    }
}
