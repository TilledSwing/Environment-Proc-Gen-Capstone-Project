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
    private TextMeshProUGUI text;
    public int objectiveGoal;
    private int objectiveCounter = 0;
    public bool lastRayState = false;
    public ScanObject currentObj;
    Renderer meshRenderer;
    Material[] originalMaterials;
    Material[] materials;

    void Awake()
    {
        playerCamera = Camera.main;
        // objectiveCounterText = GameObject.Find("Objective Counter");
        // text = objectiveCounterText.GetComponent<TextMeshProUGUI>();
        // Debug.Log($"Text assigned? {text != null}");
    }

    void Update()
    {
        // Wait for player to instantiate.
        if (PlayerController.instance == null)
            return;

        if (!PlayerController.instance.gameStarted && !PlayerController.instance.editorPlayer)
            return;

        // Block input if in a chat message block. Ensures that typing words with certain letters or numbers won't trigger input events.
        if (EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null)
        {
            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactDst, interactLayerMask))
        {
            // meshRenderer = hit.collider.gameObject.GetComponent<Renderer>();
            // originalMaterials = hit.collider.gameObject.GetComponent<ScanObject>().originalMaterials;
            // materials = meshRenderer.materials;
            // if (materials != originalMaterials) 
            //     meshRenderer.materials = originalMaterials;
            if (!lastRayState)
            {
                GameObject obj = hit.collider.gameObject;
                currentObj = obj.GetComponent<ScanObject>();
                meshRenderer = obj.GetComponent<Renderer>();
                originalMaterials = obj.GetComponent<ScanObject>().originalMaterials;
                materials = meshRenderer.materials;
                if (materials != originalMaterials)
                {
                    meshRenderer.materials = originalMaterials;
                    materials = originalMaterials;
                }
                lastRayState = true;
                foreach (Material material in materials)
                {
                    StartCoroutine(FadeInHightlight(material, 0.15f));
                }
            }
            if (Input.GetKeyDown(KeyCode.F))
            {
                // if (objectiveCounter < objectiveGoal) {
                //     text.text = ++objectiveCounter + "/" + objectiveGoal;
                // }
                objectiveCounterText = GameObject.Find("Objective Counter");
                text = objectiveCounterText.GetComponent<TextMeshProUGUI>();
                int value = hit.transform.gameObject.GetComponent<ValuableProperties>().value;
                // objectiveCounter += value;
                // text.text = "$" + objectiveCounter;
                StartCoroutine(EaseValueAdd(value, 0.5f));
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
    IEnumerator EaseValueAdd(int value, float duration)
    {
        float t = 0;
        int startValue = objectiveCounter;
        int endValue = objectiveCounter + value;
        while (t < duration)
        {
            t += Time.deltaTime;
            float ratio = Mathf.Clamp01(t / duration);
            int currentCounter = (int)Mathf.Lerp(startValue, endValue, ratio);
            text.text = "$" + currentCounter;
            yield return null;
        }
        objectiveCounter = endValue;
        text.text = "$" + objectiveCounter;
    }
}
