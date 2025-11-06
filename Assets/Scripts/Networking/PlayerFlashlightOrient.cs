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
        playerCamera = PlayerController.instance.playerCamera;
        Debug.Log("Retreived the camera");
    }

    void Update()
    {
        // Only apply updates to local player / owner of script.
        if (!base.IsOwner)
            return;

        SendFlashLightRotationServer(gameObject, PlayerController.instance.playerCamera.transform.rotation);
        // Check if flashlight hits enemy
        if (Time.time - lastFreezeTime < freezeCooldown)
            return;
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, flashlightRange, enemyLayer))
        {
            var enemy = hit.collider.GetComponentInParent<EnemyAILogic>();
            if (enemy != null && !enemy.isFrozen)
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
        EnemyAILogic enemy = enemyObj.GetComponent<EnemyAILogic>();
        if (enemy != null)
        {
            Debug.Log("Calling the EnemyAILogic freeze enemy script");
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
