using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayMenu : MonoBehaviour
{
    [SerializeField] private InputActionReference _openMenuAction;
    [SerializeField] private CanvasGroup _hudCanvas;
    [SerializeField] private CanvasGroup _mainCanvas;
    [SerializeField] private CanvasGroup _settingsCanvas;
    [SerializeField] private CanvasGroup _cheatingCanvas;
    
    private CanvasGroup _menuCanvas;
    private bool isOpen = false;
    
    public void SetOpen(bool open) => this.isOpen = open;

    private void Awake()
    {
        _menuCanvas = GetComponent<CanvasGroup>();
        if (_menuCanvas == null) return;
        _menuCanvas.alpha = 0;
    }

    public void OnOffMenu()
    {
        Cursor.lockState = isOpen ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isOpen;
        if (_menuCanvas == null) return;
        _menuCanvas.alpha = isOpen ? 0 : 1;
        _menuCanvas.interactable = !isOpen;
        _menuCanvas.blocksRaycasts = !isOpen;
        Time.timeScale = isOpen ? 1 : 0;
        if (_hudCanvas == null) return;
        _hudCanvas.alpha = isOpen ? 1 : 0;
        _hudCanvas.interactable = isOpen;
        _hudCanvas.blocksRaycasts = isOpen;
        
        Time.timeScale = isOpen ? 1 : 0;

        if (isOpen)
        {
            if (_settingsCanvas == null) return;
            _settingsCanvas.alpha = 0;
            _settingsCanvas.interactable = false;
            _settingsCanvas.blocksRaycasts = false;
            
            if (_cheatingCanvas == null) return;
            _cheatingCanvas.alpha = 0;
            _cheatingCanvas.interactable = false;
            _cheatingCanvas.blocksRaycasts = false;

            _mainCanvas.alpha = 1;
            _mainCanvas.interactable = true;
            _mainCanvas.blocksRaycasts = true;
        }
            
        isOpen = !isOpen;
    }

    private void Update()
    {
        if (_openMenuAction != null && _openMenuAction.action.WasPressedThisFrame())
        {
            OnOffMenu();
        }
    }
}
