using UnityEngine;
using UnityEngine.AI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using FishNet.Object;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Unity.AI.Navigation;
using FishNet.Demo.HashGrid;
using System.Linq;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private List<GameObject> enemies = new List<GameObject>();
    [SerializeField] private GameObject enemyPrefab;
    public int enemyCount = 20;
    public float minDistance = 25f;
    private string prefabAdress = "Assets/Stylized3DMonster/Monster04/Prefab/Monster04_01.prefab";
    private Camera playerCamera;
    private List<Vector3> usedPositions = new List<Vector3>();

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
            SpawnEnemies();
        else if (IsClientStarted && Input.GetKeyDown(KeyCode.L))
            RequestSpawnEnemy();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSpawnEnemy()
    {
        SpawnEnemies();
    }

    [Server]
    void SpawnEnemies()
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

        var navMeshSurface = FindFirstObjectByType<NavMeshSurface>();
        if (navMeshSurface == null)
        {
            Debug.LogWarning("No NavMeshSurface found in the scene for enemy spawning");
            return;
        }
        
        //Set up player transforms for reachability checks
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        var playerTransforms = players.Select(p => p.transform).ToList();

        Bounds navMeshBounds = navMeshSurface.navMeshData.sourceBounds;
        int spawned = 0;
        int attempts = 0;
        int maxAttempts = enemyCount * 50;

        while (spawned < enemyCount && attempts < maxAttempts)
        {
            attempts++;
            Vector3 randomPoint = new Vector3(
            Random.Range(navMeshBounds.min.x, navMeshBounds.max.x),
            navMeshBounds.center.y + 5f, // Start above
            Random.Range(navMeshBounds.min.z, navMeshBounds.max.z)
            );

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                Vector3 spawnPos = hit.position + Vector3.up * 0.1f;

                // Check if reachable to any player
                if (!playerTransforms.Any(p => IsReachable(spawnPos, p.position)))
                    continue;

                // Check min distance from other spawns
                if (usedPositions.Any(pos => Vector3.Distance(pos, spawnPos) < minDistance))
                    continue;

                // Spawn and enable NavMeshAgent after placement
                var enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                var agent = enemy.GetComponent<NavMeshAgent>();
                if (agent != null) agent.enabled = false; // Prevent early pathing

                ServerManager.Spawn(enemy);
                if (agent != null) agent.enabled = true; // Re-enable after spawn

                usedPositions.Add(spawnPos);
                enemies.Add(enemy);
                spawned++;
            }
        }

        Debug.Log($"Spawned {spawned} enemies after {attempts} attempts.");
        if (spawned < enemyCount)
        {
            if(usedPositions.Count == 0)
            {
                Debug.LogWarning("No valid spawn positions found. Aborting additional spawns.");
                return;
            }
            while (spawned < enemyCount)
            {
                var position = usedPositions[Random.Range(0, usedPositions.Count)];
                var enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
                var agent = enemy.GetComponent<NavMeshAgent>();
                if (agent != null) agent.enabled = false; // Prevent early pathing

                ServerManager.Spawn(enemy);
                if (agent != null) agent.enabled = true; // Re-enable after spawn
                enemies.Add(enemy);
                spawned++;
            }
            Debug.LogWarning("Could not find enough valid spawn positions. Spawned additional enemies at existing positions.");
        }

    }

    bool IsReachable(Vector3 start, Vector3 end)
    {
        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path))
        {
            return path.status == NavMeshPathStatus.PathComplete;
        }
        return false;
    }


}
