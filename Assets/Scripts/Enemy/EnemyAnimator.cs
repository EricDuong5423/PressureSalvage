using System;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyAnimator : MonoBehaviour
{
    [Header("Animator")] 
    public Animator EnemyAnim { get; private set; }

    private void Start()
    {
        EnemyAnim =  GetComponent<Animator>();
    }
}
