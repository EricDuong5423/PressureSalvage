using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Shop/Upgrade Offer")]
public class UpgradeOffer : ShopOffer
{
    public UpgradeData upgrade;
    public override bool CanBuy() => PlayerLoadout.Instance != null && upgrade.CanApply(PlayerLoadout.Instance);

    public override void Purchase() => upgrade.Apply(PlayerLoadout.Instance);
}
