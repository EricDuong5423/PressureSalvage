using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class SwarmTrap : Trap
{
    [SerializeField] private float minimumDrainPerSecond = 1f;
    [SerializeField] private float maximumDrainPerSecond = 20f;
    private float currentDrainPerSecond;
    private OxygenSystem oxygenSystem;
    private OxygenSystem.ActiveDebuff activeDebuff;
    private void Start()
    {
        currentDrainPerSecond = Random.Range(minimumDrainPerSecond, maximumDrainPerSecond);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") || oxygenSystem == null)
            return;

        oxygenSystem.activeDebuffs.Remove(activeDebuff);

        oxygenSystem = null;
        activeDebuff = null;
    }

    public override bool TryTrap(GameObject player)
    {
        if (player == null || oxygenSystem != null)
            return false;

        oxygenSystem = player.GetComponent<OxygenSystem>();

        if (oxygenSystem == null)
            return false;

        activeDebuff = new OxygenSystem.ActiveDebuff
        {
            drainPerSec = currentDrainPerSecond,
            duration = float.PositiveInfinity,
            elapsed = 0f
        };

        oxygenSystem.activeDebuffs.Add(activeDebuff);
        return true;
    }
}
