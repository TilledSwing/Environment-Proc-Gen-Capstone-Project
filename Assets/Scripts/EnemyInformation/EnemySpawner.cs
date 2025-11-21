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
using System.Collections;

public class EnemySpawner : NetworkBehaviour
{
    private GameObject enemyLandPrefab;
    private GameObject enemyWaterPrefab;


    public int landEnemyCount = 20;
    public int waterEnemyCount = 7;
    public float minDistance = 25f;
    private string landPrefabAdress = "Assets/Stylized3DMonster/Monster04/Prefab/Monster04_01.prefab";
    private string waterPrefabAdress = "Assets/Human_Mutant/Prefab/Human_Mutant.prefab";

    private List<Vector3> usedLandPositions = new List<Vector3>();
    private List<Vector3> usedWaterPositions = new List<Vector3>();

    public NavMeshSurface landNavMeshSurface;
    public NavMeshSurface waterNavMeshSurface;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsServerInitialized)
        {
            loadPrefab();
            //GlobalNavMeshUpdater.Instance.StartNavMeshBuilds();
        }
    }

    void loadPrefab()
    {
        Addressables.LoadAssetAsync<GameObject>(landPrefabAdress).Completed += handle =>
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.Log("Failed to load the land enemy pre-fab");
                return;
            }
            enemyLandPrefab = handle.Result;
        };

        Addressables.LoadAssetAsync<GameObject>(waterPrefabAdress).Completed += handle =>
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.Log("Failed to load the water enemy pre-fab");
                return;
            }
            enemyWaterPrefab = handle.Result;
        };
        landNavMeshSurface = GlobalNavMeshUpdater.Instance.landSurface;
        waterNavMeshSurface = GlobalNavMeshUpdater.Instance.waterSurface;
        return;
    }
    void Update()
    {
        // Block input if in a chat message block. Ensures that typing words with certain letters or numbers won't trigger input events.
        if (EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null)
        {
            return;
        }

        if (IsServerOnlyStarted && Input.GetKeyDown(KeyCode.L))
            SpawnLandEnemies_FilteredByAgent();
        else if (IsClientStarted && Input.GetKeyDown(KeyCode.L))
            RequestSpawnEnemy();
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnEnemy()
    {
        SpawnLandEnemies_FilteredByAgent();
        SpawnWaterEnemies_FilteredByAgent();
    }

    private struct Tri
    {
        public int i0, i1, i2;
        public Vector3 a, b, c;
    }

    private static List<Tri> GetTrianglesForAgent(int agentTypeID, float sampleMaxDist = 1.5f)
    {
        var tri = NavMesh.CalculateTriangulation();
        var result = new List<Tri>();

        if (tri.indices == null || tri.indices.Length < 3)
            return result;

        var filter = new NavMeshQueryFilter
        {
            agentTypeID = agentTypeID,
            areaMask = NavMesh.AllAreas
        };

        for (int t = 0; t < tri.indices.Length; t += 3)
        {
            int i0 = tri.indices[t];
            int i1 = tri.indices[t + 1];
            int i2 = tri.indices[t + 2];

            Vector3 a = tri.vertices[i0];
            Vector3 b = tri.vertices[i1];
            Vector3 c = tri.vertices[i2];

            // pick centroid as representative point
            Vector3 centroid = (a + b + c) / 3f;

            // sample using the agent-specific filter — only accept if sample hits the same agent navmesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(centroid, out hit, sampleMaxDist, filter))
            {
                // ensure the hit is close to centroid (optional but useful)
                if (Vector3.Distance(hit.position, centroid) <= sampleMaxDist)
                {
                    result.Add(new Tri { i0 = i0, i1 = i1, i2 = i2, a = a, b = b, c = c });
                }
            }
        }

        return result;
    }

    private static Vector3 RandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        // good uniform barycentric technique
        float r1 = Random.value;
        float r2 = Random.value;
        if (r1 + r2 > 1f) { r1 = 1f - r1; r2 = 1f - r2; }
        return a + r1 * (b - a) + r2 * (c - a);
    }

    // ----------------- Spawn using filtered triangles -----------------

    [Server]
    private void SpawnLandEnemies_FilteredByAgent()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        var playerTransforms = players.Select(p => p.transform).ToList();

        int agentType = landNavMeshSurface.agentTypeID; // your land NavMeshSurface agent type
        float sampleMaxDist = 1.5f;

        var landTris = GetTrianglesForAgent(agentType, sampleMaxDist);

        if (landTris.Count == 0)
        {
            Debug.LogError("No land triangles found for agentTypeID " + agentType);
            return;
        }

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = landEnemyCount * 50;

        while (spawned < landEnemyCount && attempts < maxAttempts)
        {
            attempts++;

            // pick a random triangle from filtered list
            var t = landTris[Random.Range(0, landTris.Count)];

            Vector3 spawnPos = RandomPointInTriangle(t.a, t.b, t.c);
            spawnPos += Vector3.up * 0.1f;

            // verify again with agent filter (defensive)
            NavMeshHit hit;
            var filter = new NavMeshQueryFilter { agentTypeID = agentType, areaMask = NavMesh.AllAreas };
            if (!NavMesh.SamplePosition(spawnPos, out hit, sampleMaxDist, filter))
                continue;

            // // Check reachable to any player (keep your IsReachable)
            // if (!playerTransforms.Any(p => IsReachable(spawnPos, p.position)))
            //     continue;

            // Check min spacing
            if (usedLandPositions.Any(pos => Vector3.Distance(pos, spawnPos) < minDistance))
                continue;

            // Spawn
            var enemy = Instantiate(enemyLandPrefab, spawnPos, Quaternion.identity);
            var agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            ServerManager.Spawn(enemy);

            if (agent != null) agent.enabled = true;

            usedLandPositions.Add(spawnPos);
            spawned++;
        }

        Debug.Log($"Spawned {spawned}/{landEnemyCount} land enemies using {landTris.Count} filtered triangles.");
    }

    [Server]
    private void SpawnWaterEnemies_FilteredByAgent()
    {
        int agentType = waterNavMeshSurface.agentTypeID; // your land NavMeshSurface agent type
        float sampleMaxDist = 1.5f;

        var waterTris = GetTrianglesForAgent(agentType, sampleMaxDist);

        if (waterTris.Count == 0)
        {
            Debug.LogError("No land triangles found for agentTypeID " + agentType);
            return;
        }

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = waterEnemyCount * 50;

        while (spawned < waterEnemyCount && attempts < maxAttempts)
        {
            attempts++;

            // pick a random triangle from filtered list
            var t = waterTris[Random.Range(0, waterTris.Count)];

            Vector3 spawnPos = RandomPointInTriangle(t.a, t.b, t.c);
            spawnPos += Vector3.up * 0.1f;

            // verify again with agent filter (defensive)
            NavMeshHit hit;
            var filter = new NavMeshQueryFilter { agentTypeID = agentType, areaMask = NavMesh.AllAreas };
            if (!NavMesh.SamplePosition(spawnPos, out hit, sampleMaxDist, filter))
                continue;

            // Check min spacing
            if (usedWaterPositions.Any(pos => Vector3.Distance(pos, spawnPos) < minDistance))
                continue;

            // Spawn
            var enemy = Instantiate(enemyWaterPrefab, spawnPos, Quaternion.identity);
            var agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            ServerManager.Spawn(enemy);

            if (agent != null) agent.enabled = true;

            usedWaterPositions.Add(spawnPos);
            spawned++;
        }

        Debug.Log($"Spawned {spawned}/{waterEnemyCount} land enemies using {waterTris.Count} filtered triangles.");
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
