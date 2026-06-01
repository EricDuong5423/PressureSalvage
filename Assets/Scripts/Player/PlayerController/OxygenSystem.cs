using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OxygenSystem : MonoBehaviour
{
    [Header("Oxygen")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float baseDrainRate = 1f;
    [SerializeField] private float sprintDrainBonus = 1.5f;
    [SerializeField] private float weightDrainPerKg = 0.3f;

    [Header("Panic")] 
    [SerializeField] private float panicThreshHold = 0.2f;
    [SerializeField] private float panicMultiplier = 1.5f;

    [Header("Events")] 
    public UnityEvent<float> OnOxygenChanged;
    public UnityEvent OnOxygenDepleted;
    
    private float currentOxygen;
    private bool isDead;
    private List<ActiveDebuff> activeDebuffs = new();

    private PlayerMotor motor;
    private PlayerInteract interact;

    private struct ActiveDebuff
    {
        public float drainPerSec;
        public float duration;
        public float elapsed;
    }

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        interact = GetComponent<PlayerInteract>();
        currentOxygen = maxOxygen;
        isDead = false;
    }

    private void Update()
    {
        if (isDead) return;
        float drain = baseDrainRate;

        //When running
        if (motor.IsSprinting) 
            drain += sprintDrainBonus;

        drain += interact.CarriedWeightKg * weightDrainPerKg;
        for (int i = activeDebuffs.Count - 1; i >= 0; i--)
        {
            var debuff = activeDebuffs[i];
            drain += debuff.drainPerSec;
            debuff.elapsed += Time.deltaTime;
            if (debuff.elapsed >= debuff.duration) activeDebuffs.RemoveAt(i);
            else activeDebuffs[i] = debuff;
        }
        
        float ratio = currentOxygen / maxOxygen;
        if(ratio <= panicThreshHold) 
            drain *=  panicMultiplier;
        
        currentOxygen = Mathf.Clamp(currentOxygen - drain * Time.deltaTime, 0f, maxOxygen);
        
        Debug.Log("Oxygen: " + currentOxygen);
        Debug.Log("Drain: " + drain);
        
        float percent = currentOxygen / maxOxygen * 100f;
        OnOxygenChanged?.Invoke(percent);
        
        if (currentOxygen <= 0f && !isDead)
        {
            isDead = true;
            OnOxygenDepleted.Invoke();
        }
    }

    public void AddDrain(float drainPerSec, float duration)
    {
        activeDebuffs.Add(new ActiveDebuff
        {
            drainPerSec = drainPerSec,
            duration = duration,
            elapsed = 0f
        });
    }

    public void RestoreOxygen(float amount)
    {
        currentOxygen = Mathf.Clamp(currentOxygen + amount, 0f, maxOxygen);
    }

    public float CurrentPercent => currentOxygen/maxOxygen * 100f;
    public bool IsPanic => CurrentPercent <= panicThreshHold;
}
