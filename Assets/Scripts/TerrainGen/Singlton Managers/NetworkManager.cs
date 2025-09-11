using FishNet.Object;
using UnityEngine;

public class NetworkManager : NetworkBehaviour
{
    /// <summary>
    /// Sets the player to the new viewer for chunk generation and disables the local chunk generator
    /// </summary>
    public override void OnStartClient()
    {
        base.OnStartClient();
        ChunkGenNetwork.Instance.viewer = GameObject.Find("Player(Clone)").transform;
        ChunkGenNetwork.Instance.SetFogActive(true);
        ChunkGenNetwork.Instance.objectiveCanvas.SetActive(true);
    }
}
