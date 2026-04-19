using UnityEngine;

public interface ICarryable
{
    public void Carry(Transform parent);
    public void Drop();
    public bool IsTwoHandRequired { get; }
}
