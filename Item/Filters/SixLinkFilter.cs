using System;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;

namespace EZVendor.Item.Filters
{
    internal class SixLinkFilter : AbstractBasicItem
    {
        public SixLinkFilter(
            GameController gameController,
            NormalInventoryItem normalInventoryItem)
            : base(gameController, normalInventoryItem)
        {
        }

        public override Actions Evaluate()
        {
            try
            {
                if (ItemModsComponent.ItemRarity == ItemRarity.Unique) return Actions.CantDecide;
                if (!Item.HasComponent<Sockets>()) return Actions.CantDecide;
                return Item.GetComponent<Sockets>().LargestLinkSize == 6
                    ? Actions.Keep
                    : Actions.CantDecide;
            }
            catch (Exception)
            {
                return Actions.Keep;
            }
        }
    }
}