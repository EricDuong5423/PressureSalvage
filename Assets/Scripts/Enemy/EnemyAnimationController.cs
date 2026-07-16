using System;
using UnityEngine;

[RequireComponent(typeof(EnemyAgentContext))]
public sealed class EnemyAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Animation Parameters")] 
    [SerializeField] private string movingBool = "isWalking";
    [SerializeField] private string attackTrigger = "isAttacking";

    private int movingHash;
    private int attackHash;
    private bool currentMoving;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        
        movingHash = Animator.StringToHash(movingBool);
        attackHash = Animator.StringToHash(attackTrigger);
    }

    public void SetMoving(bool moving)
    {
        if (animator == null || currentMoving == moving) return;

        currentMoving = moving;
        animator.SetBool(movingHash, moving);
    }

    public void PlayAttack()
    {
        if (animator == null) return;
        
        animator.SetTrigger(attackHash);
    }
}
