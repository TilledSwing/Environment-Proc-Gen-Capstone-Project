using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Example.ColliderRollbacks;

public class PlayerColorNetwork : NetworkBehaviour
{
    public GameObject body;
    public Color endColor;

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Enforces that players cannot control other players colors.
        if (!base.IsOwner)
        {
            GetComponent<PlayerColorNetwork>().enabled = false;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            ChangeColorServer(gameObject, endColor);
        }
    }

    [ServerRpc]
    public void ChangeColorServer(GameObject player, Color color)
    {
        ChangeColor(player, color);
    }

    // Send to all other client observers
    [ObserversRpc]
    public void ChangeColor(GameObject player, Color color)
    {
        player.GetComponent<PlayerColorNetwork>().body.GetComponent<Renderer>().material.color = color;
    }
}
