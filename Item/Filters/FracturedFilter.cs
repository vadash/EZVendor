using System;
using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;

namespace EZVendor.Item.Filters;

internal class FracturedFilter : AbstractBasicItem
{
    public FracturedFilter(
        GameController gameController,
        NormalInventoryItem normalInventoryItem)
        : base(gameController, normalInventoryItem)
    {
    }

    public override Actions Evaluate()
    {
        try
        {
            if (ItemBaseComponent == null) return Actions.CantDecide;
            return ItemModsComponent.FracturedStats.Count > 0
                ? Actions.Keep
                : Actions.CantDecide;
        }
        catch (Exception)
        {
            return Actions.Keep;
        }
    }
}