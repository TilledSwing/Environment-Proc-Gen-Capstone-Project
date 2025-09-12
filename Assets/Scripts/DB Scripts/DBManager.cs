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

        ChunkGenNetwork abc = FindFirstObjectByType<ChunkGenNetwork>();
        TerrainSettings settings = SeedSerializer.SerializeTerrainDensity(abc.terrainDensityData);
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
            }
            else
                Debug.Log("request failed: " + request.error + " response code: " + request.responseCode);

        }
        yield return StartCoroutine(SaveTerrainAssets(ManualAssetIdentification.PlacedAssets, loadedTerrainId));
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
                        ChunkGenNetwork cg = FindFirstObjectByType<ChunkGenNetwork>();
                        cg.UpdateClientMesh(response.data);
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
        yield return StartCoroutine(LoadTerrainAsset(2));

    }

    /// <summary>
    /// Once a terrains data has been loaded, this method is called to load in all of the users manually placed terrain assets.
    /// </summary>
    /// <param name="loadedTerrainId"></param>
    /// <returns></returns>
    IEnumerator LoadTerrainAsset(int loadedTerrainId)
    {
        ManualAssetIdentification.PlacedAssets.Clear();
        string url = "http://localhost/sqlconnect/loadAssets.php";
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
                    ManualAssetJSONList response = JsonUtility.FromJson<ManualAssetJSONList>(request.downloadHandler.text);
                    if (response.success && response.data != null)
                    {
                        foreach (ManualAssetJSON mas in response.data)
                        {
                            ManualAssetId assetId = (ManualAssetId)mas.AssetId;
                            GameObject obj = ManualAssetTracker.Create(assetId);
                            obj.transform.position = new Vector3(mas.xPos, mas.yPos, mas.zPos);
                            obj.transform.rotation = Quaternion.identity;
                            if (obj.GetComponent<Rigidbody>() == null)
                                obj.AddComponent<Rigidbody>();
                            ManualAssetIdentification asset = new(assetId, mas.xPos, mas.yPos, mas.zPos);
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
        public TerrainSettings data;
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


