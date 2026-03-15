using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TMPro;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Multiplayer.Center.Common;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ChunkGenNetwork : MonoBehaviour
{
    [HideInInspector]
    public static ChunkGenNetwork Instance;
    [HideInInspector]
    public Transform mainCameraTransform;
    public TerrainChunkPoolManager terrainChunkPool;
    public AssetObjectPoolManager assetPool;

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
    public Volume globalVolume;
    [HideInInspector]
    public DepthOfField depthOfField;
    [HideInInspector]
    public LensDistortion lensDistortion;
    [HideInInspector]
    public FilmGrain filmGrain;

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
    [HideInInspector]
    public TerrainDensityData terrainDensityData;
    [HideInInspector]
    public AssetSpawnData assetSpawnData;
    [HideInInspector]
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
    public ComputeShader grassUpdateComputeShader;
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

    // Chunk Data
    public Dictionary<long, TerrainChunk> chunkDictionary = new();
    [HideInInspector]
    public List<TerrainChunk> chunksVisibleLastUpdate = new();
    public Queue<Vector3Int> chunkLoadQueue = new();
    [HideInInspector]
    public bool isLoadingChunks = false;
    [HideInInspector]
    public bool initialLoadComplete = false;
    public Queue<TerrainChunk> chunkReturnQueue = new();
    [HideInInspector]
    public bool isReturningChunks = false;
    public HashSet<long> chunkLoadSet = new();
    public Queue<ChunkVisibility> chunkVisibilityQueue = new();

    [Space(10)]
    [Header("========== Lighting Settings ==========")]
    [Space(5)]
    // Lighting Blocker
    public GameObject lightingBlocker;
    public Light mainLight;
    private MeshRenderer lightingBlockerRenderer;

    [HideInInspector]
    public Queue<TerrainJobObject> terrainDensityJobQueue;
    [HideInInspector]
    public Queue<TerrainJobObject> terrainPolygonizationJobQueue;
    [HideInInspector]
    public Queue<VertexSortJob> vertexSortJobQueue;
    // Asset Spawn Point Creation Queue
    public Queue<TerrainJobObject> meshGenQueue = new();
    public Queue<MeshBake> collisionMeshBakeQueue = new();
    public Queue<GrassObject> grassProcessQueue = new();
    public Queue<AssetSpawnPointCreation> spawningPointCreationQueue = new();
    // Asset Instantiation Queue
    public Queue<AssetInstantiation> pendingAssetInstantiations = new();
    // Reused Marching Cubes Native Array
    public NativeArray<float3> vertexOffsetTable;
    public NativeArray<int> edgeIndexTable;
    public NativeArray<int> triangleTable;
    public NativeArray<int> staticMaxSizeVertexIndexArray;

    public FastNoise noiseGenerator;
    public int seed;
    Vector3Int[] chunkNeighborOffsets = new Vector3Int[]
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
    /* ============================================= HELPER STRUCTS START ============================================= */
    public struct ChunkVisibility
    {
        public TerrainChunk chunk;
        public bool visibility;
        public uint expectedID;
        public ChunkVisibility(TerrainChunk chunk, bool visibility, uint expectedID)
        {
            this.chunk = chunk;
            this.visibility = visibility;
            this.expectedID = expectedID;
        }
    }
    public struct TerrainJobObject
    {
        public TerrainChunk owner;
        public JobHandle jobHandle;
        public bool terraforming;
        public uint expectedID;
        public TerrainJobObject(TerrainChunk owner, JobHandle jobHandle, bool terraforming, uint expectedID)
        {
            this.owner = owner;
            this.jobHandle = jobHandle;
            this.terraforming = terraforming;
            this.expectedID = expectedID;
        }
    }
    public struct MeshBake
    {
        public TerrainChunk owner;
        public Mesh mesh;
        public MeshCollider meshCollider;
        public uint expectedID;
        public MeshBake(TerrainChunk owner, Mesh mesh, MeshCollider meshCollider, uint expectedID)
        {
            this.owner = owner;
            this.mesh = mesh;
            this.meshCollider = meshCollider;
            this.expectedID = expectedID;
        }
    }
    public struct GrassObject
    {
        public TerrainChunk owner;
        public int triangleCount;
        public uint expectedID;
        public GrassObject(TerrainChunk owner, int triangleCount, uint expectedID)
        {
            this.owner = owner;
            this.triangleCount = triangleCount;
            this.expectedID = expectedID;
        }
    }
    public struct VertexSortJob
    {
        public JobHandle jobHandle;
        public AssetSpawner assetSpawner;
        public uint expectedID;
        public VertexSortJob(JobHandle jobHandle, AssetSpawner assetSpawner, uint expectedID)
        {
            this.jobHandle = jobHandle;
            this.assetSpawner = assetSpawner;
            this.expectedID = expectedID;
        }
    }
    public struct AssetSpawnPointCreation
    {
        public TerrainChunk owner;
        public uint expectedID;
        public AssetSpawnPointCreation(TerrainChunk owner, uint expectedID)
        {
            this.owner = owner;
            this.expectedID = expectedID;
        }
    }
    public struct AssetInstantiation
    {
        public TerrainChunk owner;
        public int i;
        public int j;
        public uint seed;
        public uint expectedID;
        public AssetInstantiation(TerrainChunk owner, int i, int j, uint seed, uint expectedID)
        {
            this.owner = owner;
            this.i = i;
            this.j = j;
            this.seed = seed;
            this.expectedID = expectedID;
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
        updateDistanceThresholdSqr = updateDistanceThreshold * updateDistanceThreshold;
        maxChunkDst = (mapSize - 1) / 2;

        lightingBlockerRenderer = lightingBlocker.GetComponent<MeshRenderer>();
        lightingBlockerRenderer.enabled = false;
        mainLight.intensity = 12f;

        vertexOffsetTable = new(MarchingCubesTables.vertexOffsetTable, Allocator.Persistent);
        edgeIndexTable = new(MarchingCubesTables.edgeIndexTable, Allocator.Persistent);
        triangleTable = new(MarchingCubesTables.triangleTable, Allocator.Persistent);

        // Volume Profile Stuff
        VolumeProfile profile = globalVolume.profile;
        profile.TryGet(out depthOfField);
        profile.TryGet(out lensDistortion);
        profile.TryGet(out filmGrain);

        fogMat.SetFloat("_fogOffset", fogOffset);
        fogMat.SetFloat("_fogDensity", fogDensity);
        fogMat.SetColor("_upperFogColor", upperFogColor);
        fogMat.SetColor("_lowerFogColor", lowerFogColor);
        // Fog Shader Inits
        waterMaterial.SetFloat("_fogOffset", fogOffset);
        waterMaterial.SetFloat("_fogDensity", fogDensity);
        waterMaterial.SetColor("_fogColor", lowerFogColor);
        waterMaterial.SetFloat("_fogActive", 0);
        WaterMaterialSetup.Instance.SetupWaves(waterMaterial);
        SetFogActive(false);
        viewerPos = viewer.position;
        lastUpdateViewerPos = viewerPos;

        foreach (GrassProfile.FoliageType foliageType in landGrass.foliageList)
        {
            foliageType.grassMaterial.SetVector("_WindDir", globalWindDirection);
        }
        bushMaterial.SetVector("_WindDir", globalWindDirection);
        treeTopMaterial.SetVector("_WindDir", globalWindDirection);

        // noiseGenerator = FastNoise.FromEncodedNodeTree("HQkQ@BFkQY@BPwkWAgQICtcjPAQKJAjD9Sg/CS4AAQ@BkNAAc@BI@AgQAkH@BFkQQPQpXvxhmZmY/BAOamRk/CwAAgD8cAwAAcEIEAhYCHAkuAAE@BJJQkL@BJUQQzczMPRgAACDAIAM@B4Ag@BokCM3MzD4JCQ@AD5CEB+F6z4YzcxMPwwSJAjNzMw+CQk@BwQggB@BEM3MzL4Y@BPyQC/wsAC+xROD4EChcJDQkI@CEEEA7geBT8LexQuPwQDj8J1PBQ=");
        noiseGenerator = FastNoise.FromEncodedNodeTree
        (
            generationConfiguration.terrainConfigs[presetDropdown.value].terrainDensityData.encodedNodeTreeString
        );

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
        ToggleUnderwaterEffects(false);
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

        chunkSize = terrainDensityData.chunkSize;
        chunksVisible = Mathf.RoundToInt(maxViewDst / chunkSize);

        // Chunk Variables
        chunkDictionary = new();
        chunksVisibleLastUpdate = new();
        chunkLoadQueue = new();
        chunkReturnQueue = new();
        chunkLoadSet = new();
        isLoadingChunks = false;
        initialLoadComplete = false;
        // Action Queues
        terrainDensityJobQueue = new();
        terrainPolygonizationJobQueue = new();
        meshGenQueue = new();
        collisionMeshBakeQueue = new();
        grassProcessQueue = new();
        vertexSortJobQueue = new();
        spawningPointCreationQueue = new();
        pendingAssetInstantiations = new();

        DestroyChunks();
        assetSpawnData.ResetSpawnPoints();

        CreateVertexIndexArray();
        TextureSetup();
        AssetSetup();
        
        // Set seeds
        seed = UnityEngine.Random.Range(0, 100000);
        waterMesh = WaterPlaneGenerator.PlaneGeneratorJobHandler(terrainDensityData.chunkSize, terrainDensityData.waterLevel % terrainDensityData.chunkSize);
        UpdateChunksInitial();
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
        int size =  3 * (terrainDensityData.chunkSize + 1) * (terrainDensityData.chunkSize + 1) * (terrainDensityData.chunkSize + 1);
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
    /// <summary>
    /// Toggle underwater effects
    /// </summary>
    /// <param name="enable">toggle bool</param>
    public void ToggleUnderwaterEffects(bool enable)
    {
        if (depthOfField != null)
            depthOfField.active = enable;
        if (lensDistortion != null)
            lensDistortion.active = enable;
        if (filmGrain != null)
            filmGrain.active = enable;
    }
    /// <summary>
    /// Process chunk load queue
    /// </summary>
    /// <param name="startTime">Start of allocated time frame</param>
    public void ProcessChunkLoads(float startTime, float timeBudget)
    {
        int counter = 0;
        while (chunkLoadQueue.Count > 0 && Time.realtimeSinceStartup - startTime < timeBudget)
        {
            Vector3Int coord = chunkLoadQueue.Dequeue();
            int currentChunkCoordX = Mathf.FloorToInt(viewerPos.x / chunkSize);
            int currentChunkCoordY = Mathf.FloorToInt(viewerPos.y / chunkSize);
            int currentChunkCoordZ = Mathf.FloorToInt(viewerPos.z / chunkSize);

            long packedCoord = PackChunkCoord(coord.x, coord.y, coord.z);

            bool isInView = Mathf.Abs(currentChunkCoordX - coord.x) <= chunksVisible &&
                            Mathf.Abs(currentChunkCoordY - coord.y) <= chunksVisible &&
                            Mathf.Abs(currentChunkCoordZ - coord.z) <= chunksVisible;

            if (!chunkDictionary.TryGetValue(packedCoord, out TerrainChunk dictChunk) && isInView)
            {
                Vector3Int chunkPos = coord * chunkSize;
                bool waterChunk = terrainDensityData.waterLevel >= chunkPos.y && terrainDensityData.waterLevel < chunkPos.y + terrainDensityData.chunkSize && Instance.terrainDensityData.water;
                TerrainChunk chunk = terrainChunkPool.GetChunk(packedCoord, coord, chunkPos, waterChunk);
                chunkDictionary.Add(packedCoord, chunk);
                chunksVisibleLastUpdate.Add(chunk);
            }
            chunkLoadSet.Remove(packedCoord);

            if (++counter >= 12) break;
        }
    }
    /// <summary>
    /// Process chunk return queue
    /// </summary>
    /// <param name="startTime">Start of allocated time frame</param>
    public void ProcessChunkReturns(float startTime, float timeBudget)
    {
        int counter = 0;
        while (chunkReturnQueue.Count > 0 && Time.realtimeSinceStartup - startTime < timeBudget)
        {
            int currentChunkCoordX = Mathf.FloorToInt(viewerPos.x / chunkSize);
            int currentChunkCoordY = Mathf.FloorToInt(viewerPos.y / chunkSize);
            int currentChunkCoordZ = Mathf.FloorToInt(viewerPos.z / chunkSize);
            TerrainChunk chunk = chunkReturnQueue.Dequeue();

            bool isInView = Mathf.Abs(currentChunkCoordX - chunk.chunkCoord.x) <= chunksVisible &&
                            Mathf.Abs(currentChunkCoordY - chunk.chunkCoord.y) <= chunksVisible &&
                            Mathf.Abs(currentChunkCoordZ - chunk.chunkCoord.z) <= chunksVisible;

            if (!isInView)
            {
                terrainChunkPool.ReturnChunk(chunk, chunk.waterChunk);
                chunkDictionary.Remove(chunk.packedCoord);
            }
            else
            {
                chunksVisibleLastUpdate.Add(chunk);
            }
            if (++counter >= 12) break;
        }
    }
    /// <summary>
    /// Process chunk visibility queue
    /// </summary>
    /// <param name="startTime">Start of allocated time frame</param>
    public void ProcessChunkVisibilty(float startTime, float timeBudget)
    {
        while (chunkVisibilityQueue.Count > 0 && Time.realtimeSinceStartup - startTime < timeBudget)
        {
            ChunkVisibility chunk = chunkVisibilityQueue.Dequeue();
            if (chunk.expectedID != chunk.chunk.chunkID)
                continue;
            chunk.chunk.SetVisible(chunk.visibility);
        }
    }
    /// <summary>
    /// Process active chunk density jobs
    /// </summary>
    /// <param name="startTime">Start of allocated time frame</param>
    public void ProcessDensityJobs(float startTime, float timeBudget) {
        while (terrainDensityJobQueue.Count > 0 && Time.realtimeSinceStartup - startTime < timeBudget)
        {
            TerrainJobObject job = terrainDensityJobQueue.Peek();
            if(job.expectedID != job.owner.chunkID)
            {
                job.jobHandle.Complete();
                terrainDensityJobQueue.Dequeue();
                continue;
            }
            else if (job.jobHandle.IsCompleted)
            {
                job.jobHandle.Complete();
                FastNoise.OutputMinMax densityMinMax = job.owner.marchingCubes.densityMinMax.Value;
                if (terrainDensityData.isolevel > densityMinMax.min && terrainDensityData.isolevel < densityMinMax.max)
                {
                    job.owner.marchingCubes.MarchingCubesJobHandler(job.terraforming);
                }
                else if (terrainDensityData.isolevel > densityMinMax.max)
                {
                    job.owner.assetSpawner.emptyChunk = true;
                    spawningPointCreationQueue.Enqueue(new AssetSpawnPointCreation(job.owner, job.owner.chunkID));
                }
                terrainDensityJobQueue.Dequeue();
            }
        }
    }
    /// <summary>
    /// Process active chunk polygonization jobs
    /// </summary>
    /// <param name="startTime">Start of allocated time frame</param>
    public void ProcessPolygonizationJobs(float startTime, float timeBudget) {
        while (terrainPolygonizationJobQueue.Count > 0 && Time.realtimeSinceStartup - startTime < timeBudget)
        {
            TerrainJobObject job = terrainPolygonizationJobQueue.Peek();
            if(job.expectedID != job.owner.chunkID)
            {
                job.jobHandle.Complete();
                terrainPolygonizationJobQueue.Dequeue();
                continue;
            }
            else if (job.jobHandle.IsCompleted)
            {
                job.jobHandle.Complete();
                terrainPolygonizationJobQueue.Dequeue();
                meshGenQueue.Enqueue(job);
            }
        }
    }
    /// <summary>
    /// Process mesh creation queue
    /// </summary>
    /// <param name="startTime">Start of allocated time frame</param>
    public void ProcessMeshGenQueue(float startTime, float timeBudget)
    {
        while (meshGenQueue.Count > 0 && Time.realtimeSinceStartup - startTime < timeBudget)
        {
            TerrainJobObject meshGen = meshGenQueue.Dequeue();
            if (meshGen.expectedID != meshGen.owner.chunkID)
                continue;
            else if (meshGen.owner.chunk != null)
                meshGen.owner.marchingCubes.SetMeshValuesPerformant(meshGen.terraforming);
        }
    }
    /// <summary>
    /// Process chunk collision mesh bakes
    /// </summary>
    /// <param name="startTime">Start of allocated time frame</param>
    public void ProcessCollisionMeshBakes(float startTime, float timeBudget)
    {
        while (collisionMeshBakeQueue.Count > 0 && Time.realtimeSinceStartup - startTime < timeBudget)
        {
            MeshBake meshBake = collisionMeshBakeQueue.Dequeue();
            if (meshBake.meshCollider == null || meshBake.mesh == null || meshBake.mesh.vertexCount == 0)
                continue;
            else if(meshBake.expectedID != meshBake.owner.chunkID)
                continue;
            meshBake.meshCollider.sharedMesh = meshBake.mesh;
        }
    }
    /// <summary>
    /// Process mesh creation queue
    /// </summary>
    /// <param name="startTime">Start of allocated time frame</param>
    public void ProcessGrassQueue(float startTime, float timeBudget)
    {
        int counter = 0;
        while (grassProcessQueue.Count > 0 && Time.realtimeSinceStartup - startTime < timeBudget)
        {
            GrassObject grass = grassProcessQueue.Dequeue();
            if (grass.expectedID != grass.owner.chunkID)
                continue;
            else if (grass.owner.chunk != null)
                grass.owner.marchingCubes.ProcessGrass(grass.triangleCount);
            if (++counter >= 5) break;
        }
    }
    /// <summary>
    /// Process vertex sort jobs
    /// </summary>
    /// <param name="startTime">Start of allocated time frame</param>
    public void ProcessVertexSortJobs(float startTime, float timeBudget) {
        while (vertexSortJobQueue.Count > 0 && Time.realtimeSinceStartup - startTime < timeBudget)
        {
            VertexSortJob job = vertexSortJobQueue.Peek();
            if (job.expectedID != job.assetSpawner.owner.chunkID)
            {
                job.jobHandle.Complete();
                vertexSortJobQueue.Dequeue();
                continue;
            }
            else if (job.jobHandle.IsCompleted)
            {
                job.jobHandle.Complete();
                vertexSortJobQueue.Dequeue();
                spawningPointCreationQueue.Enqueue(new AssetSpawnPointCreation(job.assetSpawner.owner, job.assetSpawner.owner.chunkID));
            }
        }
    }
    /// <summary>
    /// Process spawn point creation queue
    /// </summary>
    /// <param name="startTime">Start of allocated time frame</param>
    public void ProcessSpawnPointCreation(float startTime, float timeBudget)
    {
        while (spawningPointCreationQueue.Count > 0 && Time.realtimeSinceStartup - startTime < timeBudget)
        {
            AssetSpawnPointCreation assetSpawner = spawningPointCreationQueue.Dequeue();
            if (assetSpawner.expectedID != assetSpawner.owner.chunkID)
                continue;
            else if (assetSpawner.owner.chunk != null)
                assetSpawner.owner.assetSpawner.SpawnAssets();
        }
    }
    /// <summary>
    /// Process asset instantiation queue
    /// </summary>
    /// <param name="startTime">Start of allocated time frame</param>
    public void ProcessAssetInstantiation(float startTime, float timeBudget)
    {
        while (pendingAssetInstantiations.Count > 0 && Time.realtimeSinceStartup - startTime < timeBudget)
        {
            AssetInstantiation assetInstantiation = pendingAssetInstantiations.Dequeue();
            if (assetInstantiation.expectedID != assetInstantiation.owner.chunkID)
                continue;
            assetInstantiation.owner.assetSpawner.AssetInstantiation(assetInstantiation.i, assetInstantiation.j, assetInstantiation.seed);
        }
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
        // Current Max: 9.9ms
        ProcessChunkReturns(Time.realtimeSinceStartup, 0.0002f); //0.2ms
        ProcessChunkLoads(Time.realtimeSinceStartup, 0.0002f); //0.2ms
        ProcessChunkVisibilty(Time.realtimeSinceStartup, 0.0005f); //0.5ms

        ProcessDensityJobs(Time.realtimeSinceStartup, 0.001f); //1ms
        ProcessPolygonizationJobs(Time.realtimeSinceStartup, 0.001f); //1ms

        ProcessMeshGenQueue(Time.realtimeSinceStartup, 0.001f); //1ms
        ProcessCollisionMeshBakes(Time.realtimeSinceStartup, 0.0005f); //0.5ms

        ProcessGrassQueue(Time.realtimeSinceStartup, 0.0005f); //0.5ms

        ProcessVertexSortJobs(Time.realtimeSinceStartup, 0.001f); //1ms
        ProcessSpawnPointCreation(Time.realtimeSinceStartup, 0.002f); //2ms
        ProcessAssetInstantiation(Time.realtimeSinceStartup, 0.002f); //2ms
    }
    /// <summary>
    /// Initial chunk load
    /// </summary>
    public void UpdateChunksInitial()
    {
        int currentChunkCoordX = Mathf.FloorToInt(viewerPos.x / chunkSize);
        int currentChunkCoordY = Mathf.FloorToInt(viewerPos.y / chunkSize);
        int currentChunkCoordZ = Mathf.FloorToInt(viewerPos.z / chunkSize);

        int minX = currentChunkCoordX - chunksVisible;
        int maxX = currentChunkCoordX + chunksVisible;
        int minY = currentChunkCoordY - chunksVisible;
        int maxY = currentChunkCoordY + chunksVisible;
        int minZ = currentChunkCoordZ - chunksVisible;
        int maxZ = currentChunkCoordZ + chunksVisible;

        for (int xOffset = -chunksVisible; xOffset <= chunksVisible; xOffset++)
        {
            int currentX = currentChunkCoordX + xOffset;

            for (int yOffset = -chunksVisible; yOffset <= chunksVisible; yOffset++)
            {
                int currentY = currentChunkCoordY + yOffset;

                if ((currentY > 0 && currentY > maxWorldYChunks) || (currentY < 0 && currentY < -maxWorldYChunks))
                    continue;

                for (int zOffset = -chunksVisible; zOffset <= chunksVisible; zOffset++)
                {
                    int currentZ = currentChunkCoordZ + zOffset;

                    if (useFixedMapSize && (math.abs(currentX) > maxChunkDst || math.abs(currentZ) > maxChunkDst))
                        continue;

                    bool isEdge = currentX == minX || currentX == maxX ||
                                  currentY == minY || currentY == maxY ||
                                  currentZ == minZ || currentZ == maxZ;

                    Vector3Int viewedChunkCoord = new Vector3Int(currentX, currentY, currentZ);
                    long packedCoord = PackChunkCoord(currentX, currentY, currentZ);
                    
                    Vector3Int chunkPos = viewedChunkCoord * chunkSize;
                    bool waterChunk = terrainDensityData.waterLevel >= chunkPos.y && terrainDensityData.waterLevel < chunkPos.y + terrainDensityData.chunkSize && Instance.terrainDensityData.water;
                    TerrainChunk chunk = terrainChunkPool.GetChunk(packedCoord, viewedChunkCoord, chunkPos, waterChunk);
                    chunkDictionary.Add(packedCoord, chunk);
                    if (isEdge)
                        chunksVisibleLastUpdate.Add(chunk);
                }
            }
        }

        initialLoadComplete = true;
    }
    /// <summary>
    /// Update all the visible chunks loading in new ones and unloading old ones that are no longer visible
    /// </summary>
    public void UpdateVisibleChunks()
    {
        if (viewerPos.y <= -40 && !lightingBlockerRenderer.enabled)
            lightingBlockerRenderer.enabled = true;
        else if (viewerPos.y >= -40 && lightingBlockerRenderer.enabled)
            lightingBlockerRenderer.enabled = false;

        int currentChunkCoordX = Mathf.FloorToInt(viewerPos.x / chunkSize);
        int currentChunkCoordY = Mathf.FloorToInt(viewerPos.y / chunkSize);
        int currentChunkCoordZ = Mathf.FloorToInt(viewerPos.z / chunkSize);

        int minX = currentChunkCoordX - chunksVisible;
        int maxX = currentChunkCoordX + chunksVisible;
        int minY = currentChunkCoordY - chunksVisible;
        int maxY = currentChunkCoordY + chunksVisible;
        int minZ = currentChunkCoordZ - chunksVisible;
        int maxZ = currentChunkCoordZ + chunksVisible;

        foreach (TerrainChunk terrainChunk in chunksVisibleLastUpdate)
        {
            if (terrainChunk.chunkCoord.x < minX || terrainChunk.chunkCoord.x > maxX ||
                terrainChunk.chunkCoord.y < minY || terrainChunk.chunkCoord.y > maxY ||
                terrainChunk.chunkCoord.z < minZ || terrainChunk.chunkCoord.z > maxZ)
            {
                if (!terrainChunk.marchingCubes.edited)
                    chunkReturnQueue.Enqueue(terrainChunk);
                else
                    chunkVisibilityQueue.Enqueue(new ChunkVisibility(terrainChunk, false, terrainChunk.chunkID));
            }
        }
        chunksVisibleLastUpdate.Clear();

        for (int xOffset = -chunksVisible; xOffset <= chunksVisible; xOffset++)
        {
            int currentX = currentChunkCoordX + xOffset;

            for (int yOffset = -chunksVisible; yOffset <= chunksVisible; yOffset++)
            {
                int currentY = currentChunkCoordY + yOffset;

                if ((currentY > 0 && currentY > maxWorldYChunks) || (currentY < 0 && currentY < -maxWorldYChunks))
                    continue;

                for (int zOffset = -chunksVisible; zOffset <= chunksVisible; zOffset++)
                {
                    int currentZ = currentChunkCoordZ + zOffset;

                    if (useFixedMapSize && (math.abs(currentX) > maxChunkDst || math.abs(currentZ) > maxChunkDst))
                        continue;

                    bool isEdge = currentX == minX || currentX == maxX ||
                                  currentY == minY || currentY == maxY ||
                                  currentZ == minZ || currentZ == maxZ;

                    if (!isEdge)
                        continue;

                    long chunkCoordId = PackChunkCoord(currentX, currentY, currentZ);

                    if (chunkDictionary.TryGetValue(chunkCoordId, out TerrainChunk dictChunk))
                    {
                        chunkVisibilityQueue.Enqueue(new ChunkVisibility(dictChunk, true, dictChunk.chunkID));
                        chunksVisibleLastUpdate.Add(dictChunk);
                    }
                    else if (chunkLoadSet.Add(chunkCoordId))
                    {
                        Vector3Int viewedChunkCoord = new Vector3Int(currentX, currentY, currentZ);
                        chunkLoadQueue.Enqueue(viewedChunkCoord);
                    }
                }
            }
        }
    }
    /// <summary>
    /// Get a TerrainChunk and its neighbors with the given chunk's coordinate
    /// </summary>
    /// <param name="chunkCoord">The chunk coordinate</param>
    /// <returns>A list containing the chunk whose coordinate was passed and its neighbors</returns>
    public TerrainChunk[] GetChunkAndNeighbors(Vector3Int chunkCoord)
    {
        TerrainChunk[] chunkAndNeighbors = new TerrainChunk[chunkNeighborOffsets.Length];

        for (int i = 0; i < chunkNeighborOffsets.Length; i++)
        {
            Vector3Int neighborCoord = chunkCoord + chunkNeighborOffsets[i];
            long packedCoord = PackChunkCoord(neighborCoord.x, neighborCoord.y, neighborCoord.z);
            chunkDictionary.TryGetValue(packedCoord, out chunkAndNeighbors[i]);
        }

        return chunkAndNeighbors;
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

                texSettingsTab.heightSlider.SetValues(biomeTextureConfig.biomeTextures[i].heightRange.heightStart, biomeTextureConfig.biomeTextures[i].heightRange.heightEnd, -maxWorldYChunks * terrainDensityData.chunkSize, maxWorldYChunks * terrainDensityData.chunkSize);

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
        public Transform chunkTransform;
        public GameObject assetParent;
        public Transform assetParentTransform;
        public uint chunkID;
        public ComputeMarchingCubes marchingCubes;
        public AssetSpawner assetSpawner;
        public GameObject waterPlaneGenerator;
        MeshFilter waterMeshFilter;
        public MeshRenderer waterMeshRenderer;
        public long packedCoord;
        public Vector3Int chunkCoord;
        public Vector3Int chunkPos;
        public Mesh mesh;
        public MeshCollider meshCollider;
        public MeshFilter meshFilter;
        public bool waterChunk = false;
        public MeshRenderer meshRenderer;
        public bool visible;
        public void StartChunk (long packedCoord, Vector3Int chunkCoord, Vector3Int chunkPos, bool waterChunk)
        {
            visible = true;
            this.packedCoord = packedCoord;
            this.chunkCoord = chunkCoord;
            this.chunkPos = chunkPos;
            chunk = new GameObject("Chunk" + chunkPos);
            chunkTransform = chunk.transform;
            chunkTransform.SetParent(Instance.chunkParent);
            chunk.layer = 3;
            assetParent = new GameObject("Assets");
            assetParentTransform = assetParent.transform;
            assetParentTransform.SetParent(chunkTransform);
            // Set up basic chunk components
            mesh = new Mesh();
            meshCollider = chunk.AddComponent<MeshCollider>();
            meshFilter = chunk.AddComponent<MeshFilter>();
            meshRenderer = chunk.AddComponent<MeshRenderer>();
            // Chunk texture
            meshRenderer.sharedMaterial = Instance.terrainMaterial;
            // Set up water generator
            if (waterChunk)
            {
                this.waterChunk = true;
                waterPlaneGenerator = new GameObject("Water");
                waterPlaneGenerator.transform.SetParent(chunk.transform);
                waterMeshFilter = waterPlaneGenerator.AddComponent<MeshFilter>();
                waterMeshRenderer = waterPlaneGenerator.AddComponent<MeshRenderer>();
                waterMeshRenderer.sharedMaterial = Instance.waterMaterial;
                waterMeshFilter.mesh = Instance.waterMesh;
                waterPlaneGenerator.transform.position = chunkPos;
            }
            // Set up the chunk's AssetSpawn script
            assetSpawner = chunk.AddComponent<AssetSpawner>();
            assetSpawner.owner = this;
            assetSpawner.chunkPos = chunkPos;
            assetSpawner.terrainDensityData = Instance.terrainDensityData;
            assetSpawner.assetSpawnData = Instance.assetSpawnData;
            // Set up the chunk's ComputeMarchingCubes script
            marchingCubes = chunk.AddComponent<ComputeMarchingCubes>();
            marchingCubes.InitializeChunk(
                this, 
                mesh,
                meshFilter, 
                meshCollider, 
                chunkCoord, 
                chunkPos, 
                assetSpawner,
                Instance.terrainDensityData
            );
            marchingCubes.GenerateChunk();
        }
        public void RenewChunk (long packedCoord, Vector3Int chunkCoord, Vector3Int chunkPos, bool waterChunk)
        {
            this.packedCoord = packedCoord;
            this.chunkCoord = chunkCoord;
            this.chunkPos = chunkPos;
            chunk.name = "Chunk" + chunkPos;
            // Chunk texture
            meshRenderer.enabled = true;
            // Set up water generator
            if (waterChunk)
            {
                waterMeshRenderer.enabled = true;
                waterPlaneGenerator.transform.position = chunkPos;
            }
            // Set up the chunk's AssetSpawn script
            assetSpawner.chunkPos = chunkPos;
            assetSpawner.emptyChunk = false;
            // Set up the chunk's ComputeMarchingCubes script
            marchingCubes.chunkCoord = chunkCoord;
            marchingCubes.chunkPos = chunkPos;
            marchingCubes.GenerateChunk();
        }
        public void ClearChunk()
        {
            chunkID++;
            mesh.Clear();
            meshRenderer.enabled = false;
            meshCollider.sharedMesh = null;
            if (waterChunk && waterMeshRenderer != null)
                waterMeshRenderer.enabled = false;
            if (marchingCubes.grass != null)
            {
                marchingCubes.grass.enabled = false;
                marchingCubes.grass.renderGrass = false;
            }
            // assetSpawner.ClearAssets();
            Instance.assetSpawnData.ResetChunkAssets(chunkPos);
            assetSpawner.DisposalReleaseHandler();
            assetSpawner.emptyChunk = false;
            Destroy(assetParent.gameObject);
            assetParent = new GameObject("Assets");
            assetParentTransform = assetParent.transform;
            assetParentTransform.SetParent(chunkTransform);
        }
        /// <summary>
        /// Set the visibility of the chunk
        /// </summary>
        /// <param name="visible">Whether the chunk is visible</param>
        public void SetVisible(bool visible)
        {
            if (this.visible == visible) return;
            // Terrain
            if (meshRenderer != null && meshRenderer.enabled != visible)
            {
                meshRenderer.enabled = visible;
                if (meshCollider != null && meshCollider.enabled != visible && meshCollider.sharedMesh.vertexCount > 0)
                {
                    meshCollider.enabled = visible;
                }
            }
            // Water
            if (waterChunk && waterMeshRenderer != null && waterMeshRenderer.enabled != visible)
            {
                waterMeshRenderer.enabled = visible;
            }
            // Assets
            if (assetSpawner.assetsSet)
            {
                foreach (Asset asset in assetSpawner.spawnedAssets)
                {
                    if (asset.meshRenderer != null && asset.meshRenderer.enabled != visible)
                    {
                        asset.meshRenderer.enabled = visible;
                        if (asset.meshCollider != null && asset.meshCollider.enabled != visible)
                        {
                            asset.meshCollider.enabled = visible;
                        }
                    }
                    if (asset.meshRenderer == null && asset.obj != null)
                    {
                        asset.obj.SetActive(visible);
                    }
                }
            }
            if (marchingCubes != null && marchingCubes.grass != null)
            {
                marchingCubes.grass.renderGrass = visible;
            }
            this.visible = visible;
        }
    }
}