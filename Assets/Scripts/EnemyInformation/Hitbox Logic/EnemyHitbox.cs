using UnityEngine;
using System.Collections.Generic;
using FishNet.Object;

public class EnemyHitbox : NetworkBehaviour
{
    public int damage = 1;
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
        if (!IsServerInitialized) 
            return;
        alreadyHit.Clear();
        col.enabled = true;
    }

    public void DisableHitbox()
    {
        if (!IsServerInitialized) 
            return;
        col.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsServerInitialized) 
            return;

        GameObject root = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.transform.root.gameObject;

        if (!root.CompareTag(targetTag) || alreadyHit.Contains(root))
            return;

        alreadyHit.Add(root);

        // // Apply damage
        var health = root.GetComponent<Health>();
        if (health != null) 
            health.TakeDamage(damage, PlayerController.instance.gameObject);
    }
}
