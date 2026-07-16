using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLoadout : MonoBehaviour
{
    [SerializeField] private int defaultTankTier;
    [SerializeField] private int defaultSlotCount = 1;
    public static PlayerLoadout Instance { get; private set; }

    public int oxygenTankTier { get; private set; }
    public int slotCount { get; private set; }
    public List<ItemData> ownedGear = new();

    public event Action OnChanged;
    
    public void ResetNewRun()
    {
        oxygenTankTier = defaultTankTier;
        slotCount = defaultSlotCount;
        ownedGear.Clear();

        OnChanged?.Invoke();
    }
    
    public bool TryUpgradeTank(int maxTier)
    {
        if (oxygenTankTier >= maxTier)
            return false;

        oxygenTankTier++;
        OnChanged?.Invoke();
        return true;
    }

    public bool TryUpgradeSlots(int maxSlots)
    {
        if (slotCount >= maxSlots)
            return false;

        slotCount++;
        OnChanged?.Invoke();
        return true;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
