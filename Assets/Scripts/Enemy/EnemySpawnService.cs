using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(GameRoot))]
public sealed class EnemySpawnService : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        TryInstallForScene(
            SceneManager.GetActiveScene());
    }

    private void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode loadMode)
    {
        TryInstallForScene(scene);
    }

    private void TryInstallForScene(
        Scene scene)
    {
        GameProgressionManager progression =
            GameProgressionManager.Instance;

        if (progression == null)
            return;

        MapData activeMap =
            progression.ActiveDiveMap;

        if (activeMap == null)
            return;

        if (!string.Equals(
                scene.name,
                activeMap.SceneName,
                StringComparison.Ordinal))
        {
            return;
        }

        if (activeMap.EnemySpawnProfile == null)
        {
            Debug.LogWarning(
                $"Map '{activeMap.name}' chưa có " +
                "EnemySpawnProfile.");

            return;
        }

        MapEnemySpawnDirector director =
            FindDirectorInScene(scene);

        if (director == null)
        {
            var runtimeRoot =
                new GameObject(
                    "[Runtime] Enemy Spawning");

            SceneManager.MoveGameObjectToScene(
                runtimeRoot,
                scene);

            director =
                runtimeRoot.AddComponent<
                    MapEnemySpawnDirector>();
        }

        director.Initialize(
            activeMap.EnemySpawnProfile,
            progression.Day);
    }

    private static MapEnemySpawnDirector
        FindDirectorInScene(Scene scene)
    {
        GameObject[] roots =
            scene.GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            MapEnemySpawnDirector director =
                root.GetComponentInChildren<
                    MapEnemySpawnDirector>(true);

            if (director != null)
                return director;
        }

        return null;
    }
}