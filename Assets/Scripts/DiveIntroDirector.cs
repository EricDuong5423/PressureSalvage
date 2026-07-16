using System.Collections;
using DG.Tweening;
using UnityEngine;

public class DiveIntroDirector : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private CageDescent cageDescent;
    [SerializeField] private EyeOpeningEffect eyeOpening;

    [Header("Camera")]
    [SerializeField] private Camera cutsceneCamera;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private AudioListener cutsceneAudioListener;

    [Header("UI")]
    [SerializeField] private CanvasGroup hudCanvasGroup;

    [Header("Timing")]
    [SerializeField] private float beforeDescentDelay = 0.4f;
    [SerializeField] private float landingHold = 0.6f;
    [SerializeField] private float doorOpenWait = 1f;
    [SerializeField] private float handoffFade = 0.35f;
    [SerializeField] private float playerFadeIn = 1f;
    
    
    private PlayerSpawner _playerSpawner;
    private SceneIntro _sceneIntro;
    private bool _started;

    private void Awake()
    {
        _playerSpawner = FindAnyObjectByType<PlayerSpawner>();
        
        _sceneIntro = FindAnyObjectByType<SceneIntro>();
        
        bool valid = _playerSpawner != null &&
                     cageDescent != null &&
                     eyeOpening != null &&
                     cutsceneCamera != null;

        if (!valid)
        {
            Debug.LogError("Error in cutscene");
            enabled = false;
            return;
        }
        
        _playerSpawner.SuppressAutoSpawn();

        if (_sceneIntro != null)
        {
            _sceneIntro.ConfigureCompletion(revealHud: false, useCameraFade: false);
            _sceneIntro.Completed += BeginIntro;

            if (hudCanvasGroup == null)
                hudCanvasGroup = _sceneIntro.HudCanvasGroup;
        }

        if (cameraShake == null)
        {
            Debug.LogError("CameraShake not set");
        }
        
        cageDescent.Prepare();
        eyeOpening.CloseInstant();

        SetCutsceneCamera(true);
        
        if (hudCanvasGroup != null) hudCanvasGroup.alpha = 0;
    }

    private void Start()
    {
        if (_sceneIntro == null || _sceneIntro.IsComplete)
        {
            BeginIntro();
        }
    }

    private void BeginIntro()
    {
        if (_started) return;
        
        _started = true;
        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        yield return eyeOpening.OpenEyes().WaitForCompletion();
        
        yield return new WaitForSeconds(beforeDescentDelay);
        
        Sequence descentSequence = DOTween.Sequence();

        descentSequence.Append(cageDescent.Descend()).Append(cameraShake.TriggerCameraShake());
        
        yield return descentSequence.WaitForCompletion();
        
        yield return new WaitForSeconds(landingHold);
        
        cageDescent.OpenDoor();
        
        yield return new WaitForSeconds(doorOpenWait);
        
        yield return eyeOpening.FadeToBlack(handoffFade).WaitForCompletion();
        
        yield return SpawnAndHandoff();
    }

    private IEnumerator SpawnAndHandoff()
    {
        GameObject player = _playerSpawner.SpawnPlayer();
        
        if (player == null)
        {
            Debug.LogError(
                "Không thể spawn player sau cutscene.",
                this);

            yield break;
        }
        
        CameraFade playerFade =
            player.GetComponentInChildren<CameraFade>(true);
        
        if (playerFade != null)
            playerFade.SetFade(1f);

        SetCutsceneCamera(false);

        if (playerFade != null)
        {
            eyeOpening.Hide();

            yield return playerFade
                .FadeIn(playerFadeIn)
                .WaitForCompletion();
        }
        else
        {
            yield return eyeOpening
                .FadeFromBlack(playerFadeIn)
                .WaitForCompletion();

            eyeOpening.Hide();
        }

        if (hudCanvasGroup != null)
        {
            yield return hudCanvasGroup
                .DOFade(1f, 0.35f)
                .SetUpdate(true)
                .WaitForCompletion();
        }
        
        cageDescent.EnableDiveExit();
    }
    
    private void SetCutsceneCamera(bool active)
    {
        if (cutsceneAudioListener != null)
            cutsceneAudioListener.enabled = active;

        if (cutsceneCamera != null)
            cutsceneCamera.enabled = active;
    }

    private void OnDestroy()
    {
        if (_sceneIntro != null)
            _sceneIntro.Completed -= BeginIntro;
    }
}
