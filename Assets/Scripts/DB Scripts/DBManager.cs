using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Networking;
using Unity.VisualScripting;
using FishNet.Example.Authenticating;
using System.Collections.Generic;
using GameKit.Dependencies.Utilities;
using LiteNetLib.Utils;
using static NoiseGenerator;
using System.Threading;
using System.Linq;
using static DBNoiseGeneratorSettings;
/// <summary>
/// This class will act as a manager script that will facilitate all DB operations
/// </summary>
public class DBManager : MonoBehaviour
{
    public int loadedTerrainId = -1;
    public bool IsTerrainLoaded = false;
    public Dictionary<int, string> responseList = new();
    /// <summary>
    /// Starts the DB process to determine if a user is registered or not
    /// </summary>
    /// <param name="steamId"></param>
    /// <param name="steamName"></param>
    public void checkRegisteredUser(ulong steamId, string steamName)
    {
        StartCoroutine(VerifyRegisteredUser(steamId, steamName));
    }

    /// <summary>
    /// Starts the DB retrieval of the users created terrains
    /// </summary>
    public void retreiveTerrainNames()
    {
        StartCoroutine(RetrieveTerrainNames(SteamValidation.steamID));
    }

    /// <summary>
    /// Starts the DB process to save a created terrain
    /// </summary>
    public void retreiveTerrainData()
    {
        StartCoroutine(LoadTerrainData());
    }

    /// <summary>
    /// This starts the DB save of current terrain data
    /// </summary>
    public void saveTerrainData(string tName)
    {
        StartCoroutine(SaveTerrainData(tName));
    }

