using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingManager : MonoBehaviour
{
    public static EndingManager Instance { get; private set; }

    [SerializeField] private string trappedEndingScene = "Trap";
    [SerializeField] private string escapeEndingScene = "Escape";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ChangeTrapEndingScene()
    {
        GameProgressionManager.Instance.ResetNewRun();
        if (trappedEndingScene == null) return;
        SceneManager.LoadScene(trappedEndingScene);
    }

    public void ChangeEscapeEndingScene()
    {
        GameProgressionManager.Instance.ResetNewRun();
        if (escapeEndingScene == null) return;
        SceneManager.LoadScene(escapeEndingScene);
    }
}
