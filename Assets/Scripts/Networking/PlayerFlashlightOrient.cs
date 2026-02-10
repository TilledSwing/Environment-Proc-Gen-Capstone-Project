using FishNet;
using FishNet.Connection;
using FishNet.Object;
using TMPro;
using UnityEngine;

public class PlayerFlashlightOrient : NetworkBehaviour
{
    public GameObject flashlight;
    public GameObject nameTag;
    private Camera playerCamera;
    public float flashlightRange = 10f;
    public LayerMask enemyLayer;
    private float freezeCooldown = 0.5f;
    private float lastFreezeTime = 0f;
    public override void OnStartClient()
    {
        base.OnStartClient();
        //if (!base.IsOwner)
        //    this.enabled = false;
    }

    void Update()
    {
        // Wait for player to instantiate.
        if (PlayerController.instance == null)
            return;

        // Only apply updates to local player / owner of script.
        if (!base.IsOwner)
        {
            nameTag.transform.LookAt(PlayerController.instance.playerCamera.transform);
            nameTag.transform.Rotate(0f, 180f, 0f);
        }
        else
        { 
            SendFlashLightRotationServer(gameObject, PlayerController.instance.playerCamera.transform.rotation);

            if (PlayerController.instance.dead)
                return;
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void SendFlashLightRotationServer(GameObject player, Quaternion flashLightRotation)
    {
        UpdateFlashLightRotation(player, flashLightRotation);
    }

    [ObserversRpc]
    public void UpdateFlashLightRotation(GameObject player, Quaternion flashLightRotation)
    {
        player.GetComponent<PlayerFlashlightOrient>().flashlight.transform.rotation = flashLightRotation;
    }
}
