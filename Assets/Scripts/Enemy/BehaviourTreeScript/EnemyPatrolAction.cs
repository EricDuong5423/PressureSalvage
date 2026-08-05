using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Enemy Patrol", story: "Make [Self] patrol", category: "Action", id: "92107726ec77fb77c62749855e913fcd")]
public partial class EnemyPatrolAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    private EnemyMovementController _movementController;
    private EnemySwimmingController _swimmingController;
    private PooledEnemy _pooledEnemy;

    protected override Status OnStart()
    {
        if (Self?.Value == null)
            return Status.Failure;

        _pooledEnemy = Self?.Value.GetComponent<PooledEnemy>();
        
        if (_pooledEnemy == null) return Status.Failure;

        if (_pooledEnemy.UsingNavMeshAgent)
        {
            _movementController = Self?.Value.GetComponent<EnemyMovementController>();
        }
        else
        {
            _swimmingController = Self?.Value.GetComponent<EnemySwimmingController>();
        }
        
        if (_movementController == null && _swimmingController == null)
            return Status.Failure;
        if (_pooledEnemy.UsingNavMeshAgent)
        {
            return _movementController.TryStartPatrol() ? Status.Running :  Status.Failure;
        }
        
        return _swimmingController.TryStartPatrol() ? Status.Running : Status.Failure;
    }

    protected override Status OnUpdate()
    {
        if (_movementController == null &&  _swimmingController == null)
            return Status.Failure;
        

        if (_pooledEnemy.UsingNavMeshAgent)
        {
            return _movementController.GetMoveProgress() switch
            {
                EnemyMovementProgress.Running => Status.Running,

                EnemyMovementProgress.Succeeded => Status.Success,

                _ => Status.Failure
            };
        }

        return _swimmingController.TickPatrol() switch
        {
            EnemyMovementProgress.Running => Status.Running,

            EnemyMovementProgress.Succeeded => Status.Success,

            _ => Status.Failure
        };
    }

    protected override void OnEnd()
    {
    }
}

