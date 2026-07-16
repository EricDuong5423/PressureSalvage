using UnityEngine;

public abstract class UpgradeData: ScriptableObject
{
    public abstract bool CanApply(PlayerLoadout l);
    public abstract void Apply(PlayerLoadout l);
}

[CreateAssetMenu(menuName = "Game Data/Upgrade/Oxygen Tank")]
public class OxygenTankUpgrade : UpgradeData
{
    public int maxTier = 4;
    public override bool CanApply(PlayerLoadout l) => l.oxygenTankTier < maxTier;
    public override void Apply(PlayerLoadout l) => l.TryUpgradeTank(maxTier);
}

[CreateAssetMenu(menuName = "Game Data/Upgrade/Slot Count")]
public class SlotCountUpgrade : UpgradeData
{
    public int maxSlots = 4;
    public override bool CanApply(PlayerLoadout l) => l.slotCount < maxSlots;
    public override void Apply(PlayerLoadout l) => l.TryUpgradeSlots(maxSlots);
}
