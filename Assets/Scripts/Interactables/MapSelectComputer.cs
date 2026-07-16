using UnityEngine;
using UnityEngine.InputSystem;

public class MapSelectComputer : Interactable
{
    [SerializeField] private GameObject holoCanvas;
    [SerializeField] private Canvas hologramCanvas;
    [SerializeField] private MapSelectUI ui;
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
        holoCanvas.SetActive(o);

        Cursor.lockState = o ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = o;
        var im = FindAnyObjectByType<InputManager>();
        if (im) im.ControlEnabled = !o;

        if (o)
        {
            if (hologramCanvas && hologramCanvas.worldCamera == null)
                hologramCanvas.worldCamera = Camera.main;   // render camera cho Screen Space - Camera
            ui.Build();
        }
    }
}
