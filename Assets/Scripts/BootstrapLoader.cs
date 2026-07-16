using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BootstrapLoader : MonoBehaviour
{
    [SerializeField] private string firstScene = "Submarine";

    private IEnumerator Start()
    {
        AsyncOperation operation =
            SceneManager.LoadSceneAsync(firstScene, LoadSceneMode.Single);

        while (!operation.isDone)
            yield return null;
    }
}