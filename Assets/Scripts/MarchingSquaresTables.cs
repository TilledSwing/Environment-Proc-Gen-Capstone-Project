using UnityEngine;

public class MarchingSquaresTables : MonoBehaviour
{
    public static readonly int[,] squareEdgeTable = new int[,] {
        {-1, -1, -1, -1},
        {0, 3, -1, -1},
        {0, 1, -1, -1},
        {1, 3, -1, -1},
        {1, 2, -1, -1},
        {0, 1, 2, 3},
        {0, 2, -1, -1},
        {2, 3, -1, -1},
        {2, 3, -1, -1},
        {0, 2, -1, -1},
        {0, 3, 1, 2},
        {1, 2, -1, -1},
        {1, 3, -1, -1},
        {0, 1, -1, -1},
        {0, 3, -1, -1},
        {-1, -1, -1, -1},
    };

    public static readonly int[,] triangleTable = new int[16, 6]
    {
        { -1, -1, -1, -1, -1, -1 }, // 0
        { 3, 0, -1, -1, -1, -1 },   // 1
        { 0, 1, -1, -1, -1, -1 },   // 2
        { 3, 1, -1, -1, -1, -1 },   // 3
        { 1, 2, -1, -1, -1, -1 },   // 4
        { 3, 0, 1, 2, -1, -1 },     // 5
        { 0, 2, -1, -1, -1, -1 },   // 6
        { 3, 2, -1, -1, -1, -1 },   // 7
        { 2, 3, -1, -1, -1, -1 },   // 8
        { 0, 2, -1, -1, -1, -1 },   // 9
        { 0, 1, 2, 3, -1, -1 },     // 10
        { 1, 2, -1, -1, -1, -1 },   // 11
        { 1, 3, -1, -1, -1, -1 },   // 12
        { 0, 1, -1, -1, -1, -1 },   // 13
        { 3, 0, -1, -1, -1, -1 },   // 14
        { -1, -1, -1, -1, -1, -1 }, // 15
    };

    // edgeOffsets for interpolation (like edgeV1 to edgeV2 in Marching Cubes)
    public static readonly Vector2[,] edgeOffsets = new Vector2[,] {
        { new Vector2(0.5f, 0), new Vector2(1, 0.5f) }, // edge 0
        { new Vector2(1, 0.5f), new Vector2(0.5f, 1) }, // edge 1
        { new Vector2(0.5f, 1), new Vector2(0, 0.5f) }, // edge 2
        { new Vector2(0, 0.5f), new Vector2(0.5f, 0) }, // edge 3
    };

}
