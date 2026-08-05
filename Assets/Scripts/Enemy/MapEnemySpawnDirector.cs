using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MapEnemySpawnDirector : MonoBehaviour
{
    [Header("Direct scene testing")]

    [Tooltip(
        "Chỉ dùng khi Play thẳng gameplay scene " +
        "mà không đi qua Bootstrap/Submarine.")]
    [SerializeField]
    private EnemySpawnProfile editorFallbackProfile;

    [SerializeField, Min(1)]
    private int editorFallbackDay = 1;

    private readonly
        Dictionary<EnemySpawnEntry, EnemySpawner>
        spawners = new();

    private readonly List<Vector3>
        occupiedPositions = new();

    private EnemySpawnProfile profile;
    private int day;

    private bool initialized;
    private bool spawnStarted;

    public void Initialize(
        EnemySpawnProfile spawnProfile,
        int currentDay)
    {
        if (spawnStarted)
        {
            Debug.LogWarning(
                $"{nameof(MapEnemySpawnDirector)} " +
                "đã bắt đầu spawn.",
                this);

            return;
        }

        profile = spawnProfile;
        day = Mathf.Max(1, currentDay);
        initialized = profile != null;
    }

    private IEnumerator Start()
    {
        if (!initialized &&
            editorFallbackProfile != null)
        {
            Initialize(
                editorFallbackProfile,
                editorFallbackDay);
        }

        if (!initialized)
        {
            Debug.LogWarning(
                $"{nameof(MapEnemySpawnDirector)} " +
                "không có EnemySpawnProfile.",
                this);

            yield break;
        }

        // Chờ NavMeshSurface và các component
        // trong scene hoàn thành Awake/Start.
        yield return null;

        spawnStarted = true;

        SpawnInitialPopulation();
    }

    private void SpawnInitialPopulation()
    {
        List<EnemySpawnEntry> eligible =
            GetEligibleEntries();

        if (eligible.Count == 0)
        {
            Debug.LogWarning(
                $"{profile.name}: không có enemy " +
                $"hợp lệ ở Day {day}.",
                this);

            return;
        }

        CreateRuntimeSpawners(eligible);

        Vector3? protectedPosition =
            GetProtectedPosition();

        var pointProvider =
            new NavMeshRandomPointProvider(
                profile.NavMeshAreaMask,
                protectedPosition);

        if (!pointProvider.IsValid)
        {
            Debug.LogError(
                $"{nameof(MapEnemySpawnDirector)} " +
                "không tìm thấy NavMesh đã bake.",
                this);

            return;
        }

        if (protectedPosition.HasValue &&
            !pointProvider.HasReachabilityOrigin)
        {
            Debug.LogWarning(
                "Không tìm thấy NavMesh gần Player Spawn. " +
                "Reachability check sẽ bị bỏ qua.",
                this);
        }

        Dictionary<EnemySpawnEntry, int> plan =
            EnemySpawnPlanBuilder.Build(
                profile,
                day);

        int plannedCount = 0;
        int spawnedCount = 0;

        foreach (EnemySpawnEntry entry in eligible)
        {
            if (!plan.TryGetValue(
                    entry,
                    out int count))
            {
                continue;
            }

            plannedCount += count;

            if (!spawners.TryGetValue(
                    entry,
                    out EnemySpawner spawner))
            {
                continue;
            }

            for (int i = 0; i < count; i++)
            {
                bool found =
                    pointProvider.TryFindPosition(
                        protectedPosition,
                        occupiedPositions,
                        profile.MinimumDistanceFromPlayerSpawn,
                        profile.MinimumDistanceBetweenEnemies,
                        profile.PlacementAttemptsPerEnemy,
                        out Vector3 position);

                if (!found)
                {
                    Debug.LogWarning(
                        $"Không tìm được vị trí cho " +
                        $"'{entry.RuntimeId}'.",
                        this);

                    continue;
                }

                Quaternion rotation =
                    Quaternion.Euler(
                        0f,
                        Random.Range(0f, 360f),
                        0f);

                PooledEnemy enemy =
                    spawner.Spawn(
                        position,
                        rotation);

                if (enemy == null)
                    continue;

                occupiedPositions.Add(position);
                spawnedCount++;
            }
        }

        Debug.Log(
            $"{name}: spawned {spawnedCount}/" +
            $"{plannedCount} enemies for Day {day}.",
            this);
    }

    private List<EnemySpawnEntry>
        GetEligibleEntries()
    {
        var result =
            new List<EnemySpawnEntry>();

        foreach (EnemySpawnEntry entry in profile.Enemies)
        {
            if (entry == null ||
                !entry.CanSpawnOnDay(day))
            {
                continue;
            }

            result.Add(entry);
        }

        return result;
    }

    private void CreateRuntimeSpawners(
        IReadOnlyList<EnemySpawnEntry> entries)
    {
        foreach (EnemySpawnEntry entry in entries)
        {
            var typeRoot =
                new GameObject(
                    entry.RuntimeId);

            typeRoot.transform.SetParent(
                transform,
                false);

            var instances =
                new GameObject("Instances");

            instances.transform.SetParent(
                typeRoot.transform,
                false);

            EnemyPool pool =
                typeRoot.AddComponent<EnemyPool>();

            pool.Configure(
                entry.Prefab,
                instances.transform,
                Mathf.Max(
                    1,
                    entry.PrewarmCount),
                entry.PoolMaximumSize);

            pool.Prewarm(
                entry.PrewarmCount);

            EnemySpawner spawner =
                typeRoot.AddComponent<EnemySpawner>();

            spawner.Initialize(
                entry.RuntimeId,
                pool);

            spawners.Add(
                entry,
                spawner);
        }
    }

    private static Vector3?
        GetProtectedPosition()
    {
        PlayerSpawner playerSpawner =
            FindFirstObjectByType<PlayerSpawner>();

        if (playerSpawner == null)
            return null;

        if (playerSpawner.CurrentPlayer != null)
        {
            return playerSpawner
                .CurrentPlayer
                .transform
                .position;
        }

        return playerSpawner.SpawnPosition;
    }
}