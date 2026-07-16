using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DiveExit : Interactable
{
    [Header("Refs")]
    [SerializeField] private SellZone _sellZone;
    [SerializeField] private Animator _cageAnimator;
    [SerializeField] private Transform riseTarget;

    [Header("CutScene")] 
    [SerializeField] private float closeDelay = 0.6f;
    [SerializeField] private float riseDuration = 5f;
    [SerializeField] private AnimationCurve riseEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Prompts")] 
    [SerializeField] private string openPrompt = "Open";
    
    private enum  State
    {
        Closed,
        Open,
        Rising
    }
    private State state = State.Closed;
    private bool playerInside;
    private Transform player;

    private void Start() => promptMessage = openPrompt;

    protected override void Interact()
    {
        switch (state)
        {
            case State.Closed:
                SetOpen(true);
                state = State.Open;
                promptMessage = openPrompt;
                break;
            case State.Open:
                if (playerInside)
                {
                    SetOpen(false);
                    state = State.Rising;
                    promptMessage = "";
                    StartCoroutine(RiseAndExit());
                }
                else
                {
                    SetOpen(false);
                    state = State.Closed;
                    promptMessage = openPrompt;
                }
                break;
        }
    }

    private void SetOpen(bool open)
    {
        if(_cageAnimator != null) _cageAnimator.SetBool("Open", open);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("Player")) return;
        playerInside = true;
        player = other.transform;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }

    private IEnumerator RiseAndExit()
    {
        var input = player ? player.GetComponent<InputManager>() : null;
        var cc    = player ? player.GetComponent<CharacterController>() : null;
        var oxygen = player ? player.GetComponent<OxygenSystem>() : null;
        if (oxygen != null) oxygen.enabled = false;
        if (input != null) input.enabled = false;
        if (cc != null) cc.enabled = false;
        if(player != null) player.SetParent(transform.parent);

        yield return new WaitForSeconds(closeDelay);
        
        Vector3 from = transform.parent.position;
        Vector3 to = riseTarget != null ? riseTarget.position : from + Vector3.up * 20f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / riseDuration;
            float e = riseEase.Evaluate(Mathf.Clamp01(t));
            transform.parent.position = Vector3.Lerp(from, to, e);
            CameraFade.Instance?.SetFade(e);
            yield return null;
        }
        if (_sellZone != null) _sellZone.SellAll();
        DiveExitUI.Instance?.Play();
    }
}
