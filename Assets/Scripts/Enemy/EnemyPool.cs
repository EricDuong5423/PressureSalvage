using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public sealed class EnemyPool : MonoBehaviour
{
    [SerializeField]
    private PooledEnemy prefab;

    [SerializeField]
    private Transform container;

    [SerializeField, Min(1)]
    private int defaultCapacity = 10;

    [SerializeField, Min(1)]
    private int maxSize = 30;

    private ObjectPool<PooledEnemy> pool;

    public int CountActive =>
        pool?.CountActive ?? 0;

    public int CountInactive =>
        pool?.CountInactive ?? 0;

    private void Awake()
    {
        if (prefab != null)
            InitializePool();
    }

    public void Configure(
        PooledEnemy enemyPrefab,
        Transform instanceContainer,
        int initialCapacity,
        int maximumSize)
    {
        if (pool != null)
        {
            Debug.LogError(
                $"{nameof(EnemyPool)} '{name}' " +
                "đã được khởi tạo.",
                this);

            return;
        }

        if (enemyPrefab == null)
        {
            Debug.LogError(
                $"{nameof(EnemyPool)} '{name}' " +
                "không có enemy prefab.",
                this);

            return;
        }

        prefab = enemyPrefab;

        container =
            instanceContainer != null
                ? instanceContainer
                : transform;

        maxSize =
            Mathf.Max(1, maximumSize);

        defaultCapacity =
            Mathf.Clamp(
                initialCapacity,
                1,
                maxSize);

        InitializePool();
    }

    public void Prewarm(int count)
    {
        if (!EnsureInitialized())
            return;

        int amount =
            Mathf.Clamp(
                count,
                0,
                maxSize);

        var instances =
            new List<PooledEnemy>(amount);

        for (int i = 0; i < amount; i++)
        {
            instances.Add(pool.Get());
        }

        foreach (PooledEnemy enemy in instances)
        {
            pool.Release(enemy);
        }
    }

    public PooledEnemy Spawn(
        Vector3 position,
        Quaternion rotation)
    {
        if (!EnsureInitialized())
            return null;

        PooledEnemy enemy =
            pool.Get();

        enemy.PrepareForSpawn(
            position,
            rotation);

        enemy.Activate();

        return enemy;
    }

    public void Release(
        PooledEnemy enemy)
    {
        if (enemy == null ||
            pool == null)
        {
            return;
        }

        pool.Release(enemy);
    }

    private void InitializePool()
    {
        if (pool != null)
            return;

        if (prefab == null)
            return;

        if (container == null)
            container = transform;

        maxSize =
            Mathf.Max(1, maxSize);

        defaultCapacity =
            Mathf.Clamp(
                defaultCapacity,
                1,
                maxSize);

        pool = new ObjectPool<PooledEnemy>(
            CreateEnemy,
            OnTakeFromPool,
            OnReturnToPool,
            OnDestroyEnemy,
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize);
    }

    private bool EnsureInitialized()
    {
        if (pool != null)
            return true;

        InitializePool();

        if (pool != null)
            return true;

        Debug.LogError(
            $"{nameof(EnemyPool)} '{name}' " +
            "chưa được Configure.",
            this);

        return false;
    }

    private PooledEnemy CreateEnemy()
    {
        PooledEnemy enemy =
            Instantiate(
                prefab,
                container);

        enemy.BindPool(Release);

        enemy.gameObject.SetActive(false);

        return enemy;
    }

    private static void OnTakeFromPool(
        PooledEnemy enemy)
    {
        // Spawn() sẽ tự đặt position
        // và kích hoạt enemy.
    }

    private static void OnReturnToPool(
        PooledEnemy enemy)
    {
        enemy.PrepareForPool();
    }

    private static void OnDestroyEnemy(
        PooledEnemy enemy)
    {
        if (enemy != null)
        {
            Object.Destroy(
                enemy.gameObject);
        }
    }

    private void OnDestroy()
    {
        pool?.Clear();
    }
}