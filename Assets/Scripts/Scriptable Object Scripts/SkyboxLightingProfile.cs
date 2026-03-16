using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[CreateAssetMenu(fileName = "SkyboxLightingProfile", menuName = "Scriptable Objects/SkyboxLightingProfile")]
public class SkyboxLightingProfile : ScriptableObject
{
    public LightingData DayLighting;
    public LightingData DuskAndDawnLighting;
    public LightingData NightLighting;
    [Serializable]
    public struct LightingData
    {
        public float lightIntensity;
        public Color lightColor;
        public Color upperSkyColor;
        public Color lowerSkyColor;
        public bool hasSunOrMoon;
        public Color sunOrMoonColor;
        public float sunOrMoonSize;
        public bool hasStars;
        public Color starColor;
    }
    /* 
        DAY
        public float lightIntensity = 4;
        public Color lightColor = FFF9DC;
        public Color upperSkyColor = 4B6DDB;
        public Color lowerSkyColor = 409FF5;
        public bool hasSunOrMoon = true;
        public Color sunOrMoonColor = FFF8AC;
        public float sunOrMoonSize = 0.1;
        public bool hasStars = false;
        public Color starColor = FFFFFF;

        NIGHT
        public float lightIntensity = 12;
        public Color lightColor = 335076;
        public Color upperSkyColor = 040F24;
        public Color lowerSkyColor = 1A2635;
        public bool hasSunOrMoon = true;
        public Color sunOrMoonColor = FFFFFF;
        public float sunOrMoonSize = 0.1;
        public bool hasStars = true;
        public Color starColor = FFFFFF;
    */
}
