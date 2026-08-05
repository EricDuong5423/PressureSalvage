using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "EnemySpawnProfile", menuName = "Abyssal/Enemy Spawn Profile")]
public class EnemySpawnProfile : ScriptableObject
{
    [Header("Population by day")] 
    [SerializeField, Min(0)] private int baseEnemyCount = 3;
    [SerializeField, Min(0f)] private float extraEnemiesPerDay = 1f;
    [SerializeField, Min(0)] private int maximumEnemyCount = 20;
    
    [Header("NavMeshReplacement")]
   [SerializeField]
    private int navMeshAreaMask = NavMesh.AllAreas;

    [SerializeField, Min(0f)]
    private float minimumDistanceFromPlayerSpawn = 12f;

    [SerializeField, Min(0f)]
    private float minimumDistanceBetweenEnemies = 3f;

    [SerializeField, Min(1)]
    private int placementAttemptsPerEnemy = 24;

    [Header("Enemy types")]

    [SerializeField]
    private List<EnemySpawnEntry> enemies = new();

    public IReadOnlyList<EnemySpawnEntry> Enemies => enemies;

    public int NavMeshAreaMask => navMeshAreaMask;

    public float MinimumDistanceFromPlayerSpawn =>
        minimumDistanceFromPlayerSpawn;

    public float MinimumDistanceBetweenEnemies =>
        minimumDistanceBetweenEnemies;

    public int PlacementAttemptsPerEnemy =>
        Mathf.Max(1, placementAttemptsPerEnemy);

    public int GetEnemyCountForDay(int day)
    {
        int safeDay = Mathf.Max(1, day);

        int count = Mathf.RoundToInt(
            baseEnemyCount +
            extraEnemiesPerDay * (safeDay - 1));

        return Mathf.Clamp(
            count,
            0,
            Mathf.Max(1, maximumEnemyCount));
    }

    private void OnValidate()
    {
        baseEnemyCount = Mathf.Max(0, baseEnemyCount);
        extraEnemiesPerDay = Mathf.Max(0f, extraEnemiesPerDay);
        maximumEnemyCount = Mathf.Max(1, maximumEnemyCount);
        placementAttemptsPerEnemy =
            Mathf.Max(1, placementAttemptsPerEnemy);

        if (enemies == null)
            return;

        foreach (EnemySpawnEntry entry in enemies)
            entry?.Validate();
    }
}

[Serializable]
public sealed class EnemySpawnEntry
{
    [Header("Identity")]

    [SerializeField]
    private string id;

    [SerializeField]
    private PooledEnemy prefab;

    [Header("Day conditions")]

    [SerializeField, Min(1)]
    private int firstDay = 1;

    [SerializeField, Min(1)]
    private int lastDay = 10;

    [Header("Population")]

    [Tooltip("Số lượng được spawn trước khi random theo Weight.")]
    [SerializeField, Min(0)]
    private int guaranteedCount;

    [Tooltip("Trọng số tại First Day.")]
    [SerializeField, Min(0f)]
    private float spawnWeight = 1f;

    [Tooltip("Weight cộng thêm sau mỗi ngày.")]
    [SerializeField, Min(0f)]
    private float weightGrowthPerDay;

    [SerializeField, Min(1)]
    private int maximumAlive = 10;

    [Header("Pool")]

    [SerializeField, Min(0)]
    private int prewarmCount = 3;

    [SerializeField, Min(1)]
    private int poolMaximumSize = 10;

    public string RuntimeId
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(id))
                return id;

            return prefab != null
                ? prefab.name
                : "Missing Enemy";
        }
    }

    public PooledEnemy Prefab => prefab;

    public int GuaranteedCount =>
        Mathf.Max(0, guaranteedCount);

    public int MaximumAlive =>
        Mathf.Max(1, maximumAlive);

    public int PrewarmCount =>
        Mathf.Clamp(
            prewarmCount,
            0,
            PoolMaximumSize);

    public int PoolMaximumSize =>
        Mathf.Max(
            Mathf.Max(1, poolMaximumSize),
            MaximumAlive);

    public bool CanSpawnOnDay(int day)
    {
        return prefab != null &&
               day >= firstDay &&
               day <= lastDay;
    }

    public float GetWeightForDay(int day)
    {
        int elapsedDays =
            Mathf.Max(0, day - firstDay);

        return Mathf.Max(
            0f,
            spawnWeight +
            weightGrowthPerDay * elapsedDays);
    }

    internal void Validate()
    {
        firstDay = Mathf.Max(1, firstDay);
        lastDay = Mathf.Max(firstDay, lastDay);

        guaranteedCount =
            Mathf.Max(0, guaranteedCount);

        spawnWeight =
            Mathf.Max(0f, spawnWeight);

        weightGrowthPerDay =
            Mathf.Max(0f, weightGrowthPerDay);

        maximumAlive =
            Mathf.Max(1, maximumAlive);

        poolMaximumSize =
            Mathf.Max(maximumAlive, poolMaximumSize);

        prewarmCount =
            Mathf.Clamp(
                prewarmCount,
                0,
                poolMaximumSize);
    }
}
