using System;
using DG.Tweening;
using UnityEngine;

public class EyeOpeningEffect : MonoBehaviour
{
    [SerializeField] private CanvasGroup blackout;
    [SerializeField] private RectTransform topEyelid;
    [SerializeField] private RectTransform bottomEyelid;

    [SerializeField] private float lidTravel = 700f;
    [SerializeField] private float openDuration = 3f;

    private Vector2 _topClosedPosition;
    private Vector2 _bottomClosedPosition;

    private void Awake()
    {
        _topClosedPosition = topEyelid.anchoredPosition;
        _bottomClosedPosition = bottomEyelid.anchoredPosition;
    }

    public void CloseInstant()
    {
        gameObject.SetActive(true);
        
        blackout.alpha = 1f;
        topEyelid.anchoredPosition = _topClosedPosition;
        bottomEyelid.anchoredPosition = _bottomClosedPosition;
    }

    public Tween OpenEyes()
    {
        gameObject.SetActive(true);
        
        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        
        sequence.Append(blackout.DOFade(0f, openDuration * 0.35f));

        sequence.Join(topEyelid.DOAnchorPosY(
            _topClosedPosition.y + lidTravel,
            openDuration));
        
        sequence.Join(bottomEyelid.DOAnchorPosY(
                _bottomClosedPosition.y - lidTravel,
                openDuration));

        sequence.SetEase(Ease.InBack);
        
        return sequence;
    }
    
    public Tween FadeToBlack(float duration)
    {
        gameObject.SetActive(true);
        blackout.alpha = 0f;

        return blackout
            .DOFade(1f, duration)
            .SetUpdate(true);
    }
    
    public Tween FadeFromBlack(float duration)
    {
        return blackout
            .DOFade(0f, duration)
            .SetUpdate(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
