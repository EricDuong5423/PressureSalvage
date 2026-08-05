using System.Collections;
using UnityEngine;

public class RandomSpatialAmbienceEmitter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioListener _listener;
    [SerializeField] private AudioClip[] clips;

    [Header("Timing")]
    [SerializeField] private Vector2 delayRange = new(10f, 15f);

    [Header("Variation")]
    [SerializeField] private Vector2 panStereoRange = new(-1f, 1f);
    [SerializeField] private Vector2 pitchRange = new(0.9f, 1.05f);

    private AudioManager _audioManager;
    private Coroutine playRoutine;
    private int lastClipIndex = -1;

    private void Awake()
    {
        _audioManager = AudioManager.Instance;
        if (_audioManager == null)
        {
            Debug.LogError("No audio Manager found");
        }
    }

    private void OnEnable()
    {
        playRoutine = StartCoroutine(PlayLoop());
    }

    private void OnDisable()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (_audioManager != null)
            _audioManager.SfxSource.Stop();
    }

    private IEnumerator PlayLoop()
    {
        while (true)
        {
            float delay = Random.Range(
                delayRange.x,
                delayRange.y);
            
            yield return new WaitForSecondsRealtime(delay);

            AudioClip clip = GetRandomClip();

            if (clip == null || _listener == null)
                continue;

            float pitch = Random.Range(pitchRange.x, pitchRange.y);
            float pan = Random.Range(panStereoRange.x, panStereoRange.y);

            _audioManager.PlaySfxWithSettings(clip, pitch, pan, 0f, 1f);
            
            while (_audioManager.SfxSource.isPlaying)
                yield return null;
        }
    }

    private AudioClip GetRandomClip()
    {
        Debug.Log("Random clip");
        if (clips == null || clips.Length == 0)
            return null;

        int index = Random.Range(0, clips.Length);

        if (clips.Length > 1 && index == lastClipIndex)
        {
            int offset = Random.Range(1, clips.Length);
            index = (index + offset) % clips.Length;
        }

        lastClipIndex = index;
        return clips[index];
    }
}
