using System;
using UnityEngine;

[RequireComponent(typeof(OxygenSystem))]
public class PlayerEquipment : MonoBehaviour
{
    private OxygenSystem oxygenSystem;

    private void Awake()
    {
        oxygenSystem = GetComponent<OxygenSystem>();
    }

    private void Start()
    {
        ApplyLoadout();

        if (PlayerLoadout.Instance != null)
            PlayerLoadout.Instance.OnChanged += ApplyLoadout;
    }

    private void OnDestroy()
    {
        if (PlayerLoadout.Instance != null)
            PlayerLoadout.Instance.OnChanged -= ApplyLoadout;
    }

    private void ApplyLoadout()
    {
        var loadout = PlayerLoadout.Instance;
        if (loadout == null)
            return;

        oxygenSystem.ApplyTankTier(loadout.oxygenTankTier);
        Inventory.Instance?.SetCapacity(loadout.slotCount);
    }
}
