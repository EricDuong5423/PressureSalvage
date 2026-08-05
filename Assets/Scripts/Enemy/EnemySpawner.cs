using UnityEngine;

public sealed class EnemySpawner : MonoBehaviour
{
    private EnemyPool enemyPool;
    private string enemyType;

    public string EnemyType =>
        enemyType;

    public int ActiveCount =>
        enemyPool?.CountActive ?? 0;

    public void Initialize(
        string typeId,
        EnemyPool pool)
    {
        enemyType = typeId;
        enemyPool = pool;
    }

    public PooledEnemy Spawn(
        Vector3 position,
        Quaternion rotation)
    {
        if (enemyPool == null)
        {
            Debug.LogError(
                $"{nameof(EnemySpawner)} '{name}' " +
                "chưa được Initialize.",
                this);

            return null;
        }

        return enemyPool.Spawn(
            position,
            rotation);
    }
}