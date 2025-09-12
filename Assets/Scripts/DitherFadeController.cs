using System.Collections;
using UnityEngine;

public class DitherFadeController : MonoBehaviour
{
    private Renderer renderer;
    private Material[] materials;
    private bool prevRendState;
    void Awake()
    {
        renderer = gameObject.GetComponent<Renderer>();
        materials = renderer.materials;
        foreach (Material material in materials) {
            material.SetFloat("_fade", 1);
            material.SetColor("_outlineColor", Color.black);
            StartCoroutine(FadeIn(material, 1f));
        }
        prevRendState = renderer.enabled;
    }
    void Update()
    {
        if (prevRendState != renderer.enabled)
        {
            renderer = gameObject.GetComponent<Renderer>();
            materials = renderer.materials;
            foreach (Material material in materials)
            {
                material.SetFloat("_fade", 1);
                StartCoroutine(FadeIn(material, 1f));
            }
            prevRendState = renderer.enabled;
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
