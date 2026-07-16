using System.Collections.Generic;
using UnityEngine;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private ShopOffer[] offers;
    [SerializeField] private ShopOfferButton buttonPrefab;
    [SerializeField] private Transform listContainer;
    
    private readonly List<ShopOfferButton> spawned = new();

    public void Build()
    {
        foreach (var b in spawned) if (b) Destroy(b.gameObject);
        spawned.Clear();
        foreach (var offer in offers)
        {
            var btn = Instantiate(buttonPrefab, listContainer);
            btn.Setup(offer, this);
            spawned.Add(btn);
        }
    }

    public void Buy(ShopOffer offer)
    {
        
        var g = GameProgressionManager.Instance;
        if (offer.CanBuy() && g != null && g.TrySpend(offer.cost))
        {
            offer.Purchase();
            if (offer is ItemOffer itemOffer && !itemOffer.consumable)
                FindAnyObjectByType<TeleportPad>()?.SpawnGear(itemOffer.item);
            
            Refresh();
        }
    }

    public void Refresh()
    {
        foreach (var b in spawned) if (b) b.Refresh();
    }
}
