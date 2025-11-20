using FishNet;
using FishNet.Connection;
using FishNet.Object;
using TMPro;
using UnityEngine;

public class PlayerNameSender : NetworkBehaviour
{
    public TextMeshPro nameTag;
    [SerializeField] private bool inEditor = true;

    public override void OnStartClient()
    {
       base.OnStartClient();
       if (base.IsOwner)
       {
            string playerName = "";
            if (inEditor)
            {
                playerName = GameObject.Find("NetworkManager/NetworkHudCanvas/NameTextBox/MessageInput").GetComponent<TMP_InputField>().text;
                if (string.IsNullOrEmpty(playerName))
                    playerName = "Anonymous";
            }
            else
            {
                playerName = BootstrapManager.getPersonalSteamName();
            }

            // Updates new player name for players already in the server.
            SendNameTagServer(gameObject, playerName);
            SyncExistingNamesServer(LocalConnection);
        }
        else
            this.enabled = false;

    }

    [ServerRpc(RequireOwnership = false)]
    public void SendNameTagServer(GameObject player, string playerName)
    {
        UpdateNameTag(player, playerName);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SyncExistingNamesServer(NetworkConnection conn)
    {
        foreach (var clientConnection in InstanceFinder.ServerManager.Clients)
        {
            int clientConnID = clientConnection.Key;
            var clientConn = clientConnection.Value;

            if (clientConn != null)
            {
                if (clientConn == conn)
                    continue;
                else
                {
                    GameObject remotePlayer = clientConn.FirstObject.gameObject;
                    UpdateNameTag(remotePlayer, LobbyBroadcast.instance.connectedPlayers[clientConnID]);
                }
            }
        }
    }

    [ObserversRpc]
    public void UpdateNameTag(GameObject player, string playerName)
    {
        player.GetComponent<PlayerNameSender>().nameTag.text = playerName;
    }
}
