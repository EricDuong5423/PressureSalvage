using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class DiveExitUI : MonoBehaviour
{
    public static DiveExitUI Instance { get; private set; }
    [SerializeField] private TMP_Text reportText;
    [SerializeField] private TMP_Text finalText;
    [SerializeField] private CanvasGroup hudCanvasGroup;
    [SerializeField] private CanvasGroup diveExitCanvasGroup;
    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.7f;
    [SerializeField] private float finalFadeDuration = 0.5f;
    [SerializeField] private float finalHoldDuration = 2.5f;
    [SerializeField] private float reportHoldDuration = 0.8f;
    [SerializeField] private float reportFadeOutDuration = 0.5f;
    [SerializeField] private float betweenTextsDelay = 0.4f;
    [SerializeField] private float finalCharacterDelay = 0.055f;
    [Header("Typewriters")]
    [SerializeField] private float characterDelay = 0.025f;
    [SerializeField] private float lineDelay = 0.18f;
    [SerializeField] private float punctuationDelay = 0.12f;

    private bool _isPlaying;

    public void Play()
    {
        if (_isPlaying) return;
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        _isPlaying = true;
        
        Time.timeScale = 0;

        hudCanvasGroup.DOFade(0f, 0.5f).SetUpdate(true).WaitForCompletion();

        reportText.text = BuildReport();
        reportText.alpha = 1f;
        reportText.maxVisibleCharacters = 0;

        finalText.text = BuildFinal();
        finalText.alpha = 0f;
        
        yield return diveExitCanvasGroup.DOFade(1f, fadeInDuration)
            .SetUpdate(true)
            .WaitForCompletion();

        yield return TypeText(reportText, characterDelay);
        
        yield return WaitRealTime(reportHoldDuration);
        
        yield return reportText
            .DOFade(0f, reportFadeOutDuration)
            .SetUpdate(true)
            .WaitForCompletion();
        
        yield return WaitRealTime(betweenTextsDelay);

        finalText.alpha = 1f;
        finalText.maxVisibleCharacters = 0;

        yield return TypeText(finalText, finalCharacterDelay);
        
        yield return WaitRealTime(finalHoldDuration);
        
        Time.timeScale = 1f;
        
        GameProgressionManager.Instance?.CompleteDive();
    }

    private string BuildFinal()
    {
        string text = "";
        var g = GameProgressionManager.Instance;
        if (g.EarnedToday > g.QuotaToday)
        {
            text += "APPRECIATION HAS <color=#E34B4B>NO</color> MONETARY VALUE.";
        }
        else if (g.EarnedToday == g.QuotaToday)
        {
            text += "ADEQUATE PERFORMANCE IS EXPECTED.\n" +
                    "IT IS NOT REWARDED.";
        }
        else
        {
            text += "YOU SURVIVED THE DIVE.\n" +
                    "YOU DID NOT COMPLETE THE SHIFT.";
        }
        return text;
    }

    private string BuildReport()
    {
        var g = GameProgressionManager.Instance;
        string text = "DEEP-SIX SALVAGE CORP.\n" +
                      $"DAILY EXTRACTION REPORT <color=#E34B4B>// DAY {g.Day}</color>\n" +
                      "WORKER RECOVERY: SUCCESSFUL\n" +
                      "BIOLOGICAL STATUS: ACCEPTABLE\n" +
                      $"GROSS SALVAGE VALUE: {g.EarnedToday}₡\n" +
                      $"DAILY QUOTA: {g.QuotaToday}₡\n";
        if (g.EarnedToday == g.QuotaToday)
        {
            text += $"<color=#2CFF05>SURPLUS: {g.EarnedToday - g.QuotaToday}₡</color>\n" +
                    "QUOTA STATUS: <color=#2CFF05>SATISFIED</color>\n" +
                    "MINIMUM CONTRACTUAL OBLIGATION MET.\n" +
                    "NO EXCESS PRODUCTIVITY RECORDED.";
        }
        else if (g.EarnedToday > g.QuotaToday)
        {
            text += $"<color=#FFEA00>SURPLUS: {g.EarnedToday - g.QuotaToday}₡</color>\n" +
                    "QUOTA STATUS: <color=#FFEA00>SATISFIED</color>\n" +
                    "PERFORMANCE RATING: ACCEPTABLE" +
                    "DEEP-SIX SALVAGE THANKS YOU\n" + 
                    "FOR YOUR EXCEPTIONAL COMPLIANCE.";
        }
        else
        {
            text += $"<color=#E34B4B>SHORTFALL: {g.QuotaToday - g.EarnedToday}₡</color>\n" +
                    $"<color=#E34B4B>DEBT CARRIED FORWARD: {(g.QuotaToday - g.EarnedToday) * 0.8f}₡</color>\n" +
                    "QUOTA STATUS: <color=#E34B4B>UNMET</color>\n" +
                    $"STRIKES: {g.Strikes + 1} / 3 \n" +
                    "PERFORMANCE DEFICIENCY RECORDED.\n" +
                    "FURTHER FAILURE MAY RESULT IN CONTRACTUAL CORRECTION.\n";
        }

        return text;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private IEnumerator TypeText(TMP_Text text, float baseCharacterDelay)
    {
        text.ForceMeshUpdate();

        int characterCount = text.textInfo.characterCount;
        text.maxVisibleCharacters = 0;

        for (int i = 0; i < characterCount; i++)
        {
            char character = text.textInfo.characterInfo[i].character;
            
            text.maxVisibleCharacters = i + 1;

            float delay = baseCharacterDelay;

            if (character == '\n')
            {
                delay = lineDelay;
            }
            else if (character == ':' ||
                     character == '.' ||
                     character == '!')
            {
                delay += punctuationDelay;
            }

            yield return WaitRealTime(delay);
        }

        text.maxVisibleCharacters = int.MaxValue;
    }
    
    private IEnumerator WaitRealTime(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_isPlaying) Time.timeScale = 1f;
    }
}
