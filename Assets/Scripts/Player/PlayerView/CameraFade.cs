using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraFade : MonoBehaviour
{
    public static CameraFade Instance { get; private set; }

    [SerializeField] private Volume fadeVolume;
    private ColorAdjustments _color;
    private float _fade;

    private void Awake()
    {
        Instance = this;
        if(fadeVolume != null) fadeVolume.profile.TryGet(out _color);
        SetFade(1f);
    }

    public void SetFade(float t)
    {
        _fade = Mathf.Clamp01(t);
        if (_color != null) _color.colorFilter.Override(Color.Lerp(Color.white, Color.black, _fade));
    }

    public Tween FadeIn(float dur = 1) => DOTween.To(() => _fade, SetFade, 0f, dur).SetUpdate(true);
    public Tween FadeOut(float dur = 1) => DOTween.To(() => _fade, SetFade, 1f, dur).SetUpdate(true);
    public void TransitionTo(string scene, float dur = 1f) => FadeOut(dur).OnComplete(() =>
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
    });
}
