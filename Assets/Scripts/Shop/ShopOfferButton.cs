using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopOfferButton : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText, costText, descText;
    [SerializeField] private Button buyButton;

    private ShopOffer offer;
    private ShopUI owner;

    public void Setup(ShopOffer o, ShopUI ui)
    {
        offer = o;
        owner = ui;
        if (iconImage != null) iconImage.sprite = o.icon;
        if (nameText != null) nameText.text = o.displayName;
        if (costText != null) costText.text = $"Cost: {o.cost}₡";
        if (descText != null) descText.text = $"Description: {o.description}";
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() =>
            {
                owner.Buy(offer);
            });
        }
    }

    public void Refresh()
    {
        if (buyButton == null) return;
        var g = GameProgressionManager.Instance;
        buyButton.interactable = offer.CanBuy() && g != null && g.Credits >= offer.cost;
    }
}
