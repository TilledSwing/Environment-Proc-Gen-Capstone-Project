using UnityEngine;
using System.Collections.Generic;
using FishNet.Object;

public class EnemyHitbox : NetworkBehaviour
{
    public int damage = 10;
    public string targetTag = "Player"; // what it can hit

    private Collider col;
    private HashSet<GameObject> alreadyHit = new HashSet<GameObject>();

    void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true;
        col.enabled = false; // disabled until attack
    }

    public void EnableHitbox()
    {
        alreadyHit.Clear();
        col.enabled = true;
    }

    public void DisableHitbox()
    {
        col.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsServerInitialized) 
            return;

        if (!col.enabled || !other.CompareTag(targetTag) || alreadyHit.Contains(other.gameObject)) 
            return;

        alreadyHit.Add(other.gameObject);

        // // Apply damage
        // var health = other.GetComponent<Health>();
        // if (health != null) health.TakeDamage(damage);
        Debug.Log("The attack connected and hit the player");

        // // Optional: small knockback or hit reaction
        // var rb = other.attachedRigidbody;
        // if (rb != null)
        //     rb.AddForce((other.transform.position - transform.position).normalized * 3f, ForceMode.Impulse);
    }
}
