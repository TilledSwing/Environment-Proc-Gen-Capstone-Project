using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class PauseToggle : MonoBehaviour
{
    public GameObject pauseMenu;
    private bool toggled;

    void Start()
    {
        toggled = false;
    }

    // Update is called once per frame
    void Update()
    {
        // Block input if pause menu is up
        if (EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null)
        {
            return;
        }

        // Toggle the chat and lobby on / off using the 'T' key. 
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            toggled = !toggled;
            pauseMenu.SetActive(toggled);         
        }
    }
}