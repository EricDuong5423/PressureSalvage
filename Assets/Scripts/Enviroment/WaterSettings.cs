using UnityEngine;

[CreateAssetMenu(fileName = "WaterSettings", menuName = "Enviroments/Water Settings")]
public class WaterSettings : ScriptableObject
{
    [Header("Show")]
    public string displayName = "SHALLOW MAP";
    [Header("Player")]
    public float walkSpeed = 2f;
    public float sprintSpeed = 3.5f;
    [Tooltip("low = floating")] public float acceleration = 3f;
    public float gravity = -2.5f;
    [Header("Item")]
    public float objectDrag = 3f;
    public float objectBuoyancy = 7f;
}
