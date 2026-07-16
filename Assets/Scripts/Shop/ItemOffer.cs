using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Shop/Item Offer")]
public class ItemOffer : ShopOffer
{
    public ItemData item;
    public bool consumable;

    public override bool CanBuy()
    {
        if (consumable) return true;
        var l = PlayerLoadout.Instance;
        return l != null && !l.ownedGear.Contains(item);
    }

    public override void Purchase()
    {
        if (consumable)
        {
            Inventory.Instance?.TryAdd(item, Random.Range(item.minValue, item.maxValue + 1));
            return;
        }
        PlayerLoadout.Instance?.ownedGear.Add(item);
    }
}
