using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Enemy Idle", story: "Keep [Agent] idle while [Target] exists", category: "Enemy/Movement", id: "8de7acfd8fdcc565f89125dfeb2def10")]
public partial class EnemyIdleAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    
    private EnemyMovementController  movement;
    private EnemySwimmingController  swimming;

    [CreateProperty] private float remainingTime;

    protected override Status OnStart()
    {
        if (Agent?.Value == null)
            return Status.Failure;

        if (Agent.Value.GetComponent<PooledEnemy>().UsingNavMeshAgent)
        {
            movement = Agent.Value.GetComponent<EnemyMovementController>();
        }
        else
        {
            swimming = Agent.Value.GetComponent<EnemySwimmingController>();
        }

        EnemyAgentContext context = Agent.Value.GetComponent<EnemyAgentContext>();
        
        if ((movement == null && swimming == null) ||
            context == null ||
            context.Stats == null)
            return Status.Failure;

        if (Agent.Value.GetComponent<PooledEnemy>().UsingNavMeshAgent)
        {
            movement.Stop();
        }
        else
        {
            swimming.Stop();
        }

        float baseDuration = Mathf.Max(0f, context.Stats.wanderWaitTime);
        
        remainingTime = UnityEngine.Random.Range(baseDuration * 0.75f, baseDuration * 1.25f);
        
        return remainingTime <= 0 ? Status.Success : Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent?.Value == null)
            return Status.Failure;

        if (Target?.Value == null ||
            !Target.Value.activeInHierarchy)
        {
            return Status.Failure;
        }
        
        remainingTime -= Time.deltaTime;
        
        return remainingTime <= 0 ? Status.Success : Status.Running;
    }

    protected override void OnEnd()
    {
        if (Agent.Value.GetComponent<PooledEnemy>().UsingNavMeshAgent)
        {
            movement?.Stop();
        }
        else
        {
            swimming?.Stop();
        }
    }
}

