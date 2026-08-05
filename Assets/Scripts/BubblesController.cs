using System;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class BubblesController : MonoBehaviour
{
    [SerializeField] private PlayerMotor _playerMotor;
    [SerializeField] private OxygenSystem _oxygenSystem;
    [Header("Emission Rate")] 
    [SerializeField] private float idleRate = 5f;
    [SerializeField] private float sprintRate = 12f;
    [SerializeField] private float panicRate = 25f;
    [SerializeField] private float lerpSpeed = 3f;
    
    private ParticleSystem.EmissionModule _emission;
    private float _currentRate;
    
    private void Awake()
    {
        _emission = GetComponent<ParticleSystem>().emission;
        _currentRate = idleRate;
    }

    private void Update()
    {
        if (UnderwaterEnvironment.Instance.Settings.displayName.Equals("SUBMARINE"))
        {
            _emission.rateOverTime = 0;
            return;
        }
        float target = idleRate;
        if(_oxygenSystem.IsPanic) target = panicRate;
        else if(_playerMotor.IsSprinting) target = sprintRate;
        
        _currentRate = Mathf.Lerp(_currentRate, target, lerpSpeed * Time.deltaTime);
        _emission.rateOverTime = _currentRate;
    }
}
