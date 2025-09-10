using UnityEngine;
using TMPro;
using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Transporting;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class LobbyBroadcast : MonoBehaviour
{
    public Transform lobbyHolder;
    public GameObject msgElement;
    private Dictionary<NetworkConnection, string> connectedPlayers;

    /// <summary>
    /// Setup event handlers and the local field. 
    /// </summary>
    private void OnEnable()
    {
        connectedPlayers = new Dictionary<NetworkConnection, string>();
        InstanceFinder.ServerManager.OnRemoteConnectionState += OnRemoteClient;
        InstanceFinder.ClientManager.RegisterBroadcast<PlayerList>(OnPlayerListReceived);
    }

    /// <summary>
    /// Disable event handlers.
    /// </summary>
    private void OnDisable()
    {
        InstanceFinder.ClientManager.UnregisterBroadcast<PlayerList>(OnPlayerListReceived);
        InstanceFinder.ServerManager.OnRemoteConnectionState -= OnRemoteClient;
    }

    /// <summary>
    /// If a client joins the server then they are added to the list of current players.
    /// If a client leaves the server then they are removed frmo the list of current players.
    /// A broadcast is then sent out to all connected clients to update their lobby views.
    /// </summary>
    /// <param name="conn">The remote connection.</param>
    /// <param name="args">Parameters that come with the connection.</param>
    private void OnRemoteClient(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            connectedPlayers.Add(conn, conn.ClientId.ToString());
            string[] curPlayerList = connectedPlayers.Values.ToArray();

            PlayerList playerList = new PlayerList() { connectedPlayers = curPlayerList};

            // Send to the new client with a small delay to ensure handler is registered
            StartCoroutine(DelayedPlayerBroadcast(playerList));

            //InstanceFinder.ServerManager.Broadcast<PlayerList>(playerList);
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            connectedPlayers.Remove(conn);
            string[] curPlayerList = connectedPlayers.Values.ToArray();

            PlayerList playerList = new PlayerList() { connectedPlayers = curPlayerList };
            InstanceFinder.ServerManager.Broadcast<PlayerList>(playerList);
        }
    }

    /// <summary>
    /// Waits 0.5 seconds after a new client has joined to broadcast the lobby list (gives the new client time to setup handlers).
    /// </summary>
    /// <param name="list"> The player list</param>
    /// <returns></returns>
    private IEnumerator DelayedPlayerBroadcast(PlayerList list)
    {
        yield return new WaitForSeconds(0.5f); 
        InstanceFinder.ServerManager.Broadcast<PlayerList>(list);
    }

    /// <summary>
    /// Client's event handler for receiving an updated player list. 
    /// Removes elements previously in the lobby and adds the updated lobby list.
    /// </summary>
    /// <param name="playerList"> The names / IDs of the current connected players. </param>
    /// <param name="channel"> The channel it was sent over (default Channel.Reliable which is like TCP)</param>
    private void OnPlayerListReceived(PlayerList playerList, Channel channel)
    {
        // Clear Lobby for Update.
        foreach (Transform child in lobbyHolder.transform)
            Destroy(child.gameObject);

        foreach (string player in playerList.connectedPlayers)
        {
            GameObject lobbyMessage = Instantiate(msgElement, lobbyHolder);

            // Player 0 is always the server host.
            lobbyMessage.GetComponent<TextMeshProUGUI>().text = player == "0" ? "Player: Server Host" : "Player: Client " + player;
        }
    }

    /// <summary>
    /// Defines a PlayerList struct which contains the names of all currently connected players.
    /// </summary>
    public struct PlayerList : IBroadcast
    {
        public string[] connectedPlayers;
    }
}
