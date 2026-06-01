using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyAnimator))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Data")] 
    public EnemyStats stats;
    
    [Header("Base References")]
    public Transform player;
    protected NavMeshAgent agent;

    [Header("Animator")] 
    protected EnemyAnimator anim;

    [Header("Vision Settings")] 
    public LayerMask obstacleMask;

    protected bool isPlayerInSight;

    protected void Start()
    {
        agent =  GetComponent<NavMeshAgent>();
        anim = GetComponent<EnemyAnimator>();

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }
    }

    protected abstract void PatrolBehavior();
    protected abstract void ChaseBehavior();
    protected abstract void AttackBehavior();

    private bool CheckVision()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > stats.viewRadius) return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        if (angleToPlayer > stats.viewAngle / 2f) return false;

        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f;
        Vector3 playerTarget = player.position + Vector3.up * 1.5f;

        if (Physics.Raycast(rayOrigin, directionToPlayer, distanceToPlayer, obstacleMask)) return false;
        return true;
    }

    protected void Update()
    {
        isPlayerInSight = CheckVision();

        if (isPlayerInSight)
        {
            ChaseBehavior();
        }
        else
        {
            PatrolBehavior();
        }
    }
}
