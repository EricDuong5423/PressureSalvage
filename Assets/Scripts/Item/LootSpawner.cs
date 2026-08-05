using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public enum SpawnShape
{
    Circle,
    Box
}

public class LootSpawner : MonoBehaviour
{
    [SerializeField] private LootProfile profile;
    [SerializeField] private float countMultiplier = 1f;
    
    [SerializeField] private float spawnRadius = 0f;
    [SerializeField] private SpawnShape spawnShape = SpawnShape.Circle;
    [SerializeField] private Vector2 boxSize = new Vector2(0f, 0f);
    [SerializeField] private Vector3 spawningOffset = new Vector3(0f, 0f, 0f);

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.limeGreen;
        switch (spawnShape)
        {
            case SpawnShape.Circle:
                Gizmos.DrawWireSphere(transform.position, spawnRadius);
                break;
            case SpawnShape.Box:
                Vector3 size = new Vector3(boxSize.x, 0, boxSize.y);
                Gizmos.DrawWireCube(transform.position, size);
                break;
            default:
                break; 
        }
    }

    private Vector3 FindValidNavMeshSpawnPoint(Vector3 center, float radius)
    {
        Vector3 randomPoint;

        switch (spawnShape)
        {
            case SpawnShape.Circle:
                randomPoint = center + Random.insideUnitSphere * radius;
                break;
            case SpawnShape.Box:
                float halfwidth = boxSize.x * 0.5f;
                float halfheight = boxSize.y * 0.5f;
                randomPoint = center + new Vector3(Random.Range(-halfwidth, halfwidth), 0f, Random.Range(-halfheight, halfheight));
                break;
            default:
                randomPoint = center;
                break;
        }

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        else
        {
            Debug.LogError("Can't find valid navmesh spawn point");
            return center;
        }
    }

    private void Start()
    {
        if (profile == null) return;
        int day = GameProgressionManager.Instance ? GameProgressionManager.Instance.Day : 1;
        int QuotaToday = GameProgressionManager.Instance ? GameProgressionManager.Instance.QuotaToday : 1;
        int count = Mathf.RoundToInt(profile.baseCount + profile.extraPerDay * (day - 1) * countMultiplier);

        for (int i = 0; i < count || QuotaToday > 0; i++)
        {
            GameObject prefab = PickItem(day);
            if (prefab == null) continue;
            Vector3 spawnPoint = FindValidNavMeshSpawnPoint(transform.position, spawnRadius);
            
            GameObject newItem = Instantiate(prefab, spawnPoint + spawningOffset, Quaternion.identity);

            CarryItem carryItem = newItem.GetComponent<CarryItem>();
            
            if (carryItem == null) continue;
            
            carryItem.SpawningItem();
            
            QuotaToday -= carryItem.Value;
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
        
        int r = Random.Range(0, total);
        if (total <= 0) return null;
        foreach (var e in profile.items)
        {
            if (e.prefab == null || e.minDay > day) continue;
            r -= e.weight;
            if (r < 0) return e.prefab;
        }
        
        return null;
    }
}
