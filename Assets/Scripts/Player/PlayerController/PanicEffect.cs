using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(OxygenSystem))]
public class PanicEffect : MonoBehaviour
{
    [SerializeField] private Volume panicVolume;
    [SerializeField] private float transitionSpeed = 2f;
    // Audio Clip
    [SerializeField] private float minBeatInterval = 0.35f;
    [SerializeField] private float maxBeatInterval = 1.1f;
    
    //Effect
    private OxygenSystem _oxygenSystem;
    private Vignette _vignette;
    private ChromaticAberration _chromaticAberration;
    private ColorAdjustments _colorAdjustments;
    private LensDistortion _lensDistortion;
    private DepthOfField _depthOfField;
    
    //Smoothed current values
    private float _curSaturation;
    private Color _curColorFilter = Color.white;
    private float _curLensDistortion;
    private float _curVignetteIntensity;
    private Color _curVignetteColor = Color.black;
    private float _curChromatic;
    private float _curDofRadius;
    
    //Coroutine
    private Coroutine heartBeatCoroutine;

    private void Awake() => _oxygenSystem = GetComponent<OxygenSystem>();

    private void Start()
    {
        var profile = panicVolume.profile;
        profile.TryGet(out _chromaticAberration);
        profile.TryGet(out _lensDistortion);
        profile.TryGet(out _depthOfField);
        profile.TryGet(out _colorAdjustments);
        profile.TryGet(out _vignette);
    }

    private void Update()
    {
        float currentOxy = _oxygenSystem.CurrentPercent;
        float smooth = transitionSpeed * Time.deltaTime;

        //Stage 1 (60% - 20%) - mất màu + tint xanh
        float colorT = Mathf.InverseLerp(60f, 20f, currentOxy);
        float targetSat = Mathf.Lerp(0f, -60f, colorT);
        Color targetFilter = Color.Lerp(Color.white, new Color(0.55f, 0.82f, 1f), colorT * 0.5f);
        _curSaturation  = Mathf.Lerp(_curSaturation, targetSat, smooth);
        _curColorFilter = Color.Lerp(_curColorFilter, targetFilter, smooth);
        _colorAdjustments?.saturation.Override(_curSaturation);
        _colorAdjustments?.colorFilter.Override(_curColorFilter);

        //Stage 2: Vignette + Lens Distortion (40% → 5%)
        float warnT = Mathf.InverseLerp(40f, 5f, currentOxy);
        float targetLens   = Mathf.Lerp(0f, -0.3f, warnT);
        Color targetVigCol = Color.Lerp(Color.black, new Color(0.78f, 0.07f, 0.28f), warnT);
        float targetVigInt = Mathf.Lerp(0f, 0.45f, warnT);
        _curLensDistortion    = Mathf.Lerp(_curLensDistortion, targetLens, smooth);
        _curVignetteColor     = Color.Lerp(_curVignetteColor, targetVigCol, smooth);
        _curVignetteIntensity = Mathf.Lerp(_curVignetteIntensity, targetVigInt, smooth);
        _lensDistortion?.intensity.Override(_curLensDistortion);
        _vignette?.color.Override(_curVignetteColor);
        _vignette?.intensity.Override(_curVignetteIntensity);

        //Stage 3: Panic (20% → 0%)
        float panicT = Mathf.InverseLerp(20f, 0f, currentOxy);
        bool isPanic = panicT > 0f;

        if (isPanic)
        {
            float pulse = (Mathf.Sin(Time.time * 1.8f * Mathf.PI * 2f) + 1f) * 0.5f;
            float targetVigPanic = 0.45f + pulse * panicT * 0.2f;
            _curVignetteIntensity = Mathf.Lerp(_curVignetteIntensity, targetVigPanic, smooth);
            _vignette?.intensity.Override(_curVignetteIntensity);

            _curChromatic = Mathf.Lerp(_curChromatic, panicT * 0.85f, smooth);
            _chromaticAberration?.intensity.Override(_curChromatic);

            _curDofRadius = Mathf.Lerp(_curDofRadius, panicT * 1.2f, smooth);
            _depthOfField?.gaussianStart.Override(0f);
            _depthOfField?.gaussianEnd.Override(0.01f);
            _depthOfField?.gaussianMaxRadius.Override(_curDofRadius);

            if (heartBeatCoroutine == null) heartBeatCoroutine = StartCoroutine(HeartbeatLoop());
        }
        else
        {
            _curChromatic = Mathf.Lerp(_curChromatic, 0f, smooth);
            _curDofRadius = Mathf.Lerp(_curDofRadius, 0f, smooth);
            _chromaticAberration?.intensity.Override(_curChromatic);
            _depthOfField?.gaussianMaxRadius.Override(_curDofRadius);

            if (heartBeatCoroutine != null)
            {
                StopCoroutine(heartBeatCoroutine);
                heartBeatCoroutine = null;
            }
        }
    }
    
    IEnumerator HeartbeatLoop()
    {
        while (true)
        {
            float t = Mathf.InverseLerp(_oxygenSystem.PanicThreshHoldRatio * 100f, 0f, _oxygenSystem.CurrentPercent);
            yield return new WaitForSeconds(Mathf.Lerp(maxBeatInterval, minBeatInterval, t));
        }
    }
}
