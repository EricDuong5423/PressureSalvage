using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScenes : MonoBehaviour
{
    [SerializeField] private AudioClip musicToChange;
    public void ChangeScene(string scene)
    {
        SceneManager.LoadScene(scene);
    }

    public void ChangeSceneWithMusic(string scene)
    {
        var audio = AudioManager.Instance;
        if (audio == null) return;
        if (musicToChange == null) return;
        SceneManager.LoadScene(scene);
        audio.PlayMusic(musicToChange);
    }

    public void ChangeSceneIndex(int scene)
    {
        SceneManager.LoadScene(scene);
    }
}
