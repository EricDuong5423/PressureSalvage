using System;
using UnityEngine;
using UnityEngine.AI;

public class RunToPlayer : MonoBehaviour
{
    [SerializeField] private Transform target;
    private NavMeshAgent agent;
    [SerializeField] private Vector3 offset;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("RunToPlayer: No target assigned");
        }
        
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        agent.SetDestination(target.position  - offset);
    }
}
