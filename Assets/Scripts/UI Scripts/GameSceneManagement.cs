using UnityEngine;
using TMPro;

public class GameSceneManagement : MonoBehaviour
{
    public static GameSceneManagement instance;

    [SerializeField] private GameObject hostMenuPanel;
    [SerializeField] private GameObject clientMenuPanel;
    [SerializeField] private TextMeshProUGUI lobbyAddressText;
    [SerializeField] private UnityEngine.UI.Button startGameButton;
    [SerializeField] private UnityEngine.UI.Button loadButton;
    [SerializeField] private UnityEngine.UI.Button randomButton;

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
}
