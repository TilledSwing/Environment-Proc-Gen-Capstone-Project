using Steamworks;
using System;
using TMPro;
using UnityEngine;

public class GameMenuManager : MonoBehaviour
{
    private static GameMenuManager instance;

    [SerializeField] private TMP_InputField lobbyInput;

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
}
