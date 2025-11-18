using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Linq;
using static GlobalNavMeshUpdater;

public class WaterLinkManager : MonoBehaviour
{
    private Dictionary<Vector3, List<GameObject>> chunkLinks = new();

    public IEnumerator UpdateWaterLinksIncremental(
        List<Vector3> changedChunks,
        Dictionary<Vector3, WaterChunkSources> waterSources,
        float agentStepHeight,
        float chunkHeight,
        float planeSizeX
    )
    {
        const int yieldAfterIterations = 15;
        int iterationCounter = 0;

        foreach (var chunkCenter in changedChunks)
        {
            if (!waterSources.TryGetValue(chunkCenter, out var chunk) || chunk.waterPlanes == null || chunk.waterPlanes.Count == 0)
                continue;

            // --- Clear previous links for this chunk ---
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
                    var link = CreateLink(planes[i], planes[i + 1], planeSizeX);
                    oldLinks.Add(link);
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
                if (!waterSources.TryGetValue(neighborCenter, out var neighborChunk)) continue;
                if (neighborChunk.waterPlanes == null || neighborChunk.waterPlanes.Count == 0) continue;

                foreach (var plane in planes)
                {
                    // nearest plane in neighbor
                    NavMeshBuildSource nearest = neighborChunk.waterPlanes
                        .OrderBy(p => Mathf.Abs(p.transform.GetColumn(3).y - plane.transform.GetColumn(3).y))
                        .First();

                    float verticalDiff = Mathf.Abs(nearest.transform.GetColumn(3).y - plane.transform.GetColumn(3).y);
                    if (verticalDiff <= agentStepHeight * 10f)
                    {
                        var link = CreateLink(plane, nearest, planeSizeX);
                        oldLinks.Add(link);
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
    /// Creates a NavMeshLink between two planes.
    /// </summary>
    private GameObject CreateLink(NavMeshBuildSource lower, NavMeshBuildSource upper, float planeWidth)
    {
        Vector3 lowerPos = lower.transform.GetColumn(3);
        Vector3 upperPos = upper.transform.GetColumn(3);

        GameObject linkObj = new GameObject($"WaterLink_{lowerPos.y}_{upperPos.y}");
        linkObj.transform.position = lowerPos;

        var link = linkObj.AddComponent<NavMeshLink>();
        link.startPoint = Vector3.zero;
        link.endPoint = upperPos - lowerPos;
        link.width = planeWidth;
        link.bidirectional = true;
        link.autoUpdate = true;
        link.area = 0; // water area

        return linkObj;
    }

}