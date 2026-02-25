using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using FishNet.Object;

public class GlowballThrow : NetworkBehaviour
{
    Camera playerCamera;
    public GameObject glowball;
    public float throwForce = 20f;

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
        GameObject thrownBall = Instantiate(glowball, Camera.main.transform.position + transform.forward * 4f, Quaternion.identity);
        thrownBall.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        Rigidbody ballRB = thrownBall.GetComponent<Rigidbody>();
        ballRB.AddForce(lookdir * throwForce, ForceMode.Impulse);
        ServerManager.Spawn(thrownBall);
    }
}
