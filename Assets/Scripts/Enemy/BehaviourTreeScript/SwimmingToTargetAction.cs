using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Swimming To Target", story: "Make [Agent] swimming to [Target]", category: "Enemy/Movement", id: "42a56f14cccd0965e517f5b56cef9785")]
public partial class SwimmingToTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    private EnemyAgentContext context;
    private EnemyVisionSensor sensor;
    private EnemySwimmingController swimming;

    protected override Status OnStart()
    {
        if (Agent?.Value == null) return Status.Failure;

        context = Agent.Value.GetComponent<EnemyAgentContext>();
        sensor = Agent.Value.GetComponent<EnemyVisionSensor>();
        swimming = Agent.Value.GetComponent<EnemySwimmingController>();
        
        return context != null && sensor != null && swimming != null ? Status.Running : Status.Failure;
    }

    protected override Status OnUpdate()
    {
        if (Target?.Value == null) return Status.Failure;
        if (!sensor.CanSee(Target.Value)) return Status.Failure;
        if (context.IsInAttackRange(Target.Value)) return Status.Success;
        
        return swimming.TryChase(Target.Value) ? Status.Running : Status.Failure;
    }

    protected override void OnEnd()
    {
        swimming?.Stop();
    }
}

