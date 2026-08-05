using System.Collections.Generic;
using UnityEngine;

public static class EnemySpawnPlanBuilder
{
    public static Dictionary<EnemySpawnEntry, int> Build(EnemySpawnProfile profile, int day)
    {
        var result = new Dictionary<EnemySpawnEntry, int>();
        
        if (profile == null)
            return result;

        var eligible = new List<EnemySpawnEntry>();

        foreach (EnemySpawnEntry entry in profile.Enemies)
        {
            if (entry == null || !entry.CanSpawnOnDay(day))
                continue;
            
            eligible.Add(entry);
            result.Add(entry, 0);
        }

        int remaining = profile.GetEnemyCountForDay(day);

        foreach (EnemySpawnEntry entry in eligible)
        {
            if (remaining <= 0)
                break;

            int amount = Mathf.Min(entry.GuaranteedCount, entry.MaximumAlive);
            
            amount = Mathf.Min(amount, remaining);

            result[entry] += amount;
            remaining -= amount;
        }

        while (remaining > 0)
        {
            EnemySpawnEntry selected = PickWeightedEntry(eligible, result, day);

            if (selected == null)
                break;

            result[selected]++;
            remaining--;
        }

        if (remaining > 0)
        {
            Debug.LogWarning(
                $"{profile.name}: MaximumAlive của các entry " +
                $"không đủ chứa toàn bộ population. " +
                $"Thiếu {remaining} enemy.");
        }

        return result; 
    }
    
    private static EnemySpawnEntry PickWeightedEntry(
        IReadOnlyList<EnemySpawnEntry> eligible,
        IReadOnlyDictionary<EnemySpawnEntry, int> counts,
        int day)
    {
        float totalWeight = 0f;

        foreach (EnemySpawnEntry entry in eligible)
        {
            if (counts[entry] >= entry.MaximumAlive)
                continue;

            totalWeight +=
                entry.GetWeightForDay(day);
        }

        if (totalWeight <= 0f)
            return null;

        float roll =
            Random.Range(0f, totalWeight);

        foreach (EnemySpawnEntry entry in eligible)
        {
            if (counts[entry] >= entry.MaximumAlive)
                continue;

            roll -= entry.GetWeightForDay(day);

            if (roll <= 0f)
                return entry;
        }

        return null;
    }
}
