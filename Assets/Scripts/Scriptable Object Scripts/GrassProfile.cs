using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GrassProfile", menuName = "Scriptable Objects/GrassProfile")]
public class GrassProfile : ScriptableObject
{
    [Header ("MAX 5 FOLIAGE")]
    public List<FoliageType> foliageList;
    public int grassDensity;
    public int maxBladesPerTriangle;
    public float maxGrassSlope;
    public bool useMinHeight;
    public float minHeight;
    public bool useMaxHeight;
    public float maxHeight;
    [Serializable]
    public struct FoliageType
    {
        public bool useUniformScale;
        public Mesh grassMesh;
        public Material grassMaterial;
        public Vector2 grassHeightRange;
        public Vector2 grassCurveRange;
        public float spawnProbabilityUpperThreshold;
    }
}