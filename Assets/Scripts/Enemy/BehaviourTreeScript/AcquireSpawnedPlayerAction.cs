using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Acquire Spawned Player", story: "Get [Target] for [Agent]", category: "Enemy/TargetingNew Category", id: "ab391369f95790b980eea70ecf8b3b72")]
public partial class AcquireSpawnedPlayerAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnUpdate()
    {
        if(Agent?.Value == null || Target == null) return Status.Failure;

        if (Target.Value != null && Target.Value.activeInHierarchy) return Status.Success;

        EnemyAgentContext context = Agent.Value.GetComponent<EnemyAgentContext>();
        
        if (context == null) return Status.Failure;
        if (!context.TryGetSpawnedPlayer(out GameObject player)) return Status.Running;

        Target.Value = player;
        return Status.Success;
    }
}

