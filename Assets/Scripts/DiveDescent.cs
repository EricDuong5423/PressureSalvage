using UnityEngine;
using UnityEngine.SceneManagement;

public class DiveDescent : Interactable
{
    private bool transitioning;

    private void Start()
    {
        promptMessage = "Dive down";
    }

    protected override void Interact()
    {
        if (transitioning)
            return;

        GameProgressionManager progression =
            GameProgressionManager.Instance;

        if (progression == null ||
            !progression.TryBeginDive(out MapData map))
        {
            promptMessage = "Please select a Map";
            return;
        }

        transitioning = true;

        if (CameraFade.Instance != null)
        {
            CameraFade.Instance.TransitionTo(
                map.SceneName);
        }
        else
        {
            SceneManager.LoadScene(
                map.SceneName);
        }
    }
}