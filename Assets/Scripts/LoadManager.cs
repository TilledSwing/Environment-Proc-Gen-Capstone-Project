using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadManager : MonoBehaviour
{
    public Button button;
    public List loadList;

    void Start()
    {
        switch(button.name)
        {
            case "LoadOne":
                button.GetComponentInChildren<TMP_Text>().text = "ht";
                break;
            case "LoadTwo":
                button.GetComponentInChildren<TMP_Text>().text = "ht";
                break;
            case "LoadThree":
                button.GetComponentInChildren<TMP_Text>().text = "ht";
                break;
            case "LoadFour":
                button.GetComponentInChildren<TMP_Text>().text = "ht";
                break;
        }
    }
}
