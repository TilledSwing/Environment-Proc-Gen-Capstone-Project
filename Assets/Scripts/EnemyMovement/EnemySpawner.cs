using UnityEngine;
using UnityEngine.AI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using FishNet.Object;
using FishNet.Connection;
using TMPro;
using UnityEngine.EventSystems;
public class EnemySpawner : NetworkBehaviour
{
    Camera playerCamera;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!base.IsOwner)
            this.enabled = false;
        else
            playerCamera = Camera.main;
    }

    void Update()
    {
        // Block input if in a chat message block. Ensures that typing words with certain letters or numbers won't trigger input events.
        if (EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("Spawning Enemy");
            SpawnEnemy("Assets/Same Gev Dudios/Sci-Fi Robots Bundle/Prefabs/Catherine.prefab", playerCamera.transform.forward);
        }
    }
    public async void SpawnEnemy(string enemyKey, Vector3 position)
    {
        Debug.Log("Spawning Enemy from handler");
        // Load the prefab dynamically by address
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(enemyKey);
        GameObject enemyPrefab = await handle.Task;

        if (enemyPrefab == null)
        {
            Debug.LogError($"Enemy with key {enemyKey} not found!");
            return;
        }

        // Instantiate the enemy
        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);

        // Optional: network spawn
        ServerManager.Spawn(enemy);

        // Snap to NavMesh if it has a NavMeshAgent
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null && NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
    }
}
