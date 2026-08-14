using System.Collections;
using UnityEngine;

public class BearTrap : Trap
{
    [SerializeField, Min(0.1f)]
    private float minimumHoldDuration = 0.5f;
    [SerializeField, Min(0.1f)]
    private float maximumHoldDuration = 2f; 
    private InputManager inputManager;
    public override bool TryTrap(GameObject player)
    {
        if (isTrap || player == null) return false;
        
        inputManager = player.GetComponent<InputManager>();
        
        if (inputManager == null) return false;
        
        isTrap = true;
        
        if (trapCollider != null) trapCollider.enabled = false;
        
        StartCoroutine(StopMovingEffect());
        return true;
    }

    private IEnumerator StopMovingEffect()
    {
        bool controlsWereEnabled = inputManager.isTrap;
        inputManager.isTrap = true;

        float duration = Random.Range(
            minimumHoldDuration,
            maximumHoldDuration);

        yield return new WaitForSecondsRealtime(duration);
        
        if (!controlsWereEnabled && inputManager != null)
            inputManager.isTrap = false;
    }   
}
