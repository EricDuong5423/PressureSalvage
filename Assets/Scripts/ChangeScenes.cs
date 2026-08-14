using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScenes : MonoBehaviour
{
    [SerializeField] private AudioClip musicToChange;
    public void ChangeScene(string scene)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
    }

    public void ChangeSceneWithMusic(string scene)
    {
        if (string.IsNullOrWhiteSpace(scene))
        {
            Debug.LogError("Scene name is empty.");
            return;
        }
        
        if (scene != "MainMenu")
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        Time.timeScale = 1f;

        AudioManager.Instance?.PlayMusic(musicToChange);
        SceneManager.LoadScene(scene);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void ChangeSceneIndex(int scene)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
    }
}
