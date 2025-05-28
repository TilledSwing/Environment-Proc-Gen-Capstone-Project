using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Serializing.Helping;
using UnityEngine;
using UnityEngine.Rendering;

public class ChunkGenNetwork : NetworkBehaviour
{
    public static ChunkGenNetwork Instance;
    // Viewer Settings
    public float maxViewDst = 100;
    public Transform viewer;
    public static Vector3 viewerPos;
    public float updateDistanceThreshold = 5f;
    private Vector3 lastUpdateViewerPos;
    public int chunkSize;
    public int chunksVisible;
    // Scriptable Object References
    public TerrainDensityData1 terrainDensityData;
    public AssetSpawnData assetSpawnData;
    // Compute Shader References
    public ComputeShader marchingCubesComputeShader;
    public ComputeShader terrainDensityComputeShader;
    public ComputeShader terrainNoiseComputeShader;
    public ComputeShader caveNoiseComputeShader;
    public ComputeShader terraformComputeShader;
    public ComputeShader spawnPointsComputeShader;
    // Material References
    public Material terrainMaterial;
    public Material waterMaterial;
    // Chunk Variables
    public Dictionary<Vector3, TerrainChunk> chunkDictionary = new Dictionary<Vector3, TerrainChunk>();
    public List<TerrainChunk> chunksVisibleLastUpdate = new List<TerrainChunk>();
    private Queue<Vector3Int> chunkLoadQueue = new Queue<Vector3Int>();
    private bool isLoadingChunks = false;
    public bool initialLoadComplete = false;
    // Lighting Blocker
    public GameObject lightingBlocker;
    private MeshRenderer lightingBlockerRenderer;
    // Action Queues
    public bool hasPendingMeshInits = false;
    public Queue<Action> pendingMeshInits = new Queue<Action>();
    private bool isLoadingMeshes = false;
    public bool hasPendingReadbacks = false;
    public Queue<ReadbackRequest> pendingReadbacks = new Queue<ReadbackRequest>();
    private bool isLoadingReadbacks = false;
    // Data structure pools
    public class ReadbackRequest
    {
        public ComputeBuffer buffer;
        public Action<AsyncGPUReadbackRequest> readbackRequest;

        public ReadbackRequest(ComputeBuffer buffer, Action<AsyncGPUReadbackRequest> readbackRequest)
        {
            this.buffer = buffer;
            this.readbackRequest = readbackRequest;
        }
    }

