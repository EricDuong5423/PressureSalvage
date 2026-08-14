using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class BarbedWire : Trap
{
    
    [SerializeField] private float minimumSlowPercent = 5f;
    [SerializeField] private float maximumSlowPercent = 80f;
    private float slowPercent;
    private PlayerMotor _motor;
    private float normalSprintSpeed;
    private float normalWalkSpeed;

    private void Start()
    {
        slowPercent = Random.Range(minimumSlowPercent, maximumSlowPercent);
    }

    private void OnTriggerExit(Collider other)
    {
        _motor.sprintSpeed =  normalSprintSpeed;
        _motor.walkSpeed = normalWalkSpeed;
    }

    public override bool TryTrap(GameObject player)
    {
        if (player == null) return false;
        _motor = player.GetComponent<PlayerMotor>();
        if (_motor == null) return false;
        normalSprintSpeed = _motor.sprintSpeed;
        _motor.sprintSpeed = _motor.sprintSpeed * (1 - slowPercent / 100f);
        normalWalkSpeed = _motor.walkSpeed;
        _motor.walkSpeed = _motor.walkSpeed * (1 - slowPercent / 100f);
        return true;
    }
}
