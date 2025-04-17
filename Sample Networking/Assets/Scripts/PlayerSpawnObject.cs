using UnityEngine;
using FishNet.Object;

public class PlayerSpawn : NetworkBehaviour
{
    public GameObject objToSpawn;
    [HideInInspector] public GameObject spawnedObject;


    public override void OnStartClient()
    {
        base.OnStartClient();

        // Ensures that other clients cannot control 'this' client's item spawner.
        if (!base.IsOwner)
            GetComponent<PlayerSpawn>().enabled = false;
    }

    private void Update()
    {
        // Client hasn't spawned an object yet and pressed the '1' key.
        if (spawnedObject == null && Input.GetKeyDown(KeyCode.Alpha1))
        {
            SpawnObject(objToSpawn, transform, this);
        }

        // Client has spawned an object and pressed the '2' key.
        if (spawnedObject != null && Input.GetKeyDown(KeyCode.Alpha2))
        {
            DespawnObject(spawnedObject);
        }
    }

    // Always want to spawn objects via the server
    [ServerRpc]

    /**
     * Takes in the object to spawn (obj)
     * The position of the player (player)
     * The instance of the PlayerSpawnScript (script)
     */
    public void SpawnObject(GameObject obj, Transform player, PlayerSpawn script)
    {
        // Instantiates new object in front of player with a standard rotation. (Spawns locally)
        GameObject spawned = Instantiate(obj, player.position + player.forward, Quaternion.identity);

        // Spawns it over the network for clients to see. (Spawns for every other player)
        ServerManager.Spawn(spawned);

        SetSpawnedObject(spawned, script);
    }

    // Run funcs for all clients / players
    [ObserversRpc]
    public void SetSpawnedObject(GameObject spawned, PlayerSpawn script)
    {
        script.spawnedObject = spawned;
    }

    // Means that the player who spawned the shared object doesn't have to be the one to despawn it.
    [ServerRpc(RequireOwnership = false)]
    public void DespawnObject(GameObject obj)
    {
        ServerManager.Despawn(obj);
    }
}
