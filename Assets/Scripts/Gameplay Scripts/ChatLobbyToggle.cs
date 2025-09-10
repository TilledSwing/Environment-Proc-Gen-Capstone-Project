using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChatLobbyToggle : MonoBehaviour
{
    public Canvas[] chatLobbyElements;
    private bool toggled;

    void Start()
    {
        toggled = true;
    }

    // Update is called once per frame
    void Update()
    {
        // Block input if in a chat message block. Ensures that typing words with certain letters or numbers won't trigger input events.
        if (EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null)
        {
            return;
        }

        // Toggle the chat and lobby on / off using the 'T' key. 
        if (Input.GetKeyDown(KeyCode.T))
        {
            toggled = !toggled;
            foreach (Canvas canvasElement in chatLobbyElements)
            {
                CanvasGroup cg = canvasElement.GetComponent<CanvasGroup>();
                cg.alpha = toggled ? 1f : 0f;
                cg.interactable = toggled;
                cg.blocksRaycasts = toggled;
            }
            
        }
    }
}
