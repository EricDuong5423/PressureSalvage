using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class SceneIntro : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private CanvasGroup _hudCanvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text quotaText;
    [SerializeField] private string title = "SUBMARINE";
    [SerializeField] private bool showDayQuota = false;
    [SerializeField] private float holdTime = 1.2f;
    
    public event Action Completed;

    public bool IsComplete { get; private set; }

    public CanvasGroup HudCanvasGroup => _hudCanvasGroup;
    private bool _revealHudOnComplete = true;
    private bool _useCameraFadeOnComplete = true;
    private bool _ownsTimeScale;

    public void ConfigureCompletion(
        bool revealHud,
        bool useCameraFade)
    {
        _revealHudOnComplete = revealHud;
        _useCameraFadeOnComplete = useCameraFade;
    }

    private void Start()
    {
        PlayIntro();
    }
    
    private void BeginCompletion()
    {
        CameraFade fade = CameraFade.Instance;

        if (_useCameraFadeOnComplete && fade != null)
        {
            fade.FadeIn(1f).OnComplete(CompleteIntro);
            return;
        }

        CompleteIntro();
    }

    private void CompleteIntro()
    {
        if (IsComplete)
            return;

        IsComplete = true;

        if (_ownsTimeScale)
        {
            Time.timeScale = 1f;
            _ownsTimeScale = false;
        }

        Completed?.Invoke();
    }

    public void PlayIntro()
    {
        var env = UnderwaterEnvironment.Instance;
        if (env != null && env.Settings != null && !string.IsNullOrEmpty(env.Settings.displayName))
            title = env.Settings.displayName;
        
        titleText.text = title;
        var g = GameProgressionManager.Instance;
        if (showDayQuota && g != null)
        {
            dayText.text  = $"DAY {g.Day} / {GameProgressionManager.MaxDays}";
            quotaText.text = $"QUOTA {g.QuotaToday}₡";
        }
        Time.timeScale = 0;
        _ownsTimeScale = true;
        _hudCanvasGroup.alpha = 0f;
        _canvasGroup.alpha = 1f;
        SetA(titleText, 0);
        SetA(dayText, 0);
        SetA(quotaText, 0);
        
        Sequence seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(titleText.DOFade(1f, 0.5f));
        if (showDayQuota)
        {
            seq.AppendInterval(0.4f).Append(dayText.DOFade(1f, 0.4f));
            seq.AppendInterval(0.3f).Append(quotaText.DOFade(1f, 0.4f));
        }

        seq.AppendInterval(holdTime);
        seq.Append(_canvasGroup.DOFade(0f, 0.5f));
        if (_revealHudOnComplete)
            seq.Join(_hudCanvasGroup.DOFade(1f, 0.5f));
        seq.AppendCallback(BeginCompletion);
    }

    private void OnDisable()
    {
        if (_ownsTimeScale)
        {
            Time.timeScale = 1f;
            _ownsTimeScale = false;
        }
    }

    private void SetA(TMP_Text t, float a){ if (t){ var c = t.color; c.a = a; t.color = c; } }
}
