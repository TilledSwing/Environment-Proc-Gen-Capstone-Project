using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

public class WaterMaterialSetup : MonoBehaviour
{
    public static WaterMaterialSetup Instance;
    private Material material;
    // public Material waterMaterial;
    public List<Wave> waves;
    ComputeBuffer waveBuffer;
    [Serializable]
    public struct Wave
    {
        public Vector4 direction;
        public float amplitude;
        public float wavelength;
        public float speed;
        public float phase;
    };
    void Awake()
    {
        // Make a singleton
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            if (waveBuffer != null)
            {
                waveBuffer = new(waves.Count, 32);
                waveBuffer.SetData(waves);
                material.SetBuffer("_Waves", waveBuffer);
                material.SetInt("_WaveCount", waves.Count);
            }
        }
    }

    public void SetupWaves(Material waterMaterial)
    {
        material = waterMaterial;
        waveBuffer = new(waves.Count, 32);
        waveBuffer.SetData(waves);
        waterMaterial.SetBuffer("_Waves", waveBuffer);
        waterMaterial.SetInt("_WaveCount", waves.Count);
    }

    void OnDestroy()
    {
        if (waveBuffer != null)
        {
            waveBuffer.Release();
        }
    }
}
