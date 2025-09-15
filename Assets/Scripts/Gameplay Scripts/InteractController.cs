using System;
using System.Collections;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InteractController : MonoBehaviour
{
    Camera playerCamera;
    public float interactDst;
    public LayerMask interactLayerMask;
    public Color interactHighlightColor;
    private GameObject objectiveCounterText;
    public int objectiveGoal;
    private int objectiveCounter = 0;
    private bool lastRayState = false;
    Renderer meshRenderer;
    Material[] materials;
    
    void Awake()
    {
        playerCamera = Camera.main;
    }

    void Update()
    {
        // Block input if in a chat message block. Ensures that typing words with certain letters or numbers won't trigger input events.
        if (EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null)
        {
            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactDst, interactLayerMask))
        {
            if (!lastRayState)
            {
                lastRayState = true;
                meshRenderer = hit.collider.gameObject.GetComponent<Renderer>();
                materials = meshRenderer.materials;
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
