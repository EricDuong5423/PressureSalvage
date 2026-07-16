using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameProgressionManager : MonoBehaviour
{
    public static GameProgressionManager Instance { get; private set; }
    public MapData SelectedMap { get; private set; }
    public const int MaxDays = 10;
    private readonly HashSet<string> unlockedMaps = new();

    public int Day { get; private set; } = 1;
    public int CarriedQuota { get; private set; } //Quota chua du
    public int EarnedToday { get; private set; } //So tien loot duoc
    public int Credits { get; private set; } //So tien du ra
    public int Strikes { get; private set; } //Khong dat du Quota = 1 trikes, 3 trikes = Bad ending
    public int ReinstatementCount { get; private set; } //So lan hoi sinh
    public int TotalItem { get; private set; }

    public int QuotaToday => BaseQuota(Day) + CarriedQuota;

    public event System.Action OnStateChanged; //HUD refresh
    public event System.Action<int> OnStrike;
    public event System.Action OnTrappedEnding;

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ResetNewRun()
    {
        Day = 1;
        CarriedQuota = 0;
        EarnedToday = 0;
        Credits = 0;
        Strikes = 0;
        ReinstatementCount = 0;
        SelectedMap = null;
        
        unlockedMaps.Clear();
        OnStateChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private static int BaseQuota(int d) => d <= 3 ? 1000 : d <= 7 ? 1500 : 2000;

    public void SetSelectedMap(MapData map)
    {
        SelectedMap = map;
        OnStateChanged?.Invoke();
    }

    public bool IsMapAvailable(MapData map)
    {
        if (map.UnlockedByDefault) return true;
        if (unlockedMaps.Contains(map.Id)) return true;
        if (map.RequiredDay > 0 && Day >= map.RequiredDay) return true;
        return false;
    }

    public bool TryPurchaseUnlock(MapData map)
    {
        if (IsMapAvailable(map)) return true;
        if (map.UnlockCost <= 0 || Credits < map.UnlockCost) return false;
        Credits -= map.UnlockCost;
        unlockedMaps.Add(map.Id);
        OnStateChanged?.Invoke();
        return true;
    }
    
    public bool TrySpend(int cost)
    {
        if (cost <= 0) return true;
        if (Credits < cost) return false;
        Credits -= cost;
        OnStateChanged?.Invoke();
        return true;
    }

    public void AddEarnings(int value)
    {
        EarnedToday += value;
        Credits += Mathf.Max(0, EarnedToday - QuotaToday);
        OnStateChanged?.Invoke();
    }

    public void CompleteDive()
    {
        Settle(false);
    }

    public void Reinstate()
    {
        var I = Inventory.Instance;
        if (I != null) I.ClearAll();
        ReinstatementCount++;
        EarnedToday = 0;
        Settle(true);
        
    }

    private void Settle(bool died)
    {
        int shortfall = Mathf.Max(0, QuotaToday - EarnedToday);
        if (shortfall > 0)
        {
            Strikes++;
            CarriedQuota = Mathf.RoundToInt(shortfall * 0.8f);
            OnStrike?.Invoke(Strikes);
            if (Strikes >= 3)
            {
                OnTrappedEnding?.Invoke();
                /*TODO: Ending manager*/
                return;
            }
        }
        else
        {
            CarriedQuota = 0;
        }
        AdvanceDay();
    }

    private void AdvanceDay()
    {
        EarnedToday = 0;
        Day++;
        OnStateChanged?.Invoke();
        if (Day > MaxDays)
        {
            OnTrappedEnding?.Invoke();
            /*TODO: Check Escape route*/
            return;
        }
    
        SceneManager.LoadScene("Submarine");
    }
}
