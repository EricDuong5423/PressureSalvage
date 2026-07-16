using System;
using UnityEngine;

[RequireComponent(typeof(EnemyAgentContext))]
public sealed class EnemyVisionSensor : MonoBehaviour
{
    [SerializeField] private Transform eyePoint;
    [SerializeField] private float targetHeight = 1f;
    [SerializeField] private LayerMask obstacleMask;

    private EnemyAgentContext context;

    private void Awake()
    {
        context = GetComponent<EnemyAgentContext>();
    }

    public bool CanSee(GameObject target)
    {
        if (target == null || context.Stats == null) return false;
        Vector3 origin = eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 1.5f;
        Vector3 targetPoint = target.transform.position + Vector3.up * targetHeight;
        Vector3 delta = targetPoint - origin;
        float distance = delta.magnitude;
        
        if (distance <= Mathf.Epsilon) return true;
        if (distance > context.Stats.viewRadius) return false;
        
        Vector3 direction = delta / distance;
        
        float angle = Vector3.Angle(transform.forward, direction);

        if (angle > context.Stats.viewAngle * 0.5f) return false;
        
        bool blocked = Physics.Raycast(origin, direction, distance, obstacleMask, QueryTriggerInteraction.Ignore);
        return !blocked;
    }

    private void OnDrawGizmosSelected()
    {
        if (context == null)
            context = GetComponent<EnemyAgentContext>();
        if (context == null || context.Stats == null) return;

        Vector3 origin = eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 1.5f;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, context.Stats.viewRadius);
    }
}
