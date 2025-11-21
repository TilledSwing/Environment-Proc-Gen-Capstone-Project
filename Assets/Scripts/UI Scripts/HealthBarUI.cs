using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;
public class HealthBarUI : NetworkBehaviour
{
    public Slider healthSlider;
    public Transform target; 
    public Vector3 offset = new Vector3(0, 1.3f, 0);

    public Image fillImage;       // assign the Fill image here
    public Color highHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    private Camera mainCam;

    public override void OnStartClient()
    {
        base.OnStartClient();
        mainCam = Camera.main;

        // Only show for other players
        NetworkObject targetNetObj = target.GetComponentInParent<NetworkObject>();
        if (targetNetObj != null && targetNetObj.IsOwner)
        {
            healthSlider.gameObject.SetActive(false);
        }
        else
        {
            healthSlider.gameObject.SetActive(true);
        }

        // Position initially
        healthSlider.transform.position = target.position + offset;
        healthSlider.transform.forward = Camera.main.transform.forward;
    }
    void LateUpdate()
    {
        if (target == null || !healthSlider.gameObject.activeSelf)
            return;
        
        healthSlider.transform.position = target.position + offset;

        // Face the camera
        if (mainCam != null)
            healthSlider.transform.forward = mainCam.transform.forward;
        
    }

    public void SetHealth(float current, float max)
    {
        float normalized = current / max;
        healthSlider.value = normalized;

        fillImage.color = (normalized <= 0.33f) ? lowHealthColor : highHealthColor;
    }
}
