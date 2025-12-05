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
    public GameObject loadPanel;
    List<int> keyList = new();

    public void PopulateList()
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
        int i = 0;
        foreach (Transform child in loadPanel.transform)
        {
            TMP_Text buttonText = child.GetComponentInChildren<TMP_Text>();
            if (buttonText != null && i < 4)
            {
                buttonText.text = list[i];
                i++;
            }
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
