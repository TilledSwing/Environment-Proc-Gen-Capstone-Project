using Steamworks;
using System;
using System.Windows.Forms;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

public class GameMenuManager : MonoBehaviour
{
    public static GameMenuManager instance;

    [SerializeField] private TMP_InputField lobbyInput;
    [SerializeField] private GameObject gameLobbyCanvas;
    [SerializeField] private GameObject inGameLobbyContainer;
    [SerializeField] private GameObject chatContainer;
    [SerializeField] private GameObject chatLobbyToggle;
    [SerializeField] private GameObject hostMenuPanel;
    [SerializeField] private GameObject clientMenuPanel;
    [SerializeField] private TextMeshProUGUI lobbyAddressText;
    [SerializeField] private UnityEngine.UI.Button startGameButton;
    [SerializeField] private UnityEngine.UI.Button loadButton;
    [SerializeField] private UnityEngine.UI.Button randomButton;

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

    public void LoadPreGameFeatures(bool isHost)
    {
        inGameLobbyContainer.SetActive(true);
        chatContainer.SetActive(true);
        chatLobbyToggle.SetActive(true);

        if (isHost)
            LoadHostMenuPanel();
        else
            LoadClientMenuPanel();

    }

    public void LoadHostMenuPanel()
    {
        clientMenuPanel.SetActive(false);
        hostMenuPanel.SetActive(true);
        lobbyAddressText.text = BootstrapManager.CurrentLobbyID.ToString();
    }

    public void LoadClientMenuPanel()
    {
        hostMenuPanel.SetActive(false);
        clientMenuPanel.SetActive(true);
    }

    public void DisableHostTerrainButtons()
    {
        startGameButton.enabled = false;
        loadButton.enabled = false;
        randomButton.enabled = false;
    }

    public void QuitGameScene()
    {
        BootstrapManager.LeaveLobby();
        SceneManager.LoadScene("MainMenu");
    }
}
