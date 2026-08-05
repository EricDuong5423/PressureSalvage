using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class PooledEnemy : MonoBehaviour
{
    [SerializeField]
    private NavMeshAgent navMeshAgent;

    [SerializeField]
    private Animator animator;

    [SerializeField]
    private EnemyMovementController movementController;
    
    [SerializeField]
    private EnemySwimmingController  swimmingController;

    [SerializeField]
    private BehaviorGraphAgent behaviorGraphAgent;
    
    [SerializeField]
    private bool usingNavMeshAgent = true;

    private IEnemyAttack enemyAttack;

    private Action<PooledEnemy> releaseAction;

    private bool isSpawned;
    private bool releaseRequested;

    public bool UsingNavMeshAgent => usingNavMeshAgent;

    private void Awake()
    {
        if (navMeshAgent == null && usingNavMeshAgent)
        {
            navMeshAgent =
                GetComponent<NavMeshAgent>();
        }

        if (animator == null)
        {
            animator =
                GetComponent<Animator>();
        }

        if (movementController == null && usingNavMeshAgent)
        {
            movementController =
                GetComponent<EnemyMovementController>();
        }

        if (swimmingController == null && !usingNavMeshAgent)
        {
            swimmingController = GetComponent<EnemySwimmingController>();
        }

        if (behaviorGraphAgent == null)
        {
            behaviorGraphAgent =
                GetComponent<BehaviorGraphAgent>();
        }

        enemyAttack =
            GetComponent<IEnemyAttack>();
    }

    public void BindPool(
        Action<PooledEnemy> release)
    {
        releaseAction = release;
    }

    public void PrepareForSpawn(
        Vector3 position,
        Quaternion rotation)
    {
        releaseRequested = false;

        transform.SetPositionAndRotation(
            position,
            rotation);

        enemyAttack?.ResetState();

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    public void Activate()
    {
        gameObject.SetActive(true);

        if (navMeshAgent != null && usingNavMeshAgent)
        {
            navMeshAgent.enabled = true;

            if (navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.Warp(
                    transform.position);
            }
            else
            {
                Debug.LogError(
                    $"{name} được spawn ngoài NavMesh.",
                    this);
            }
        }

        if (usingNavMeshAgent)
        {
            movementController?.ResetState();
        }
        else
        {
            swimmingController?.ResetState();
        }

        isSpawned = true;

        behaviorGraphAgent?.Restart();
    }

    public void RequestDespawn()
    {
        if (!isSpawned ||
            releaseRequested)
        {
            return;
        }

        releaseRequested = true;

        if (releaseAction != null)
        {
            releaseAction.Invoke(this);
        }
        else
        {
            Debug.LogError(
                $"{name} không được bind vào EnemyPool.",
                this);
        }
    }

    public void PrepareForPool()
    {
        isSpawned = false;

        behaviorGraphAgent?.End();

        if (usingNavMeshAgent)
        {
            movementController?.Stop();
        }
        else
        {
            swimmingController?.Stop();
        }
        enemyAttack?.ResetState();

        if (navMeshAgent != null &&
            navMeshAgent.enabled && usingNavMeshAgent)
        {
            if (navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
            }

            navMeshAgent.enabled = false;
        }

        gameObject.SetActive(false);
    }
}