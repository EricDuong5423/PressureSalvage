using UnityEngine;

public abstract class ShopOffer : ScriptableObject
{
    public string displayName;
    public Sprite icon;
    [TextArea] public string description;
    public int cost;

    public abstract bool CanBuy();
    public abstract void Purchase();
}