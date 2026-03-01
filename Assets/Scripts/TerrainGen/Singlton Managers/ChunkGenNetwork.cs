using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TMPro;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ChunkGenNetwork : MonoBehaviour
{
    [HideInInspector]
    public static ChunkGenNetwork Instance;
    [HideInInspector]
    public Transform mainCameraTransform;

    [Header ("========== Fog Settings ==========")]
    [Space(5)]
    public Material fogMat;
    public float fogDensity;
    public float fogOffset;
    public Color upperFogColor;
    public Color lowerFogColor;
    public Color darkFogColor;

    [Space(10)]
    [Header("========== Render Data ==========")]
    [Space(5)]
    public UniversalRendererData rendererData;
    [HideInInspector]
    public FogRenderPassFeature fogRenderPassFeature;
    public UniversalRenderPipelineAsset mainUrpAsset;
    public UniversalRenderPipelineAsset underwaterUrpAsset;

    [Space(10)]
    [Header("========== Objective UI ==========")]
    [Space(5)]
    // Objective Text Stuff
    public GameObject objectiveCanvas;
    public GameObject hudCanvas;

    [Space(10)]
    [Header("========== Chat and Lobby ==========")]
    [Space(5)]
    // Chat & Lobby
    public GameObject chatContainer;
    public GameObject lobbyContainer;

    [Space(10)]
    [Header("========== Viewer Settings ==========")]
    [Space(5)]
    // Viewer Settings
    public Transform viewer;
    [HideInInspector]
    public Vector3 viewerPos;
    public float maxViewDst;
    public int maxWorldYChunks;
    public float updateDistanceThreshold;
    float updateDistanceThresholdSqr;
    public Transform chunkParent;
    private Vector3 lastUpdateViewerPos;
    [HideInInspector]
    public int chunkSize;
    [HideInInspector]
    public int chunksVisible;

    [Space(10)]
    [Header("========== Map Settings ==========")]
    [Space(5)]
    public bool useFixedMapSize;
    public int mapSize;
    int maxChunkDst;
    public int resolution;

    [Space(10)]
    [Header("========== Scriptable Object Settings ==========")]
    [Space(5)]
    // Scriptable Object References
    public GenerationConfiguration generationConfiguration;
    public TerrainDensityData terrainDensityData;
    public AssetSpawnData assetSpawnData;
    public TerrainTextureData terrainTextureData;

    [Space(10)]
    [Header("========== Terrain and Water Data ==========")]
    [Space(5)]
    // Material References
    public Material terrainMaterial;
    public Material waterMaterial;
    [HideInInspector]
    public Mesh waterMesh;

    [Space(10)]
    [Header("========== Foliage Settings ==========")]
    [Space(5)]
    // Foliage Stuff
    public Vector2 globalWindDirection;
    public ComputeShader grassPositionComputeShader;
    public GrassProfile landGrass;
    public GrassProfile seaGrass;
    public Material bushMaterial;
    public Material treeTopMaterial;
    public ParticleSystem leafParticleSystem;

    // Texture Arrays
    [HideInInspector]
    public Texture2DArray textureArray;
    [HideInInspector]
    public float[] useHeights;
    [HideInInspector]
    public float[] heightStarts;
    [HideInInspector]
    public float[] heightEnds;
    [HideInInspector]
    public float[] useSlopes;
    [HideInInspector]
    public float[] slopeStarts;
    [HideInInspector]
    public float[] slopeEnds;

    [Space(10)]
    [Header("========== Texture Settings UI ==========")]
    [Space(5)]
    // Texture Window Stuff
    public GameObject textureWindow;
    public GameObject textureSettingsTab;

    [Space(10)]
    [Header("========== Asset Settings UI ==========")]
    [Space(5)]
    // Asset Window Stuff
    public GameObject assetWindow;
    public GameObject assetSettingsTab;

    [Space(10)]
    [Header("========== Preset Settings UI ==========")]
    [Space(5)]
    // Preset Dropdown
    public TMP_Dropdown presetDropdown;
    // Chunk Variables
    float maxViewDstSqr;
    Vector3 chunkVec;
    [HideInInspector]
    public Vector3 halfChunkVec;
    [HideInInspector]
    public float halfChunkSize;

    // Chunk Data
    public Dictionary<long, TerrainChunk> chunkDictionary = new();
    [HideInInspector]
    public List<long> chunksVisibleLastUpdate = new();
    List<long> chunksToDestroy = new();
    public PriorityQueue<Vector3Int> chunkLoadQueue = new();
    public HashSet<long> chunkLoadSet = new();
    public Queue<ChunkVisibility> chunkVisibilityQueue = new();
    public HashSet<long> chunksToHide = new();
    public HashSet<long> chunksToShow = new();
    [HideInInspector]
    public bool isLoadingChunkVisibility = false;
    [HideInInspector]
    public bool isLoadingChunks = false;
    [HideInInspector]
    public bool initialLoadComplete = false;

    [Space(10)]
    [Header("========== Lighting Settings ==========")]
    [Space(5)]
    // Lighting Blocker
    public GameObject lightingBlocker;
    public Light mainLight;
    private MeshRenderer lightingBlockerRenderer;

    // Readback Queue
    [HideInInspector]
    public List<TerrainJobObject> terrainDensityJobList;
    public List<TerrainJobObject> terrainDensityJobRemovalList;
    public List<TerrainJobObject> terrainPolygonizationJobList;
    public List<TerrainJobObject> terrainPolygonizationJobRemovalList;
    // Asset Instantiation Queue
    public Queue<AssetInstantiation> pendingAssetInstantiations = new();


    public Queue<TerrainChunk> marchingCubesJobQueue = new();
    // Reused Marching Cubes Native Array
    public NativeArray<float3> vertexOffsetTable;
    public NativeArray<int> edgeIndexTable;
    public NativeArray<int> triangleTable;
    public NativeArray<int> staticMaxSizeVertexIndexArray;

    public FastNoise noiseTest;
    public int seed;
    /* ============================================= HELPER STRUCTS START ============================================= */
    public struct ChunkVisibility
    {
        public TerrainChunk chunk;
        public bool visibility;
        public ChunkVisibility(TerrainChunk chunk, bool visibility)
        {
            this.chunk = chunk;
            this.visibility = visibility;
        }
    }
    public struct AssetInstantiation
    {
        public TerrainChunk terrainChunk;
        public int i;
        public int j;
        public uint seed;
        public AssetInstantiation(TerrainChunk terrainChunk, int i, int j, uint seed)
        {
            this.terrainChunk = terrainChunk;
            this.i = i;
            this.j = j;
            this.seed = seed;
        }
    }
    public class ReadbackRequest
    {
        public Vector3Int chunkCoord;
        public ComputeBuffer buffer;
        public Action<AsyncGPUReadbackRequest> readbackRequest;

        public ReadbackRequest(Vector3Int chunkCoord, ComputeBuffer buffer, Action<AsyncGPUReadbackRequest> readbackRequest)
        {
            this.chunkCoord = chunkCoord;
            this.buffer = buffer;
            this.readbackRequest = readbackRequest;
        }
    }
    public class TerrainJobObject
    {
        public TerrainChunk owner;
        public JobHandle jobHandle;
        public bool terraforming;
        public TerrainJobObject(TerrainChunk owner, JobHandle jobHandle, bool terraforming)
        {
            this.owner = owner;
            this.jobHandle = jobHandle;
            this.terraforming = terraforming;
        }
    }
    /* ============================================= HELPER STRUCTS END ============================================= */
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
        
        mainCameraTransform = Camera.main.transform;
        chunkParent = GameObject.Find("ChunkParent").transform;
        updateDistanceThresholdSqr = updateDistanceThreshold * updateDistanceThreshold;
        maxViewDstSqr = maxViewDst * maxViewDst;
        maxChunkDst = (mapSize - 1) / 2;
        chunkVec = Vector3.one * chunkSize;
        halfChunkVec = new Vector3(0.5f, 0.5f, 0.5f) * chunkSize;
        halfChunkSize = chunkSize * 0.5f;

        lightingBlockerRenderer = lightingBlocker.GetComponent<MeshRenderer>();
        lightingBlockerRenderer.enabled = false;
        mainLight.intensity = 12f;

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
        WaterMaterialSetup.Instance.SetupWaves(waterMaterial);
        SetFogActive(false);
        viewerPos = viewer.position;
        lastUpdateViewerPos = viewerPos;

        landGrass.grassMaterial.SetVector("_WindDir", globalWindDirection);
        bushMaterial.SetVector("_WindDir", globalWindDirection);
        treeTopMaterial.SetVector("_WindDir", globalWindDirection);
        // var forceModule = leafParticleSystem.forceOverLifetime;
        // forceModule.x = globalWindDirection.x;
        // forceModule.z = globalWindDirection.y;

        noiseTest = FastNoise.FromEncodedNodeTree("HQkQ@BFkQY@BPwkWAgQICtcjPAQKJAjD9Sg/CS4AAQ@BkNAAc@BI@AgQAkH@BFkQQPQpXvxhmZmY/BAOamRk/CwAAgD8cAwAAcEIEAhYCHAkuAAE@BJJQkL@BJUQQzczMPRgAACDAIAM@B4Ag@BokCM3MzD4JCQ@AD5CEB+F6z4YzcxMPwwSJAjNzMw+CQk@BwQggB@BEM3MzL4Y@BPyQC/wsAC+xROD4EChcJDQkI@CEEEA7geBT8LexQuPwQDj8J1PBQ=");
        // NativeArray<float> density = new NativeArray<float>(
        //     33 * 33 * 33,
        //     Allocator.Persistent
        // );
        // Stopwatch sw;
        // sw = Stopwatch.StartNew();
        // for (int i = 0; i < 1000; i++)
        // {
        //     // sw = Stopwatch.StartNew();
            // noiseTest.GenUniformGrid3D(density, 0, 0, 0, 33, 33, 33, 0.01f, 0.01f, 0.01f, 1337);
        //     // sw.Stop();
        //     // UnityEngine.Debug.Log(sw.Elapsed.TotalMilliseconds);
        // }
        // sw.Stop();
        // UnityEngine.Debug.Log(sw.Elapsed.TotalMilliseconds);
        // UnityEngine.Debug.Log(density[32]);
        // density.Dispose();

        InitializeGenerator();
    }
    void OnDisable()
    {
        if(staticMaxSizeVertexIndexArray != null && staticMaxSizeVertexIndexArray.IsCreated)
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

        if(staticMaxSizeVertexIndexArray != null && staticMaxSizeVertexIndexArray.IsCreated)
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
    public void InitializeGenerator()
    {
        if (terrainTextureData != null)
            terrainTextureData.RestoreToOriginalState();
        if (assetSpawnData != null)
            assetSpawnData.RestoreToOriginalState();

        terrainDensityData = generationConfiguration.terrainConfigs[presetDropdown.value].terrainDensityData;
        terrainTextureData = generationConfiguration.terrainConfigs[presetDropdown.value].terrainTextureData;
        assetSpawnData = generationConfiguration.terrainConfigs[presetDropdown.value].assetSpawnData;

        // Unity.Mathematics.Random rng = new((uint)UnityEngine.Random.Range(0, 100000));
        // int rand = rng.NextInt(0, generationConfiguration.terrainConfigs.Count);
        // terrainDensityData = generationConfiguration.terrainConfigs[rand].terrainDensityData;
        // terrainTextureData = generationConfiguration.terrainConfigs[rand].terrainTextureData;
        // assetSpawnData = generationConfiguration.terrainConfigs[rand].assetSpawnData;

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
        terrainDensityJobList = new();
        terrainDensityJobRemovalList = new();
        terrainPolygonizationJobList = new();
        terrainPolygonizationJobRemovalList = new();
        pendingAssetInstantiations = new();

        DestroyChunks();
        assetSpawnData.ResetSpawnPoints();

        TextureSetup();
        AssetSetup();
        
        // Set seeds
        seed = UnityEngine.Random.Range(0, 100000);
        CreateVertexIndexArray();
        waterMesh = WaterPlaneGenerator.PlaneGeneratorJobHandler(terrainDensityData.width, terrainDensityData.waterLevel % terrainDensityData.width);
        UpdateVisibleChunks();
    }
    /// <summary>
    /// Destroy all chunks
    /// </summary>
    public void DestroyChunks()
    {
        foreach (Transform chunk in chunkParent)
        {
            Destroy(chunk.gameObject);
        }
    }
    /// <summary>
    /// Create max size native array of vertex indices
    /// </summary>
    public void CreateVertexIndexArray()
    {
        int size =  3 * (terrainDensityData.width + 1) * (terrainDensityData.width + 1) * (terrainDensityData.width + 1);
        staticMaxSizeVertexIndexArray = new NativeArray<int>(size, Allocator.Persistent);
        for (int i = 0; i < size; i++)
        {
            staticMaxSizeVertexIndexArray[i] = i;
        }
    }
    /// <summary>
    /// Use bitmasking to pack an integer xyz coordinate into a long
    /// </summary>
    /// <param name="x">x coordinate</param>
    /// <param name="y">y coordinate</param>
    /// <param name="z">z coordinate</param>
    /// <returns>The packed long version of the xyz integer coordinate</returns>
    public long PackChunkCoord(int x, int y, int z)
    {
        return ((long)(x & 0x1FFFFF) << 42) | ((long)(y & 0x1FFFFF) << 21) | ((long)(z & 0x1FFFFF));
    }
    public float CalculateDstFromBound(Vector3Int chunkCoord, Vector3 fromPos)
    {
        float x = chunkCoord.x * chunkSize;
        float y = chunkCoord.y * chunkSize;
        float z = chunkCoord.z * chunkSize;

        float vx = fromPos.x - x;
        float vy = fromPos.y - y;
        float vz = fromPos.z - z;

        float dx = (vx < 0f ? -vx : vx) - chunkSize;
        dx = dx > 0f ? dx : 0f;
        float dy = (vy < 0f ? -vy : vy) - chunkSize;
        dy = dy > 0f ? dy : 0f;
        float dz = (vz < 0f ? -vz : vz) - chunkSize;
        dz = dz > 0f ? dz : 0f;

        float dstFromBound = dx*dx + dy*dy + dz*dz;

        return dstFromBound;
    }
    /// <summary>
    /// Checks if a given chunk coordinate was visited based on the current chunk coordinate
    /// </summary>
    /// <param name="currentX"></param>
    /// <param name="currentY"></param>
    /// <param name="currentZ"></param>
    /// <param name="centerX"></param>
    /// <param name="centerY"></param>
    /// <param name="centerZ"></param>
    /// <returns></returns>
    public bool WasVisited(int currentX, int currentY, int currentZ, int centerX, int centerY, int centerZ)
    {
        return
            currentX >= (centerX - chunksVisible) && currentX <= (centerX + chunksVisible) &&
            currentY >= (centerY - chunksVisible) && currentY <= (centerY + chunksVisible) &&
            currentZ >= (centerZ - chunksVisible) && currentZ <= (centerZ + chunksVisible);
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

    void Update()
    {
        // Position updates
        viewerPos = viewer.position;
        lightingBlocker.transform.position = new Vector3(viewerPos.x, viewerPos.y + 100f, viewerPos.z);
        // Darker fog at lower world heights
        float depthFactor = Mathf.Clamp01(-viewerPos.y * 0.01f);
        Color currentFog = Color.Lerp(lowerFogColor, darkFogColor, depthFactor);
        fogMat.SetColor("_lowerFogColor", currentFog);
        waterMaterial.SetColor("_fogColor", currentFog);

        // Update chunks
        if ((viewerPos - lastUpdateViewerPos).sqrMagnitude > updateDistanceThresholdSqr && initialLoadComplete)
        {
            UpdateVisibleChunks();
            lastUpdateViewerPos = viewerPos;
        }
        float start = Time.realtimeSinceStartup;
        while (terrainDensityJobList.Count > 0 && Time.realtimeSinceStartup - start < 0.003f)
        {
            terrainDensityJobRemovalList.Clear();
            foreach (TerrainJobObject job in terrainDensityJobList)
            {
                if (job.jobHandle.IsCompleted)
                {
                    job.jobHandle.Complete();
                    terrainDensityJobRemovalList.Add(job);
                    job.owner.marchingCubes.MarchingCubesJobHandler(job.terraforming);
                }
            }
            foreach(TerrainJobObject job in terrainDensityJobRemovalList)
            {
                terrainDensityJobList.Remove(job);
            }
        }
        start = Time.realtimeSinceStartup;
        while (terrainPolygonizationJobList.Count > 0 && Time.realtimeSinceStartup - start < 0.003f)
        {
            terrainPolygonizationJobRemovalList.Clear();
            foreach (TerrainJobObject job in terrainPolygonizationJobList)
            {
                if (job.jobHandle.IsCompleted)
                {
                    job.jobHandle.Complete();
                    terrainPolygonizationJobRemovalList.Add(job);
                    job.owner.marchingCubes.SetMeshValuesPerformant(job.terraforming);
                }
            }
            foreach(TerrainJobObject job in terrainPolygonizationJobRemovalList)
            {
                terrainPolygonizationJobList.Remove(job);
            }
        }
        start = Time.realtimeSinceStartup;
        while (chunkVisibilityQueue.Count > 0 && Time.realtimeSinceStartup - start < 0.003f) // 2ms
        {
            ChunkVisibility chunk = chunkVisibilityQueue.Dequeue();
            chunk.chunk.SetVisible(chunk.visibility);
        }
        start = Time.realtimeSinceStartup;
        while (pendingAssetInstantiations.Count > 0 && Time.realtimeSinceStartup - start < 0.002f) // 2ms
        {
            AssetInstantiation assetInstantiation = pendingAssetInstantiations.Dequeue();
            assetInstantiation.terrainChunk.assetSpawner.AssetInstantiation(assetInstantiation.i, assetInstantiation.j, assetInstantiation.seed);
        }
    }
    /// <summary>
    /// Update all the visible chunks loading in new ones and unloading old ones that are no longer visible
    /// </summary>
    public void UpdateVisibleChunks()
    {
        chunksToHide.Clear();
        chunksToShow.Clear();
        chunksToDestroy.Clear();
        foreach (long chunk in chunksVisibleLastUpdate)
        {
            chunksToHide.Add(chunk);
        }
        chunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.FloorToInt(viewerPos.x / chunkSize);
        int currentChunkCoordY = Mathf.FloorToInt(viewerPos.y / chunkSize);
        int currentChunkCoordZ = Mathf.FloorToInt(viewerPos.z / chunkSize);

        if (viewerPos.y <= -40 && !lightingBlockerRenderer.enabled)
        {
            lightingBlockerRenderer.enabled = true;
        }
        else if (viewerPos.y >= -40 && lightingBlockerRenderer.enabled)
        {
            lightingBlockerRenderer.enabled = false;
        }

        Vector3 cameraForwardVector = mainCameraTransform.forward;
        Vector3 movementVector = Vector3.Normalize(viewerPos - lastUpdateViewerPos); 
        Vector3 movementDir = movementVector.sqrMagnitude > 0.01f ? movementVector : cameraForwardVector; 

        for (int xOffset = -chunksVisible; xOffset <= chunksVisible; xOffset++)
        {
            int currentX = currentChunkCoordX + xOffset;

            for (int yOffset = -chunksVisible; yOffset <= chunksVisible; yOffset++)
            {
                int currentY = currentChunkCoordY + yOffset;

                if ((currentY > 0 && currentChunkCoordY > maxWorldYChunks) || (currentY < 0 && currentChunkCoordY < -maxWorldYChunks))
                    continue;

                for (int zOffset = -chunksVisible; zOffset <= chunksVisible; zOffset++)
                {
                    int currentZ = currentChunkCoordZ + zOffset;
                    Vector3Int viewedChunkCoord = new Vector3Int(currentX, currentY, currentZ);
                    long chunkCoordId = PackChunkCoord(currentX, currentY, currentZ);

                    if (useFixedMapSize && (math.abs(currentX) > maxChunkDst || math.abs(currentZ) > maxChunkDst))
                        continue;

                    Bounds bounds = new Bounds((viewedChunkCoord * chunkSize) + (new Vector3(0.5f, 0.5f, 0.5f) * chunkSize), chunkVec);
                    float viewerDstFromBound = bounds.SqrDistance(viewerPos);
                    // Vector3 chunkCenter = (viewedChunkCoord * chunkSize) + halfChunkVec;
                    // float viewerDstFromBound = (chunkCenter - viewerPos).sqrMagnitude - (halfChunkSize * halfChunkSize * 2);
                    bool isInView = viewerDstFromBound <= maxViewDstSqr;

                    if (!isInView)
                        continue;

                    if (!initialLoadComplete)
                    {
                        TerrainChunk chunk = new TerrainChunk(chunkCoordId, viewedChunkCoord, chunkSize, chunkParent, 
                                                              terrainDensityData, assetSpawnData, 
                                                              terrainMaterial, waterMaterial, initialLoadComplete);
                        chunkDictionary.Add(chunkCoordId, chunk);
                        chunk.UpdateChunk(isInView);
                        if (chunk.visible)
                            chunksVisibleLastUpdate.Add(chunkCoordId);
                        continue;
                    }

                    if (chunkDictionary.TryGetValue(chunkCoordId, out TerrainChunk dictChunk))
                    {
                        dictChunk.UpdateChunk(isInView);
                        if (dictChunk.visible)
                            chunksVisibleLastUpdate.Add(chunkCoordId);
                    }
                    else
                    {
                        Vector3 chunkCenter = (viewedChunkCoord * chunkSize) + halfChunkVec;
                        Vector3 toChunk = Vector3.Normalize(chunkCenter - viewerPos); 
                        float angle = Vector3.Angle(movementDir, toChunk); 
                        if (angle > 60f) continue;

                        if (chunkLoadSet.Add(chunkCoordId))
                        {
                            chunkLoadQueue.Enqueue(viewedChunkCoord, viewerDstFromBound);
                        }
                    }
                }
            }
        }

        if (!initialLoadComplete)
            initialLoadComplete = true;
            
        // Check if coroutines need to run
        if (!isLoadingChunks)
            StartCoroutine(LoadChunksOverTime());

        foreach (long coord in chunksToHide)
        {
            TerrainChunk chunk = chunkDictionary[coord];
            if (!chunk.marchingCubes.edited)
            {
                chunksToDestroy.Add(chunk.chunkId);
            }
        }

        foreach (var coord in chunksToDestroy)
        {
            TerrainChunk chunk = chunkDictionary[coord];
            Destroy(chunk.chunk);
            chunkDictionary.Remove(coord);
            assetSpawnData.assets.Remove(chunk.chunkPos);
        }

        foreach (long coord in chunksToHide)
        {
            TerrainChunk chunkToHide;
            if (!chunkDictionary.TryGetValue(coord, out chunkToHide))
                continue;
            chunkVisibilityQueue.Enqueue(new ChunkVisibility(chunkToHide, false));
        }
        foreach (long coord in chunksToShow)
        {
            TerrainChunk chunkToShow;
            if (!chunkDictionary.TryGetValue(coord, out chunkToShow))
                continue;
            chunkVisibilityQueue.Enqueue(new ChunkVisibility(chunkToShow, true));
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
            long packedCoord = PackChunkCoord(coord.x, coord.y, coord.z);
            chunkLoadSet.Remove(packedCoord);
            
            // float viewerDstFromBound = CalculateDstFromBound(coord, viewerPos);
            Bounds bounds = new Bounds(coord * chunkSize + (new Vector3(0.5f, 0.5f, 0.5f) * chunkSize), Vector3.one * chunkSize);
            float viewerDstFromBound = bounds.SqrDistance(viewerPos);
            // Vector3 chunkCenter = (coord * chunkSize) + halfChunkVec;
            // float viewerDstFromBound = (chunkCenter - viewerPos).sqrMagnitude - (halfChunkSize * halfChunkSize * 2);
            bool isInView = viewerDstFromBound <= maxViewDstSqr;

            if (!chunkDictionary.TryGetValue(packedCoord, out TerrainChunk dictChunk) && isInView)
            {
                var chunk = new TerrainChunk(packedCoord, coord, chunkSize, chunkParent, terrainDensityData, assetSpawnData, 
                                            terrainMaterial, waterMaterial, initialLoadComplete);
                chunkDictionary.Add(packedCoord, chunk);
                chunk.UpdateChunk(isInView);
                if (chunk.visible)
                    chunksVisibleLastUpdate.Add(packedCoord);
                chunkBatchCounter++;
            }

            if (chunkBatchCounter % 8 == 0)
            {
                yield return null;
            }
        }

        isLoadingChunks = false;
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
            long packedCoord = PackChunkCoord(neighborCoord.x, neighborCoord.y, neighborCoord.z);
            chunkDictionary.TryGetValue(packedCoord, out chunkAndNeighbors[i]);
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
            textureArray = new(textureWidth, textureHeight, textureCount, textureFormat, true, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
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
        public MeshRenderer waterMeshRenderer;
        public long chunkId;
        public Vector3Int chunkCoord;
        public Vector3Int chunkPos;
        public MeshCollider meshCollider;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public bool visible = false;
        public TerrainChunk(long chunkId, Vector3Int chunkCoord, int chunkSize, Transform parent, TerrainDensityData terrainDensityData, 
                            AssetSpawnData assetSpawnData, Material terrainMaterial, 
                            Material waterMaterial, bool initialLoadComplete)
        {
            this.chunkId = chunkId;
            this.chunkCoord = chunkCoord;
            chunkPos = chunkCoord * chunkSize;
            chunk = new GameObject("Chunk" + chunkPos);
            chunk.transform.SetParent(parent);
            chunk.layer = 3;
            // Set up basic chunk components
            meshCollider = chunk.AddComponent<MeshCollider>();
            meshFilter = chunk.AddComponent<MeshFilter>();
            meshRenderer = chunk.AddComponent<MeshRenderer>();
            // Chunk texture
            meshRenderer.sharedMaterial = terrainMaterial;
            // Set up water generator
            if (terrainDensityData.waterLevel > chunkPos.y && terrainDensityData.waterLevel < Mathf.RoundToInt(chunkPos.y + terrainDensityData.width) && terrainDensityData.water)
            {
                waterPlaneGenerator = new GameObject("Water");
                waterPlaneGenerator.transform.SetParent(chunk.transform);
                MeshFilter waterGenMeshFilter = waterPlaneGenerator.AddComponent<MeshFilter>();
                waterMeshRenderer = waterPlaneGenerator.AddComponent<MeshRenderer>();
                waterMeshRenderer.sharedMaterial = waterMaterial;
                waterGenMeshFilter.mesh = Instance.waterMesh;
                waterPlaneGenerator.transform.position = chunkPos;
            }
            // Set up the chunk's AssetSpawn script
            assetSpawner = chunk.AddComponent<AssetSpawner>();
            assetSpawner.chunkPos = chunkPos;
            assetSpawner.terrainDensityData = terrainDensityData;
            assetSpawner.assetSpawnData = assetSpawnData;
            // Set up the chunk's ComputeMarchingCubes script
            marchingCubes = chunk.AddComponent<ComputeMarchingCubes>();
            marchingCubes.InitializeChunk(
                this, 
                meshFilter, 
                meshCollider, 
                chunkCoord, 
                chunkPos, 
                assetSpawner,
                terrainDensityData, 
                initialLoadComplete
            );
            marchingCubes.GenerateChunk();
        }
        /// <summary>
        /// Update the visibility of the chunk
        /// </summary>
        /// <param name="maxViewDst">The maximum view distance of the player</param>
        /// <param name="chunkSize">The chunk size</param>
        public void UpdateChunk(bool isInView)
        {
            visible = isInView;
            if (visible)
            {
                if (!Instance.chunksToHide.Remove(chunkId))
                    Instance.chunksToShow.Add(chunkId);
            }
        }
        /// <summary>
        /// Set the visibility of the chunk
        /// </summary>
        /// <param name="visible">Whether the chunk is visible</param>
        public void SetVisible(bool visible)
        {
            // Terrain
            if (meshRenderer != null && meshRenderer.enabled != visible)
            {
                meshRenderer.enabled = visible;
                if (meshCollider != null && meshCollider.enabled != visible)
                {
                    meshCollider.enabled = visible;
                }
            }
            // Water
            if (Instance.terrainDensityData.waterLevel > chunkPos.y && Instance.terrainDensityData.waterLevel < Mathf.RoundToInt(chunkPos.y + Instance.terrainDensityData.width) && Instance.terrainDensityData.water)
            {
                if (waterMeshRenderer != null && waterMeshRenderer.enabled != visible)
                {
                    waterMeshRenderer.enabled = visible;
                }
            }
            // Assets
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
    }
}




