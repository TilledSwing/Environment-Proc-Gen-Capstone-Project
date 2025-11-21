using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Transporting;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static LobbyBroadcast;

public class LobbyBroadcast : MonoBehaviour
{
    public static LobbyBroadcast instance;

    public Transform lobbyHolder;
    public GameObject msgElement;
    public TMP_InputField nameField;
    public Dictionary<int, string> connectedPlayers;

    /// <summary>
    /// Setup event handlers and the local field. 
    /// </summary>
    private void OnEnable()
    {
        instance = this;
        connectedPlayers = new Dictionary<int, string>();
        InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;
        InstanceFinder.ServerManager.OnRemoteConnectionState += OnRemoteClientLeave;
        InstanceFinder.ClientManager.RegisterBroadcast<PlayerList>(OnPlayerListReceived);
        InstanceFinder.ServerManager.RegisterBroadcast<PlayerName>(OnRemoteJoin);
    }

    /// <summary>
    /// Disable event handlers.
    /// </summary>
    private void OnDisable()
    {
        InstanceFinder.ClientManager.UnregisterBroadcast<PlayerList>(OnPlayerListReceived);
        InstanceFinder.ServerManager.UnregisterBroadcast<PlayerName>(OnRemoteJoin);
        InstanceFinder.ServerManager.OnRemoteConnectionState -= OnRemoteClientLeave;
        InstanceFinder.ClientManager.OnClientConnectionState -= OnClientConnectionState;
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            string initialPlayerName = "";
            if (nameField != null)
            {
                initialPlayerName = nameField.text.Trim();
                if (string.IsNullOrEmpty(initialPlayerName))
                    initialPlayerName = "Anonymous";
            }
            else
            {
                initialPlayerName = BootstrapManager.getPersonalSteamName();
            }

            PlayerName playerName = new PlayerName() { connectedPlayer = initialPlayerName };
            InstanceFinder.ClientManager.Broadcast<PlayerName>(playerName);
        }
    }

    private void OnRemoteJoin(NetworkConnection connection, PlayerName name, Channel channel)
    {
        Debug.Log("This is the connection id: " + connection.ClientId.ToString());
        if (nameField != null && connection.ClientId.ToString() == "0")
            connectedPlayers.Add(connection.ClientId, name.connectedPlayer + " (Host)");
        else if (nameField == null && BootstrapManager.IsHost())
            connectedPlayers.Add(connection.ClientId, name.connectedPlayer + " (Host)");
        else
            connectedPlayers.Add(connection.ClientId, name.connectedPlayer);


        string[] curPlayerList = connectedPlayers.Values.ToArray();

        PlayerList playerList = new PlayerList() { connectedPlayers = curPlayerList };

        // Send to the new client with a small delay to ensure handler is registered
        StartCoroutine(DelayedPlayerBroadcast(playerList));

        //InstanceFinder.ServerManager.Broadcast<PlayerList>(playerList);
    }

    /// <summary>
    /// If a client joins the server then they are added to the list of current players.
    /// If a client leaves the server then they are removed from the list of current players.
    /// A broadcast is then sent out to all connected clients to update their lobby views.
    /// </summary>
    /// <param name="conn">The remote connection.</param>
    /// <param name="args">Parameters that come with the connection.</param>
    private void OnRemoteClientLeave(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            connectedPlayers.Remove(conn.ClientId);
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
            lobbyMessage.GetComponent<TextMeshProUGUI>().text = player;
                //== "0" ? "Player: Server Host" : "Player: Client " + player;
        }
    }

    /// <summary>
    /// Defines a PlayerList struct which contains the names of all currently connected players.
    /// </summary>
    public struct PlayerList : IBroadcast
    {
        public string[] connectedPlayers;
    }

    /// <summary>
    /// Defines a PlayerList struct which contains the names of all currently connected players.
    /// </summary>
    public struct PlayerName : IBroadcast
    {
        public string connectedPlayer;
    }
}
