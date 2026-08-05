using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Targets
{
    public Transform Position;
    public Quaternion Rotation;
}
public class MoveToTarget : MonoBehaviour
{
    [SerializeField] List<Targets> targets = new List<Targets>();
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 2f;

    private void Start()
    {
        if (targets != null && targets.Count > 0)
        {
            StartCoroutine(MoveToTargetInfinite());
        }
    }

    private IEnumerator MoveToTargetInfinite()
    {
        int currentIndex = 0;

        while (true)
        {
            Targets currentTarget = targets[currentIndex];

            if (currentTarget.Position != null)
            {
                while (Vector3.Distance(transform.position, currentTarget.Position.position) > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position, 
                        currentTarget.Position.position, 
                        moveSpeed * Time.deltaTime
                    );
                    
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation, 
                        currentTarget.Rotation, 
                        rotateSpeed * Time.deltaTime
                    );
                    yield return null; 
                }
            }
            currentIndex =  (currentIndex + 1) % targets.Count;
        }
    }
}
