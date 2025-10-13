using UnityEngine;
using UnityEngine.AI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using FishNet.Object;
using TMPro;
using UnityEngine.EventSystems;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private GameObject enemy;
    [SerializeField] private GameObject enemyPrefab;

    private string prefabAdress = "Assets/Stylized3DMonster/Monster04/Prefab/Monster04_01.prefab";
    private Camera playerCamera;

    public override void OnStartClient()
    {
        Debug.Log("SpawnerIsActive");
        base.OnStartClient();
        if (IsClientStarted)
        {
            playerCamera = Camera.main;
        }
        if (IsServerStarted)
        {
            loadPrefab();
        }
    }

    void loadPrefab()
    {
        Debug.Log("Entering load prefab method");
        Addressables.LoadAssetAsync<GameObject>(prefabAdress).Completed += handle =>
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.Log("Failed to load the desired pre-fab");
                return;
            }
            Debug.Log($"The results of the prefab load is: {handle.Status.ToString()}");
            enemyPrefab = handle.Result;
        };

        return;
    }

    void Update()
    {
        // Block input if in a chat message block. Ensures that typing words with certain letters or numbers won't trigger input events.
        if (EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null)
        {
            Debug.Log("exiting the loop before keycode");
            return;
        }

        if (IsServerOnlyStarted && Input.GetKeyDown(KeyCode.L))
            SpawnEnemy();
        else if (IsClientStarted && Input.GetKeyDown(KeyCode.L))
            RequestSpawnEnemy();
    }


    [Server]
    public void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("The server failed to load the enemy prefab");
            return;
        }
        if (playerCamera == null)
        {
            Debug.LogWarning("Didn't get the player camera on start");
            return;
        }

        Debug.Log("Spawning Enemy from handler");
        // Load the prefab dynamically by address
        float spawnDist = 2f;
        Vector3 spawnPos = playerCamera.transform.position + playerCamera.transform.forward * spawnDist;
        // Instantiate the enemy
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(spawnPos, out navHit, 5f, NavMesh.AllAreas))
        {
            spawnPos = navHit.position;
            enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            ServerManager.Spawn(enemy);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSpawnEnemy()
    {
        SpawnEnemy();
    }
}