    /// <summary>
    /// Once a user selects save, this is called to save the users terrain in the database. 
    /// </summary>
    /// <param name="steamId"></param>
    /// <param name="steamName"></param>
    /// <returns></returns>
    IEnumerator SaveTerrainData(string terrainName) //int seed, int width, int height, float noiseScale, float isolevel, bool lerp
    {
        //Set up connection
        string url = "http://localhost/sqlconnect/saveTerrain.php";
        WWWForm form = new();

        DBTerrainSettings settings =DBSeedSerializer.SerializeTerrainDensity(ChunkGenNetwork.Instance.terrainDensityData);
        string json = JsonUtility.ToJson(settings);

        form.AddField("TerrainSettings", json);
        form.AddField("SteamId", SteamValidation.steamID.ToString());
        form.AddField("TerrainName", terrainName);

        //Send web request
        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            // Set timeout at 10 seconds
            request.timeout = 10;

            yield return request.SendWebRequest();

            //Log the results. This can be deleted later
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Raw Response: " + request.downloadHandler.text);
                PHPSaveTerrainResponse response = JsonUtility.FromJson<PHPSaveTerrainResponse>(request.downloadHandler.text);
                loadedTerrainId = response.terrainId;
                var key = responseList.FirstOrDefault(x => x.Value == terrainName).Key;

                if (key != 0) // Default(int) is 0 if not found
                {
                    responseList.Remove(key);
                }
                responseList.Add(response.terrainId, terrainName);
            }
            else
                Debug.Log("request failed: " + request.error + " response code: " + request.responseCode);

        }
    }

    /// <summary>
    /// Once a terrain is saved this is called to save all of the manually placed assets. 
    /// </summary>
    /// <param name="steamId"></param>
    /// <param name="steamName"></param>
    /// <returns></returns>
    IEnumerator SaveTerrainAssets(List<ManualAssetIdentification> manualAssets, int terrainId) //int seed, int width, int height, float noiseScale, float isolevel, bool lerp
    {
        //Set up connection
        string url = "http://localhost/sqlconnect/saveAssets.php";
        WWWForm form = new();

        form.AddField("TerrainId", terrainId);

        ManualAssetJSONList assets = new();
        List<ManualAssetJSON> assetsList = new();
        foreach (ManualAssetIdentification placedAsset in manualAssets)
        {
            assetsList.Add(new ManualAssetJSON(placedAsset));
        }
        assets.data = assetsList.ToArray();
        form.AddField("Assets", JsonUtility.ToJson(assets));

        //Send web request
        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            // Set timeout at 10 seconds
            request.timeout = 10;

            yield return request.SendWebRequest();

            //Log the results. This can be deleted later
            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log("Raw Response: " + request.downloadHandler.text);
            else
                Debug.Log("request failed: " + request.error + " response code: " + request.responseCode);
        }
    }
    /// <summary>
    /// On Startup this method will call a php script that checks if the user exists in out system. If they do not we will add them into the 
    /// db to facilitate the loading and storing of terrains.
    /// </summary>
    /// <param name="steamId"></param>
    /// <param name="steamName"></param>
    /// <returns></returns>
    IEnumerator VerifyRegisteredUser(ulong steamId, string steamName) //int seed, int width, int height, float noiseScale, float isolevel, bool lerp
    {
        //Set up connection
        string url = "http://localhost/sqlconnect/validateUser.php";
        WWWForm form = new();

        //Need to pass the SteamId and steamId to the php to determine if the user exists, or if we need to add them to the db
        form.AddField("SteamId", steamId.ToString());
        form.AddField("SteamName", steamName);

        //Send web request
        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            // Set timeout at 10 seconds
            request.timeout = 10;

            yield return request.SendWebRequest();

            //Log the results. This can be deleted later
            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log("Raw Response: " + request.downloadHandler.text);
            else
                Debug.Log("request failed: " + request.error + " response code: " + request.responseCode);
        }
    }


    /// <summary>
    /// The method will use the loadCreatedTerrainNames.php script to retreive the names of all the terrains that a user has created and saved to the DB
    /// This data will then need to be displayed in order to allow the user to select what terrain they want to fully load
    /// </summary>
    /// <param name="steamId"></param>
    /// <param name="steamName"></param>
    /// <returns></returns>
    IEnumerator RetrieveTerrainNames(ulong steamId) //int seed, int width, int height, float noiseScale, float isolevel, bool lerp
    {
        //Set up connection
        string url = "http://localhost/sqlconnect/loadCreatedTerrainNames.php";
        WWWForm form = new();

        //Need to pass the SteamId to the php to be used in the query
        form.AddField("SteamId", steamId.ToString());
        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            // Set timeout at 10 seconds
            request.timeout = 10;

            yield return request.SendWebRequest();

            //If the return result shows it was a sucess (The php script didnt break)
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    // Parse the JSON response
                    PHPTerrainNameResponse response = JsonUtility.FromJson<PHPTerrainNameResponse>(request.downloadHandler.text);
                    if (response.success)
                    {
                        responseList.Clear();
                        // Process data
                        foreach (var item in response.data)
                        {
                            responseList.Add(item.TerrainId, item.TerrainName);
                            Debug.Log($"Terrain Name: {item.TerrainName} TerrainId: {item.TerrainId}");
                        }
                    }
                    else
                    {
                        Debug.LogError("PHP Error: " + response.message);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("JSON Parse Error: " + e.Message);
                    Debug.Log("Raw Response: " + request.downloadHandler.text);
                }
            }
            else
            {
                Debug.LogError("Request Failed: " + request.error);
                Debug.LogError("Response Code: " + request.responseCode);
            }
        }
    }

    /// <summary>
    /// Once a user selects the Load tab and clicks the saved terrain they wish to load, this method is called and all the terrain data information is loaded
    /// </summary>
    /// <returns></returns>
    IEnumerator LoadTerrainData()
    {
        Debug.Log($"Loading terrain {loadedTerrainId}");
        string url = "http://localhost/sqlconnect/loadTerrainData.php";
        WWWForm form = new();

        //Need to pass the SteamId and steamId to the php to determine if the user exists, or if we need to add them to the db
        form.AddField("terrainId", loadedTerrainId);
        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            // Set timeout (in seconds)
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    // Parse the JSON response
                    PHPTerrainDataResponse response = JsonUtility.FromJson<PHPTerrainDataResponse>(request.downloadHandler.text);
                    if (response.success)
                    {
                        Debug.Log("Loaded the terrain now going to see if we can deserealize and load");
                        GameObject chunk = GameObject.Find("ChunkParent");

                        while (chunk.transform.childCount > 0)
                        {
                            DestroyImmediate(chunk.transform.GetChild(0).gameObject);
                        }
                        Debug.Log("Deleted the terrain");
                        TerrainDensityData terrainDensityDataNew = DBSeedSerializer.DeserializeTerrainDensity(response.data);
                        ChunkGenNetwork.Instance.terrainDensityData = terrainDensityDataNew;
                        Debug.Log("de-serealized the terrain response data");

                        // Reset action and chunking to defaults (loading in from fresh)
                        // Chunk Variables
                        ChunkGenNetwork.Instance.chunkDictionary = new();
                        ChunkGenNetwork.Instance.chunksVisibleLastUpdate = new();
                        ChunkGenNetwork.Instance.chunkLoadQueue = new();
                        ChunkGenNetwork.Instance.chunkLoadSet = new();
                        ChunkGenNetwork.Instance.chunkHideQueue = new();
                        ChunkGenNetwork.Instance.chunkShowQueue = new();
                        ChunkGenNetwork.Instance.isLoadingChunkVisibility = false;
                        // queueUpdateDistanceThreshold = 15f;
                        ChunkGenNetwork.Instance.isLoadingChunks = false;
                        // Action Queues
                        ChunkGenNetwork.Instance.hasPendingReadbacks = false;
                        ChunkGenNetwork.Instance.pendingReadbacks = new();
                        ChunkGenNetwork.Instance.isLoadingReadbacks = false;
                        ChunkGenNetwork.Instance.hasPendingAssetInstantiations = false;
                        ChunkGenNetwork.Instance.pendingAssetInstantiations = new();
                        ChunkGenNetwork.Instance.isLoadingAssetInstantiations = false;

                        ChunkGenNetwork.Instance.chunkSize = ChunkGenNetwork.Instance.terrainDensityData.width;
                        ChunkGenNetwork.Instance.chunksVisible = Mathf.RoundToInt(ChunkGenNetwork.Instance.maxViewDst / ChunkGenNetwork.Instance.chunkSize);

                        ChunkGenNetwork.Instance.assetSpawnData.ResetSpawnPoints();
                        ChunkGenNetwork.Instance.initialLoadComplete = false;
                        ChunkGenNetwork.Instance.UpdateVisibleChunks();
                    }
                    else
                    {
                        Debug.LogError("PHP Error: " + response.message);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("JSON Parse Error: " + e.Message);
                    Debug.Log("Raw Response: " + request.downloadHandler.text);
                }
            }
            else
            {
                Debug.LogError("Request Failed: " + request.error);
                Debug.LogError("Response Code: " + request.responseCode);
            }
        }
    }

    // Helper classes for JSON parsing of returned data
    [System.Serializable]
    public class PHPResponse
    {
        public bool success;
        public string message;
        public UserData[] data;
    }

    // Helper classes for JSON parsing of returned data
    [System.Serializable]
    public class PHPSaveTerrainResponse
    {
        public bool success;
        public string message;
        public int terrainId;
    }

    [System.Serializable]
    public class UserData
    {
        public string ID;
        public string Name;
    }

    [System.Serializable]
    public class PHPTerrainNameResponse
    {
        public bool success;
        public string message;
        public TerrainNames[] data;
    }

    [System.Serializable]
    public class TerrainNames
    {
        public string TerrainName;

        //Terrain Id is retreived also so that the query to retreive the terrain info will be faster
        public int TerrainId;
    }
    [System.Serializable]
    public class PHPTerrainDataResponse
    {
        public bool success;
        public string message;
        public DBTerrainSettings data;
    }

    [System.Serializable]
    public class ManualAssetJSON
    {
        public int AssetId;
        public float xPos;
        public float yPos;
        public float zPos;

        public ManualAssetJSON(ManualAssetIdentification asset)
        {
            AssetId = (int)asset.Id;
            xPos = asset.xCord;
            yPos = asset.yCord;
            zPos = asset.zCord;
        }
    }
    [System.Serializable]
    public class ManualAssetJSONList
    {
        public bool success;
        public string message;
        public ManualAssetJSON[] data;
    }
}

