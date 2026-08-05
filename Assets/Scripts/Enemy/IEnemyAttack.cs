using UnityEngine;

public interface IEnemyAttack
{
    bool TryAttack(GameObject target);
    void ApplyHitFromAnimationEvent();
    void ResetState();
}
