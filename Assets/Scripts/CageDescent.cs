using DG.Tweening;
using UnityEngine;

public class CageDescent : MonoBehaviour
{
    [SerializeField] private Transform cageDropRoot;
    [SerializeField] private Animator cageAnimator;
    [SerializeField] private DiveExit diveExit;

    [SerializeField] private string openParameter = "Open";
    [SerializeField] private float startHeight = 30f;
    [SerializeField] private float descentDuration = 8f;
    [SerializeField] private AnimationCurve descentCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector3 _landingPosition;
    
    public float Duration => descentDuration;

    public void Prepare()
    {
        _landingPosition = cageDropRoot.position;
        
        cageDropRoot.position = _landingPosition + Vector3.up * startHeight;
        
        if (cageAnimator != null)
            cageAnimator.SetBool(openParameter, false);

        if (diveExit != null)
            diveExit.enabled = false;
    }

    public Tween Descend()
    {
        return cageDropRoot.DOMove(_landingPosition, descentDuration)
            .SetEase(descentCurve);
    }

    public void OpenDoor()
    {
        if (cageAnimator != null)
            cageAnimator.SetBool(openParameter, true);
    }

    public void EnableDiveExit()
    {
        if (diveExit != null)
            diveExit.enabled = true;
    }
}
