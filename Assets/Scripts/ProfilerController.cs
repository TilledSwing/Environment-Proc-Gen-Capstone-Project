#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;
using UnityEngine.Profiling;

public class ProfilerToggle : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F10))
        {
            Profiler.enabled = !Profiler.enabled;
            Debug.Log("Profiler enabled: " + Profiler.enabled);
        }
    }
}
#endif