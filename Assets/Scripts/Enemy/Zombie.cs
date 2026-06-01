using UnityEngine;

public class Zombie : EnemyBase
{
    private float wanderTimer;
    private float attackTimer;
    protected override void PatrolBehavior()
    {
        agent.speed = stats.walkSpeed;
        agent.isStopped = true;
        //Animation Idle
        anim.EnemyAnim.SetBool("isWalking", false);
    }

    protected override void AttackBehavior()
    {
        anim.EnemyAnim.SetTrigger("isAttacking");
        attackTimer += Time.deltaTime;
        if (attackTimer >= stats.attackCoolDown)
        {
            attackTimer = 0f;
        }
    }

    protected override void ChaseBehavior()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer < stats.attackRange)
        {
            anim.EnemyAnim.SetBool("isWalking", false);
            agent.isStopped = true;

            Vector3 lookPos = player.position - transform.position;
            lookPos.y = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation
                               , Quaternion.LookRotation(lookPos)
                               , Time.deltaTime * 5f);
            
            AttackBehavior();
        }
        else
        {
            agent.speed = stats.runSpeed;
            agent.isStopped = false;
            agent.SetDestination(player.position);
            attackTimer = stats.attackCoolDown;
            anim.EnemyAnim.SetBool("isWalking", true);
        }
    }
}
