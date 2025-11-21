using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class StartGameScript : NetworkBehaviour
{
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!base.IsOwner)
            this.enabled = false;
    }

    public void StartButtonPressed()
    {
        StartGameServer();
        //GameMenuManager.instance.DisableHostTerrainButtons();
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServer()
    {
        StartGameClients();
    }

    [ObserversRpc]
    public void StartGameClients()
    {
        //Debug.Log("Reached Observer");
        BootstrapManager.DisablePreGameLobby();
    }
}
