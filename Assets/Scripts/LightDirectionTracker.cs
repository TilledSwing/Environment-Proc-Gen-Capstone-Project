using UnityEngine;

public class LightDirectionTracker : MonoBehaviour
{    
    LightDirectionTracker Instance;
    public Vector3 mainLightDirection;
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
        mainLightDirection = transform.forward;
        Shader.SetGlobalVector("_sunDirection", mainLightDirection);
    }
}
