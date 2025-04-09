using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Networking;
/// <summary>
/// This class will act as a manager script that will facilitate all DB operations
/// </summary>
public class DBManager : MonoBehaviour
{
    public void checkRegisteredUser(ulong steamId, string steamName){
        Debug.Log("In the checkRegistration with values being: " + steamId + " , " + steamName);
        StartCoroutine(VerifyRegisteredUser(steamId, steamName));
    }
    /// <summary>
    /// Called once the user selects to save a terrain
    /// </summary>
    public void saveTerrain(){
        StartCoroutine(SaveTerrain());
    }

    // Update is called once per frame
    void Update()
    {
        
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
       
        string url = "http://localhost/sqlconnect/validateUser.php";
        WWWForm form = new();
        form.AddField("SteamId", steamId.ToString());
        form.AddField("SteamName", steamName);
        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            // Set timeout (in seconds)
            request.timeout = 10;
            
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log("Raw Response: " + request.downloadHandler.text);
            else
                Debug.Log("request failed: " + request.error + " response code: " + request.responseCode);
        }
    }

    IEnumerator SaveTerrain() //int seed, int width, int height, float noiseScale, float isolevel, bool lerp
    {
        string url = "http://localhost/sqlconnect/saveTerrain.php";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Set timeout (in seconds)
            request.timeout = 10;
            
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try 
                {
                    // Parse the JSON response
                    PHPResponse response = JsonUtility.FromJson<PHPResponse>(request.downloadHandler.text);
                    if (response.success)
                    {
                        Debug.Log("PHP Success: " + response.message);
                        
                        // Process your data here
                        foreach (var item in response.data)
                        {
                            Debug.Log($"ID: {item.ID}, Name: {item.Name}");
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
}


