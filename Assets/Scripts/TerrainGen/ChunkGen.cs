using System;
using UnityEngine;

public class ChunkGen : MonoBehaviour
{
    public float maxViewDst = 300;
    public Transform viewer;
    public static Vector3 viewerPos;
    public int chunkSize;
    public int chunksVisible;
    public TerrainDensityData terrainDensityData;

    void Start()
    {
        chunkSize = terrainDensityData.width;
        chunksVisible = Mathf.RoundToInt(maxViewDst/chunkSize);
    }
}