    void Awake()
    {
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }
        chunkSize = terrainDensityData.width;
        chunksVisible = Mathf.RoundToInt(maxViewDst / chunkSize);
        lightingBlockerRenderer = lightingBlocker.GetComponent<MeshRenderer>();
        lightingBlockerRenderer.enabled = false;
         // Set seeds
        terrainDensityData.noiseSeed = UnityEngine.Random.Range(0, 100000);
        terrainDensityData.caveNoiseSeed = UnityEngine.Random.Range(0, 100000);
        terrainDensityData.domainWarpSeed = UnityEngine.Random.Range(0, 100000);
        terrainDensityData.caveDomainWarpSeed = UnityEngine.Random.Range(0, 100000);
    }
    /// <summary>
    /// Sets the player to the new viewer for chunk generation and disables the local chunk generator
    /// </summary>
    public override void OnStartClient()
    {
        base.OnStartClient();
        viewer = GameObject.Find("Player(Clone)").transform;
        GameObject localChunkManager = GameObject.Find("LocalChunkManager");
        if (localChunkManager)
        {
            if (localChunkManager.activeSelf) localChunkManager.SetActive(false);
        }
    }

    void Update()
    {
        // Position updates
        viewerPos = new Vector3(viewer.position.x, viewer.position.y, viewer.position.z);
        lightingBlocker.transform.position = new Vector3(viewerPos.x, 0, viewerPos.z);
        // Update chunks
        if ((viewerPos - lastUpdateViewerPos).sqrMagnitude > updateDistanceThreshold * updateDistanceThreshold && initialLoadComplete)
        {
            UpdateVisibleChunks();
            lastUpdateViewerPos = viewerPos;
        }
        else if (!initialLoadComplete) {
            UpdateVisibleChunks();
        }
    }
    /// <summary>
    /// Update all the visible chunks loading in new ones and unloading old ones that are no longer visible
    /// </summary>
    public void UpdateVisibleChunks()
    {
        for (int i = 0; i < chunksVisibleLastUpdate.Count; i++)
        {
            chunksVisibleLastUpdate[i].SetVisible(false);
        }
        chunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPos.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPos.y / chunkSize);
        int currentChunkCoordZ = Mathf.RoundToInt(viewerPos.z / chunkSize);

        if (viewerPos.y <= -5)
        {
            if (!lightingBlockerRenderer.enabled)
            {
                lightingBlockerRenderer.enabled = true;
            }
        }
        else
        {
            if (lightingBlockerRenderer.enabled)
            {
                lightingBlockerRenderer.enabled = false;
            }
        }

        for (int xOffset = -chunksVisible; xOffset <= chunksVisible; xOffset++)
        {
            for (int yOffset = -chunksVisible; yOffset <= chunksVisible; yOffset++)
            {
                for (int zOffset = -chunksVisible; zOffset <= chunksVisible; zOffset++)
                {
                    Vector3Int viewedChunkCoord = new Vector3Int(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset, currentChunkCoordZ + zOffset);

                    if (chunkDictionary.ContainsKey(viewedChunkCoord))
                    {
                        chunkDictionary[viewedChunkCoord].UpdateChunk(maxViewDst);
                        if (chunkDictionary[viewedChunkCoord].IsVisible())
                        {
                            chunksVisibleLastUpdate.Add(chunkDictionary[viewedChunkCoord]);
                        }
                    }
                    else
                    {
                        if (!initialLoadComplete)
                        {
                            // Generate immediately during first load
                            TerrainChunk chunk = new TerrainChunk(viewedChunkCoord, chunkSize, transform, terrainDensityData, assetSpawnData,
                                                         marchingCubesComputeShader, terrainDensityComputeShader, terrainNoiseComputeShader,
                                                         caveNoiseComputeShader, terraformComputeShader, spawnPointsComputeShader,
                                                         terrainMaterial, waterMaterial, initialLoadComplete);

                            chunkDictionary.Add(viewedChunkCoord, chunk);
                            chunk.UpdateChunk(maxViewDst);

                            if (chunk.IsVisible())
                            {
                                chunksVisibleLastUpdate.Add(chunk);
                            }
                        }
                        else
                        {
                            if (!chunkLoadQueue.Contains(viewedChunkCoord))
                            {
                                chunkLoadQueue.Enqueue(viewedChunkCoord);
                            }
                        }
                    }
                }
            }
        }
        if (!initialLoadComplete)
        {
            initialLoadComplete = true;
        }
        if (!isLoadingChunks)
        {
            StartCoroutine(LoadChunksOverTime());
        }
        if (!isLoadingReadbacks)
        {
            StartCoroutine(LoadReadbacksOverTime());
        }
        if (!isLoadingMeshes)
        {
            StartCoroutine(LoadMeshesOverTime());
        }
    }
    /// <summary>
    /// Coroutine for loading chunks asynchronously
    /// </summary>
    /// <returns>yield return</returns>
    private IEnumerator LoadChunksOverTime()
    {
        isLoadingChunks = true;

        int chunkBatchCounter = 0;
        while (chunkLoadQueue.Count > 0)
        {
            Vector3Int coord = chunkLoadQueue.Dequeue();

            if (!chunkDictionary.ContainsKey(coord))
            {
                var chunk = new TerrainChunk(coord, chunkSize, transform, terrainDensityData, assetSpawnData,
                                            marchingCubesComputeShader, terrainDensityComputeShader,
                                            terrainNoiseComputeShader, caveNoiseComputeShader,
                                            terraformComputeShader, spawnPointsComputeShader,
                                            terrainMaterial, waterMaterial, initialLoadComplete);
                chunkDictionary.Add(coord, chunk);
                if (chunk.IsVisible())
                {
                    chunksVisibleLastUpdate.Add(chunk);
                }
                chunkBatchCounter++;
            }

            // if (chunkBatchCounter % 2 == 0)
            // {
                yield return new WaitForEndOfFrame();
            // }
        }

        isLoadingChunks = false;
    }
    /// <summary>
    /// Coroutine for loading chunks asynchronously
    /// </summary>
    /// <returns>yield return</returns>
    private IEnumerator LoadReadbacksOverTime()
    {
        isLoadingReadbacks = true;

        int readbackBatchCounter = 0;
        while (pendingReadbacks.Count > 0)
        {
            ReadbackRequest pendingReadback = pendingReadbacks.Dequeue();

            AsyncGPUReadback.Request(pendingReadback.buffer, pendingReadback.readbackRequest);

            readbackBatchCounter++;

            if (readbackBatchCounter % 2 == 0)
            {
                yield return new WaitForEndOfFrame();
            }
        }

        isLoadingReadbacks = false;
    }
    /// <summary>
    /// Coroutine for loading chunks asynchronously
    /// </summary>
    /// <returns>yield return</returns>
    private IEnumerator LoadMeshesOverTime()
    {
        isLoadingMeshes = true;

        int meshBatchCounter = 0;
        while (pendingMeshInits.Count > 0)
        {
            pendingMeshInits.Dequeue()?.Invoke();

            meshBatchCounter++;

            // if (meshBatchCounter % 2 == 0)
            // {
                yield return new WaitForEndOfFrame();
            // }
        }

        isLoadingMeshes = false;
    }
    /// <summary>
    /// Get a TerrainChunk and its neighbors with the given chunk's coordinate
    /// </summary>
    /// <param name="chunkCoord">The chunk coordinate</param>
    /// <returns>A list containing the chunk whose coordinate was passed and its neighbors</returns>
    public TerrainChunk[] GetChunkAndNeighbors(Vector3Int chunkCoord)
    {
        TerrainChunk[] chunkAndNeighbors = new TerrainChunk[] {
            chunkDictionary[new Vector3Int(chunkCoord.x, chunkCoord.y, chunkCoord.z)],       // 1
            chunkDictionary[new Vector3Int(chunkCoord.x, chunkCoord.y+1, chunkCoord.z)],     // 2
            chunkDictionary[new Vector3Int(chunkCoord.x, chunkCoord.y, chunkCoord.z+1)],     // 3
            chunkDictionary[new Vector3Int(chunkCoord.x, chunkCoord.y+1, chunkCoord.z+1)],   // 4
            chunkDictionary[new Vector3Int(chunkCoord.x, chunkCoord.y-1, chunkCoord.z)],     // 5
            chunkDictionary[new Vector3Int(chunkCoord.x, chunkCoord.y, chunkCoord.z-1)],     // 6
            chunkDictionary[new Vector3Int(chunkCoord.x, chunkCoord.y-1, chunkCoord.z-1)],   // 7
            chunkDictionary[new Vector3Int(chunkCoord.x, chunkCoord.y+1, chunkCoord.z-1)],   // 8
            chunkDictionary[new Vector3Int(chunkCoord.x, chunkCoord.y-1, chunkCoord.z+1)],   // 9
            chunkDictionary[new Vector3Int(chunkCoord.x+1, chunkCoord.y, chunkCoord.z)],     // 10
            chunkDictionary[new Vector3Int(chunkCoord.x+1, chunkCoord.y+1, chunkCoord.z)],   // 11
            chunkDictionary[new Vector3Int(chunkCoord.x+1, chunkCoord.y, chunkCoord.z+1)],   // 12
            chunkDictionary[new Vector3Int(chunkCoord.x+1, chunkCoord.y+1, chunkCoord.z+1)], // 13
            chunkDictionary[new Vector3Int(chunkCoord.x+1, chunkCoord.y-1, chunkCoord.z)],   // 14
            chunkDictionary[new Vector3Int(chunkCoord.x+1, chunkCoord.y, chunkCoord.z-1)],   // 15
            chunkDictionary[new Vector3Int(chunkCoord.x+1, chunkCoord.y-1, chunkCoord.z-1)], // 16
            chunkDictionary[new Vector3Int(chunkCoord.x+1, chunkCoord.y+1, chunkCoord.z-1)], // 17
            chunkDictionary[new Vector3Int(chunkCoord.x+1, chunkCoord.y-1, chunkCoord.z+1)], // 18
            chunkDictionary[new Vector3Int(chunkCoord.x-1, chunkCoord.y, chunkCoord.z)],     // 19
            chunkDictionary[new Vector3Int(chunkCoord.x-1, chunkCoord.y+1, chunkCoord.z)],   // 20
            chunkDictionary[new Vector3Int(chunkCoord.x-1, chunkCoord.y, chunkCoord.z+1)],   // 21
            chunkDictionary[new Vector3Int(chunkCoord.x-1, chunkCoord.y+1, chunkCoord.z+1)], // 22
            chunkDictionary[new Vector3Int(chunkCoord.x-1, chunkCoord.y-1, chunkCoord.z)],   // 23
            chunkDictionary[new Vector3Int(chunkCoord.x-1, chunkCoord.y, chunkCoord.z-1)],   // 24
            chunkDictionary[new Vector3Int(chunkCoord.x-1, chunkCoord.y-1, chunkCoord.z-1)], // 25
            chunkDictionary[new Vector3Int(chunkCoord.x-1, chunkCoord.y+1, chunkCoord.z-1)], // 26
            chunkDictionary[new Vector3Int(chunkCoord.x-1, chunkCoord.y-1, chunkCoord.z+1)], // 27
        };
        return chunkAndNeighbors;
    }
    /// <summary>
    /// Clear out unnecessary data when quitting the application
    /// </summary>
    void OnApplicationQuit()
    {
        assetSpawnData.assets.Clear();
        chunkDictionary.Clear();
    }
    /// <summary>
    /// Custom class to store chunk objects and their relevant information and data
    /// </summary>
    public class TerrainChunk
    {
        public GameObject chunk;
        public ComputeMarchingCubes marchingCubes;
        public AssetSpawner assetSpawner;
        public GameObject waterPlaneGenerator;
        public WaterPlaneGenerator waterGen;
        public Vector3Int chunkPos;
        public Bounds bounds;
        public MeshCollider meshCollider;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public TerrainChunk(Vector3Int chunkCoord, int chunkSize, Transform parent, TerrainDensityData1 terrainDensityData, AssetSpawnData assetSpawnData,
                            ComputeShader marchingCubesComputeShader, ComputeShader terrainDensityComputeShader, ComputeShader terrainNoiseComputeShader,
                            ComputeShader caveNoiseComputeShader, ComputeShader terraformComputeShader, ComputeShader spawnPointsComputeShader,
                            Material terrainMaterial, Material waterMaterial, bool initialLoadComplete)
        {
            chunkPos = chunkCoord * chunkSize;
            bounds = new Bounds(chunkPos + (new Vector3(0.5f, 0.5f, 0.5f) * chunkSize), Vector3.one * chunkSize);
            chunk = new GameObject("Chunk" + chunkPos);
            chunk.layer = 3;
            // Set up basic chunk components
            meshCollider = chunk.AddComponent<MeshCollider>();
            meshFilter = chunk.AddComponent<MeshFilter>();
            meshRenderer = chunk.AddComponent<MeshRenderer>();
            meshRenderer.material = terrainMaterial;
            meshRenderer.material.SetFloat("_UnderwaterTexHeightEnd", terrainDensityData.waterLevel - 15f);
            meshRenderer.material.SetFloat("_Tex1HeightStart", terrainDensityData.waterLevel - 18f);
            // Set up the chunk's AssetSpawn script
            assetSpawner = chunk.AddComponent<AssetSpawner>();
            assetSpawner.chunkPos = chunkPos;
            assetSpawner.terrainDensityData = terrainDensityData;
            assetSpawner.assetSpawnData = assetSpawnData;
            assetSpawner.spawnPointsComputeShader = spawnPointsComputeShader;
            // Set up the chunk's ComputeMarchingCubes script
            marchingCubes = chunk.AddComponent<ComputeMarchingCubes>();
            marchingCubes.meshFilter = meshFilter;
            marchingCubes.meshCollider = meshCollider;
            marchingCubes.chunkPos = chunkPos;
            marchingCubes.assetSpawner = assetSpawner;
            marchingCubes.marchingCubesComputeShader = marchingCubesComputeShader;
            marchingCubes.terrainDensityComputeShader = terrainDensityComputeShader;
            marchingCubes.terrainNoiseComputeShader = terrainNoiseComputeShader;
            marchingCubes.caveNoiseComputeShader = caveNoiseComputeShader;
            marchingCubes.terraformComputeShader = terraformComputeShader;
            marchingCubes.terrainDensityData = terrainDensityData;
            marchingCubes.terrainMaterial = terrainMaterial;
            marchingCubes.waterMaterial = waterMaterial;
            marchingCubes.initialLoadComplete = initialLoadComplete;
            // Set up water generator
            if (terrainDensityData.waterLevel > chunkPos.y && terrainDensityData.waterLevel < Mathf.RoundToInt(chunkPos.y + terrainDensityData.width))
            {
                waterPlaneGenerator = new GameObject("Water");
                waterPlaneGenerator.transform.SetParent(chunk.transform);
                MeshFilter waterGenMeshFilter = waterPlaneGenerator.AddComponent<MeshFilter>();
                MeshRenderer waterMat = waterPlaneGenerator.AddComponent<MeshRenderer>();
                waterMat.material = waterMaterial;
                waterGen = waterPlaneGenerator.AddComponent<WaterPlaneGenerator>();
                waterGen.meshFilter = waterGenMeshFilter;
                waterGen.terrainDensityData = terrainDensityData;
                waterGen.chunkPos = chunkPos;
                waterGen.marchingCubes = marchingCubes;
                marchingCubes.waterGen = waterGen;
            }
            chunk.transform.SetParent(parent);
            SetVisible(false);
        }
        /// <summary>
        /// Update the visibility of the chunk
        /// </summary>
        /// <param name="maxViewDst">The maximum view distance of the player</param>
        /// <param name="chunkSize">The chunk size</param>
        public void UpdateChunk(float maxViewDst)
        {
            float viewerDstFromBound = Mathf.Sqrt(bounds.SqrDistance(viewerPos));
            bool visible = viewerDstFromBound <= maxViewDst;
            SetVisible(visible);
        }
        /// <summary>
        /// Set the visibility of the chunk
        /// </summary>
        /// <param name="visible">Whether the chunk is visible</param>
        public void SetVisible(bool visible)
        {
            if (meshRenderer != null)
            {
                if (meshRenderer.enabled != visible)
                {
                    meshRenderer.enabled = visible;
                    meshCollider.enabled = visible;
                }
            }
            if (assetSpawner.assetsSet)
            {
                for (int i = 0; i < assetSpawner.spawnedAssets.Count; i++)
                {
                    foreach (Asset asset in assetSpawner.spawnedAssets[i])
                    {
                        if (asset.meshRenderer != null)
                        {
                            asset.meshRenderer.enabled = visible;
                        }
                        if (asset.meshCollider != null)
                        {
                            asset.meshCollider.enabled = visible;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Check chunk visibility
        /// </summary>
        /// <returns>If the chunk is visible or not</returns>
        public bool IsVisible()
        {
            // return chunk.activeSelf;
            return meshRenderer.enabled;
        }
    }
}
