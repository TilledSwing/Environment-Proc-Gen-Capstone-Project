using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Connection;
using FishNet.Object;

// Template by Bobsi Unity - Youtube
// Modified by Jacob Ormsby

public class PlayerController : NetworkBehaviour {
    [Header("Base setup")]
    public float walkingSpeed = 7.5f;
    public float runningSpeed = 20f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 75.0f;
    public float flightSpeed = 6.0f;

    CharacterController characterController;
    Vector3 moveDirection = Vector3.zero;
    float rotationX = 0;

    [HideInInspector]
    public bool canMove = true;

    [SerializeField]
    private float cameraYOffset = 0.4f;
    private Camera playerCamera;

    private bool isFlightMode = false;


    public override void OnStartClient() {
        base.OnStartClient();
        if (base.IsOwner) {
            playerCamera = Camera.main;
            playerCamera.transform.position = new Vector3(transform.position.x, transform.position.y + cameraYOffset, transform.position.z);
            playerCamera.transform.SetParent(transform);
        }
        else {
            gameObject.GetComponent<PlayerController>().enabled = false;
        }
    }

    void Start() {
        characterController = GetComponent<CharacterController>();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update() {

        // Only apply updates to local player / owner of script.
        if (!IsOwner)
            return;

        bool isRunning = false;

        // Press S to run
        isRunning = Input.GetKey(KeyCode.RightShift);


        // Goes from first person to a pseudo pause screen and vice versa on escape.
        if (Input.GetKeyDown(KeyCode.Escape)) {
            bool isCursorVisible = Cursor.visible;
            Cursor.visible = !isCursorVisible;
            Cursor.lockState = isCursorVisible ? CursorLockMode.Locked : CursorLockMode.None;

            canMove = isCursorVisible;
        }

        // Allows player to switch between walking and flying mode.
        if (Input.GetKeyDown(KeyCode.M))
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
            if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
            {
                moveDirection.y = jumpSpeed;
            }
            else
            {
                moveDirection.y = movementDirectionY;
            }

            if (!characterController.isGrounded)
            {
                moveDirection.y -= gravity * Time.deltaTime;
            }
        } else
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
        if (canMove && playerCamera != null) {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }
    }
}
