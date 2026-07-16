using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyAgentContext))]
[RequireComponent(typeof(EnemyAnimationController))]
public class EnemyMovementController : MonoBehaviour
{
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

    public void Stop()
    {
        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
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
}
