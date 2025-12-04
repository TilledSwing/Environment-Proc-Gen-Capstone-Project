using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour {


    public void LoadScene(string sceneName) {
        SceneManager.LoadScene(sceneName);
        Debug.Log("next scene loaded");
    }

    public void QuitApp() {
        Application.Quit();
        Debug.Log("app quit");
    }

    void Start()
    {
        Screen.fullScreen = true;
    }
}
