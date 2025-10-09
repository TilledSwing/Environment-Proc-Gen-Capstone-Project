using UnityEngine;
using UnityEngine.AI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using FishNet.Object;
using FishNet.Connection;
using TMPro;
using UnityEngine.EventSystems;
using FishNet.Managing.Object;
using UnityEditor.Build.Pipeline;
using GameKit.Dependencies.Utilities;
public class EnemySpawner : NetworkBehaviour
{
    Camera playerCamera;
    GameObject enemy;
    GameObject enemyPrefab;

    public override void OnStartClient()
    {
        playerCamera = Camera.main;
        loadPrefab();
        // base.OnStartClient();
        // if (!base.IsOwner)
        //     this.enabled = false;
        // else
        // {
        //     playerCamera = Camera.main;
        //     loadPrefab();
        // }
    }

    void loadPrefab()
    {
        string prefabAdress = "Assets/Same Gev Dudios/Sci-Fi Robots Bundle/Prefabs/Catherine.prefab";
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

            // var prefabId = prefabAdress.GetStableHashU16();
            // SinglePrefabObjects sp = (SinglePrefabObjects)NetworkManager.GetPrefabObjects<SinglePrefabObjects>(prefabId, true);
            // sp.AddObject(enemyPrefab);
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

        if (Input.GetKeyDown(KeyCode.L) && enemyPrefab != null)
        {
            SpawnEnemy(playerCamera.transform.forward);
        }
    }
    public void SpawnEnemy(Vector3 position)
    {
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

            var ai = enemy.GetComponent<EnemyAIMovement>();
            if (ai != null)
            {
                ai.BeginWandering();
            }
        }
        

    }
}
