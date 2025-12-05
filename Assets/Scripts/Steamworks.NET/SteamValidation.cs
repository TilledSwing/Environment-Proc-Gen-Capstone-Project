/**
This class will act as the controller for the steam authorization. A user must have their steam engine open for this to work. This class
will utilize Steam Works, whos script can be found in the Steamworks.NET folder under SteamManager.cs
*/
using System.Collections;
using Steamworks;
using UnityEngine;

public class SteamValidation : MonoBehaviour
{
    //Variables that will hold the users Steam Name and Steam ID
    public static ulong steamID { get; private set; }
    public static string steamProfileName { get; private set; }
    public static bool IsInitialized { get; private set; }

    /// <summary>
    /// On the initilization of the app this method will look for the steam manager and then retreive user information
    /// </summary>
    private  void Awake()
    {
        if (!SteamAPI.Init())
        {
            Debug.Log("Please open your steam engine");
            return;
        }
        steamProfileName = SteamFriends.GetPersonaName();
        steamID = SteamUser.GetSteamID().m_SteamID;
        IsInitialized = true;
        //Sanity print to ensure it is working
        Debug.Log("Logged in user is: " + steamProfileName + ", and you Steam ID is: " + steamID);

        DBManager db = GameObject.FindFirstObjectByType<DBManager>();
        db.checkRegisteredUser(steamID, steamProfileName);
        db.retreiveTerrainNames();
    }
}
