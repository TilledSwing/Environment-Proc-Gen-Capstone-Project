using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Networking;
/// <summary>
/// This class will act as a manager script that will facilitate all DB operations
/// </summary>
public class DBManager : MonoBehaviour
{
    private int loadedTerrainId = 1;
    /// <summary>
    /// Starts the DB process to determine if a user is registered or not
    /// </summary>
    /// <param name="steamId"></param>
    /// <param name="steamName"></param>
    public void checkRegisteredUser(ulong steamId, string steamName){
        Debug.Log("In the checkRegistration with values being: " + steamId + " , " + steamName);
        StartCoroutine(VerifyRegisteredUser(steamId, steamName));
    }

    /// <summary>
    /// Starts the DB retrieval of the users created terrains
    /// </summary>
    public void retreiveTerrainNames(){
        StartCoroutine(RetrieveTerrainNames(SteamValidation.steamID));
    }

    /// <summary>
    /// Starts the DB process to save a created terrain
    /// </summary>
    public void retreiveTerrainData(){
        StartCoroutine(LoadTerrainData());
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
                        // Process your data here
                        foreach (var item in response.data)
                        {
                            Debug.Log($"Seed: {item.Seed}, Width: {item.Width}, Height: {item.Height}, NoiseScale: {item.NoiseScale}, IsoLevel: {item.IsoLevel}, Lerp: {item.Lerp}");
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
        public int Seed;
        public int Width;
        public int Height;
        public float NoiseScale;
        public float IsoLevel;
        public bool Lerp;
    }
}


