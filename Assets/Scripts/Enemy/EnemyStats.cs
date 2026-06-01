using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Stats", menuName = "Game Data/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Header("Basic info")] 
    public string enemyName = "Unknown Enemy";
    public float maxHealth = 100f;

    [Header("Movement")] 
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float angularSpeed = 120f;

    [Header("Vision & Detection")] 
    public float viewRadius = 10f;
    [Range(0, 360)] public float viewAngle = 90f;

    [Header("Combat")] 
    public float attackRange = 2f;
    public float attackCoolDown = 1.5f;
    public float damage = 10f;

    [Header("Patrol (optional)")] 
    public float wanderRadius = 10f;
    public float wanderWaitTime = 3f;
}
