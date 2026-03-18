using UnityEngine;

public class LightDirectionTracker : MonoBehaviour
{    
    LightDirectionTracker Instance;
    public Transform sun;
    [HideInInspector]
    public Vector3 sunLightDirection;
    public Transform moon;
    [HideInInspector]
    public Vector3 moonLightDirection;
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
        sunLightDirection = sun.forward;
        Shader.SetGlobalVector("_sunDirection", sunLightDirection);
        moonLightDirection = moon.forward;
        Shader.SetGlobalVector("_moonDirection", moonLightDirection);
    }
}
