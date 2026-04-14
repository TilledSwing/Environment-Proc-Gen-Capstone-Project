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
        sunTransform = sun.transform;
        moonTransform = moon.transform;

        // Rotation
        float currentCyle = time / dayNightCycleLength % 1f;
        float normalizedOffset = startOffset / 360f;
        timeOfDay = (currentCyle + normalizedOffset) % 1f;
        float angle = timeOfDay * 360f;
        sunTransform.rotation = Quaternion.Euler(angle, sunTransform.rotation.y, sunTransform.rotation.z);
        moonTransform.rotation = Quaternion.Euler(angle + 180f, moonTransform.rotation.y, moonTransform.rotation.z);
        moon.color = lightingSettings.NightLighting.lightColor;

        // Colors and Lighting
        sunHeight = Vector3.Dot(sunTransform.forward, Vector3.down);

        // Interpolation values
        float interpolator = Mathf.InverseLerp(-0.2f, 0.2f, sunHeight);
        float duskDawnFactor = 1f - Mathf.Abs(interpolator * 2f - 1f);
        depthFactor = Mathf.Clamp01(-ChunkGenNetwork.Instance.viewerPos.y * 0.01f);
        
        // Skybox/Fog color changes
        Color upperColor = Color.Lerp(
                                        Color.Lerp(lightingSettings.NightLighting.upperSkyColor, lightingSettings.DayLighting.upperSkyColor, interpolator), 
                                        lightingSettings.DuskAndDawnLighting.upperSkyColor, 
                                        duskDawnFactor);
        Color lowerColor = Color.Lerp(
                                        Color.Lerp(lightingSettings.NightLighting.lowerSkyColor, lightingSettings.DayLighting.lowerSkyColor, interpolator),
                                        lightingSettings.DuskAndDawnLighting.lowerSkyColor,
                                        duskDawnFactor);

        ChunkGenNetwork.Instance.fogMat.SetColor("_upperFogColor", upperColor);
        Color currentFog = Color.Lerp(lowerColor, ChunkGenNetwork.Instance.darkFogColor, depthFactor);
        ChunkGenNetwork.Instance.fogMat.SetColor("_lowerFogColor", currentFog);
        ChunkGenNetwork.Instance.waterMaterial.SetColor("_fogColor", currentFog);
        ChunkGenNetwork.Instance.starMaterial.SetColor("_StarColor", Color.Lerp(lightingSettings.starColor, Color.black, interpolator));

        // Directional Light Changes
        sun.intensity = Mathf.Lerp(0f, lightingSettings.DayLighting.lightIntensity, interpolator);
        moon.intensity = Mathf.Lerp(lightingSettings.NightLighting.lightIntensity, 0f, interpolator);

        sun.color = Color.Lerp(lightingSettings.DuskAndDawnLighting.lightColor, lightingSettings.DayLighting.lightColor, interpolator);
        sun.color = Color.Lerp(sun.color, lightingSettings.NightLighting.lightColor, depthFactor);

        // Ambient Light
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        float ambientIntensity = Mathf.Lerp(Mathf.Lerp(lightingSettings.NightLighting.ambientIntensity, lightingSettings.DayLighting.ambientIntensity, interpolator), 
                                                     lightingSettings.DuskAndDawnLighting.ambientIntensity, 
                                                     duskDawnFactor);
        ambientIntensity = Mathf.Lerp(ambientIntensity, 0.1f, depthFactor);
        RenderSettings.ambientSkyColor = Color.Lerp(upperColor * ambientIntensity, ChunkGenNetwork.Instance.darkFogColor, depthFactor);
        RenderSettings.ambientEquatorColor = Color.Lerp(Color.Lerp(upperColor, lowerColor, 0.5f) * ambientIntensity, ChunkGenNetwork.Instance.darkFogColor, depthFactor);
        RenderSettings.ambientGroundColor = Color.Lerp(lowerColor * ambientIntensity, ChunkGenNetwork.Instance.darkFogColor, depthFactor);
    }
    void Update()
    {
        if (!ChunkGenNetwork.Instance.initialLoadComplete) 
            return ;

        updateTimer += Time.deltaTime;
        time += Time.deltaTime;

        if (updateTimer < updateInterval) 
            return ;

        updateTimer -= updateInterval;

        // Rotation
        float currentCyle = time / dayNightCycleLength % 1f;
        float normalizedOffset = startOffset / 360f;
        timeOfDay = (currentCyle + normalizedOffset) % 1f;
        float angle = timeOfDay * 360f;
        sunTransform.rotation = Quaternion.Euler(angle, sunTransform.rotation.y, sunTransform.rotation.z);
        moonTransform.rotation = Quaternion.Euler(angle + 180f, moonTransform.rotation.y, moonTransform.rotation.z);

        // Colors and Lighting
        sunHeight = Vector3.Dot(sunTransform.forward, Vector3.down);
        if (sunHeight < -0.25f || sunHeight > 0.25f)

            return ;

        // Interpolation values
        float interpolator = Mathf.InverseLerp(-0.2f, 0.2f, sunHeight);
        float duskDawnFactor = 1f - Mathf.Abs(interpolator * 2f - 1f);
        depthFactor = Mathf.Clamp01(-ChunkGenNetwork.Instance.viewerPos.y * 0.01f);
        
        // Skybox/Fog color changes
        Color upperColor = Color.Lerp(
                                        Color.Lerp(lightingSettings.NightLighting.upperSkyColor, lightingSettings.DayLighting.upperSkyColor, interpolator), 
                                        lightingSettings.DuskAndDawnLighting.upperSkyColor, 
                                        duskDawnFactor);
        Color lowerColor = Color.Lerp(
                                        Color.Lerp(lightingSettings.NightLighting.lowerSkyColor, lightingSettings.DayLighting.lowerSkyColor, interpolator),
                                        lightingSettings.DuskAndDawnLighting.lowerSkyColor,
                                        duskDawnFactor);

        ChunkGenNetwork.Instance.fogMat.SetColor("_upperFogColor", upperColor);
        Color currentFog = Color.Lerp(lowerColor, ChunkGenNetwork.Instance.darkFogColor, depthFactor);
        ChunkGenNetwork.Instance.fogMat.SetColor("_lowerFogColor", currentFog);
        ChunkGenNetwork.Instance.waterMaterial.SetColor("_fogColor", currentFog);
        ChunkGenNetwork.Instance.starMaterial.SetColor("_StarColor", Color.Lerp(lightingSettings.starColor, Color.black, interpolator));

        // Directional Light Changes
        sun.intensity = Mathf.Lerp(0f, lightingSettings.DayLighting.lightIntensity, interpolator);
        moon.intensity = Mathf.Lerp(lightingSettings.NightLighting.lightIntensity, 0f, interpolator);

        sun.color = Color.Lerp(lightingSettings.DuskAndDawnLighting.lightColor, lightingSettings.DayLighting.lightColor, interpolator);
        sun.color = Color.Lerp(sun.color, lightingSettings.NightLighting.lightColor, depthFactor);

        // Ambient Light Changes
        float ambientIntensity = Mathf.Lerp(Mathf.Lerp(lightingSettings.NightLighting.ambientIntensity, lightingSettings.DayLighting.ambientIntensity, interpolator), 
                                                     lightingSettings.DuskAndDawnLighting.ambientIntensity, 
                                                     duskDawnFactor);
        ambientIntensity = Mathf.Lerp(ambientIntensity, 0.1f, depthFactor);
        RenderSettings.ambientSkyColor = Color.Lerp(upperColor * ambientIntensity, ChunkGenNetwork.Instance.darkFogColor, depthFactor);
        RenderSettings.ambientEquatorColor = Color.Lerp(Color.Lerp(upperColor, lowerColor, 0.5f) * ambientIntensity, ChunkGenNetwork.Instance.darkFogColor, depthFactor);
        RenderSettings.ambientGroundColor = Color.Lerp(lowerColor * ambientIntensity, ChunkGenNetwork.Instance.darkFogColor, depthFactor);
    }
}
