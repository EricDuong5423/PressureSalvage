using System;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
public class CarryItem : Interactable
{
    protected Rigidbody rb;
    [Header("Item data")]
    public ItemData data;
    
    public int Value { get; private set; }
    private bool rolled;
    private Camera cam;
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;
    }

    public void SpawningItem()
    {
        if (rolled || data == null) return;
        
        Value = Random.Range(data.minValue, data.maxValue + 1);
        
        rolled = true;
    }

    public void SetValue(int v)
    {
        Value = v;
        rolled = true;
    }

    protected override void Interact()
    {
        
    }
}
