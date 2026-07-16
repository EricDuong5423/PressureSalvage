using UnityEngine;

public class EnemyAgentContext : MonoBehaviour
{
    [Header("Data")] 
    [SerializeField] private EnemyStats stats;

    [Header("References")] 
    [SerializeField] private PlayerSpawner playerSpawner;
    
    public EnemyStats Stats => stats;

    public bool TryGetSpawnedPlayer(out GameObject player)
    {
        if (playerSpawner == null)
        {
            playerSpawner = FindFirstObjectByType<PlayerSpawner>();
        }

        player = playerSpawner != null ? playerSpawner.CurrentPlayer : null;
        return player != null && player.activeInHierarchy;
    }

    public bool IsInAttackRange(GameObject target)
    {
        if (target == null || stats == null) return false;
        
        float sqrtDst = (target.transform.position - transform.position).sqrMagnitude;

        return sqrtDst <= stats.attackRange * stats.attackRange;
    }
}
