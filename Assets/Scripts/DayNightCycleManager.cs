using UnityEngine;

public class DayNightCycleManager : MonoBehaviour
{
    public Light sun;
    [HideInInspector]
    public Transform sunTransform;
    public Light moon;
    [HideInInspector]
    public Transform moonTransform;
    public float dayNightCycleLength;
    public float startOffset;
    public float sunHeight;
    // [HideInInspector]
    public float time;
    public float timeOfDay;
    public float updateInterval;
    float updateTimer;
    [HideInInspector]
    public SkyboxLightingProfile lightingSettings;
    [HideInInspector]
    public float depthFactor;
    void Start()
    {
        lightingSettings = ChunkGenNetwork.Instance.terrainConfig.lightingSettings;
        sun.color = lightingSettings.DayLighting.lightColor;
        moon.color = lightingSettings.NightLighting.lightColor;
        sunTransform = sun.transform;
        moonTransform = moon.transform;
    }
    void Update()
    {
        if (!ChunkGenNetwork.Instance.initialLoadComplete) 
            return ;

        updateTimer += Time.deltaTime;
        time += Time.deltaTime;

        // Rotation
        float currentCyle = time / dayNightCycleLength % 1f;
        float normalizedOffset = startOffset / 360f;
        timeOfDay = (currentCyle + normalizedOffset) % 1f;
        float angle = timeOfDay * 360f;
        sunTransform.rotation = Quaternion.Euler(angle, sunTransform.rotation.y, sunTransform.rotation.z);
        moonTransform.rotation = Quaternion.Euler(angle + 180f, moonTransform.rotation.y, moonTransform.rotation.z);

        if (updateTimer < updateInterval) 
            return ;

        updateTimer -= updateInterval;
        
        // Colors and Lighting
        depthFactor = Mathf.Clamp01(-ChunkGenNetwork.Instance.viewerPos.y * 0.01f);
        sunHeight = Vector3.Dot(sunTransform.forward, Vector3.down);

        float interpolator = Mathf.InverseLerp(-0.2f, 0.2f, sunHeight);
        float duskDawnFactor = 1f - Mathf.Abs(interpolator * 2f - 1f);
        sun.intensity = Mathf.Lerp(0f, lightingSettings.DayLighting.lightIntensity, interpolator);
        moon.intensity = Mathf.Lerp(lightingSettings.NightLighting.lightIntensity, 0f, interpolator);

        sun.color = Color.Lerp(lightingSettings.DuskAndDawnLighting.lightColor, lightingSettings.DayLighting.lightColor, interpolator);
        ChunkGenNetwork.Instance.starMaterial.SetColor("_StarColor", Color.Lerp(lightingSettings.starColor, Color.black, interpolator));
        
        Color upperColor = Color.Lerp(
                                        Color.Lerp(lightingSettings.NightLighting.upperSkyColor, lightingSettings.DayLighting.upperSkyColor, interpolator), 
                                        lightingSettings.DuskAndDawnLighting.upperSkyColor, 
                                        duskDawnFactor);
        Color lowerColor = Color.Lerp(
                                        Color.Lerp(lightingSettings.NightLighting.lowerSkyColor, lightingSettings.DayLighting.lowerSkyColor, interpolator),
                                        lightingSettings.DuskAndDawnLighting.lowerSkyColor,
                                        duskDawnFactor);
        ChunkGenNetwork.Instance.fogMat.SetColor("_upperFogColor", upperColor);
        ChunkGenNetwork.Instance.fogMat.SetColor("_lowerFogColor", lowerColor);
        ChunkGenNetwork.Instance.waterMaterial.SetColor("_fogColor", lowerColor);

        Color currentFog = Color.Lerp(lowerColor, ChunkGenNetwork.Instance.darkFogColor, depthFactor);
        ChunkGenNetwork.Instance.fogMat.SetColor("_lowerFogColor", currentFog);
        ChunkGenNetwork.Instance.waterMaterial.SetColor("_fogColor", currentFog);
    }
}
