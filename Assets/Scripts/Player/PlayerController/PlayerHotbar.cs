using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHotbar : MonoBehaviour
{
    [SerializeField] private Transform holdPoint;
    [SerializeField] private Vector3 dropOffset = new Vector3(0f, 0f, 1.2f);

    [SerializeField] private InputActionReference useAction;
    [SerializeField] private InputActionReference dropAction;

    private GameObject heldGO;
    private IUsable heldUsable;
    
    private Inventory Inv => Inventory.Instance;

    private void Start()
    {
        if(Inv != null && PlayerLoadout.Instance != null)
            Inv.SetCapacity(PlayerLoadout.Instance.slotCount);
        RefreshHeld();
    }

    private void Update()
    {
        if (Inv == null) return;
        
        if (useAction != null && useAction.action.WasPressedThisFrame())
        {
            heldUsable?.Use();
        }
        if (dropAction != null && dropAction.action.WasPressedThisFrame())
        {
            DropActive();
        }
        
        var kb = Keyboard.current;
        if (kb == null) return;
        for (int i = 0; i < Inv.Capacity && i < 9; i++)
        {
            var k = Key.Digit1 + i;
            if (kb[k].wasPressedThisFrame)
            {
                Inv.SetActive(i);
                RefreshHeld();
                break;
            }
        }
    }

    public void RefreshHeld()
    {
        heldUsable?.OnUnequip();
        if (heldGO != null) Destroy(heldGO);
        
        heldGO = null;
        heldUsable = null;

        var slot = Inv != null ? Inv.Active : null;
        if (slot == null || slot.Empty || slot.data.worldPrefab == null || holdPoint == null) return;
        
        heldGO = Instantiate(slot.data.worldPrefab, holdPoint.transform);
        heldGO.transform.localPosition = Vector3.zero;
        heldGO.transform.localRotation = Quaternion.identity;
        ConfigureHeld(heldGO);
        
        heldUsable = heldGO.GetComponent<IUsable>();
        heldUsable?.OnEquip(gameObject);
    }

    private static void ConfigureHeld(GameObject go)
    {
        if (go.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }
        foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
        var uw = go.GetComponent<UnderwaterBody>();
        if (uw != null) uw.enabled = false;
    }

    public bool TryPickup(CarryItem item)
    {
        if (Inv == null || item == null || item.data == null) return false;
        if (!Inv.TryAdd(item.data, item.Value)) return false;
        
        Destroy(item.gameObject);
        RefreshHeld();
        return true;
    }

    public void DropActive()
    {
        var slot = Inv != null ? Inv.Active : null;
        if (slot == null || slot.Empty || slot.data.worldPrefab == null) return;
        
        Vector3 pos = holdPoint != null ? holdPoint.TransformPoint(dropOffset) 
                                           : transform.position + transform.forward * 1.2f;
        var go = Instantiate(slot.data.worldPrefab, pos, Quaternion.identity);
        if (go.TryGetComponent(out CarryItem ci)) ci.SetValue(slot.Value);
        
        Inv.ClearActive();
        RefreshHeld();
    }
}
