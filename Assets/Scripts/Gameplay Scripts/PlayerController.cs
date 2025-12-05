using FishNet.Object;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

// Template by Bobsi Unity - Youtube
// Modified by Jacob Ormsby

public class PlayerController : NetworkBehaviour
{
    public static PlayerController instance;

    [Header("Base setup")]
    public float walkingSpeed = 7.5f;
    public float runningSpeed = 20f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 75.0f;
    public float flightSpeed = 6.0f;
    public bool editorPlayer = true;
    public bool gameStarted = false;

    public List<Vector3> terraformCenters;
    public List<Vector3Int> hitChunkPositions;
    public List<int> terraformTypes;

    [HideInInspector]
    public bool canMove = true;
    public int waterLevel = 0;

    [SerializeField]
    private float cameraYOffset = 0.4f;
    public Camera playerCamera;

    private bool isFlightMode = false;
    private bool isSubmerged = false;
    private bool underwater = false;
    public bool dead = false;

    public CharacterController characterController;
    public Vector3 moveDirection = Vector3.zero;
    float rotationX = 0;


    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            playerCamera = Camera.main;
            //playerCamera = new Camera();
            playerCamera.transform.position = new Vector3(transform.position.x, transform.position.y + cameraYOffset, transform.position.z);
            playerCamera.transform.SetParent(transform);

            Transform eye1 = transform.Find("Visor");
            Transform eye2 = transform.Find("Visor (1)");

            eye1.gameObject.SetActive(false);
            eye2.gameObject.SetActive(false);

            if (base.IsServerStarted)
            {
                terraformCenters = new List<Vector3>();
                hitChunkPositions = new List<Vector3Int>();
                terraformTypes = new List<int>();
            }

            instance = this;
        }
        else
        {
            gameObject.GetComponent<PlayerController>().enabled = false;
        }
    }

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {

        // Only apply updates to local player / owner of script.
        if (!IsOwner)
            return;

        // Block input if in a chat message block. Ensures that typing words with certain letters or numbers won't trigger input events.
        if (EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null)
        {
            return;
        }

        bool isRunning = false;

        // Press S to run
        isRunning = Input.GetKey(KeyCode.RightShift);
        isSubmerged = transform.position.y < waterLevel;

        if (isSubmerged && ChunkGenNetwork.Instance.terrainDensityData.water)
        {
            gravity = 3.5f;
            jumpSpeed = 3.5f;

            if (!isFlightMode)
            {
                walkingSpeed = 3.5f;
                runningSpeed = 5.0f;
            }
        }
        else
        {
            gravity = 20.0f;
            jumpSpeed = 8.0f;
            walkingSpeed = 7.5f;
            runningSpeed = 20f;
        }
        if (playerCamera.transform.position.y - 0.08f < waterLevel && !underwater && ChunkGenNetwork.Instance.terrainDensityData.water)
        {
            underwater = true;
            GraphicsSettings.defaultRenderPipeline = ChunkGenNetwork.Instance.underwaterUrpAsset;
            QualitySettings.renderPipeline = ChunkGenNetwork.Instance.underwaterUrpAsset;
            ChunkGenNetwork.Instance.fogRenderPassFeature = ChunkGenNetwork.Instance.rendererData.rendererFeatures.Find(f => f is FogRenderPassFeature) as FogRenderPassFeature;
            ChunkGenNetwork.Instance.fogMat.SetFloat("_fogDensity", 0.018f);
            ChunkGenNetwork.Instance.fogMat.SetFloat("_fogOffset", -15f);
            ChunkGenNetwork.Instance.waterMaterial.SetFloat("_fogDensity", 0.018f);
            ChunkGenNetwork.Instance.waterMaterial.SetFloat("_fogOffset", -15f);

            // ChunkGenNetwork.Instance.waterMaterial.SetFloat("_fogDensity", 0.018f);
            // ChunkGenNetwork.Instance.waterMaterial.SetFloat("_fogOffset", -15f);
        }
        else if(playerCamera.transform.position.y - 0.08f > waterLevel && underwater && ChunkGenNetwork.Instance.terrainDensityData.water)
        {
            underwater = false;
            GraphicsSettings.defaultRenderPipeline = ChunkGenNetwork.Instance.mainUrpAsset;
            QualitySettings.renderPipeline = ChunkGenNetwork.Instance.mainUrpAsset;
            ChunkGenNetwork.Instance.fogRenderPassFeature = ChunkGenNetwork.Instance.rendererData.rendererFeatures.Find(f => f is FogRenderPassFeature) as FogRenderPassFeature;
            ChunkGenNetwork.Instance.fogMat.SetFloat("_fogDensity", ChunkGenNetwork.Instance.fogDensity);
            ChunkGenNetwork.Instance.fogMat.SetFloat("_fogOffset", ChunkGenNetwork.Instance.fogOffset);
            ChunkGenNetwork.Instance.waterMaterial.SetFloat("_fogDensity", ChunkGenNetwork.Instance.fogDensity);
            ChunkGenNetwork.Instance.waterMaterial.SetFloat("_fogOffset", ChunkGenNetwork.Instance.fogOffset);

            // ChunkGenNetwork.Instance.waterMaterial.SetFloat("_fogDensity", ChunkGenNetwork.Instance.fogDensity);
            // ChunkGenNetwork.Instance.waterMaterial.SetFloat("_fogOffset", ChunkGenNetwork.Instance.fogOffset);
        }

        // Goes from first person to a pseudo pause screen and vice versa on escape.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool isCursorVisible = Cursor.visible;
            Cursor.visible = !isCursorVisible;
            Cursor.lockState = isCursorVisible ? CursorLockMode.Locked : CursorLockMode.None;

            canMove = isCursorVisible;
        }

        // Allows player to switch between walking and flying mode. Only available in editor mode or after the player dies in game mode.
        if ((editorPlayer || dead) && Input.GetKeyDown(KeyCode.M))
            isFlightMode = !isFlightMode;

        // We are grounded, so recalculate move direction based on axis
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        float curSpeedX = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (!isFlightMode)
        {
            if (Input.GetButton("Jump") && canMove && (characterController.isGrounded || isSubmerged))
            {
                moveDirection.y = jumpSpeed;
            }
            else
            {
                moveDirection.y = movementDirectionY;
            }

            if (!characterController.isGrounded)
            {
                if (isSubmerged)
                {
                    // Dampen speed to match water "gravity". Smooth corrections for gravity that is close.
                    moveDirection.y = Mathf.Lerp(moveDirection.y, -gravity, Time.deltaTime * 3f);
                }
                else
                {
                    moveDirection.y -= gravity * Time.deltaTime;
                }
            }
        }
        else
        {
            moveDirection.y = 0f;

            // Free flight settings - not affected by gravity.
            if (Input.GetButton("Jump") && canMove)
            {
                moveDirection.y = flightSpeed;
            }
            else if (Input.GetKey(KeyCode.LeftShift) && canMove)
            {
                moveDirection.y = -flightSpeed;
            }
        }

        // Move the controller
        characterController.Move(moveDirection * Time.deltaTime);

        // Player and Camera rotation
        if (canMove && playerCamera != null)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }
    }
}
