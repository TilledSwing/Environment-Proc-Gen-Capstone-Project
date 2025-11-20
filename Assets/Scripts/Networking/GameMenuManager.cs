using Steamworks;
using System;
using TMPro;
using UnityEngine;

public class GameMenuManager : MonoBehaviour
{
    public static GameMenuManager instance;

    [SerializeField] private TMP_InputField lobbyInput;
    [SerializeField] private GameObject gameLobbyCanvas;
    [SerializeField] private GameObject inGameLobbyContainer;
    [SerializeField] private GameObject chatContainer;
    [SerializeField] private GameObject activeServerManager;

    private void Awake()
    {
        instance = this;
    }

    public void JoinLobby()
    {
        CSteamID steamID = new CSteamID(Convert.ToUInt64(lobbyInput.text));
        BootstrapManager.JoinByID(steamID);
    }

    public void CreateLobby()
    {
        BootstrapManager.CreateLobby();
    }

    public void DisableLobbyMenu()
    {
        gameLobbyCanvas.SetActive(false);
    }

    public void LoadPreGameFeatures()
    {
        inGameLobbyContainer.SetActive(true);
        chatContainer.SetActive(true);
        activeServerManager.SetActive(true);
    }
}