/// <summary>
/// Struct of the TerrainDensityData Object class so it can be serialized over the network.
/// </summary>
[System.Serializable]
public struct DBTerrainSettings
{
    public DBNoiseGeneratorSettings[] noiseSettings;
    // Terrain Values
    public int width;
    public int height;
    public float isolevel;
    public int waterLevel;
    public bool lerp;
    public bool terracing;
    public int terraceHeight;
}

/// <summary>
/// Struct of the NoiseGenerator Object class so it can be serialized over the network.
/// </summary>
[System.Serializable]
public struct DBNoiseGeneratorSettings
{
    public bool activated;
    public float[] remoteTexture;
    public int noiseGeneratorType;
    // Noise and Fractal Settings
    public int selectedNoiseDimension;
    public int noiseDimension;
    public int selectedNoiseType;
    public int noiseType;
    public int selectedNoiseFractalType;
    public int noiseFractalType;
    public int selectedRotationType3D;
    public int rotationType3D;
    public int noiseSeed;
    public int noiseFractalOctaves;
    public float noiseFractalLacunarity;
    public float noiseFractalGain;
    public float fractalWeightedStrength;
    public float noiseFrequency;
    // Domain Warp Values
    public bool domainWarpToggle;
    public int selectedDomainWarpType;
    public int domainWarpType;
    public int selectedDomainWarpFractalType;
    public int domainWarpFractalType;
    public float domainWarpAmplitude;
    public int domainWarpSeed;
    public int domainWarpFractalOctaves;
    public float domainWarpFractalLacunarity;
    public float domainWarpFractalGain;
    public float domainWarpFrequency;
    // Cellular(Voronoi) Values
    public int selectedCellularDistanceFunction;
    public int cellularDistanceFunction;
    public int selectedCellularReturnType;
    public int cellularReturnType;
    public float cellularJitter;
    // Terrain Values
    public float noiseScale;

