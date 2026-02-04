using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ChunkGenNetwork : MonoBehaviour
{
    public static ChunkGenNetwork Instance;
    // Fog Render Feature Stuff
    public Material fogMat;
    public float fogDensity;
    public float fogOffset;
    public Color upperFogColor;
    public Color lowerFogColor;
    public Color darkFogColor;
    public UniversalRendererData rendererData;
    public FogRenderPassFeature fogRenderPassFeature;
    public UniversalRenderPipelineAsset mainUrpAsset;
    public UniversalRenderPipelineAsset underwaterUrpAsset;
    // Objective Text Stuff
    public GameObject objectiveCanvas;
    public GameObject hudCanvas;
    // Chat & Lobby
    public GameObject chatContainer;
    public GameObject lobbyContainer;
    // Viewer Settings
    public int maxWorldYChunks = 10;
    public float maxViewDst;
    public Transform viewer;
    public Vector3 viewerPos;
    public float updateDistanceThreshold = 5f;
    private Vector3 lastUpdateViewerPos;
    public int chunkSize;
    public int chunksVisible;
    public bool useFixedMapSize;
    public int mapSize;
    public int resolution;
    public int maxYChunksVisible;
    // Scriptable Object References
    public GenerationConfiguration generationConfiguration;
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
    // Grass Stuff
    public ComputeShader grassPositionComputeShader;
    public Mesh grassMesh;
    public Material grassMaterial;
    public int grassDensity;
    // Texture Arrays
    public Texture2DArray textureArray;
    public float[] useHeights;
    public float[] heightStarts;
    public float[] heightEnds;
    public float[] useSlopes;
    public float[] slopeStarts;
    public float[] slopeEnds;
    // Texture Window Stuff
    public GameObject textureWindow;
    public GameObject textureSettingsTab;
    // Asset Window Stuff
    public GameObject assetWindow;
    public GameObject assetSettingsTab;
    // Preset Dropdown
    public TMP_Dropdown presetDropdown;
    // Chunk Variables
    public Dictionary<Vector3, TerrainChunk> chunkDictionary = new();
    public List<TerrainChunk> chunksVisibleLastUpdate = new();
    public PriorityQueue<Vector3Int> chunkLoadQueue = new();
    public HashSet<Vector3Int> chunkLoadSet = new();
    public Queue<Action> chunkVisibilityQueue = new();
    public HashSet<TerrainChunk> chunksToHide = new();
    public HashSet<TerrainChunk> chunksToShow = new();
    public bool isLoadingChunkVisibility = false;
    public float queueUpdateDistanceThreshold = 15f;
    public bool isLoadingChunks = false;
    public bool initialLoadComplete = false;
    public Texture2D[] noiseGeneratorTextureArray;
    // Lighting Blocker
    public GameObject lightingBlocker;
    private MeshRenderer lightingBlockerRenderer;
    public Light lightChange;
    // Action Queues
    public bool hasPendingReadbacks = false;
    public PriorityQueue<ReadbackRequest> pendingReadbacks = new();
    public bool isLoadingReadbacks = false;
    public bool hasPendingAssetInstantiations = false;
    public Queue<Action> pendingAssetInstantiations = new();
    public bool isLoadingAssetInstantiations = false;
    public Queue<Action> marchingCubesJobQueue = new();
    // Data structure pools
    public event Action OnTerrainReady;
    public bool IsTerrainReady { get; private set; }
    // Reused Marching Cubes Native Array
    public NativeArray<float3> vertexOffsetTable;
    public NativeArray<int> edgeIndexTable;
    public NativeArray<int> triangleTable;

    public NativeArray<int> staticMaxSizeVertexIndexArray;
    public void CreateVertexIndexArray()
    {
        int size = (terrainDensityData.width + 1) * (terrainDensityData.width + 1) * (terrainDensityData.width + 1);
        staticMaxSizeVertexIndexArray = new NativeArray<int>(size, Allocator.Persistent);
        for (int i = 0; i < size; i++)
        {
            staticMaxSizeVertexIndexArray[i] = i;
        }
    }
    
    public class ReadbackRequest
    {
        public Bounds bounds;
        public ComputeBuffer buffer;
        public Action<AsyncGPUReadbackRequest> readbackRequest;

        public ReadbackRequest(Bounds bounds, ComputeBuffer buffer, Action<AsyncGPUReadbackRequest> readbackRequest)
        {
            this.bounds = bounds;
            this.buffer = buffer;
            this.readbackRequest = readbackRequest;
        }
    }
    void Awake()
    {
        // Make a singleton
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // VSYNC ON
        QualitySettings.vSyncCount = 1;   // wait for monitor refresh
        Application.targetFrameRate = -1; // let vsync control it

        // DATA LEAK STACK TRACES ENABLED
        NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
        
        lightingBlockerRenderer = lightingBlocker.GetComponent<MeshRenderer>();
        lightingBlockerRenderer.enabled = false;
        lightChange.intensity = 12f;

        vertexOffsetTable = new(MarchingCubesTables.vertexOffsetTable, Allocator.Persistent);
        edgeIndexTable = new(MarchingCubesTables.edgeIndexTable, Allocator.Persistent);
        triangleTable = new(MarchingCubesTables.triangleTable, Allocator.Persistent);

        // Fog Shader Inits
        fogRenderPassFeature = rendererData.rendererFeatures.Find(f => f is FogRenderPassFeature) as FogRenderPassFeature;
        
        fogMat.SetFloat("_fogOffset", fogOffset);
        fogMat.SetFloat("_fogDensity", fogDensity);
        fogMat.SetColor("_upperFogColor", upperFogColor);
        fogMat.SetColor("_lowerFogColor", lowerFogColor);

        waterMaterial.SetFloat("_fogOffset", fogOffset);
        waterMaterial.SetFloat("_fogDensity", fogDensity);
        waterMaterial.SetColor("_fogColor", lowerFogColor);
        waterMaterial.SetFloat("_fogActive", 0);
        SetFogActive(false);
        viewerPos = viewer.position;
        lastUpdateViewerPos = viewerPos;

        InitializeGenerator();
    }
    public void InitializeGenerator()
    {
        if (terrainTextureData != null)
            terrainTextureData.RestoreToOriginalState();
        if (assetSpawnData != null)
            assetSpawnData.RestoreToOriginalState();

        // Uncomment to see desert preset
        // Unity.Mathematics.Random rng = new((uint)UnityEngine.Random.Range(0, 100000));
        // int rand = rng.NextInt(0, generationConfiguration.terrainConfigs.Count);
        terrainDensityData = generationConfiguration.terrainConfigs[presetDropdown.value].terrainDensityData;
        terrainTextureData = generationConfiguration.terrainConfigs[presetDropdown.value].terrainTextureData;
        assetSpawnData = generationConfiguration.terrainConfigs[presetDropdown.value].assetSpawnData;
        // terrainDensityData = generationConfiguration.terrainConfigs[0].terrainDensityData;
        // terrainTextureData = generationConfiguration.terrainConfigs[0].terrainTextureData;
        // assetSpawnData = generationConfiguration.terrainConfigs[0].assetSpawnData;

        chunkSize = terrainDensityData.width;
        chunksVisible = Mathf.RoundToInt(maxViewDst / chunkSize);

        // Chunk Variables
        chunkDictionary = new();
        chunksVisibleLastUpdate = new();
        chunkLoadQueue = new();
        chunkLoadSet = new();
        isLoadingChunkVisibility = false;
        isLoadingChunks = false;
        initialLoadComplete = false;
        // Action Queues
        hasPendingReadbacks = false;
        pendingReadbacks = new();
        isLoadingReadbacks = false;
        hasPendingAssetInstantiations = false;
        pendingAssetInstantiations = new();
        isLoadingAssetInstantiations = false;

        DestroyChunks();
        assetSpawnData.ResetSpawnPoints();

        TextureSetup();
        AssetSetup();
        
        // Set seeds
        foreach (NoiseGenerator noiseGenerator in terrainDensityData.noiseGenerators)
        {
            noiseGenerator.noiseSeed = UnityEngine.Random.Range(0, 100000);
            noiseGenerator.domainWarpSeed = UnityEngine.Random.Range(0, 100000);
        }

        noiseGeneratorTextureArray = CreateNoiseCurveTextures();
        CreateVertexIndexArray();
        UpdateVisibleChunks();
    }
    /// <summary>
    /// Destroy all chunks
    /// </summary>
    public void DestroyChunks()
    {
        foreach (Transform chunk in GameObject.Find("ChunkParent").transform)
        {
            Destroy(chunk.gameObject);
        }
    }
    /// <summary>
    /// Toggle fog effect
    /// </summary>
    /// <param name="active">Set whether thhe fog should be active or inactive</param>
    public void SetFogActive(bool active)
    {
        fogMat.SetFloat("_fogOffset", active ? fogOffset : 1000);
        fogMat.SetFloat("_fogDensity", active ? fogDensity : 1);
        waterMaterial.SetFloat("_fogOffset", active ? fogOffset : 1000);
        waterMaterial.SetFloat("_fogDensity", active ? fogDensity : 1);
    }

    public void UpdateFromDB(TerrainSettings settings)
    {
        terrainDensityData = SeedSerializer.DeserializeTerrainDensity(settings);

        // Reset action and chunking to defaults (loading in from fresh)
        // Chunk Variables
        chunkDictionary = new();
        chunksVisibleLastUpdate = new();
        chunkLoadQueue = new();
        chunkLoadSet = new();
        isLoadingChunkVisibility = false;
        isLoadingChunks = false;
        initialLoadComplete = false;
        // Action Queues
        hasPendingReadbacks = false;
        pendingReadbacks = new();
        isLoadingReadbacks = false;
        hasPendingAssetInstantiations = false;
        pendingAssetInstantiations = new();
        isLoadingAssetInstantiations = false;
    }
    void Update()
    {
        // Position updates
        viewerPos = viewer.position;
        lightingBlocker.transform.position = new Vector3(viewerPos.x, 0, viewerPos.z);
        // Darker fog at lower world heights
        float depthFactor = Mathf.Clamp01(-viewerPos.y * 0.01f);
        Color currentFog = Color.Lerp(lowerFogColor, darkFogColor, depthFactor);
        fogMat.SetColor("_lowerFogColor", currentFog);
        waterMaterial.SetColor("_fogColor", currentFog);

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
        if (!isLoadingAssetInstantiations && pendingAssetInstantiations.Count > 0)
        {
            StartCoroutine(LoadAssetInstantiationsOverTime());
        }
        if (!IsTerrainReady)
        {
            CheckInitialTerrainFinish();
        }
        float start = Time.realtimeSinceStartup;
        while (marchingCubesJobQueue.Count > 0 && Time.realtimeSinceStartup - start < 0.003f) // 3ms
        {
            marchingCubesJobQueue.Dequeue()?.Invoke();
        }
        start = Time.realtimeSinceStartup;
        while (chunkVisibilityQueue.Count > 0 && Time.realtimeSinceStartup - start < 0.003f) // 3ms
        {
            chunkVisibilityQueue.Dequeue()?.Invoke();
        }
    }

    private void CheckInitialTerrainFinish()
    {
        if (initialLoadComplete &&
        !hasPendingAssetInstantiations &&
        !isLoadingAssetInstantiations &&
        !hasPendingReadbacks &&
        !isLoadingReadbacks &&
        !isLoadingChunks)
        {
            IsTerrainReady = true;
            OnTerrainReady?.Invoke();
        }
        
    }
    /// <summary>
    /// Update all the visible chunks loading in new ones and unloading old ones that are no longer visible
    /// </summary>
    public void UpdateVisibleChunks()
    {
        chunksToHide.Clear();
        chunksToShow.Clear();
        foreach (TerrainChunk chunk in chunksVisibleLastUpdate)
        {
            chunksToHide.Add(chunk);
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
                    if (math.abs(yOffset) > maxYChunksVisible) {
                        continue;
                    }
                    Vector3Int viewedChunkCoord = new Vector3Int(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset, currentChunkCoordZ + zOffset);

                    if (math.abs(viewedChunkCoord.y) > maxWorldYChunks)
                    {
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
                        dictChunk.UpdateChunk(maxViewDst);
                        if (dictChunk.IsVisible())
                        {
                            chunksVisibleLastUpdate.Add(dictChunk);
                        }
                    }
                    else
                    {
                        Bounds bounds = new Bounds((viewedChunkCoord * chunkSize) + (new Vector3(0.5f, 0.5f, 0.5f) * chunkSize), Vector3.one * chunkSize);
                        if (!initialLoadComplete && bounds.SqrDistance(viewerPos) <= maxViewDst * maxViewDst)
                        {
                            // Generate immediately during first load
                            TerrainChunk chunk = new TerrainChunk(viewedChunkCoord, chunkSize, GameObject.Find("ChunkParent").transform, terrainDensityData, assetSpawnData, terrainTextureData,
                                                         marchingCubesComputeShader, terrainDensityComputeShader, terrainNoiseComputeShader, terraformComputeShader,
                                                         terrainMaterial, waterMaterial, initialLoadComplete, noiseGeneratorTextureArray);
                            chunkDictionary.Add(viewedChunkCoord, chunk);
                            chunk.UpdateChunk(maxViewDst);

                            if (chunk.IsVisible())
                            {
                                chunksVisibleLastUpdate.Add(chunk);
                            }
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

                                bounds = new Bounds((viewedChunkCoord * chunkSize) + (new Vector3(0.5f, 0.5f, 0.5f) * chunkSize), Vector3.one * chunkSize);
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
        if (!isLoadingReadbacks)
        {
            StartCoroutine(LoadReadbacksOverTime());
        }
        if (!isLoadingAssetInstantiations)
        {
            StartCoroutine(LoadAssetInstantiationsOverTime());
        }

        foreach (TerrainChunk chunkToHide in chunksToHide)
        {
            chunkVisibilityQueue.Enqueue(() => chunkToHide.SetVisible(false));
        }
        foreach (TerrainChunk chunkToShow in chunksToShow)
        {
            chunkVisibilityQueue.Enqueue(() => chunkToShow.SetVisible(true));
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
                                            terrainNoiseComputeShader, terraformComputeShader,
                                            terrainMaterial, waterMaterial, initialLoadComplete, noiseGeneratorTextureArray);
                chunkDictionary.Add(coord, chunk);
                chunk.UpdateChunk(maxViewDst);
                if (chunk.IsVisible())
                {
                    chunksVisibleLastUpdate.Add(chunk);
                }
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
    /// Coroutine for loading gpu readbacks asynchronously
    /// </summary>
    /// <returns>yield return</returns>
    private IEnumerator LoadReadbacksOverTime()
    {
        Vector3 startViewerPos = viewerPos;
        isLoadingReadbacks = true;

        List<AsyncGPUReadbackRequest> activeRequests = ListPoolManager<AsyncGPUReadbackRequest>.Get();

        int count = 0;
        while (pendingReadbacks.Count > 0 || activeRequests.Count > 0)
        {
            if (Vector3.Distance(startViewerPos, viewerPos) >= queueUpdateDistanceThreshold)
            {
                List<ReadbackRequest> oldChunkReadbacks = new();
                while (pendingReadbacks.Count > 0)
                {
                    oldChunkReadbacks.Add(pendingReadbacks.Dequeue());
                }
                foreach (ReadbackRequest chunk in oldChunkReadbacks)
                {
                    float chunkViewerDstFromBound = chunk.bounds.SqrDistance(viewerPos);
                    pendingReadbacks.Enqueue(chunk, chunkViewerDstFromBound);
                }
                startViewerPos = viewerPos;
            }
            for (int i = activeRequests.Count - 1; i >= 0; i--)
            {
                if (activeRequests[i].done)
                {
                    activeRequests.RemoveAt(i);
                }
            }

            // 1 readback per 20 fps with a min and max of 2 and 6
            int maxActiveReadbacks = Mathf.Clamp(Mathf.RoundToInt(1f / Time.smoothDeltaTime / 20f), 2, 6);

            while (activeRequests.Count <= maxActiveReadbacks && pendingReadbacks.Count > 0)
            {
                ReadbackRequest pendingReadback = pendingReadbacks.Dequeue();

                activeRequests.Add(AsyncGPUReadback.Request(pendingReadback.buffer, pendingReadback.readbackRequest));
            }

            // Allow 2 dispatched per frame
            if(++count % 2 == 0) 
                yield return null;
        }

        ListPoolManager<AsyncGPUReadbackRequest>.Return(activeRequests);

        isLoadingReadbacks = false;
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
    /// Create the textures used for density calculation from animation curves
    /// </summary>
    /// <returns>An array of the textures used in density calculation</returns>
    public Texture2D[] CreateNoiseCurveTextures()
    {
        Texture2D[] textureArray = new Texture2D[terrainDensityData.noiseGenerators.Length];
        
        int i = 0;
        foreach (NoiseGenerator noiseGenerator in terrainDensityData.noiseGenerators)
        {
            textureArray[i] = SplineCurveFunctions.ArrayToTexture(SplineCurveFunctions.CurveToArray(noiseGenerator.valueCurve));
            i++;
        }

        return textureArray;
    }
    void OnDisable()
    {
        if(staticMaxSizeVertexIndexArray.IsCreated)
        {
            staticMaxSizeVertexIndexArray.Dispose();
        }
    }
    /// <summary>
    /// Clear out unnecessary data when quitting the application
    /// </summary>
    void OnApplicationQuit()
    {
        vertexOffsetTable.Dispose();
        edgeIndexTable.Dispose();
        triangleTable.Dispose();

        if(staticMaxSizeVertexIndexArray.IsCreated)
        {
            staticMaxSizeVertexIndexArray.Dispose();
        }

        assetSpawnData.ResetSpawnPoints();
        assetSpawnData.RestoreToOriginalState();
        terrainTextureData.RestoreToOriginalState();
        chunkDictionary.Clear();
        
        SetFogActive(false);
        GraphicsSettings.defaultRenderPipeline = mainUrpAsset;
        QualitySettings.renderPipeline = mainUrpAsset;
    }
    /// <summary>
    /// Initialize all texture data
    /// </summary>
    public void TextureSetup()
    {
        foreach (Transform texture in textureWindow.transform)
        {
            Destroy(texture.gameObject);
        }
        terrainTextureData.BackupOriginalState();
        foreach (BiomeTextureConfigs biomeTextureConfig in terrainTextureData.biomeTextureConfigs)
        {
            float textureScale = biomeTextureConfig.textureScale;
            int textureWidth = biomeTextureConfig.biomeTextures[0].texture.width;
            int textureHeight = biomeTextureConfig.biomeTextures[0].texture.height;
            int textureCount = biomeTextureConfig.MAX_TEXTURE_LAYERS;
            TextureFormat textureFormat = biomeTextureConfig.biomeTextures[0].texture.format;
            textureArray = new(textureWidth, textureHeight, textureCount, textureFormat, true, false);
            textureArray.wrapMode = TextureWrapMode.Repeat;
            textureArray.filterMode = FilterMode.Bilinear;
            useHeights = new float[biomeTextureConfig.MAX_TEXTURE_LAYERS];
            heightStarts = new float[biomeTextureConfig.MAX_TEXTURE_LAYERS];
            heightEnds = new float[biomeTextureConfig.MAX_TEXTURE_LAYERS];
            useSlopes = new float[biomeTextureConfig.MAX_TEXTURE_LAYERS];
            slopeStarts = new float[biomeTextureConfig.MAX_TEXTURE_LAYERS];
            slopeEnds = new float[biomeTextureConfig.MAX_TEXTURE_LAYERS];

            float lowestStartHeight = float.MaxValue;
            float greatestEndHeight = float.MinValue;

            for (int i = 0; i < biomeTextureConfig.biomeTextures.Count; i++)
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
                // Initializing texture settings window to currently applied textures
                TextureSettingsTabController texSettingsTab = Instantiate(textureSettingsTab, textureWindow.transform).GetComponent<TextureSettingsTabController>();
                texSettingsTab.textureIndex = i;

                texSettingsTab.texturePreview.texture = biomeTextureConfig.biomeTextures[i].texture;

                texSettingsTab.heightToggle.isOn = biomeTextureConfig.biomeTextures[i].useHeightRange;

                texSettingsTab.heightSlider.SetValues(biomeTextureConfig.biomeTextures[i].heightRange.heightStart, biomeTextureConfig.biomeTextures[i].heightRange.heightEnd, -maxWorldYChunks * terrainDensityData.width, maxWorldYChunks * terrainDensityData.width);

                texSettingsTab.slopeToggle.isOn = biomeTextureConfig.biomeTextures[i].useSlopeRange;

                texSettingsTab.slopeSlider.SetValues(biomeTextureConfig.biomeTextures[i].slopeRange.slopeStart, biomeTextureConfig.biomeTextures[i].slopeRange.slopeEnd, 0, 360);
            }
            terrainMaterial.SetFloat("_Scale", textureScale);
            terrainMaterial.SetTexture("_TextureArray", textureArray);
            terrainMaterial.SetFloatArray("_UseHeightsArray", useHeights);
            terrainMaterial.SetFloatArray("_HeightStartsArray", heightStarts);
            terrainMaterial.SetFloatArray("_HeightEndsArray", heightEnds);
            terrainMaterial.SetFloatArray("_UseSlopesArray", useSlopes);
            terrainMaterial.SetFloatArray("_SlopeStartsArray", slopeStarts);
            terrainMaterial.SetFloatArray("_SlopeEndsArray", slopeEnds);
            terrainMaterial.SetInt("_LayerCount", biomeTextureConfig.biomeTextures.Count);
            terrainMaterial.SetFloat("_LowestStartHeight", lowestStartHeight);
            terrainMaterial.SetFloat("_GreatestEndHeight", greatestEndHeight);
        }
    }
    /// <summary>
    /// Initialize assset UI data
    /// </summary>
    public void AssetSetup()
    {
        foreach (Transform asset in assetWindow.transform)
        {
            Destroy(asset.gameObject);
        }
        assetSpawnData.BackupOriginalState();
        int count = 0;
        foreach(SpawnableAsset asset in assetSpawnData.spawnableAssets)
        {
            AssetSettingsTabController assSettingsTab = Instantiate(assetSettingsTab, assetWindow.transform).GetComponent<AssetSettingsTabController>();
            assSettingsTab.assetIndex = count;
            assSettingsTab.canvasGroup = assetWindow.GetComponent<CanvasGroup>();
            assSettingsTab.assetSpawnData = assetSpawnData;
            // Header Settings
            assSettingsTab.assetPreview.texture = asset.icon;
            assSettingsTab.assetName.text = asset.name;
            assSettingsTab.rotateToFaceNormalToggle.isOn = asset.rotateToFaceNormal;
            // Spawn Probability Settings
            assSettingsTab.spawnProbInput.text = asset.spawnProbability.ToString();
            assSettingsTab.spawnProbSlider.value = asset.spawnProbability;
            // Max Per Chunk Settings
            assSettingsTab.maxPerChunkInput.text = asset.maxPerChunk.ToString();
            assSettingsTab.maxPerChunkSlider.value = asset.maxPerChunk;
            // Min Height Settings
            assSettingsTab.useMinHeightToggle.isOn = asset.useMinHeight;
            assSettingsTab.minHeightInput.text = asset.minHeight.ToString();
            assSettingsTab.minHeightSlider.value = asset.minHeight;
            // Max Height Settings
            assSettingsTab.useMaxHeightToggle.isOn = asset.useMaxHeight;
            assSettingsTab.maxHeightInput.text = asset.maxHeight.ToString();
            assSettingsTab.maxHeightSlider.value = asset.maxHeight;
            // Min Slope Settings
            assSettingsTab.useMinSlopeToggle.isOn = asset.useMinSlope;
            assSettingsTab.minSlopeInput.text = asset.minSlope.ToString();
            assSettingsTab.minSlopeSlider.value = asset.minSlope;
            // Max Slope Settings
            assSettingsTab.useMaxSlopeToggle.isOn = asset.useMaxSlope;
            assSettingsTab.maxSlopeInput.text = asset.maxSlope.ToString();
            assSettingsTab.maxSlopeSlider.value = asset.maxSlope;
            // Underwater Settings
            assSettingsTab.underwaterToggle.isOn = asset.underwaterAsset;
            assSettingsTab.minDepthInput.text = asset.minDepth.ToString();
            assSettingsTab.minDepthSlider.value = asset.minDepth;
            // Underground Settings
            assSettingsTab.undergroundToggle.isOn = asset.undergroundAsset;
            assSettingsTab.minDensityInput.text = asset.minDensity.ToString();
            assSettingsTab.minDensitySlider.value = asset.minDensity;
            // Valuable Settings
            assSettingsTab.valueableToggle.isOn = asset.isValuable;
            assSettingsTab.valueRangeSlider.SetValues(assSettingsTab.valueRangeSlider.Values.minLimit, assSettingsTab.valueRangeSlider.Values.maxLimit, asset.minValue, asset.maxValue);

            assSettingsTab.initialized = true;
            count++;
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
        public bool isWater = false;
        public NavMeshSurface navMeshSurface;
        public TerrainChunk(Vector3Int chunkCoord, int chunkSize, Transform parent, TerrainDensityData terrainDensityData, AssetSpawnData assetSpawnData, TerrainTextureData terrainTextureData,
                            ComputeShader marchingCubesComputeShader, ComputeShader terrainDensityComputeShader, ComputeShader terrainNoiseComputeShader,
                            ComputeShader terraformComputeShader,
                            Material terrainMaterial, Material waterMaterial, bool initialLoadComplete, Texture2D[] noiseGeneratorTextureArray)
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
            meshRenderer.sharedMaterial = terrainMaterial;
            // Set up the chunk's AssetSpawn script
            assetSpawner = chunk.AddComponent<AssetSpawner>();
            assetSpawner.chunkPos = chunkPos;
            assetSpawner.terrainDensityData = terrainDensityData;
            assetSpawner.assetSpawnData = assetSpawnData;
            // Set up the chunk's ComputeMarchingCubes script
            marchingCubes = chunk.AddComponent<ComputeMarchingCubes>();
            marchingCubes.OnMeshGenerated += HandleMeshReady;
            marchingCubes.meshFilter = meshFilter;
            marchingCubes.meshCollider = meshCollider;
            marchingCubes.chunkPos = chunkPos;
            marchingCubes.bounds = bounds;
            marchingCubes.assetSpawner = assetSpawner;
            marchingCubes.marchingCubesComputeShader = marchingCubesComputeShader;
            marchingCubes.terrainDensityComputeShader = terrainDensityComputeShader;
            marchingCubes.terrainNoiseComputeShader = terrainNoiseComputeShader;
            marchingCubes.terraformComputeShader = terraformComputeShader;
            marchingCubes.terrainDensityData = terrainDensityData;
            marchingCubes.initialLoadComplete = initialLoadComplete;
            marchingCubes.noiseGeneratorTextureArray = noiseGeneratorTextureArray;
            // Set up water generator
            if (terrainDensityData.waterLevel > chunkPos.y && terrainDensityData.waterLevel < Mathf.RoundToInt(chunkPos.y + terrainDensityData.width) && terrainDensityData.water)
            {
                waterPlaneGenerator = new GameObject("Water");
                waterPlaneGenerator.transform.SetParent(chunk.transform);
                MeshFilter waterGenMeshFilter = waterPlaneGenerator.AddComponent<MeshFilter>();
                MeshRenderer waterMat = waterPlaneGenerator.AddComponent<MeshRenderer>();
                waterMat.sharedMaterial = waterMaterial;
                waterGen = waterPlaneGenerator.AddComponent<WaterPlaneGenerator>();
                waterGen.meshFilter = waterGenMeshFilter;
                waterGen.meshRenderer = waterMat;
                waterGen.terrainDensityData = terrainDensityData;
                waterGen.chunkPos = chunkPos;
                waterGen.marchingCubes = marchingCubes;
                marchingCubes.waterGen = waterGen;
            }    
            if( terrainDensityData.waterLevel > bounds.min.y)
            {
                isWater = true;
            }
            chunk.transform.SetParent(parent);
        }
        /// <summary>
        /// Update the visibility of the chunk
        /// </summary>
        /// <param name="maxViewDst">The maximum view distance of the player</param>
        /// <param name="chunkSize">The chunk size</param>
        public void UpdateChunk(float maxViewDst)
        {
            float viewerDstFromBound = bounds.SqrDistance(Instance.viewerPos);
            bool visible = viewerDstFromBound <= (maxViewDst * maxViewDst);
            this.visible = visible;
            if (visible)
            {
                if (Instance.chunksToHide.Contains(this))
                {
                    Instance.chunksToHide.Remove(this);
                }
                else
                {
                    Instance.chunksToShow.Add(this);
                }
            }
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
            if (Instance.terrainDensityData.waterLevel > chunkPos.y && Instance.terrainDensityData.waterLevel < Mathf.RoundToInt(chunkPos.y + Instance.terrainDensityData.width) && Instance.terrainDensityData.water)
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
            return visible;
        }

        private void HandleMeshReady(Mesh mesh)
        {
            GlobalNavMeshUpdater.Instance.AddChunkForNavMeshUpdate(this);
        }

        private void OnDestroy()
        {
            if (marchingCubes != null)
                marchingCubes.OnMeshGenerated -= HandleMeshReady;
        }
    }
}




