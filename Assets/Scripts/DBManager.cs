using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Networking;
using Unity.VisualScripting;
using FishNet.Example.Authenticating;
using System.Collections.Generic;
/// <summary>
/// This class will act as a manager script that will facilitate all DB operations
/// </summary>
public class DBManager : MonoBehaviour
{
    public int loadedTerrainId = 4;
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
    public void saveTerrainData(string tName){
        MarchingCubes mc = FindFirstObjectByType<MarchingCubes>();
        StartCoroutine(SaveTerrainData(mc.terrainDensityData, tName));
        Debug.Log("Got name:");
    }

     /// <summary>
    /// On Startup this method will call a php script that checks if the user exists in out system. If they do not we will add them into the 
    /// db to facilitate the loading and storing of terrains.
    /// </summary>
    /// <param name="steamId"></param>
    /// <param name="steamName"></param>
    /// <returns></returns>
     
     IEnumerator SaveTerrainData(TerrainDensityData terrainDensityData, string terrainName) //int seed, int width, int height, float noiseScale, float isolevel, bool lerp
    {
         //Set up connection
        string url = "http://localhost/sqlconnect/saveTerrain.php";
        WWWForm form = new();

        //Noise Settings
        form.AddField("NoiseDimensions", terrainDensityData.noiseDimension.ToString());
        form.AddField("NoiseTypes", terrainDensityData.noiseType.ToString());
        form.AddField("Seed", terrainDensityData.noiseSeed.ToString());
        form.AddField("Width", terrainDensityData.width.ToString());
        form.AddField("Height", terrainDensityData.height.ToString());
        form.AddField("NoiseScale", terrainDensityData.noiseScale.ToString());
        form.AddField("IsoLevel", terrainDensityData.isolevel.ToString());
        form.AddField("Lerp", terrainDensityData.lerp.ToString());
        form.AddField("NoiseFrequency", terrainDensityData.noiseFrequency.ToString());

        //Warp Settings 
        form.AddField("WarpType", terrainDensityData.domainWarpType.ToString());
        form.AddField("WarpFractalTypes", terrainDensityData.domainWarpFractalType.ToString());
        form.AddField("WarpAmplitude", terrainDensityData.domainWarpAmplitude.ToString());
        form.AddField("WarpSeed", terrainDensityData.domainWarpSeed.ToString());
        form.AddField("WarpFrequency", terrainDensityData.domainWarpFrequency.ToString());
        form.AddField("WarpFractalOctaves", terrainDensityData.domainWarpFractalOctaves.ToString());
        form.AddField("WarpFractalLacunarity", terrainDensityData.domainWarpFractalLacunarity.ToString());
        form.AddField("WarpFractalGain", terrainDensityData.domainWarpFractalGain.ToString());
        form.AddField("DomainWarp", terrainDensityData.domainWarpToggle.ToString());

        //Fractal settings 
        form.AddField("FractalTypes", terrainDensityData.noiseFractalType.ToString());
        form.AddField("FractalOctaves", terrainDensityData.noiseFractalOctaves.ToString());
        form.AddField("FractalLacunarity", terrainDensityData.noiseFractalLacunarity.ToString());
        form.AddField("FractalGain", terrainDensityData.noiseFractalGain.ToString());
        form.AddField("FractalWeightedStrength", terrainDensityData.fractalWeightedStrength.ToString());
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

    IEnumerator LoadTerrainData()
    {
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
                        TerrainData data = response.data[0];
                        MarchingCubes mc = GameObject.FindFirstObjectByType<MarchingCubes>(); 

                        //Set noise settings
                        FastNoiseLite.NoiseType noiseType = (FastNoiseLite.NoiseType)Enum.Parse(typeof(FastNoiseLite.NoiseType), data.NoiseTypes.Replace(" ", "")); 
                        mc.terrainDensityData.noiseType = noiseType;
                        mc.terrainDensityData.selectedNoiseType = Array.IndexOf(mc.terrainDensityData.noiseTypeOptions, noiseType);
                        TerrainDensityData.NoiseDimension dimension = (TerrainDensityData.NoiseDimension)Enum.Parse(typeof(TerrainDensityData.NoiseDimension), data.NoiseDimensions.Replace(" ", ""));
                        mc.terrainDensityData.noiseDimension = dimension;
                        mc.terrainDensityData.selectedNoiseDimension = Array.IndexOf(mc.terrainDensityData.noiseDimensionOptions, dimension);

                        mc.terrainDensityData.noiseSeed = data.Seed;
                        mc.terrainDensityData.width = data.Width;
                        mc.terrainDensityData.height = data.Height;
                        mc.terrainDensityData.noiseScale = data.NoiseScale;
                        mc.terrainDensityData.isolevel = data.IsoLevel;
                        mc.terrainDensityData.lerp = data.Lerp;
                        mc.terrainDensityData.noiseFrequency = data.NoiseFrequency;

                        //Domain warp settings
                        FastNoiseLite.DomainWarpType domainWarp = (FastNoiseLite.DomainWarpType)Enum.Parse(typeof(FastNoiseLite.DomainWarpType), data.WarpType); 
                        mc.terrainDensityData.domainWarpType = domainWarp;
                        mc.terrainDensityData.selectedDomainWarpType = Array.IndexOf(mc.terrainDensityData.domainWarpTypeOptions, domainWarp);
                        FastNoiseLite.FractalType WarpfractalType = (FastNoiseLite.FractalType)Enum.Parse(typeof(FastNoiseLite.FractalType), data.WarpFractalTypes.Replace(" ", "")); 
                        mc.terrainDensityData.domainWarpFractalType = WarpfractalType;
                        mc.terrainDensityData.selectedDomainWarpFractalType = Array.IndexOf(mc.terrainDensityData.domainWarpFractalTypeOptions, WarpfractalType);
                        mc.terrainDensityData.domainWarpAmplitude = data.WarpAmplitude;
                        mc.terrainDensityData.domainWarpSeed = data.WarpSeed;
                        mc.terrainDensityData.domainWarpFrequency = data.WarpFrequency;
                        mc.terrainDensityData.domainWarpFractalOctaves = data.WarpFractalOctaves;
                        mc.terrainDensityData.domainWarpFractalLacunarity = data.WarpFractalLacunarity;
                        mc.terrainDensityData.domainWarpFractalGain = data.WarpFractalGain;
                        mc.terrainDensityData.domainWarpToggle = data.DomainWarp;

                        //Fractal Settings
                        FastNoiseLite.FractalType fractalType = (FastNoiseLite.FractalType)Enum.Parse(typeof(FastNoiseLite.FractalType), data.FractalTypes.Replace(" ", "")); 
                        mc.terrainDensityData.noiseFractalType = fractalType;
                        mc.terrainDensityData.selectedNoiseFractalType = Array.IndexOf(mc.terrainDensityData.noiseFractalTypeOptions, fractalType);
                        mc.terrainDensityData.noiseFractalOctaves = data.FractalOctaves;
                        mc.terrainDensityData.noiseFractalLacunarity = data.FractalLacunarity;
                        mc.terrainDensityData.noiseFractalGain = data.FractalGain;
                        mc.terrainDensityData.fractalWeightedStrength = data.FractalWeightedStrength;

                        //Update mesh with new values
                        mc.UpdateMesh();
                      
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
        public TerrainData[] data;
    }

    [System.Serializable]
    public class TerrainData
    {
        //Noise settings
        public string NoiseDimensions;
        public string NoiseTypes;
        public int Seed;
        public int Width;
        public int Height;
        public float NoiseScale;
        public float IsoLevel;
        public bool Lerp;
        public float NoiseFrequency;
        
        // DomainWarpSettings 
        public string WarpType; 
        public string WarpFractalTypes;
        public float WarpAmplitude;
        public int WarpSeed;
        public float WarpFrequency;
        public int WarpFractalOctaves;
        public float WarpFractalLacunarity;
        public float WarpFractalGain;
        public bool DomainWarp;
        
        // FractalSettings 
        public string FractalTypes;
        public int FractalOctaves;
        public float FractalLacunarity;
        public float FractalGain;
        public float FractalWeightedStrength;
    }
}


