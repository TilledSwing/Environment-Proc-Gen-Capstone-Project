using System;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractController : MonoBehaviour
{
    Camera playerCamera;
    public float interactDst;
    public LayerMask interactLayerkMask;
    public Color interactHighlightColor;
    private GameObject objectiveCounterText;
    public int objectiveGoal;
    private int objectiveCounter = 0;
    private bool lastRayState = false;
    Renderer renderer;
    Material[] materials;
    
    void Awake()
    {
        playerCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactDst, interactLayerkMask))
        {
            if (!lastRayState)
            {
                lastRayState = true;
                renderer = hit.collider.gameObject.GetComponent<Renderer>();
                materials = renderer.materials;
                foreach (Material material in materials)
                {
                    StartCoroutine(FadeInHightlight(material, 0.15f));
                }
            }
            if (Input.GetKeyDown(KeyCode.F))
            {
                objectiveCounterText = GameObject.Find("ObjectiveCounter");
                TextMeshProUGUI text = objectiveCounterText.GetComponent<TextMeshProUGUI>();
                if (objectiveCounter < objectiveGoal) {
                    text.text = ++objectiveCounter + "/" + objectiveGoal;
                }
                foreach (Material material in materials)
                {
                    StartCoroutine(FadeOutDitherAndDestroy(material, hit.collider.gameObject, 0.5f));
                }
            }
        }
        else if (lastRayState)
        {
            lastRayState = false;
            foreach (Material material in materials)
            {
                StartCoroutine(FadeOutHightlight(material, 0.15f));
            }
        }
    }

    IEnumerator FadeInHightlight(Material mat, float duration)
    {
        float t = 0;
        Color startColor = mat.GetColor("_outlineColor");
        while (t < duration)
        {
            t += Time.deltaTime;
            float color = Mathf.Clamp01(t / duration);
            Color currentColor = Color.Lerp(startColor, interactHighlightColor, color);
            mat.SetColor("_outlineColor", currentColor);
            yield return null;
        }
    }
    IEnumerator FadeOutHightlight(Material mat, float duration)
    {
        float t = 0;
        Color startColor = mat.GetColor("_outlineColor");
        while (t < duration)
        {
            t += Time.deltaTime;
            float color = Mathf.Clamp01(t / duration);
            Color currentColor = Color.Lerp(startColor, Color.black, color);
            mat.SetColor("_outlineColor", currentColor);
            yield return null;
        }
    }
    IEnumerator FadeOutDitherAndDestroy(Material mat, GameObject gameObject, float duration)
    {
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / duration);
            mat.SetFloat("_fade", alpha);
            yield return null;
        }
        Destroy(gameObject);
    }
}
