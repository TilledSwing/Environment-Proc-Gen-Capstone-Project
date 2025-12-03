using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using FishNet.Object;
using FishNet.Connection;

public class BombThrow : NetworkBehaviour
{
    Camera playerCamera;
    public GameObject bomb;
    public float throwForce = 10f;
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
        
        //Debug.Log("In Bomb Throw Update");
        // Block input if in a chat message block. Ensures that typing words with certain letters or numbers won't trigger input events.
        if (EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            ThrowBomb(playerCamera.transform.forward);
        }
    }

    /// <summary>
    /// Throw a glowball object from in front of the player in the direction they are looking
    /// </summary>
    [ServerRpc]
    public void ThrowBomb(Vector3 lookdir)
    {
        GameObject thrownBall = Instantiate(bomb, gameObject.transform.position + gameObject.transform.forward * 2f, Quaternion.identity);
        Rigidbody bombRB = thrownBall.GetComponent<Rigidbody>();
        bombRB.AddForce(lookdir * throwForce, ForceMode.Impulse);
        ServerManager.Spawn(thrownBall);
    }

}
