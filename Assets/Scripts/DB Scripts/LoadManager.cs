using System.Collections.Generic;
using LiteNetLib;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class LoadManager : MonoBehaviour
{
    public Button button;
    public Dictionary<int, string> loadList = new();
    public DBManager db;
    List<int> keyList = new();

    void Start()
    {
        db = FindFirstObjectByType<DBManager>();
        loadList = db.responseList;
        List<string> list = new();

        foreach(KeyValuePair<int, string> entry in loadList)
        {
            list.Add(entry.Value);
        }
        while(list.Count < 4)
        {
            list.Add("Blank");
        }

        switch(button.name)
        {
            case "LoadOne":
                button.GetComponentInChildren<TMP_Text>().text = list[0];
                break;
            case "LoadTwo":
                button.GetComponentInChildren<TMP_Text>().text = list[1];
                break;
            case "LoadThree":
                button.GetComponentInChildren<TMP_Text>().text = list[2];
                break;
            case "LoadFour":
                button.GetComponentInChildren<TMP_Text>().text = list[3];
                break;
        }

        foreach(KeyValuePair<int, string> entry in loadList)
        {
            keyList.Add(entry.Key);
        }
    }

    public void LoadButton1()
    {
        Debug.Log("LoadButton1 code called");
        db.loadedTerrainId = keyList[0];
        db.retreiveTerrainData();
    }
    public void LoadButton2()
    {
        Debug.Log("LoadButton2 code called");
        db.loadedTerrainId = keyList[1];
        db.retreiveTerrainData();
    }
    public void LoadButton3()
    {
        Debug.Log("LoadButton3 code called");
        db.loadedTerrainId = keyList[2];
        db.retreiveTerrainData();
    }
    public void LoadButton4()
    {
        Debug.Log("LoadButton4 code called");
        db.loadedTerrainId = keyList[3];
        db.retreiveTerrainData();
    }
}
