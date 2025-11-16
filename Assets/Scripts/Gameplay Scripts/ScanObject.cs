using System.Collections;
using UnityEngine;

public class ScanObject : MonoBehaviour
{
    public Material[] originalMaterials;
    Color originalColor;
    float originalSmoothness;
    float originalAlpha;
    Renderer myRenderer;
    void Awake()
    {
        myRenderer = gameObject.GetComponent<Renderer>();
        originalMaterials = myRenderer.materials;
        // originalColor = originalMaterial.GetColor("_outlineColor");
        // originalSmoothness = originalMaterial.GetFloat("_smoothness");
        // originalAlpha = originalMaterial.GetFloat("_alpha");
    }

    public IEnumerator SetScan(Material scanMaterial, float scanDuration)
    {
        myRenderer.material = scanMaterial;
        yield return new WaitForSeconds(scanDuration);
        if (myRenderer != null)
            myRenderer.materials = originalMaterials;
    }
}
