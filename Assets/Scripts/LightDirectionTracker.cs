using UnityEngine;

public class LightDirectionTracker : MonoBehaviour
{    
    void Update()
    {
        Shader.SetGlobalVector("_sunDirection", transform.forward);
    }
}
