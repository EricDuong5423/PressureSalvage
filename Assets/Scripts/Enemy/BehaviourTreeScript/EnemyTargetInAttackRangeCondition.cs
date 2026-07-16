using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Enemy Target In Attack Range", story: "Is [Target] in attack range of [Agent]", category: "Enemy/Conditions", id: "41f40eae4380e598a492ed51b7e2f72f")]
public partial class EnemyTargetInAttackRangeCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    public override bool IsTrue()
    {
        if (Agent?.Value == null || Target?.Value == null)
            return false;

        EnemyAgentContext context = Agent.Value.GetComponent<EnemyAgentContext>();
        
        return context != null && context.IsInAttackRange(Target.Value);
    }
}
