using FishNet;
using FishNet.Connection;
using FishNet.Object;
using TMPro;
using UnityEngine;

public class PlayerFlashlightOrient : NetworkBehaviour
{
    public GameObject flashlight;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!base.IsOwner)
            this.enabled = false;
    }

    void Update()
    {
        // Only apply updates to local player / owner of script.
        if (!base.IsOwner)
            return;

        SendFlashLightRotationServer(gameObject, PlayerController.instance.playerCamera.transform.rotation);
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
