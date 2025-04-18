/**
This class will act as the controller for the steam authorization. A user must have their steam engine open for this to work. This class
will utilize Steam Works, whos script can be found in the Steamworks.NET folder under SteamManager.cs
*/
using System.Collections;
using Steamworks;
using UnityEngine;

public class SteamValidation
{
    //Variables that will hold the users Steam Name and Steam ID
    public static ulong steamID { get; private set; }
    public static string steamProfileName { get; private set; }
    public static bool IsInitialized { get; private set; }

    /// <summary>
    /// On the initilization of the app this method will look for the steam manager and then retreive user information
    /// </summary>

    [RuntimeInitializeOnLoadMethod]
    [System.Obsolete]
    public static void OnRuntimeMethodLoad()
    {
        //Checks if the steam manager is open. If it is not it will simply return
        if (!SteamManager.Initialized)
        {
            Debug.Log("Your steam engine is not open, please open it and re-run the program to utilize steam functionality.");
            return; //Ensure steam is open
        }

        //Retreive user information
        steamProfileName = SteamFriends.GetPersonaName();
        CSteamID csteamID = SteamUser.GetSteamID();
        steamID = csteamID.m_SteamID;
        IsInitialized = true;
        //Sanity print to ensure it is working
        Debug.Log("Logged in user is: " + steamProfileName + ", and you Steam ID is: " + steamID);

        DBManager db = GameObject.FindObjectOfType<DBManager>();
        db.checkRegisteredUser(steamID, steamProfileName);
    }
}
