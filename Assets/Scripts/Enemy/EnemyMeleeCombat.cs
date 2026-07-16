using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public sealed class EnemyHitEvent : UnityEvent<GameObject, float>
{
    
}
[RequireComponent(typeof(EnemyAgentContext))]
[RequireComponent(typeof(EnemyAnimationController))]
public sealed class EnemyMeleeCombat : MonoBehaviour
{
   [SerializeField] private EnemyHitEvent onHit = new EnemyHitEvent();
   private EnemyAgentContext context;
   private EnemyAnimationController animationController;

   private GameObject lockedTarget;
   private float nextAttackTime;

   private void Awake()
   {
      context = GetComponent<EnemyAgentContext>();
      animationController = GetComponent<EnemyAnimationController>();
   }

   public bool TryAttack(GameObject target)
   {
      if (target == null ||
          context.Stats == null ||
          !context.IsInAttackRange(target) ||
          Time.time < nextAttackTime)
      {
         return false;
      }

      lockedTarget = target;

      nextAttackTime = Time.time + context.Stats.attackCoolDown;
      
      animationController.PlayAttack();

      return true;
   }

   public void ApplyHitFromAnimationEvent()
   {
      if (lockedTarget == null ||
          context.Stats == null ||
          !context.IsInAttackRange(lockedTarget))
      {
         return;
      }
      
      onHit.Invoke(lockedTarget, context.Stats.damage);
   }
}
