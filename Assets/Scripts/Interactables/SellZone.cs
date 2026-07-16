using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Collider))]
public class SellZone : MonoBehaviour
{
    [SerializeField] private TMP_Text boardText;
    private readonly HashSet<CarryItem> items = new();
    public int Total { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        if (TryGetItem(other, out CarryItem item) && items.Add(item)) Recalculate();
    }

    private void OnTriggerExit(Collider other)
    {
        if(TryGetItem(other, out CarryItem item) && items.Remove(item)) Recalculate();
    }

    private bool TryGetItem(Collider collider, out CarryItem item)
    {
        item = null;
        Rigidbody rb = collider.attachedRigidbody;
        return rb != null && rb.TryGetComponent(out item) && item.data != null;
    }

    private void Recalculate()
    {
        items.RemoveWhere(i => i == null);
        int total = 0;
        foreach (CarryItem item in items) total += item.Value;
        Total = total;
        Debug.Log($"{Total}₡");
        if(boardText != null) boardText.text = $"{Total}₡";
    }

    public void SellAll()
    {
        items.RemoveWhere(i => i == null);
        int total = 0;
        foreach (CarryItem item in items) total += item.Value;
        GameProgressionManager.Instance?.AddEarnings(total);

        foreach (CarryItem item in items)
        {
            if(item != null) Destroy(item.gameObject);
        }
        items.Clear();
        Total = 0;
        if (boardText != null) boardText.text = "0₡";
    }
}