    public static class DBSeedSerializer
    {
        public static DBTerrainSettings SerializeTerrainDensity(TerrainDensityData settings)
        {
            var noiseGenSettings = new DBNoiseGeneratorSettings[settings.noiseGenerators.Length];
            for (int i = 0; i < noiseGenSettings.Length; i++)
            {
                noiseGenSettings[i] = SerializeNoiseDensity(settings.noiseGenerators[i]);
            }

            return new DBTerrainSettings
            {
                noiseSettings = noiseGenSettings,
                // Terrain Values
                width = settings.width,
                height = settings.height,
                isolevel = settings.isolevel,
                waterLevel = settings.waterLevel,
                lerp = settings.lerp,
                terracing = settings.terracing,
                terraceHeight = settings.terraceHeight
            };
        }

        private static DBNoiseGeneratorSettings SerializeNoiseDensity(NoiseGenerator settings)
        {
            return new DBNoiseGeneratorSettings
            {
                activated = settings.activated,
                remoteTexture = SplineCurveFunctions.CurveToArray(settings.valueCurve),
                noiseGeneratorType = (int)settings.noiseGeneratorType,
                // Noise and Fractal Settings
                selectedNoiseDimension = settings.selectedNoiseDimension,
                noiseDimension = (int)settings.noiseDimension,
                selectedNoiseType = settings.selectedNoiseType,
                noiseType = (int)settings.noiseType,
                selectedNoiseFractalType = settings.selectedNoiseFractalType,
                noiseFractalType = (int)settings.noiseFractalType,
                selectedRotationType3D = settings.selectedRotationType3D,
                rotationType3D = (int)settings.rotationType3D,
                noiseSeed = settings.noiseSeed,
                noiseFractalOctaves = settings.noiseFractalOctaves,
                noiseFractalLacunarity = settings.noiseFractalLacunarity,
                noiseFractalGain = settings.noiseFractalGain,
                fractalWeightedStrength = settings.fractalWeightedStrength,
                noiseFrequency = settings.noiseFrequency,
                // Domain Warp Values
                domainWarpToggle = settings.domainWarpToggle,
                selectedDomainWarpType = settings.selectedDomainWarpType,
                domainWarpType = (int)settings.domainWarpType,
                selectedDomainWarpFractalType = settings.selectedDomainWarpFractalType,
                domainWarpFractalType = (int)settings.domainWarpFractalType,
                domainWarpAmplitude = settings.domainWarpAmplitude,
                domainWarpSeed = settings.domainWarpSeed,
                domainWarpFractalOctaves = settings.domainWarpFractalOctaves,
                domainWarpFractalLacunarity = settings.domainWarpFractalLacunarity,
                domainWarpFractalGain = settings.domainWarpFractalGain,
                domainWarpFrequency = settings.domainWarpFrequency,
                // Cellular(Voronoi) Values
                selectedCellularDistanceFunction = settings.selectedCellularDistanceFunction,
                cellularDistanceFunction = (int)settings.cellularDistanceFunction,
                selectedCellularReturnType = settings.selectedCellularReturnType,
                cellularReturnType = (int)settings.cellularReturnType,
                cellularJitter = settings.cellularJitter,
                // Terrain Values
                noiseScale = settings.noiseScale,
            };
        }

