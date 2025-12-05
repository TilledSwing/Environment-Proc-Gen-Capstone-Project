using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class Health : NetworkBehaviour
{
    // Use the generic SyncVar instead of the old attribute.
    private readonly SyncVar<float> _currentHealth = new();

    public float maxHealth = 5f;
    public HealthBarUI healthBar;

    //private void Awake()
    //{
    //    _currentHealth.OnChange += OnHealthChanged;
    //}

    public override void OnStartServer()
    {
        base.OnStartServer();
        _currentHealth.Value = maxHealth; // start at full health
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!base.IsOwner)
            this.enabled = false;
        else
        {
            // When a client starts, set the UI to the current health.
            healthBar?.SetHealth(_currentHealth.Value, maxHealth);
        }
    }

    private void Update()
    {
        if (!base.IsOwner || PlayerController.instance == null)
            return;

        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(1, gameObject);
            Debug.Log(_currentHealth.Value);
            if (_currentHealth.Value - 1 <= 0)
            {
                BroadcastPlayerDeath(LocalConnection.ClientId);
            }
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            Heal(1, gameObject);
            Debug.Log(_currentHealth.Value);
        }

    }

    [ServerRpc(RequireOwnership = false)]
    public void BroadcastPlayerDeath(int clientConnectionID)
    {
        ChatBroadcast.instance.ChatBroadcastPlayerDeath(clientConnectionID);
        LobbyBroadcast.instance.PlayerDeath(clientConnectionID);
    }

    //private void OnHealthChanged(float previous, float next, bool asServer)
    //{
    //    // Update the health bar UI whenever the health changes.
    //    healthBar?.SetHealth(next, maxHealth);
    //}

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamage(float amount, GameObject player)
    {
        if (!IsServerInitialized) return;

        float newHealth = Mathf.Clamp(_currentHealth.Value - amount, 0f, maxHealth);
        _currentHealth.Value = newHealth;
        UpdateHealthBar(player);
    }

    [ObserversRpc]
    public void UpdateHealthBar(GameObject player)
    {
        player.GetComponent<Health>().healthBar.SetHealth(_currentHealth.Value, maxHealth);
    }

    [ServerRpc(RequireOwnership = false)]
    public void Heal(float amount, GameObject player)
    {
        if (!IsServerInitialized) return;

        float newHealth = Mathf.Clamp(_currentHealth.Value + amount, 0f, maxHealth);
        _currentHealth.Value = newHealth;
        UpdateHealthBar(player);
    }
}
