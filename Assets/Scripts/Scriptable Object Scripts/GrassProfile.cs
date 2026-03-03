using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GrassProfile", menuName = "Scriptable Objects/GrassProfile")]
public class GrassProfile : ScriptableObject
{
    [Header ("MAX 5 FOLIAGE")]
    public Mesh grassMesh;
    public Material grassMaterial;
    public int grassDensity;
    public int maxBladesPerTriangle;
    public float maxGrassSlope;
    public Vector2 grassHeightRange;
    public Vector2 grassCurveRange;
    [Header ("SPAWN PROBABILITY MUST TOTAL 1")]
    public float spawnProbability;
}