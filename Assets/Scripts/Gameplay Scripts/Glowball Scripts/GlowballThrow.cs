using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using FishNet.Object;
using FishNet.Connection;

public class GlowballThrow : NetworkBehaviour
{
    Camera playerCamera;
    public GameObject glowball;
    public float throwForce = 20f;

    //void Start()
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

        if (PlayerController.instance.dead)
            return;

        // Block input if in a chat message block. Ensures that typing words with certain letters or numbers won't trigger input events.
        if (EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            ThrowGlowball(playerCamera.transform.forward);
        }
    }
    /// <summary>
    /// Throw a glowball object from in front of the player in the direction they are looking
    /// </summary>
    [ServerRpc]
    public void ThrowGlowball(Vector3 lookdir)
    {
        GameObject thrownBall = Instantiate(glowball, transform.position + transform.up * 0.5f + transform.forward * 2f, Quaternion.identity);
        Rigidbody ballRB = thrownBall.GetComponent<Rigidbody>();
        ballRB.AddForce(lookdir * throwForce, ForceMode.Impulse);
        ServerManager.Spawn(thrownBall);
    }
}
