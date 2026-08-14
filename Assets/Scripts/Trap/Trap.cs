using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class Trap: MonoBehaviour
{
    [SerializeField] protected Collider trapCollider;
    public bool isTrap = false;

    private void Awake()
    {
        if (trapCollider == null) trapCollider = GetComponent<Collider>();
        trapCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TryTrap(other.gameObject);
        }
    }

    public abstract bool TryTrap(GameObject player);
}
