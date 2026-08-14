using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

[Serializable]
public class TrapSettings
{
    public Trap trap;
    
    [Header("Availability")]
    [Min(1)] public int firstDay = 1;
    [Min(1)] public int lastDay = 10;
    [Min(0)] public int minimumLootValue;

    [Header("Population")]
    [Min(0)] public int maximumPerDay = 3;
    [Min(0)] public int weight = 1;

    public bool CanSpawn(int day, int lootValue, int spawnedCount)
    {
        return trap != null &&
               day >= firstDay &&
               day <= lastDay &&
               lootValue >= minimumLootValue &&
               spawnedCount < maximumPerDay &&
               weight > 0;
    }
}

public class TrapSpawner : MonoBehaviour
{
    [Header("Daily population")]
    [SerializeField, Min(0)] private int baseTrapLimit = 2;
    [SerializeField, Min(0)] private int extraTrapLimitPerDay = 1;
    [SerializeField, Min(0)] private int maximumTrapLimit = 8;

    [Header("Risk per loot")]
    [SerializeField, Range(0f, 1f)] private float baseChance = 0.05f;
    [SerializeField, Range(0f, 1f)] private float chancePerDay = 0.03f;
    [SerializeField, Range(0f, 1f)] private float valueRiskBonus = 0.25f;

    [Tooltip("Loot nhỏ hơn giá trị này gần như không tăng risk.")]
    [SerializeField, Min(0)] private int lowValue = 100;

    [Tooltip("Loot bằng hoặc lớn hơn giá trị này nhận toàn bộ value risk bonus.")]
    [SerializeField, Min(1)] private int highValue = 2000;

    [Header("Placement")]
    [SerializeField, Min(0.1f)] private float radiusAroundLoot = 3f;
    [SerializeField, Min(1)] private int placementAttempts = 10;
    [SerializeField, Min(0f)] private float minimumTrapSpacing = 4f;

    [Header("Trap types")]
    [SerializeField] private List<TrapSettings> traps = new();

    private readonly Dictionary<TrapSettings, int> trapCounts = new();
    private readonly List<Vector3> spawnedPositions = new();

    private int currentDay;
    private int dailyTrapLimit;
    private int spawnedTrapCount;

    private void Awake()
    {
        foreach (TrapSettings setting in traps)
        {
            if (setting != null)
                trapCounts[setting] = 0;
        }
    }

    public bool TrySpawnTrapForLoot(
        Vector3 lootPosition,
        int lootValue,
        int day)
    {
        InitializeDay(day);

        if (spawnedTrapCount >= dailyTrapLimit)
            return false;

        float valueRatio = Mathf.InverseLerp(
            lowValue,
            highValue,
            lootValue);

        float chance = baseChance +
                       chancePerDay * (currentDay - 1) +
                       valueRiskBonus * valueRatio;

        chance = Mathf.Clamp01(chance);

        if (Random.value > chance)
            return false;

        TrapSettings setting = PickTrap(lootValue);

        if (setting == null)
            return false;

        if (!TryFindPositionNearLoot(
                lootPosition,
                out Vector3 trapPosition))
        {
            return false;
        }

        Instantiate(
            setting.trap,
            trapPosition,
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

        trapCounts[setting]++;
        spawnedTrapCount++;
        spawnedPositions.Add(trapPosition);

        return true;
    }
    
    private void InitializeDay(int day)
    {
        day = Mathf.Max(1, day);

        if (currentDay == day)
            return;

        currentDay = day;
        spawnedTrapCount = 0;
        spawnedPositions.Clear();

        foreach (TrapSettings setting in traps)
        {
            if (setting != null)
                trapCounts[setting] = 0;
        }

        dailyTrapLimit = Mathf.Clamp(
            baseTrapLimit +
            extraTrapLimitPerDay * (currentDay - 1),
            0,
            maximumTrapLimit);
    }

    private TrapSettings PickTrap(int lootValue)
    {
        int totalWeight = 0;

        foreach (TrapSettings setting in traps)
        {
            if (setting == null)
                continue;

            trapCounts.TryGetValue(setting, out int count);

            if (!setting.CanSpawn(
                    currentDay,
                    lootValue,
                    count))
            {
                continue;
            }

            totalWeight += setting.weight;
        }

        if (totalWeight <= 0)
            return null;

        int roll = Random.Range(0, totalWeight);

        foreach (TrapSettings setting in traps)
        {
            if (setting == null)
                continue;

            trapCounts.TryGetValue(setting, out int count);

            if (!setting.CanSpawn(
                    currentDay,
                    lootValue,
                    count))
            {
                continue;
            }

            roll -= setting.weight;

            if (roll < 0)
                return setting;
        }

        return null;
    }

    private bool TryFindPositionNearLoot(
        Vector3 lootPosition,
        out Vector3 position)
    {
        for (int i = 0; i < placementAttempts; i++)
        {
            Vector2 offset =
                Random.insideUnitCircle * radiusAroundLoot;

            Vector3 candidate = lootPosition + new Vector3(
                offset.x,
                0f,
                offset.y);

            if (!NavMesh.SamplePosition(
                    candidate,
                    out NavMeshHit hit,
                    2f,
                    NavMesh.AllAreas))
            {
                continue;
            }

            if (IsTooCloseToTrap(hit.position))
                continue;

            position = hit.position;
            return true;
        }

        position = default;
        return false;
    }

    private bool IsTooCloseToTrap(Vector3 candidate)
    {
        float minimumDistanceSqr =
            minimumTrapSpacing * minimumTrapSpacing;

        foreach (Vector3 existing in spawnedPositions)
        {
            if ((existing - candidate).sqrMagnitude <
                minimumDistanceSqr)
            {
                return true;
            }
        }

        return false;
    }
}
