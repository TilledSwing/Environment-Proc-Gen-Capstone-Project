using System.Collections;
using UnityEngine;

public class ScanObject : MonoBehaviour
{
    Material originalMaterial;
    Color originalColor;
    float originalSmoothness;
    float originalAlpha;
    Renderer myRenderer;
    void Awake()
    {
        myRenderer = gameObject.GetComponent<Renderer>();
        originalMaterial = myRenderer.material;
        originalColor = originalMaterial.GetColor("_outlineColor");
        originalSmoothness = originalMaterial.GetFloat("_smoothness");
        originalAlpha = originalMaterial.GetFloat("_alpha");
    }

    public IEnumerator SetScan(Material scanMaterial, float scanDuration)
    {
        myRenderer.material = scanMaterial;
        yield return new WaitForSeconds(scanDuration);
        myRenderer.material = originalMaterial;
    }
}
