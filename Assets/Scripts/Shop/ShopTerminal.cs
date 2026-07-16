using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShopTerminal : Interactable
{
    [SerializeField] private GameObject shopCanvas;
    [SerializeField] private Canvas screenCanvas;
    [SerializeField] private ShopUI ui;
    private bool open;

    protected override void Interact() => SetOpen(!open);
    public void Close() => SetOpen(false);

    private void Update()
    {
        if (open && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            SetOpen(false);
    }

    private void SetOpen(bool o)
    {
        open = o;
        shopCanvas.SetActive(open);
        Cursor.lockState = o ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = o;
        var im = FindAnyObjectByType<InputManager>();
        if (im != null) im.ControlEnabled =  !o;

        if (o)
        {
            if (screenCanvas && screenCanvas.worldCamera == null) screenCanvas.worldCamera = Camera.main;
            ui.Build();
        }
    }
}
