using System;
using UnityEngine;

public class Flashlight : MonoBehaviour, IUsable
{
    [SerializeField] private Light beam;
    private bool on;

    private void Awake()
    {
        if (beam) beam.enabled = false;
    }

    public void OnEquip(GameObject holder)
    {
        if (beam) beam.enabled = on;
    }

    public void OnUnequip()
    {
        if (beam) beam.enabled = false;
    }

    public void Use()
    {
        on = !on;
        if (beam) beam.enabled = on;
    }
}
