using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class Health : NetworkBehaviour
{
    // Use the generic SyncVar instead of the old attribute.
    private readonly SyncVar<float> _currentHealth = new();

    public float maxHealth = 5f;
    public HealthBarUI healthBar;

    private void Awake()
    {
        _currentHealth.OnChange += OnHealthChanged;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _currentHealth.Value = maxHealth; // start at full health
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        // When a client starts, set the UI to the current health.
        healthBar?.SetHealth(_currentHealth.Value, maxHealth);
    }

    private void OnHealthChanged(float previous, float next, bool asServer)
    {
        // Update the health bar UI whenever the health changes.
        healthBar?.SetHealth(next, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (!IsServerInitialized) return;

        float newHealth = Mathf.Clamp(_currentHealth.Value - amount, 0f, maxHealth);
        _currentHealth.Value = newHealth;
    }

    public void Heal(float amount)
    {
        if (!IsServerInitialized) return;

        float newHealth = Mathf.Clamp(_currentHealth.Value + amount, 0f, maxHealth);
        _currentHealth.Value = newHealth;
    }
}
