using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _sfxSource;

    [SerializeField] private AudioClip _defaultMenuMusicSource;
    [SerializeField] private AudioClip _defaultClickSfxSource;
    
    public float MusicVolume { get; private set; }
    public float SfxVolume { get; private set; }
    
    public AudioSource MusicSource => _musicSource;
    public AudioSource SfxSource => _sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        MusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        SfxVolume = PlayerPrefs.GetFloat("SfxVolume", 1f);
        _musicSource.volume = MusicVolume;
        _sfxSource.volume = SfxVolume;
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = volume;
        _musicSource.volume = MusicVolume;
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = volume;
        _sfxSource.volume = SfxVolume;
        PlayerPrefs.SetFloat("SfxVolume", volume);
    }

    private void Start()
    {
        if (_defaultMenuMusicSource == null) return;
        PlayMusic(_defaultMenuMusicSource);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || _musicSource == null) return;
        _musicSource.clip = clip;
        _musicSource.loop = true;
        _musicSource.Play();
    }

    public void PlaySfx(AudioClip clip)
    {
        PlaySfxWithSettings(clip, 1f, 0f, 0f, 1f);
    }

    public void PlaySfxWithSettings(AudioClip clip, float pitch, float panStereo, float spatialBlend, float reverbZoneMix)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.clip = clip;
        _sfxSource.pitch = pitch;
        _sfxSource.spatialBlend = spatialBlend;
        _sfxSource.panStereo = panStereo;
        _sfxSource.reverbZoneMix = reverbZoneMix;
        _sfxSource.PlayOneShot(clip);
    }

    public void PlayDefaultClickSfx()
    {
        if (_defaultClickSfxSource == null) return;
        PlaySfx(_defaultClickSfxSource);
    }
}