        public static TerrainDensityData DeserializeTerrainDensity(DBTerrainSettings settings)
        {
            var deserializedDensity = ScriptableObject.CreateInstance<TerrainDensityData>();

            var noiseGenerators = new NoiseGenerator[settings.noiseSettings.Length];
            for (int i = 0; i < noiseGenerators.Length; i++)
            {
                noiseGenerators[i] = DeserializeNoiseDensity(settings.noiseSettings[i]);
            }

            deserializedDensity.noiseGenerators = noiseGenerators;

            // Terrain Values
            deserializedDensity.width = settings.width;
            deserializedDensity.height = settings.height;
            deserializedDensity.isolevel = settings.isolevel;
            deserializedDensity.waterLevel = settings.waterLevel;
            deserializedDensity.lerp = settings.lerp;
            deserializedDensity.terracing = settings.terracing;
            deserializedDensity.terraceHeight = settings.terraceHeight;

            return deserializedDensity;
        }

        private static NoiseGenerator DeserializeNoiseDensity(DBNoiseGeneratorSettings settings)
        {
            var deserializedNoise = ScriptableObject.CreateInstance<NoiseGenerator>();

            deserializedNoise.activated = settings.activated;
            deserializedNoise.remoteTexture = SplineCurveFunctions.ArrayToTexture(settings.remoteTexture);
            deserializedNoise.noiseGeneratorType = (NoiseGeneratorType)settings.noiseGeneratorType;
            // Noise and Fractal Settings
            deserializedNoise.selectedNoiseDimension = settings.selectedNoiseDimension;
            deserializedNoise.noiseDimension = (fnl_noise_dimension)settings.noiseDimension;
            deserializedNoise.selectedNoiseType = settings.selectedNoiseType;
            deserializedNoise.noiseType = (fnl_noise_type)settings.noiseType;
            deserializedNoise.selectedNoiseFractalType = settings.selectedNoiseFractalType;
            deserializedNoise.noiseFractalType = (fnl_fractal_type)settings.noiseFractalType;
            deserializedNoise.selectedRotationType3D = settings.selectedRotationType3D;
            deserializedNoise.rotationType3D = (fnl_rotation_type_3d)settings.rotationType3D;
            deserializedNoise.noiseSeed = settings.noiseSeed;
            deserializedNoise.noiseFractalOctaves = settings.noiseFractalOctaves;
            deserializedNoise.noiseFractalLacunarity = settings.noiseFractalLacunarity;
            deserializedNoise.noiseFractalGain = settings.noiseFractalGain;
            deserializedNoise.fractalWeightedStrength = settings.fractalWeightedStrength;
            deserializedNoise.noiseFrequency = settings.noiseFrequency;
            // Domain Warp Values
            deserializedNoise.domainWarpToggle = settings.domainWarpToggle;
            deserializedNoise.selectedDomainWarpType = settings.selectedDomainWarpType;
            deserializedNoise.domainWarpType = (fnl_domain_warp_type)settings.domainWarpType;
            deserializedNoise.selectedDomainWarpFractalType = settings.selectedDomainWarpFractalType;
            deserializedNoise.domainWarpFractalType = (fnl_domain_warp_fractal_type)settings.domainWarpFractalType;
            deserializedNoise.domainWarpAmplitude = settings.domainWarpAmplitude;
            deserializedNoise.domainWarpSeed = settings.domainWarpSeed;
            deserializedNoise.domainWarpFractalOctaves = settings.domainWarpFractalOctaves;
            deserializedNoise.domainWarpFractalLacunarity = settings.domainWarpFractalLacunarity;
            deserializedNoise.domainWarpFractalGain = settings.domainWarpFractalGain;
            deserializedNoise.domainWarpFrequency = settings.domainWarpFrequency;
            // Cellular(Voronoi) Values
            deserializedNoise.selectedCellularDistanceFunction = settings.selectedCellularDistanceFunction;
            deserializedNoise.cellularDistanceFunction = (fnl_cellular_distance_func)settings.cellularDistanceFunction;
            deserializedNoise.selectedCellularReturnType = settings.selectedCellularReturnType;
            deserializedNoise.cellularReturnType = (fnl_cellular_return_type)settings.cellularReturnType;
            deserializedNoise.cellularJitter = settings.cellularJitter;
            // Terrain Values
            deserializedNoise.noiseScale = settings.noiseScale;

            return deserializedNoise;
        }
    }
}


