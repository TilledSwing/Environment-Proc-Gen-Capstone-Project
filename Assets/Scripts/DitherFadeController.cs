using System.Collections;
using UnityEngine;

public class DitherFadeController : MonoBehaviour
{
    private Renderer meshRenderer;
    private Material[] materials;
    private bool prevRendState;
    void Awake()
    {
        meshRenderer = gameObject.GetComponent<Renderer>();
        materials = meshRenderer.materials;
        foreach (Material material in materials) {
            material.SetFloat("_fade", 1);
            material.SetColor("_outlineColor", Color.black);
            StartCoroutine(FadeIn(material, 1f));
        }
        prevRendState = meshRenderer.enabled;
    }
    void Update()
    {
        if (prevRendState != meshRenderer.enabled)
        {
            meshRenderer = gameObject.GetComponent<Renderer>();
            materials = meshRenderer.materials;
            foreach (Material material in materials)
            {
                material.SetFloat("_fade", 1);
                StartCoroutine(FadeIn(material, 1f));
            }
            prevRendState = meshRenderer.enabled;
        }
    }
    IEnumerator FadeIn(Material mat, float duration) {
        float t = 0;
        while (t < duration) {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / duration);
            mat.SetFloat("_fade", 1 - alpha);
            yield return null;
        }
    }

}
