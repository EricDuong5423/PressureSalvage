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

    protected override Status OnStart()
    {
        if (Agent?.Value == null)
            return Status.Failure;

        movement = Agent.Value.GetComponent<EnemyMovementController>();
        
        if (movement == null)
            return Status.Failure;

        movement.Stop();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Target?.Value == null || !Target.Value.activeInHierarchy)
            return Status.Failure;
        
        return Status.Running;
    }

    protected override void OnEnd()
    {
        movement?.Stop();
    }
}

