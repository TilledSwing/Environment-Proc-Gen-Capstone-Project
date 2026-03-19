using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[CreateAssetMenu(fileName = "SkyboxLightingProfile", menuName = "Scriptable Objects/SkyboxLightingProfile")]
public class SkyboxLightingProfile : ScriptableObject
{
    public LightingData DayLighting;
    public Color sunColor;
    public float sunSize;
    public LightingData DuskAndDawnLighting;
    public LightingData NightLighting;
    public Color moonColor;
    public float moonSize;
    public Color starColor;
    [Serializable]
    public struct LightingData
    {
        public float lightIntensity;
        public float ambientIntensity;
        public Color lightColor;
        public Color upperSkyColor;
        public Color lowerSkyColor;
    }
}
