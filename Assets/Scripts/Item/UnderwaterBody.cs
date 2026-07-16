using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class UnderwaterBody : MonoBehaviour
{
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        var s = UnderwaterEnvironment.Instance?.Settings;
        rb.linearDamping = s ? s.objectDrag : 3f;
        rb.angularDamping = 2f;
    }

    private void FixedUpdate()
    {
        if (rb.isKinematic) return;
        float buoyance = UnderwaterEnvironment.Instance ? UnderwaterEnvironment.Instance.Settings.objectBuoyancy : 7f;
        rb.AddForce(Vector3.up * buoyance, ForceMode.Acceleration);
    }
}
