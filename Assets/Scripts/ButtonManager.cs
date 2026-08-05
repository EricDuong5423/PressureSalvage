using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private bool usingDefault = true;
    [SerializeField] private List<AudioClip> _randomClips = new List<AudioClip>();
    
    private AudioManager _audioManager;
    
    private void Awake()
    {
        if(_button == null) _button = GetComponent<Button>();
    }

    private void Start()
    {
        _audioManager = AudioManager.Instance;
        if (_audioManager == null) return;
        if (usingDefault)
        {
            _button.onClick.AddListener(_audioManager.PlayDefaultClickSfx);
            return;
        }
        _button.onClick.AddListener(PlayRandomClip);
    }

    private void PlayRandomClip()
    {
        if (_randomClips.Count == 0 || _randomClips == null) return;
        AudioClip playClip = _randomClips[Random.Range(0, _randomClips.Count)];
        _audioManager.PlaySfx(playClip);
    }

    private void OnDestroy()
    {
        if (_audioManager == null) return;
        if (usingDefault)
        {
            _button.onClick.RemoveListener(_audioManager.PlayDefaultClickSfx);
            return;
        }
        _button.onClick.RemoveListener(PlayRandomClip);
    }
}
