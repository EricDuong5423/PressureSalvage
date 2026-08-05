using System;
using TMPro;
using UnityEngine;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text quotaText;
    [SerializeField] private TMP_Text creditsText;
    [SerializeField] private TMP_Text strikesText;

    private void Start()
    {
        var gpm = GameProgressionManager.Instance;
        if (gpm == null) return;
        gpm.OnStateChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (GameProgressionManager.Instance != null)
            GameProgressionManager.Instance.OnStateChanged -= Refresh;
    }

    private void Refresh()
    {
        var g =  GameProgressionManager.Instance;
        if (g == null) return;
        if (dayText != null) dayText.text = $"DAY: {g.Day.ToString()} / {GameProgressionManager.MaxDays}";
        if (quotaText != null) quotaText.text = $"QUOTA: {g.EarnedToday} / {g.QuotaToday}₡";
        if (creditsText != null) creditsText.text = $"CREDIT: {g.Credits}₡";
        if (strikesText != null) strikesText.text = $"STRIKE: {g.Strikes}";
    }
}
