using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "EnemyTargetVisibleCondition", story: "Can [Agent] see [Target]", category: "Enemy/Conditions", id: "93c620dd551ed3c7b2ec7d5d65e1d536")]
public partial class EnemyTargetVisibleCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    public override bool IsTrue()
    {
        if (Agent?.Value == null || Target?.Value == null)
            return false;
        EnemyVisionSensor visionSensor = Agent.Value.GetComponent<EnemyVisionSensor>();
        
        return visionSensor != null && visionSensor.CanSee(Target.Value);
    }
}
