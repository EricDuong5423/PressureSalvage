using System;
using System.Collections.Generic;
using UnityEngine;

public class LootSpawner : MonoBehaviour
{
    [SerializeField] private LootProfile profile;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float countMultiplier = 1f;

    private void Start()
    {
        if (profile == null || spawnPoints.Length == 0) return;

        int day = GameProgressionManager.Instance ? GameProgressionManager.Instance.Day : 1;
        int count = Mathf.RoundToInt(profile.baseCount + profile.extraPerDay * (day - 1) * countMultiplier);
        count = Mathf.Clamp(count, 0, spawnPoints.Length);
        
        List<Transform> points = new (spawnPoints);
        for (int i = 0; i < count; i++)
        {
            int j = UnityEngine.Random.Range(i, points.Count);
            (points[i], points[j]) = (points[j], points[i]);
            GameObject prefab = PickItem(day);
            if(prefab != null) Instantiate(prefab, points[i].position, points[i].rotation);
        }
    }

    private GameObject PickItem(int day)
    {
        int total = 0;
        foreach (var e in profile.items)
        {
            if (e.prefab != null && e.minDay <= day)
            {
                total += e.weight;
            }
        }

        int r = UnityEngine.Random.Range(0, total);
        if (total <= 0) return null;
        foreach (var e in profile.items)
        {
            if (e.prefab == null || e.minDay > day) return null;
            r -= e.weight;
            if (r < 0) return e.prefab;
        }

        return null;
    }
}
