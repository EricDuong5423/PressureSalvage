using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class CarryItem : Interactable, ICarryable
{
    protected Rigidbody rb;
    private Collider col;
    public abstract bool IsTwoHandRequired { get; }

    private Transform cameraTransform;
    [SerializeField] private float followSpeed = 5f;
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }
    
    public virtual void Carry(Transform camera)
    {
        col.enabled = false;
        cameraTransform = camera;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        rb.isKinematic = true;
    }

    public virtual void Drop()
    {
        cameraTransform = null;
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        col.enabled = true;
    }

    protected override void Interact()
    {
        
    }

    private void Update()
    {
        if (cameraTransform == null) return;
        transform.position = Vector3.Lerp(transform.position, cameraTransform.position, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, cameraTransform.rotation, followSpeed * Time.deltaTime);
    }
}
