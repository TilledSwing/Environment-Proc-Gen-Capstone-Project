using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Serializing.Helping;
using Unity.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class ChunkGenNetwork : NetworkBehaviour
{
    public static ChunkGenNetwork Instance;
    // Viewer Settings
    public int maxWorldYChunks = 10;
    public float maxViewDst = 100;
    public Transform viewer;
    public static Vector3 viewerPos;
    public float updateDistanceThreshold = 5f;
    private Vector3 lastUpdateViewerPos;
    public int chunkSize;
    public int chunksVisible;
    public bool useFixedMapSize;
    public int mapSize;
    public int resolution = 2;
    public LODData lod1Data = new LODData { lod = LOD.LOD1, resolution = 1 };
    public LODData lod2Data = new LODData { lod = LOD.LOD2, resolution = 2 };
    public LODData lod3Data = new LODData { lod = LOD.LOD3, resolution = 3 };
    public LODData lod6Data = new LODData { lod = LOD.LOD6, resolution = 6 };
    // Scriptable Object References
    public TerrainDensityData1 terrainDensityData;
    public AssetSpawnData assetSpawnData;
    public TerrainTextureData terrainTextureData;
    // Compute Shader References
    public ComputeShader marchingCubesComputeShader;
    public ComputeShader terrainDensityComputeShader;
    public ComputeShader terrainNoiseComputeShader;
    public ComputeShader caveNoiseComputeShader;
    public ComputeShader terraformComputeShader;
    // Material References
    public Material terrainMaterial;
    public Material waterMaterial;
    // Chunk Variables
    public Dictionary<Vector3, TerrainChunk> chunkDictionary = new();
    public List<TerrainChunk> chunksVisibleLastUpdate = new();
    private Queue<Vector3Int> chunkLoadQueue = new();
    private bool isLoadingChunks = false;
    public bool initialLoadComplete = false;
    // Lighting Blocker
    public GameObject lightingBlocker;
    private MeshRenderer lightingBlockerRenderer;
    // Action Queues
    public bool hasPendingMeshInits = false;
    public Queue<Action> pendingMeshInits = new();
    private bool isLoadingMeshes = false;
    public bool hasPendingReadbacks = false;
    public Queue<ReadbackRequest> pendingReadbacks = new();
    private bool isLoadingReadbacks = false;
    public bool hasPendingAssetInstantiations = false;
    public Queue<Action> pendingAssetInstantiations = new();
    private bool isLoadingAssetInstantiations = false;
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
    public enum LOD
    {
        LOD1 = 1,
        LOD2 = 2,
        LOD3 = 3,
        LOD6 = 6
    }
    [Serializable]
    public class LODData
    {
        public LOD lod;
        public int resolution;
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
        TextureSetup();
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

                    if (math.abs(viewedChunkCoord.y) > maxWorldYChunks) {
                        continue;
                    }

                    if (useFixedMapSize)
                    {
                        if (math.abs(viewedChunkCoord.x) > ((mapSize - 1) / 2) || math.abs(viewedChunkCoord.z) > ((mapSize - 1) / 2))
                        {
                            continue;
                        }
                    }

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
                            TerrainChunk chunk = new TerrainChunk(viewedChunkCoord, chunkSize, transform, terrainDensityData, assetSpawnData, terrainTextureData,
                                                         marchingCubesComputeShader, terrainDensityComputeShader, terrainNoiseComputeShader,
                                                         caveNoiseComputeShader, terraformComputeShader,
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
        if (!isLoadingAssetInstantiations)
        {
            StartCoroutine(LoadAssetInstantiationsOverTime());
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
                var chunk = new TerrainChunk(coord, chunkSize, transform, terrainDensityData, assetSpawnData, terrainTextureData,
                                            marchingCubesComputeShader, terrainDensityComputeShader,
                                            terrainNoiseComputeShader, caveNoiseComputeShader,
                                            terraformComputeShader,
                                            terrainMaterial, waterMaterial, initialLoadComplete);
                chunkDictionary.Add(coord, chunk);
                if (chunk.IsVisible())
                {
                    chunksVisibleLastUpdate.Add(chunk);
                }
                chunkBatchCounter++;
            }

            if (chunkBatchCounter % 2 == 0)
            {
                yield return new WaitForEndOfFrame();
            }
        }

        isLoadingChunks = false;
    }
    /// <summary>
    /// Coroutine for loading gpu readbacks asynchronously
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
    /// Coroutine for loading chunk mesh data asynchronously
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
    /// Coroutine for loading chunks asynchronously
    /// </summary>
    /// <returns>yield return</returns>
    private IEnumerator LoadAssetInstantiationsOverTime()
    {
        isLoadingAssetInstantiations = true;

        int assetInstantiationBatchCounter = 0;
        while (pendingAssetInstantiations.Count > 0)
        {
            pendingAssetInstantiations.Dequeue()?.Invoke();

            assetInstantiationBatchCounter++;

            if (assetInstantiationBatchCounter % 50 == 0)
            {
                yield return new WaitForEndOfFrame();
            }
        }

        isLoadingAssetInstantiations = false;
    }
    /// <summary>
    /// Get a TerrainChunk and its neighbors with the given chunk's coordinate
    /// </summary>
    /// <param name="chunkCoord">The chunk coordinate</param>
    /// <returns>A list containing the chunk whose coordinate was passed and its neighbors</returns>
    public TerrainChunk[] GetChunkAndNeighbors(Vector3Int chunkCoord)
    {
        Vector3Int[] offsets = new Vector3Int[]
        {
            new Vector3Int( 0,  0,  0), new Vector3Int( 1,  0,  0), new Vector3Int(-1,  0,  0),
            new Vector3Int( 0,  1,  0), new Vector3Int( 1,  1,  0), new Vector3Int(-1,  1,  0),
            new Vector3Int( 0,  0,  1), new Vector3Int( 1,  0,  1), new Vector3Int(-1,  0,  1),
            new Vector3Int( 0,  1,  1), new Vector3Int( 1,  1,  1), new Vector3Int(-1,  1,  1),
            new Vector3Int( 0, -1,  0), new Vector3Int( 1, -1,  0), new Vector3Int(-1, -1,  0),
            new Vector3Int( 0,  0, -1), new Vector3Int( 1,  0, -1), new Vector3Int(-1,  0, -1),
            new Vector3Int( 0, -1, -1), new Vector3Int( 1, -1, -1), new Vector3Int(-1, -1, -1),
            new Vector3Int( 0,  1, -1), new Vector3Int( 1,  1, -1), new Vector3Int(-1,  1, -1),
            new Vector3Int( 0, -1,  1), new Vector3Int( 1, -1,  1), new Vector3Int(-1, -1,  1),
        };

        TerrainChunk[] chunkAndNeighbors = new TerrainChunk[offsets.Length];

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3Int neighborCoord = chunkCoord + offsets[i];
            chunkDictionary.TryGetValue(neighborCoord, out chunkAndNeighbors[i]);
        }

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

    void TextureSetup()
    {
        foreach (TerrainTextureData.BiomeTextureConfigs biomeTextureConfig in terrainTextureData.biomeTextureConfigs)
        {
            float textureScale = biomeTextureConfig.textureScale;
            int textureWidth = biomeTextureConfig.biomeTextures[0].texture.width;
            int textureHeight = biomeTextureConfig.biomeTextures[0].texture.height;
            int textureCount = biomeTextureConfig.biomeTextures.Length;
            TextureFormat textureFormat = biomeTextureConfig.biomeTextures[0].texture.format;
            Texture2DArray textureArray = new(textureWidth, textureHeight, textureCount, textureFormat, true, false);
            textureArray.wrapMode = TextureWrapMode.Repeat;
            textureArray.filterMode = FilterMode.Bilinear;
            float[] useHeights = new float[biomeTextureConfig.MAX_TEXTURE_LAYERS];
            float[] heightStarts = new float[biomeTextureConfig.MAX_TEXTURE_LAYERS];
            float[] heightEnds = new float[biomeTextureConfig.MAX_TEXTURE_LAYERS];
            float[] useSlopes = new float[biomeTextureConfig.MAX_TEXTURE_LAYERS];
            float[] slopeStarts = new float[biomeTextureConfig.MAX_TEXTURE_LAYERS];
            float[] slopeEnds = new float[biomeTextureConfig.MAX_TEXTURE_LAYERS];
            for (int i = 0; i < biomeTextureConfig.biomeTextures.Length; i++)
            {
                Graphics.CopyTexture(biomeTextureConfig.biomeTextures[i].texture, 0, textureArray, i);
                useHeights[i] = biomeTextureConfig.biomeTextures[i].useHeightRange ? 1 : 0;
                heightStarts[i] = biomeTextureConfig.biomeTextures[i].heightRange.heightStart;
                heightEnds[i] = biomeTextureConfig.biomeTextures[i].heightRange.heightEnd;
                useSlopes[i] = biomeTextureConfig.biomeTextures[i].useSlopeRange ? 1 : 0;
                slopeStarts[i] = biomeTextureConfig.biomeTextures[i].slopeRange.slopeStart;
                slopeEnds[i] = biomeTextureConfig.biomeTextures[i].slopeRange.slopeEnd;
            }
            // textureArray.Apply(false);
            terrainMaterial.SetFloat("_Scale", textureScale);
            terrainMaterial.SetTexture("_TextureArray", textureArray);
            terrainMaterial.SetFloatArray("_UseHeightsArray", useHeights);
            terrainMaterial.SetFloatArray("_HeightStartsArray", heightStarts);
            terrainMaterial.SetFloatArray("_HeightEndsArray", heightEnds);
            terrainMaterial.SetFloatArray("_UseSlopesArray", useSlopes);
            terrainMaterial.SetFloatArray("_SlopeStartsArray", slopeStarts);
            terrainMaterial.SetFloatArray("_SlopeEndsArray", slopeEnds);
            terrainMaterial.SetInt("_LayerCount", biomeTextureConfig.biomeTextures.Length);
        }
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
        public TerrainChunk(Vector3Int chunkCoord, int chunkSize, Transform parent, TerrainDensityData1 terrainDensityData, AssetSpawnData assetSpawnData, TerrainTextureData terrainTextureData,
                            ComputeShader marchingCubesComputeShader, ComputeShader terrainDensityComputeShader, ComputeShader terrainNoiseComputeShader,
                            ComputeShader caveNoiseComputeShader, ComputeShader terraformComputeShader,
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
            // Chunk texture
            meshRenderer.material = terrainMaterial;
            // meshRenderer.material.SetFloat("_UnderwaterTexHeightEnd", terrainDensityData.waterLevel - 15f);
            // meshRenderer.material.SetFloat("_Tex1HeightStart", terrainDensityData.waterLevel - 18f);
            // Set up the chunk's AssetSpawn script
            assetSpawner = chunk.AddComponent<AssetSpawner>();
            assetSpawner.chunkPos = chunkPos;
            assetSpawner.terrainDensityData = terrainDensityData;
            assetSpawner.assetSpawnData = assetSpawnData;
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
