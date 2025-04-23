using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadManager : MonoBehaviour
{
    public Button button;
    public Dictionary<int, string> loadList = new();
    public DBManager db;

    void Start()
    {
        db.retreiveTerrainNames();
        loadList = db.responseList;

        foreach(KeyValuePair<int, string> entry in loadList)
        {
            switch(button.name)
            {
                case "LoadOne":
                    button.GetComponentInChildren<TMP_Text>().text = entry.Value;
                    break;
                case "LoadTwo":
                    button.GetComponentInChildren<TMP_Text>().text = entry.Value;
                    break;
                case "LoadThree":
                    button.GetComponentInChildren<TMP_Text>().text = entry.Value;
                    break;
                case "LoadFour":
                    button.GetComponentInChildren<TMP_Text>().text = entry.Value;
                    break;
            }
        }
    }

    public void LoadTerrain()
    {
        foreach(KeyValuePair<int, string> entry in loadList)
        {
            switch(button.name)
            {
                case "LoadOne":
                    db.loadedTerrainId = entry.Key;
                    db.retreiveTerrainData();
                    break;
                case "LoadTwo":
                    db.loadedTerrainId = entry.Key;
                    db.retreiveTerrainData();
                    break;
                case "LoadThree":
                    db.loadedTerrainId = entry.Key;
                    db.retreiveTerrainData();
                    break;
                case "LoadFour":
                    db.loadedTerrainId = entry.Key;
                    db.retreiveTerrainData();
                    break;
            }
        }
    }
}
