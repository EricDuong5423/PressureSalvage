using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Enemy Chase Target", story: "Make [Agent] chase [Target]", category: "Enemy/Movement", id: "fd5a1d30d5a41d012da11681ba7627ee")]
public partial class EnemyChaseTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    
    private EnemyAgentContext context;
    private EnemyVisionSensor vision;
    private EnemyMovementController movement;
    
    protected override Status OnStart()
    {
        if (Agent?.Value == null)
            return Status.Failure;

        context =
            Agent.Value.GetComponent<EnemyAgentContext>();

        vision =
            Agent.Value.GetComponent<EnemyVisionSensor>();

        movement =
            Agent.Value.GetComponent<EnemyMovementController>();

        return context != null &&
               vision != null &&
               movement != null
            ? Status.Running
            : Status.Failure;
    }

    protected override Status OnUpdate()
    {
        if (Target?.Value == null)
            return Status.Failure;
        
        if (!vision.CanSee(Target.Value))
            return Status.Failure;

        if (context.IsInAttackRange(Target.Value))
            return Status.Success;
        
        return movement.TryChase(Target.Value) ? Status.Running : Status.Failure;
    }

    protected override void OnEnd()
    {
        movement?.Stop();
    }
}

