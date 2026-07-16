using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Enemy Melee Attack", story: "Make [Agent] attack [Target]", category: "Enemy/Combat", id: "0135badfb489699055ff983840e91ed4")]
public partial class EnemyMeleeAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    private EnemyAgentContext context;
    private EnemyVisionSensor vision;
    private EnemyMovementController movement;
    private EnemyMeleeCombat combat;

    protected override Status OnStart()
    {
        if (Agent?.Value == null) return Status.Failure;

        context = Agent.Value.GetComponent<EnemyAgentContext>();
        vision = Agent.Value.GetComponent<EnemyVisionSensor>();
        movement = Agent.Value.GetComponent<EnemyMovementController>();
        combat = Agent.Value.GetComponent<EnemyMeleeCombat>();
        
        if (context == null ||
            vision == null ||
            movement == null ||
            combat == null)
        {
            return Status.Failure;
        }

        if (Target?.Value != null)
            movement.TryChase(Target.Value);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Target?.Value == null)
            return Status.Failure;

        if (!vision.CanSee(Target.Value) ||
            !context.IsInAttackRange(Target.Value))
        {
            return Status.Failure;
        }

        movement.TryChase(Target.Value);
        movement.FaceTarget(Target.Value);

        combat.TryAttack(Target.Value);
        
        return Status.Running;
    }

    protected override void OnEnd()
    {
    }
}

