using UnityEngine;
using UnityEngine.AI;

public enum EnemyMovementProgress
{
    Running,
    Succeeded,
    Failed
}

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyAgentContext))]
[RequireComponent(typeof(EnemyAnimationController))]
public class EnemyMovementController : MonoBehaviour
{
    [SerializeField] private float randomRadius = 15f;
    [SerializeField] private float searchRange = 5f;
    private NavMeshAgent agent;
    private EnemyAgentContext context;
    private EnemyAnimationController  animationController;

    private void Awake()
    {
        agent =  GetComponent<NavMeshAgent>();
        context = GetComponent<EnemyAgentContext>();
        animationController = GetComponent<EnemyAnimationController>();
    }

    public bool TryChase(GameObject target)
    {
        if (target == null
            || !agent.enabled
            || !agent.isOnNavMesh
            || context.Stats == null)
        {
            Stop();
            return false;
        }

        agent.speed = context.Stats.runSpeed;
        agent.angularSpeed = context.Stats.angularSpeed;
        agent.stoppingDistance = Mathf.Max(0.1f, context.Stats.attackRange * 0.85f);
        
        agent.isStopped = false;
        
        bool accepted = agent.SetDestination(target.transform.position);
        
        animationController.SetMoving(accepted);
        
        return accepted;
    }
    
    public bool TryStartPatrol()
    {
        if (!agent.enabled
            || !agent.isOnNavMesh
            || context.Stats == null)
        {
            Stop();
            return false;
        }

        if (!TryGetRandomPatrolPosition(out Vector3 destination))
        {
            Stop();
            return false;
        }

        agent.speed = context.Stats.walkSpeed;
        agent.angularSpeed = context.Stats.angularSpeed;
        agent.stoppingDistance = 0.2f;
        
        agent.ResetPath();
        agent.isStopped = false;
        
        bool accepted = agent.SetDestination(destination);
        
        animationController.SetMoving(accepted);
        
        return accepted;
    }

    public EnemyMovementProgress GetMoveProgress()
    {
        if (!agent.enabled ||
            !agent.isOnNavMesh)
        {
            return EnemyMovementProgress.Failed;
        }

        if (agent.pathPending)
            return EnemyMovementProgress.Running;
        
        float arrivalDistance = agent.stoppingDistance + 0.15f;
        
        bool reachedDestination = agent.remainingDistance <= arrivalDistance && (!agent.hasPath || agent.velocity.sqrMagnitude <= 0.01f);

        if (reachedDestination)
            return EnemyMovementProgress.Succeeded;
        
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid ||
            agent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            return EnemyMovementProgress.Failed;
        }

        return EnemyMovementProgress.Running;
    }

    private bool TryGetRandomPatrolPosition(out Vector3 position)
    {
        position = transform.position;

        float radius = Mathf.Max(1f, context.Stats.wanderRadius);

        const int attempts = 8;

        const float minimumDistance = 2f;

        for (int i = 0; i < attempts; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;

            if (offset.sqrMagnitude < minimumDistance * minimumDistance)
            {
                continue;
            }
            
            Vector3 candidate = transform.position + new Vector3(offset.x, 0, offset.y);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, searchRange, agent.areaMask))
            {
                continue;
            }

            if ((hit.position - position).sqrMagnitude < minimumDistance * minimumDistance)
            {
                continue;
            }
            
            position = hit.position;
            return true;
        }

        return false;
    }

    public void Stop()
    {
        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        
        animationController.SetMoving(false);
    }

    public void FaceTarget(GameObject target)
    {
        if (target == null || context.Stats == null)
            return;
        
        Vector3 direction =
            target.transform.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;
        
        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized);
        
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, context.Stats.angularSpeed * Time.deltaTime);
    }

    #region For ObjectPooling

    public void ResetState()
    {
        if (agent == null) return;
        if (!agent.enabled)
            agent.enabled = true;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        
        animationController.SetMoving(false);
    }

    #endregion
}
