using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameProgressionManager : MonoBehaviour
{
    public static GameProgressionManager Instance { get; private set; }

    public const int MaxDays = 10;

    private readonly HashSet<string> unlockedMaps = new();

    public MapData SelectedMap { get; private set; }

    public MapData ActiveDiveMap { get; private set; }

    public int Day { get; private set; } = 1;
    public int CarriedQuota { get; private set; }
    public int EarnedToday { get; private set; }
    public int Credits { get; private set; }
    public int Strikes { get; private set; }
    public int ReinstatementCount { get; private set; }
    public int TotalItem { get; private set; }

    public bool EscapeQuestComplete
    {
        get;
        private set;
    }

    public int QuotaToday =>
        BaseQuota(Day) + CarriedQuota;

    public event System.Action OnStateChanged;
    public event System.Action<int> OnStrike;
    public event System.Action OnTrappedEnding;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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
        ActiveDiveMap = null;

        unlockedMaps.Clear();

        OnStateChanged?.Invoke();
    }

    private static int BaseQuota(int day)
    {
        if (day <= 3)
            return 1000;

        if (day <= 7)
            return 1500;

        return 2000;
    }

    public void SetSelectedMap(MapData map)
    {
        SelectedMap = map;
        OnStateChanged?.Invoke();
    }

    public bool TryBeginDive(out MapData map)
    {
        map = SelectedMap;

        if (map == null)
            return false;

        if (!IsMapAvailable(map))
            return false;

        if (string.IsNullOrWhiteSpace(map.SceneName))
        {
            Debug.LogError(
                $"MapData '{map.name}' chưa có SceneName.");

            return false;
        }

        ActiveDiveMap = map;
        return true;
    }

    public bool IsMapAvailable(MapData map)
    {
        if (map == null)
            return false;

        if (map.UnlockedByDefault)
            return true;

        if (unlockedMaps.Contains(map.Id))
            return true;

        if (map.RequiredDay > 0 &&
            Day >= map.RequiredDay)
        {
            return true;
        }

        return false;
    }

    public bool TryPurchaseUnlock(MapData map)
    {
        if (map == null)
            return false;

        if (IsMapAvailable(map))
            return true;

        if (map.UnlockCost <= 0 ||
            Credits < map.UnlockCost)
        {
            return false;
        }

        Credits -= map.UnlockCost;
        unlockedMaps.Add(map.Id);

        OnStateChanged?.Invoke();
        return true;
    }

    public bool TrySpend(int cost)
    {
        if (cost <= 0)
            return true;

        if (Credits < cost)
            return false;

        Credits -= cost;

        OnStateChanged?.Invoke();
        return true;
    }

    public void AddEarnings(int value)
    {
        int safeValue = Mathf.Max(0, value);

        int previousSurplus = Mathf.Max(0, EarnedToday - QuotaToday);

        EarnedToday += safeValue;

        int currentSurplus = Mathf.Max(0, EarnedToday - QuotaToday);

        Credits += currentSurplus - previousSurplus;

        OnStateChanged?.Invoke();
    }

    public void CompleteDive()
    {
        SettleDive();
    }

    public void Reinstate()
    {
        Inventory inventory = Inventory.Instance;

        if (inventory != null)
            inventory.ClearAll();

        ReinstatementCount++;
        EarnedToday = 0;

        SettleDive();
    }

    private void SettleDive()
    {
        ClearDiveSelection();

        int shortfall =
            Mathf.Max(
                0,
                QuotaToday - EarnedToday);

        if (shortfall > 0)
        {
            Strikes++;

            CarriedQuota =
                Mathf.RoundToInt(
                    shortfall * 0.8f);

            OnStrike?.Invoke(Strikes);
        }
        else
        {
            CarriedQuota = 0;
        }
        
        if (Strikes >= 3)
        {
            OnTrappedEnding?.Invoke();
            EndingManager.Instance.ChangeTrapEndingScene();
            return;
        }

        if (Day >= MaxDays)
        {
            if (EscapeQuestComplete)
            {
                EndingManager.Instance.ChangeEscapeEndingScene();
                return;
            }
            EndingManager.Instance.ChangeTrapEndingScene();
        }

        AdvanceDay();
    }

    private void ClearDiveSelection()
    {
        ActiveDiveMap = null;
        SelectedMap = null;
    }

    private void AdvanceDay()
    {
        EarnedToday = 0;
        Day++;

        OnStateChanged?.Invoke();

        SceneManager.LoadScene("Submarine");
    }
}