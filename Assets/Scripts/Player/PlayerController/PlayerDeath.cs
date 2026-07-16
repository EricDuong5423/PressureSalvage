using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(OxygenSystem))]
public class PlayerDeath : MonoBehaviour
{
    public UnityEvent OnPlayerDrown;

    private OxygenSystem _oxygenSystem;
    private InputManager _inputManager;
    private PlayerInteract _playerInteract;
    
    private bool _isDrowning;

    private void Awake()
    {
        _oxygenSystem =  GetComponent<OxygenSystem>();
        _inputManager = GetComponent<InputManager>();
        _playerInteract = GetComponent<PlayerInteract>();
        
        _oxygenSystem.OnOxygenDepleted.AddListener(OnDrown);
    }
    
    private void OnDestroy()
    {
        if (_oxygenSystem != null)
            _oxygenSystem.OnOxygenDepleted.RemoveListener(OnDrown);
    }

    private void OnDrown()
    {
        StartCoroutine(PlayingDeathAnimation());
        if (_isDrowning)
            return;

        _isDrowning = true;

        if (_inputManager != null)
            _inputManager.enabled = false;

        if (_playerInteract != null)
            _playerInteract.enabled = false;

        OnPlayerDrown?.Invoke();

        if (DeathSceneUI.Instance != null)
        {
            DeathSceneUI.Instance.Play();
        }
        else
        {
            Debug.LogWarning(
                "Không có DeathScreenUI, reinstate ngay.",
                this);

            GameProgressionManager.Instance?.Reinstate();
        }
    }

    private IEnumerator PlayingDeathAnimation()
    {
        yield return transform.DORotate(new Vector3(90, 0, 0), 2f)
            .SetUpdate(true)
            .SetEase(Ease.InOutBounce)
            .WaitForCompletion();
    }
}
