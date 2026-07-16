using System;
using DG.Tweening;
using UnityEditor;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("Shake settings")]
    [SerializeField] private float shakeDuration;
    [SerializeField] private Vector3 strength = new Vector3(10f, 2f, 5f);
    [SerializeField] private int vibrato = 15;
    [SerializeField] private float randomness = 45f;

    public Tween TriggerCameraShake(float fallSpeed = 0.5f)
    {
        float intensityMultiplier = Mathf.Clamp(fallSpeed / 10f, 1f, 2.5f);
        Vector3 dynamicStrength = strength * intensityMultiplier;
        
        return transform.DOShakeRotation(shakeDuration, dynamicStrength, vibrato, randomness).SetEase(Ease.OutBounce);
    }
}
