using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;

public class GlobalNavMeshUpdater : MonoBehaviour
{
    public NavMeshSurface navMeshSurface;
    private AsyncOperation currentOp;
    private bool isBuilding = false;

    private void Awake()
    {
        if (navMeshSurface == null)
        {
            navMeshSurface = FindFirstObjectByType<NavMeshSurface>();
            if (navMeshSurface == null)
            {
                navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
            }
        }
        navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        navMeshSurface.overrideVoxelSize = true;
        navMeshSurface.voxelSize = 0.1f;
        navMeshSurface.overrideTileSize = true;
        navMeshSurface.tileSize = 256;

        // ✅ Correct way: Modify build settings directly on NavMeshSurface
        var buildSettings = navMeshSurface.GetBuildSettings();
        buildSettings.agentSlope = 70f;   // Allow very steep slopes
        buildSettings.agentClimb = 1.5f;  // Increase step/climb height
        buildSettings.agentRadius = 0.3f;
        
    }
    public IEnumerator RebuildNavMeshAsync(Bounds updateRegion, Dictionary<Vector3, ChunkGenNetwork.TerrainChunk> chunkDictionary)
    {
        if (isBuilding)
            yield break;

        isBuilding = true;

        var sources = new List<NavMeshBuildSource>();
        foreach (var kvp in chunkDictionary)
        {
            ChunkGenNetwork.TerrainChunk chunk = kvp.Value;
            var mesh = chunk.marchingCubes.meshFilter.mesh;
            if (chunk.marchingCubes?.meshFilter?.mesh == null)
                continue;

            if (mesh.vertexCount == 0 || mesh.triangles.Length == 0){
                continue;
            }
            var src = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Mesh,
                sourceObject = mesh,
                transform = chunk.marchingCubes.meshFilter.transform.localToWorldMatrix,
                area = 0
            };
            sources.Add(src);
        }

        if (navMeshSurface.navMeshData == null)
        {
            navMeshSurface.navMeshData = new NavMeshData(navMeshSurface.agentTypeID);
            NavMesh.AddNavMeshData(navMeshSurface.navMeshData);
        }

        var settings = navMeshSurface.GetBuildSettings();
        currentOp = NavMeshBuilder.UpdateNavMeshDataAsync(
            navMeshSurface.navMeshData,
            settings,
            sources,
            updateRegion
        );

        while (!currentOp.isDone)
            yield return null;

        isBuilding = false;
        Debug.Log($"NavMesh async update done for region {updateRegion.center}");
    }
}