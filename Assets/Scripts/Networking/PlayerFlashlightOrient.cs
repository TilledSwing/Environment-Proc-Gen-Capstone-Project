using FishNet;
using FishNet.Connection;
using FishNet.Object;
using TMPro;
using UnityEngine;

public class PlayerFlashlightOrient : NetworkBehaviour
{
    public GameObject flashlight;
    private Camera playerCamera;
    public float flashlightRange = 10f;
    public LayerMask enemyLayer;
    private float freezeCooldown = 0.5f;
    private float lastFreezeTime = 0f;
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!base.IsOwner)
            this.enabled = false;
    }

    void Update()
    {
        // Wait for player to instantiate.
        if (PlayerController.instance == null)
            return;

        // Only apply updates to local player / owner of script.
        if (!base.IsOwner)
            return;

        SendFlashLightRotationServer(gameObject, PlayerController.instance.playerCamera.transform.rotation);
        // Check if flashlight hits enemy
        if (Time.time - lastFreezeTime < freezeCooldown)
            return;
        Ray ray = new Ray(PlayerController.instance.playerCamera.transform.position, PlayerController.instance.playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, flashlightRange, enemyLayer))
        {
            var enemy = hit.collider.GetComponentInParent<LandEnemyAILogic>();
            if (enemy != null)
            {
                // Tell the server to freeze this enemy
                FreezeEnemyServer(enemy.gameObject);
                lastFreezeTime = Time.time;

            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void FreezeEnemyServer(GameObject enemyObj)
    {
        LandEnemyAILogic enemy = enemyObj.GetComponent<LandEnemyAILogic>();
        if (enemy != null)
        {
            enemy.SetFrozen(true);
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
