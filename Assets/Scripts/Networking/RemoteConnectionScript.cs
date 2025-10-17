using FishNet.Transporting.Tugboat;
using TMPro;
using UnityEngine;

public class RemoteConnectionScript : MonoBehaviour
{
    private TMP_InputField ipField;
    private Tugboat tugboat;

    private void Start()
    {
        ipField = transform.Find("NetworkHudCanvas/RemoteJoinTextBox/MessageInput")?.GetComponent<TMP_InputField>();
        tugboat = GetComponent<Tugboat>();
        tugboat.SetClientAddress("localhost"); // Default local host.
        ipField.onEndEdit.AddListener(OnEndEditIPAddr);
    }

    private void OnEndEditIPAddr(string input)
    {
        string ipAddress = input.Trim();
        if (!string.IsNullOrEmpty(ipAddress))
        {
            if (!(ipAddress == "127.0.0.1" || ipAddress == "localhost"))
            {
                tugboat.SetClientAddress(ipAddress);
            }
        }
        else
        {
            tugboat.SetClientAddress("localhost");
        }
    }
}
