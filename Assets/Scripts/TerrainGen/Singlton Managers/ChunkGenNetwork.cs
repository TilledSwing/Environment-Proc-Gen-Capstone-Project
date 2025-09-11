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
using UnityEngine.Rendering.Universal;

public class ChunkGenNetwork : MonoBehaviour
{
    public static ChunkGenNetwork Instance;
    // Fog Render Feature Stuff
    public Material fogMat;
    public Color fogColor = new Color(160f, 196f, 233f, 1f);
    public Color darkFogColor;
    public UniversalRendererData rendererData;
    private FogRenderPassFeature fogRenderPassFeature;
    // Objective Text Stuff
    public GameObject objectiveCanvas;
    public GameObject objectiveHeader;
    public GameObject objectiveCounterText;
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
    public int resolution;
    public LODData[] lodData =
    {
        new LODData { lod = LOD.LOD1, resolution = 1 },
        new LODData { lod = LOD.LOD2, resolution = 2 },
        new LODData { lod = LOD.LOD3, resolution = 3 },
        new LODData { lod = LOD.LOD6, resolution = 6 }
    };
    // Scriptable Object References
    public TerrainDensityData terrainDensityData;
    public AssetSpawnData assetSpawnData;
    public TerrainTextureData terrainTextureData;
    // Compute Shader References
    public ComputeShader marchingCubesComputeShader;
    public ComputeShader terrainDensityComputeShader;
    public ComputeShader terrainNoiseComputeShader;
    public ComputeShader terraformComputeShader;
    // Material References
    public Material terrainMaterial;
    public Material waterMaterial;
    // Chunk Variables
    public Dictionary<Vector3, TerrainChunk> chunkDictionary = new();
    public List<TerrainChunk> chunksVisibleLastUpdate = new();
    private PriorityQueue<Vector3Int> chunkLoadQueue = new();
    private HashSet<Vector3Int> chunkLoadSet = new();
    public Queue<TerrainChunk> chunkHideQueue = new();
    public Queue<TerrainChunk> chunkShowQueue = new();
    private bool isLoadingChunkVisibility = false;
    public float queueUpdateDistanceThreshold = 15f;
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
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        chunkSize = terrainDensityData.width;
        chunksVisible = Mathf.RoundToInt(maxViewDst / chunkSize);
        lightingBlockerRenderer = lightingBlocker.GetComponent<MeshRenderer>();
        lightingBlockerRenderer.enabled = false;
        TextureSetup();
        // Set seeds
        foreach (NoiseGenerator noiseGenerator in terrainDensityData.noiseGenerators)
        {
            noiseGenerator.noiseSeed = UnityEngine.Random.Range(0, 100000);
            noiseGenerator.domainWarpSeed = UnityEngine.Random.Range(0, 100000);
        }
        // Fog Shader Inits
        fogRenderPassFeature = rendererData.rendererFeatures.Find(f => f is FogRenderPassFeature) as FogRenderPassFeature;
        fogMat.SetFloat("_fogOffset", maxViewDst - 20f);
        fogMat.SetColor("_fogColor", fogColor);
        
