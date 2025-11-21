using UnityEngine;
using System.Collections.Generic;
using FishNet.Object;

public class WaterAgentHitbox : NetworkBehaviour
{
    public int damage = 1;
    public string targetTag = "Player";

    private Collider[] colliders;          // all colliders under this hitbox
    private AttackHitRegistry registry;

    void Awake()
    {
        // Collect *all* colliders on this object + children
        colliders = GetComponentsInChildren<Collider>(true);

        foreach (var c in colliders)
        {
            c.isTrigger = true;
            c.enabled = false;
        }
        registry = GetComponentInParent<AttackHitRegistry>();
    }

    public void EnableHitbox()
    {
        if (!IsServerInitialized) 
            return;
        foreach (var c in colliders)
            c.enabled = true;
    }

    public void DisableHitbox()
    {
        if (!IsServerInitialized) 
            return;
        foreach (var c in colliders)
            c.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsServerInitialized)
            return;

        // Identify root target so multiple colliders don't double-damage
        GameObject root = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.transform.root.gameObject;

        if (!root.CompareTag(targetTag) || registry.alreadyHit.Contains(root))
            return;

        
        registry.alreadyHit.Add(root);
        
        // // Apply damage
        var health = root.GetComponent<Health>();
        if (health != null) 
            health.TakeDamage(damage);
    }
}
