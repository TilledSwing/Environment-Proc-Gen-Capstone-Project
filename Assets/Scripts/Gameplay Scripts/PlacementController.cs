using System.Collections.Generic;
using UnityEngine;

public class PlacementController : MonoBehaviour
{
    Camera playerCamera;
    public float interactDst;
    public LayerMask terrainLayerMask;
    public GameObject placeableObject;
    private List<Material> originalMaterials = new();
    public Material placeableObjectMaterial;
    private List<Material> tempPlaceableObjectMaterialList = new();
    private GameObject currentObjectRef;
    private Renderer currentObjectRenderer;
    private Material[] currentObjectMaterials;
    private bool placementMode = false;
    private bool lastRayState = false;
    void Awake()
    {
        playerCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            placementMode = !placementMode;
            Destroy(currentObjectRef);
        }
        if (placementMode)
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, interactDst, terrainLayerMask))
            {
                lastRayState = true;
                if (currentObjectRef == null)
                {
                    currentObjectRef = Instantiate(placeableObject, hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
                    currentObjectRenderer = currentObjectRef.GetComponent<Renderer>();
                    currentObjectMaterials = currentObjectRenderer.materials;
                    for (int i = 0; i < currentObjectMaterials.Length; i++)
                    {
                        originalMaterials.Add(currentObjectMaterials[i]);
                        tempPlaceableObjectMaterialList.Add(placeableObjectMaterial);
                    }
                    currentObjectRenderer.materials = tempPlaceableObjectMaterialList.ToArray();
                }
                else
                {
                    currentObjectRef.transform.position = hit.point;
                    currentObjectRef.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                }
                if (Input.GetKeyDown(KeyCode.F))
                {
                    // // currentObjectRenderer = currentObjectRef.GetComponent<Renderer>();
                    // // currentObjectMaterials = currentObjectRenderer.materials;
                    // for (int i = 0; i < currentObjectMaterials.Length; i++)
                    // {
                    //     currentObjectMaterials[i] = originalMaterials[i];
                    //     // material.SetFloat("_alpha", 1);
                    //     // material.SetColor("_outlineColor", Color.black);
                    // }
                    currentObjectRenderer.materials = originalMaterials.ToArray();
                    currentObjectRef.GetComponent<BoxCollider>().enabled = true;
                    currentObjectRef = null;
                }
            }
            else if (lastRayState)
            {
                lastRayState = false;
                Destroy(currentObjectRef);
                currentObjectRef = null;
            }
        }
    }
}
