using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(
    typeof(PlayerMotor),
    typeof(PlayerLook))]
public class InputManager : MonoBehaviour
{
    
    private PlayerInput playerInput;
    private PlayerInput.OnFootActions onFoot;
    public PlayerInput.OnFootActions OnFoot => onFoot;
    public bool ControlEnabled = true;

    private PlayerLook look;
    private PlayerMotor motor;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        playerInput = new PlayerInput();
        onFoot = playerInput.OnFoot;
        
        motor = GetComponent<PlayerMotor>();
        look = GetComponent<PlayerLook>();
        
        onFoot.Jump.performed += ctx => motor.Jump();
        onFoot.Sprint.performed += ctx => motor.Sprint(true);
        onFoot.Sprint.canceled += ctx => motor.Sprint(false);
    }

    private void OnEnable()
    {
        onFoot.Enable();
    }

    private void OnDisable()
    {
        onFoot.Disable();   
    }
    
    private void Update()
    {
        if (!ControlEnabled || motor == null)
            return;

        motor.ProcessMove(
            onFoot.Movement.ReadValue<Vector2>());
    }

    private void LateUpdate()
    {
        if (!ControlEnabled) return;
        look.ProcessLook(onFoot.Look.ReadValue<Vector2>());
    }
}
