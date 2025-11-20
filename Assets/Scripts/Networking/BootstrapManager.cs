// Tutorial by Bobsi on YouTube, edited by Jacob Ormsby

using UnityEngine;
using UnityEngine.SceneManagement;
using FishNet.Managing;
using Steamworks;

public class BootstrapManager : MonoBehaviour
{
    private static BootstrapManager instance;

    [SerializeField] private string menuName = "GameActive";
    [SerializeField] private FishNet.Managing.NetworkManager _networkManager;
    [SerializeField] private FishySteamworks.FishySteamworks _fishySteamworks;

    protected Callback<LobbyCreated_t> LobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> JoinRequest;
    protected Callback<LobbyEnter_t> LobbyEntered;

    public static ulong CurrentLobbyID;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        JoinRequest = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
        LobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    public static void CreateLobby()
    {
        // Max number of people in the lobby is 4 and only Steam friends can join.
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 4);
    }

    public void LoadMenu()
    {
        SceneManager.LoadScene(menuName, LoadSceneMode.Additive);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        //Debug.Log("Starting Lobby Creation: " + callback.m_eResult.ToString());

        // If the lobby failed to create, don't do anything else.
        if (callback.m_eResult != EResult.k_EResultOK)
            return;

        // Set up the lobby's connection address with the host's steam ID.
        CurrentLobbyID = callback.m_ulSteamIDLobby;
        SteamMatchmaking.SetLobbyData(new CSteamID(CurrentLobbyID), "HostAddress", SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(new CSteamID(CurrentLobbyID), "name", SteamFriends.GetPersonaName().ToString() + "'s PEGG Lobby");
        _fishySteamworks.SetClientAddress(SteamUser.GetSteamID().ToString());
        _fishySteamworks.StartConnection(true);
        Debug.Log("Lobby Successfully Created");
    }

    private void OnJoinRequest(GameLobbyJoinRequested_t callback)
    {
        // Check if game has already started.
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        CurrentLobbyID = callback.m_ulSteamIDLobby;
        _fishySteamworks.SetClientAddress(SteamMatchmaking.GetLobbyData(new CSteamID(CurrentLobbyID), "HostAddress"));
        _fishySteamworks.StartConnection(false);

        // Start up game sequence
        GameMenuManager.instance.DisableLobbyMenu();
        GameMenuManager.instance.LoadPreGameFeatures();
        // UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(menuName);
        // SceneManager.LoadScene("GameActive", LoadSceneMode.Additive);
    }

    public static void JoinByID(CSteamID steamID)
    {
        Debug.Log("Attempting to Join Steam Lobby with ID: " + steamID.m_SteamID);
        if (SteamMatchmaking.RequestLobbyData(steamID))
            SteamMatchmaking.JoinLobby(steamID);
        else
            Debug.Log("Failed to Join Steam Lobby with ID: " + steamID.m_SteamID);
    }

    public static void LeaveLobby()
    {
        SteamMatchmaking.LeaveLobby(new CSteamID(CurrentLobbyID));
        CurrentLobbyID = 0;

        instance._fishySteamworks.StopConnection(false);
        if (instance._networkManager.IsServerStarted)
            instance._fishySteamworks.StopConnection(true);
    }

    public static string getPersonalSteamName()
    {
        return SteamFriends.GetPersonaName().ToString();
    }

    public static bool IsHost()
    {
        return instance._networkManager.IsServerStarted;
        //string suffix = "'s PEGG lobby";
        //string hostLobbyName = SteamMatchmaking.GetLobbyData(new CSteamID(CurrentLobbyID), "name");
        //return steamName.Equals(hostLobbyName.Substring(0, hostLobbyName.Length - suffix.Length));
    }
}
