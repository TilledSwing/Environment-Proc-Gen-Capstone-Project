using UnityEngine;

[CreateAssetMenu(fileName = "SkyboxLightingProfile", menuName = "Scriptable Objects/SkyboxLightingProfile")]
public class SkyboxLightingProfile : ScriptableObject
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
