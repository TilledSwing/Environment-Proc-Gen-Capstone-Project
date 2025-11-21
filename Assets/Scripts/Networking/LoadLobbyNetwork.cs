using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet;

public class LoadLobbyNetwork : NetworkBehaviour
{

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!base.IsOwner)
            this.enabled = false;

        Debug.Log("Loaded");
        UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("GameMenu");
        SceneLoadData sld = new SceneLoadData("GameActive");
        InstanceFinder.SceneManager.LoadConnectionScenes(InstanceFinder.ClientManager.Connection, sld);
    }
}
