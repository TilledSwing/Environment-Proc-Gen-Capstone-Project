using UnityEngine;
using TMPro;
using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Transporting;


public class ChatBroadcast : MonoBehaviour
{
    public Transform chatHolder;
    public GameObject msgElement;
    public TMP_InputField playerMsg;
    public TMP_InputField nameField;


    /// <summary>
    /// Setup Broadcast event handlers.
    /// </summary>
    private void OnEnable()
    {
        InstanceFinder.ClientManager.RegisterBroadcast<Message>(OnMessageReceived);
        InstanceFinder.ServerManager.RegisterBroadcast<Message>(OnClientMessageReceived);
    }

    /// <summary>
    /// Disable Broadcast event handlers.
    /// </summary>
    private void OnDisable()
    {
        InstanceFinder.ClientManager.UnregisterBroadcast<Message>(OnMessageReceived);
        InstanceFinder.ServerManager.UnregisterBroadcast<Message>(OnClientMessageReceived);
    }

    /// <summary>
    /// When a client presses the enter key it will send a message in their chatbox.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            SendMessage();
        }
    }


    /// <summary>
    /// Broadcasts a message over the network. 
    /// Distinguishes automatically between server host / remote client sending it.
    /// </summary>
    private void SendMessage()
    {
        // Don't send empty messages
        if (playerMsg.text == "")
        {
            return;
        }

        Message msg = new Message()
        {
            username = "Anonymous",
            message = playerMsg.text
        };

        playerMsg.text = "";

        var conn = InstanceFinder.ClientManager.Connection;
        msg.username = LobbyBroadcast.instance.connectedPlayers[conn];
        InstanceFinder.ServerManager.Broadcast<Message>(msg);

        // Distinguishes if the server host or the remote client sent the message.
        // Remote Client 1, 2, 3... ordered by when they joined.
        //if (InstanceFinder.IsServerStarted)
        //{
        //msg.username = LobbyBroadcast.instance.connectedPlayers[conn]
        //    InstanceFinder.ServerManager.Broadcast<Message>(msg);
        //}
        //else if (InstanceFinder.IsClientStarted)
        //{
        //    msg.username = "Client " + conn.ClientId.ToString();
        //    InstanceFinder.ClientManager.Broadcast<Message>(msg);
        //}

    }

    /// <summary>
    /// Client's event handler for receiving a message from the server.
    /// Updates their Unity view of the chatbox with the message.
    /// </summary>
    /// <param name="message"> The message to send </param>
    /// <param name="channel"> The channel it was sent over (default Channel.Reliable which is like TCP)</param>
    private void OnMessageReceived(Message message, Channel channel)
    {
        GameObject finalMessage = Instantiate(msgElement, chatHolder);
        finalMessage.GetComponent<TextMeshProUGUI>().text = message.username + ": " + message.message;
    }

    /// <summary>
    /// Server's event handler for receiving a message from a client.
    /// Broadcasts the message to all connected clients.
    /// </summary>
    /// <param name="connection"> The client sending the message (unused in this case). </param>
    /// <param name="message"> The message that was sent. </param>
    /// <param name="channel"> The channel it was sent over (default Channel.Reliable which is like TCP) </param>
    private void OnClientMessageReceived(NetworkConnection connection, Message message, Channel channel)
    {
        InstanceFinder.ServerManager.Broadcast(message);
    }

    /// <summary>
    /// Defines a Message Struct which is broadcast over the network to clients / server. 
    /// </summary>
    public struct Message : IBroadcast
    {
        public string username;
        public string message;
    }
}
