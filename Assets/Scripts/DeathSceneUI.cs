using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class DeathSceneUI : MonoBehaviour
{
    public static DeathSceneUI Instance { get; private set; }
    
    [Header("References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup canvasHudGroup;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text reportText;
    [SerializeField] private TMP_Text finalText;

    [Header("Typewriters")]
    [SerializeField] private float characterDelay = 0.025f;
    [SerializeField] private float lineDelay = 0.18f;
    [SerializeField] private float punctuationDelay = 0.12f;
    
    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.7f;
    [SerializeField] private float finalHoldDuration = 2.5f;
    [SerializeField] private float reportHoldDuration = 0.8f;
    [SerializeField] private float reportFadeOutDuration = 0.5f;
    [SerializeField] private float betweenTextsDelay = 0.4f;
    [SerializeField] private float finalCharacterDelay = 0.055f;

    private bool _isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        panelRoot.SetActive(false);
    }

    public void Play()
    {
        if (_isPlaying) return;

        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        _isPlaying = true;
        
        Time.timeScale = 0f;
        
        panelRoot.SetActive(true);
        canvasGroup.alpha = 0f;
        canvasHudGroup.alpha = 0f;
        
        reportText.text = BuildReport();
        reportText.alpha = 1f;
        reportText.maxVisibleCharacters = 0;
        
        finalText.text =
            "YOUR BODY HAS EXPIRED.\n" +
            "<color=#E34B4B>YOUR CONTRACT HAS NOT.</color>";

        finalText.alpha = 0f;
        
        yield return canvasGroup.DOFade(1f, fadeInDuration)
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

        finalText.rectTransform
            .DOPunchScale(Vector3.one * 0.04f, 0.4f, 5, 0.4f)
            .SetUpdate(true);

        yield return WaitRealTime(finalHoldDuration);
        
        Time.timeScale = 1f;
        
        GameProgressionManager.Instance?.Reinstate();
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

    private string BuildReport()
    {
        GameProgressionManager progression =  GameProgressionManager.Instance;
        int nextStrike = progression != null ? Mathf.Clamp(progression.Strikes + 1, 0, 3) : 1;

        string resultText = "WORKER STATUS: DECEASED\n" +
                            "CONTRACT STATUS: ACTIVE\n\n" +

                            "CAUSE OF LOSS: OXYGEN DEPRIVATION\n" +
                            "SALVAGE RECOVERY: FAILED\n" +
                            "DAILY QUOTA: UNMET\n" +
                            $"STRIKE: {nextStrike} / 3\n\n" +

                            "BIOLOGICAL REINSTATEMENT CLAUSE INVOKED\n\n" +

                            "DEATH DOES NOT RELEASE YOU FROM DEBT.\n\n" +

                            "RECONSTITUTING WORKER...\n";
        
        resultText = progression.Strikes == 2 ? resultText + "ONE ADDITIONAL FAILURE WILL CONSTITUTE CONTRACT DEFAULT." 
                                              : resultText + "YOUR SHIFT WILL RESUME SHORTLY.";

        return resultText;
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
