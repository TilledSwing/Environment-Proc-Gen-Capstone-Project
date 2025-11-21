using UnityEngine;
using FishNet.Object;

public class InitializeClientPanels : NetworkBehaviour
{
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!base.IsOwner)
            this.enabled = false;

        if (base.IsServerStarted)
            GameSceneManagement.instance.LoadHostMenuPanel();
        else
            GameSceneManagement.instance.LoadClientMenuPanel();
    }
}
