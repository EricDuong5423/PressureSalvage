using UnityEngine;

public interface IUsable
{
    void OnEquip(GameObject holder);
    void OnUnequip();
    void Use();
}