        // terrainDensityData.noiseSeed = UnityEngine.Random.Range(0, 100000);
        // terrainDensityData.caveNoiseSeed = UnityEngine.Random.Range(0, 100000);
        // terrainDensityData.domainWarpSeed = UnityEngine.Random.Range(0, 100000);
        // terrainDensityData.caveDomainWarpSeed = UnityEngine.Random.Range(0, 100000);
    }
    public void SetFogActive(bool active)
    {
        if (fogRenderPassFeature != null)
        {
            fogRenderPassFeature.SetActive(active);
        }
    }

    void Update()
    {
        // Position updates
        viewerPos = viewer.position;
        lightingBlocker.transform.position = new Vector3(viewerPos.x, 0, viewerPos.z);

        // Darker fog at lower world heights
        float depthFactor = Mathf.Clamp01(-viewerPos.y * 0.01f); 
        Color currentFog = Color.Lerp(fogColor, darkFogColor, depthFactor);
        fogMat.SetColor("_fogColor", currentFog);

        // Update chunks
        if ((viewerPos - lastUpdateViewerPos).sqrMagnitude > updateDistanceThreshold * updateDistanceThreshold && initialLoadComplete)
        {
            UpdateVisibleChunks();
            lastUpdateViewerPos = viewerPos;
        }
        else if (!initialLoadComplete)
        {
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
            // chunkHideQueue.Enqueue(chunksVisibleLastUpdate[i]);
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

                    if (chunkDictionary.TryGetValue(viewedChunkCoord, out TerrainChunk dictChunk))
                    {
                        dictChunk.UpdateChunk(maxViewDst, terrainDensityData.width);
                        if (dictChunk.IsVisible())
                        {
                            chunksVisibleLastUpdate.Add(dictChunk);
                            // chunkShowQueue.Enqueue(dictChunk);
                        }
                        // else
                        // {
                        //     chunkHideQueue.Enqueue(dictChunk);
                        // }
                    }
                    else
                    {
                        if (!initialLoadComplete)
                        {
                            // Generate immediately during first load
                            TerrainChunk chunk = new TerrainChunk(viewedChunkCoord, chunkSize, GameObject.Find("ChunkParent").transform, terrainDensityData, assetSpawnData, terrainTextureData,
                                                         marchingCubesComputeShader, terrainDensityComputeShader, terrainNoiseComputeShader, terraformComputeShader,
                                                         terrainMaterial, waterMaterial, initialLoadComplete);

                            chunkDictionary.Add(viewedChunkCoord, chunk);
                            chunk.UpdateChunk(maxViewDst, terrainDensityData.width);

                            if (chunk.IsVisible())
                            {
                                chunksVisibleLastUpdate.Add(chunk);
                                // chunkShowQueue.Enqueue(chunk);
                            }
                            // else
                            // {
                            //     chunkHideQueue.Enqueue(dictChunk);
                            // }
                        }
                        else
                        {
                            if (!chunkLoadSet.Contains(viewedChunkCoord))
                            {
                                Vector3 chunkCenter = (viewedChunkCoord * chunkSize) + Vector3.one * chunkSize * 0.5f;
                                Vector3 toChunk = (chunkCenter - viewerPos).normalized;

                                Vector3 movementVector = viewerPos - lastUpdateViewerPos;
                                Vector3 movementDir = movementVector.sqrMagnitude > 0.01f ? movementVector.normalized : Camera.main.transform.forward;

                                float angle = Vector3.Angle(movementDir, toChunk);
                                if (angle > 60f) continue;

                                Bounds bounds = new Bounds((viewedChunkCoord * chunkSize) + (new Vector3(0.5f, 0.5f, 0.5f) * chunkSize), Vector3.one * chunkSize);
                                float viewerDstFromBound = bounds.SqrDistance(viewerPos);
                                chunkLoadQueue.Enqueue(viewedChunkCoord, viewerDstFromBound);
                                chunkLoadSet.Add(viewedChunkCoord);
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
        // if (!isLoadingChunkVisibility)
        // {
        //     StartCoroutine(LoadChunkVisibilityOverTime());
        // }
        if (!isLoadingReadbacks)
        {
            StartCoroutine(LoadReadbacksOverTime());
        }
        // if (!isLoadingMeshes)
        // {
        //     StartCoroutine(LoadMeshesOverTime());
        // }
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
        Vector3 startViewerPos = viewerPos;
        isLoadingChunks = true;

        int chunkBatchCounter = 0;

        while (chunkLoadQueue.Count > 0)
        {
            if (Vector3.Distance(startViewerPos, viewerPos) >= queueUpdateDistanceThreshold)
            {
                List<Vector3Int> oldChunkCoords = new();
                while (chunkLoadQueue.Count > 0)
                {
                    oldChunkCoords.Add(chunkLoadQueue.Dequeue());
                }
                foreach (Vector3Int chunkCoord in oldChunkCoords)
                {
                    Bounds chunkBounds = new Bounds((chunkCoord * chunkSize) + (new Vector3(0.5f, 0.5f, 0.5f) * chunkSize), Vector3.one * chunkSize);
                    float chunkViewerDstFromBound = chunkBounds.SqrDistance(viewerPos);
                    chunkLoadQueue.Enqueue(chunkCoord, chunkViewerDstFromBound);
                }
                startViewerPos = viewerPos;
            }
            Vector3Int coord = chunkLoadQueue.Dequeue();
            chunkLoadSet.Remove(coord);
            Bounds bounds = new Bounds((coord * chunkSize) + (new Vector3(0.5f, 0.5f, 0.5f) * chunkSize), Vector3.one * chunkSize);
            float viewerDstFromBound = bounds.SqrDistance(viewerPos);

            if (!chunkDictionary.TryGetValue(coord, out TerrainChunk dictChunk) && viewerDstFromBound <= (maxViewDst * maxViewDst))
            {
                var chunk = new TerrainChunk(coord, chunkSize, GameObject.Find("ChunkParent").transform, terrainDensityData, assetSpawnData, terrainTextureData,
                                            marchingCubesComputeShader, terrainDensityComputeShader,
                                            terrainNoiseComputeShader,
                                            terraformComputeShader,
                                            terrainMaterial, waterMaterial, initialLoadComplete);
                chunkDictionary.Add(coord, chunk);
                chunk.UpdateChunk(maxViewDst, chunkSize);
                if (chunk.IsVisible())
                {
                    chunksVisibleLastUpdate.Add(chunk);
                    // chunkShowQueue.Enqueue(chunk);
                }
                // else
                // {
                //     chunkHideQueue.Enqueue(chunk);
                // }
                chunkBatchCounter++;
            }

            if (chunkBatchCounter % 4 == 0)
            {
                yield return new WaitForEndOfFrame();
            }
        }

        isLoadingChunks = false;
    }
    /// <summary>
    /// Coroutine for loading chunks visibility asynchronously
    /// </summary>
    /// <returns>yield return</returns>
    private IEnumerator LoadChunkVisibilityOverTime()
    {
        isLoadingChunkVisibility = true;

        int maxPerFrame = 8;
        while (chunkHideQueue.Count > 0 || chunkShowQueue.Count > 0)
        {
            int processedChunks = 0;
            while (processedChunks <= maxPerFrame && chunkHideQueue.Count > 0)
            {
                TerrainChunk chunk = chunkHideQueue.Dequeue();
                chunk.SetVisible(false);
                processedChunks++;
            }
            while (processedChunks <= maxPerFrame && chunkShowQueue.Count > 0)
            {
                TerrainChunk chunk = chunkShowQueue.Dequeue();
                chunk.SetVisible(true);
                processedChunks++;
            }

            yield return null;
        }

        isLoadingChunkVisibility = false;
    }
    /// <summary>
    /// Coroutine for loading gpu readbacks asynchronously
    /// </summary>
    /// <returns>yield return</returns>
    private IEnumerator LoadReadbacksOverTime()
    {
        isLoadingReadbacks = true;

        List<AsyncGPUReadbackRequest> activeRequests = ListPoolManager<AsyncGPUReadbackRequest>.Get();

        while (pendingReadbacks.Count > 0 || activeRequests.Count > 0)
        {
            for (int i = 0; i < activeRequests.Count; i++)
            {
                if (activeRequests[i].done)
                {
                    activeRequests.RemoveAt(i);
                }
            }

            while (activeRequests.Count <= 2 && pendingReadbacks.Count > 0)
            {
                ReadbackRequest pendingReadback = pendingReadbacks.Dequeue();

                activeRequests.Add(AsyncGPUReadback.Request(pendingReadback.buffer, pendingReadback.readbackRequest));
            }

            yield return null;
        }

        ListPoolManager<AsyncGPUReadbackRequest>.Return(activeRequests);

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
        fogRenderPassFeature.SetActive(false);
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

            float lowestStartHeight = float.MaxValue;
            float greatestEndHeight = float.MinValue;

            for (int i = 0; i < biomeTextureConfig.biomeTextures.Length; i++)
            {
                Graphics.CopyTexture(biomeTextureConfig.biomeTextures[i].texture, 0, textureArray, i);
                useHeights[i] = biomeTextureConfig.biomeTextures[i].useHeightRange ? 1 : 0;
                heightStarts[i] = biomeTextureConfig.biomeTextures[i].heightRange.heightStart;
                heightEnds[i] = biomeTextureConfig.biomeTextures[i].heightRange.heightEnd;
                useSlopes[i] = biomeTextureConfig.biomeTextures[i].useSlopeRange ? 1 : 0;
                slopeStarts[i] = biomeTextureConfig.biomeTextures[i].slopeRange.slopeStart;
                slopeEnds[i] = biomeTextureConfig.biomeTextures[i].slopeRange.slopeEnd;

                if (heightStarts[i] < lowestStartHeight)
                    lowestStartHeight = heightStarts[i] + 1;

                if (heightEnds[i] > greatestEndHeight)
                    greatestEndHeight = heightEnds[i] - 1;
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
            terrainMaterial.SetFloat("_LowestStartHeight", lowestStartHeight);
            terrainMaterial.SetFloat("_GreatestEndHeight", greatestEndHeight);
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
        public bool visible = false;
        public TerrainChunk(Vector3Int chunkCoord, int chunkSize, Transform parent, TerrainDensityData terrainDensityData, AssetSpawnData assetSpawnData, TerrainTextureData terrainTextureData,
                            ComputeShader marchingCubesComputeShader, ComputeShader terrainDensityComputeShader, ComputeShader terrainNoiseComputeShader,
                            ComputeShader terraformComputeShader,
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
            marchingCubes.terraformComputeShader = terraformComputeShader;
            marchingCubes.terrainDensityData = terrainDensityData;
            marchingCubes.terrainMaterial = terrainMaterial;
            marchingCubes.waterMaterial = waterMaterial;
            marchingCubes.initialLoadComplete = initialLoadComplete;
            // float viewerDstFromBound = bounds.SqrDistance(viewerPos);
            // if (viewerDstFromBound <= chunkSize * 2) marchingCubes.currentLOD = LOD.LOD1;
            // else if (viewerDstFromBound <= chunkSize * 4) marchingCubes.currentLOD = LOD.LOD2;
            // else if (viewerDstFromBound <= chunkSize * 6) marchingCubes.currentLOD = LOD.LOD3;
            // else if (viewerDstFromBound <= chunkSize * 8) marchingCubes.currentLOD = LOD.LOD6;
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
                waterGen.meshRenderer = waterMat;
                waterGen.terrainDensityData = terrainDensityData;
                waterGen.chunkPos = chunkPos;
                waterGen.marchingCubes = marchingCubes;
                marchingCubes.waterGen = waterGen;
                waterPlaneGenerator.AddComponent<DitherFadeController>();
            }
            chunk.transform.SetParent(parent);
            // Instance.chunkHideQueue.Enqueue(this);
            SetVisible(false);
        }
        /// <summary>
        /// Update the visibility of the chunk
        /// </summary>
        /// <param name="maxViewDst">The maximum view distance of the player</param>
        /// <param name="chunkSize">The chunk size</param>
        public void UpdateChunk(float maxViewDst, int chunkSize)
        {
            float viewerDstFromBound = bounds.SqrDistance(viewerPos);
            // if (viewerDstFromBound <= chunkSize * 2) marchingCubes.UpdateMesh(LOD.LOD1);
            // else if (viewerDstFromBound <= chunkSize * 4) marchingCubes.UpdateMesh(LOD.LOD2);
            // else if (viewerDstFromBound <= chunkSize * 6) marchingCubes.UpdateMesh(LOD.LOD3);
            // else if (viewerDstFromBound <= chunkSize * 8) marchingCubes.UpdateMesh(LOD.LOD6);
            bool visible = viewerDstFromBound <= (maxViewDst * maxViewDst);
            // this.visible = visible;
            SetVisible(visible);
        }
        /// <summary>
        /// Set the visibility of the chunk
        /// </summary>
        /// <param name="visible">Whether the chunk is visible</param>
        public void SetVisible(bool visible)
        {
            if (meshRenderer != null && meshRenderer.enabled != visible)
            {
                meshRenderer.enabled = visible;
                if (meshCollider != null && meshCollider.enabled != visible)
                {
                    meshCollider.enabled = visible;
                }
            }
            if (Instance.terrainDensityData.waterLevel > chunkPos.y && Instance.terrainDensityData.waterLevel < Mathf.RoundToInt(chunkPos.y + Instance.terrainDensityData.waterLevel))
            {
                if (waterGen.meshRenderer != null && waterGen.meshRenderer.enabled != visible)
                {
                    waterGen.meshRenderer.enabled = visible;
                }
            }
            if (assetSpawner.assetsSet)
            {
                for (int i = 0; i < assetSpawner.spawnedAssets.Count; i++)
                {
                    foreach (Asset asset in assetSpawner.spawnedAssets[i])
                    {
                        if (asset.meshRenderer != null && asset.meshRenderer.enabled != visible)
                        {
                            asset.meshRenderer.enabled = visible;
                            if (asset.meshCollider != null && asset.meshCollider.enabled != visible)
                            {
                                asset.meshCollider.enabled = visible;
                            }
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
            // return visible;
        }
    }
}
