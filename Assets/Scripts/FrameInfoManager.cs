using TMPro;
using UnityEngine;

public class FrameInfoManager : MonoBehaviour
{
    public TextMeshProUGUI fpsLabel;
    public TextMeshProUGUI frameTimeLabel;
    float smoothedTime;
    float updateTimer;
    void Update()
    {
        float time = Time.unscaledDeltaTime;
        smoothedTime = Mathf.Lerp(smoothedTime, time, 0.5f);

        updateTimer += time;
        
        if(updateTimer > 0.1f)
        {
            fpsLabel.text = "FPS: " + Mathf.RoundToInt(1 / time);
            frameTimeLabel.text = (time * 1000).ToString("F1") + "ms";
            updateTimer = 0;
        }
    }
}
