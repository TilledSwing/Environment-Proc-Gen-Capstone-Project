using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using FishNet.Object;
using FishNet.Connection;

public class PlacementController : NetworkBehaviour
{
    Camera playerCamera;
    public float interactDst;
    public LayerMask terrainLayerMask;
    public GameObject placeableObject;
    public GameObject placeableObjectNetwork;
    public float rotationSensitivity;
    private List<Material> originalMaterials = new();
    public Material placeableObjectMaterial;
    private List<Material> tempPlaceableObjectMaterialList = new();
    private GameObject currentObjectRef;
    private Renderer currentObjectRenderer;
    private Material[] currentObjectMaterials;
    private bool placementMode = false;
    private bool lastRayState = false;
    private float scrollRotation;
    //void Awake()
    //{
    //    playerCamera = Camera.main;
    //}

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!base.IsOwner)
            this.enabled = false;
        else
            playerCamera = Camera.main;
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
                    Quaternion normalRotationAlignment = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    Vector2 scroll = Mouse.current.scroll.ReadValue();

                    if (scroll.y != 0f)
                    {
                        scrollRotation += scroll.y * rotationSensitivity * Time.deltaTime;
                    }

                    currentObjectRef.transform.rotation = normalRotationAlignment * Quaternion.Euler(0f, scrollRotation, 0f);
                    Component[] components = placeableObject.GetComponents<Component>();
                    foreach (var c in components)
                    {
                        Debug.Log(c.GetType());
                    }
                }
                if (Input.GetKeyDown(KeyCode.F))
                {
                    PlaceManualAsset(currentObjectRef.transform.position, currentObjectRef.transform.rotation);
                    Destroy(currentObjectRef);
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

    /// <summary>
    /// The actual asset to place that gets synchronized over the network.
    /// </summary>
    [ServerRpc]
    public void PlaceManualAsset(Vector3 placementCoords, Quaternion placementRotation)
    {
        GameObject assetToPlace = Instantiate(placeableObjectNetwork, placementCoords, placementRotation);
        ServerManager.Spawn(assetToPlace);
    }
}
