using System;
using UnityEngine;
using UnityEngine.UI;

public class SoundAdjuster : MonoBehaviour
{
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Slider _sfxSlider;
    
    private AudioManager _audioManager;

    private void Start()
    {
        _audioManager = AudioManager.Instance;
        if (_audioManager == null) return;
        if (_musicSlider == null || _sfxSlider == null) return;
        
        _musicSlider.value = _audioManager.MusicVolume;
        _sfxSlider.value = _audioManager.SfxVolume;
        
        _musicSlider.onValueChanged.AddListener(OnChangeMusicVolume);
        _sfxSlider.onValueChanged.AddListener(OnChangeSFXVolume);
    }

    private void OnDestroy()
    {
        if (_musicSlider == null || _sfxSlider == null) return;
        _musicSlider.onValueChanged.RemoveListener(OnChangeMusicVolume);
        _sfxSlider.onValueChanged.RemoveListener(OnChangeSFXVolume);
    }

    private void OnChangeSFXVolume(float volume)
    {
        _audioManager.SetSfxVolume(volume);
    }

    private void OnChangeMusicVolume(float volume)
    {
        _audioManager.SetMusicVolume(volume);
    }
}
