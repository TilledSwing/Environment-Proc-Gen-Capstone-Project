using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Linq;

public class WaterLinkManager : MonoBehaviour
{
    // Stores created NavMeshLink objects per chunk
    private Dictionary<Vector3, List<GameObject>> chunkLinks = new();
    private int agentTypeID = 0; // make sure to set this to your NavMeshAgent's type ID
    private GameObject linkParent;

    private void Awake()
    {
        linkParent = new GameObject("WaterLinks");
        linkParent.transform.localPosition = Vector3.zero;
    }
    /// <summary>
    /// Incrementally updates water links for changed chunks.
    /// Ensures all link endpoints are on the baked NavMesh.
    /// </summary>
    /// 
    public IEnumerator UpdateWaterLinksIncremental(
        List<Vector3> changedChunks,
        Dictionary<Vector3, GlobalNavMeshUpdater.WaterChunkSources> waterSources,
        float agentStepHeight,
        float chunkHeight,
        float planeWidth, int waterAgentID
    )
    {
        const int yieldAfterIterations = 15;
        int iterationCounter = 0;
        agentTypeID = waterAgentID;
        foreach (var chunkCenter in changedChunks)
        {
            if (!waterSources.TryGetValue(chunkCenter, out var chunk) ||
                chunk.waterPlanes == null || chunk.waterPlanes.Count == 0)
                continue;

            // --- Clear previous links ---
            if (!chunkLinks.TryGetValue(chunkCenter, out var oldLinks))
            {
                oldLinks = new List<GameObject>();
                chunkLinks[chunkCenter] = oldLinks;
            }
            else
            {
                foreach (var l in oldLinks)
                    if (l != null) Destroy(l);
                oldLinks.Clear();
            }

            var planes = chunk.waterPlanes;

            // --- Vertical links within same chunk ---
            for (int i = 0; i < planes.Count - 1; i++)
            {
                float verticalDiff = planes[i + 1].transform.GetColumn(3).y - planes[i].transform.GetColumn(3).y;
                if (verticalDiff <= agentStepHeight * 10f)
                {
                    var link = CreateSnappedLink(planes[i], planes[i + 1], planeWidth);
                    if (link != null) oldLinks.Add(link);
                }

                iterationCounter++;
                if (iterationCounter >= yieldAfterIterations)
                {
                    iterationCounter = 0;
                    yield return null;
                }
            }

            // --- Neighbor links (above, below, diagonals) ---
            Vector3[] neighborOffsets = new Vector3[]
            {
                new Vector3(0, chunkHeight, 0),
                new Vector3(0, -chunkHeight, 0),
                new Vector3(chunkHeight, chunkHeight, 0),
                new Vector3(-chunkHeight, chunkHeight, 0),
                new Vector3(chunkHeight, -chunkHeight, 0),
                new Vector3(-chunkHeight, -chunkHeight, 0),
                new Vector3(0, chunkHeight, chunkHeight),
                new Vector3(0, -chunkHeight, chunkHeight),
                new Vector3(0, chunkHeight, -chunkHeight),
                new Vector3(0, -chunkHeight, -chunkHeight),
            };

            foreach (var offset in neighborOffsets)
            {
                var neighborCenter = chunkCenter + offset;
                if (!waterSources.TryGetValue(neighborCenter, out var neighborChunk) ||
                    neighborChunk.waterPlanes == null || neighborChunk.waterPlanes.Count == 0)
                    continue;

                foreach (var plane in planes)
                {
                    // Find nearest plane in neighbor by height
                    var nearest = neighborChunk.waterPlanes
                        .OrderBy(p => Mathf.Abs(p.transform.GetColumn(3).y - plane.transform.GetColumn(3).y))
                        .First();

                    float verticalDiff = Mathf.Abs(nearest.transform.GetColumn(3).y - plane.transform.GetColumn(3).y);
                    if (verticalDiff <= agentStepHeight * 10f)
                    {
                        var link = CreateSnappedLink(plane, nearest, planeWidth);
                        if (link != null) oldLinks.Add(link);
                    }

                    iterationCounter++;
                    if (iterationCounter >= yieldAfterIterations)
                    {
                        iterationCounter = 0;
                        yield return null;
                    }
                }
            }
        }

        Debug.Log("Water links updated incrementally.");
    }

    /// <summary>
    /// Creates a NavMeshLink between two NavMeshBuildSources, snapping endpoints to the baked NavMesh.
    /// Returns null if either endpoint cannot be placed on the NavMesh.
    /// </summary>
    private GameObject CreateSnappedLink(NavMeshBuildSource lower, NavMeshBuildSource upper, float planeWidth)
    {
        // Create parent if it doesn't exist
        Vector3 lowerPos = lower.transform.GetColumn(3);
        Vector3 upperPos = upper.transform.GetColumn(3);

        // Snap positions to NavMesh
        if (!NavMesh.SamplePosition(lowerPos, out var hitLower, 1f, NavMesh.AllAreas)) return null;
        if (!NavMesh.SamplePosition(upperPos, out var hitUpper, 1f, NavMesh.AllAreas)) return null;

        GameObject linkObj = new GameObject($"WaterLink_{hitLower.position.y}_{hitUpper.position.y}");
        linkObj.transform.position = hitLower.position;
        linkObj.transform.SetParent(linkParent.transform);

        var link = linkObj.AddComponent<NavMeshLink>();
        link.startPoint = Vector3.zero;
        link.endPoint = hitUpper.position - hitLower.position;
        link.width = planeWidth;
        link.bidirectional = true;
        link.autoUpdate = true;
        link.area = 3; // water area
        link.agentTypeID = agentTypeID; // make sure 'agent' is a reference to your NavMeshAgent

        return linkObj;
    }
}
