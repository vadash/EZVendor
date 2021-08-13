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
                if (!Item.HasComponent<Sockets>()) return Actions.CantDecide; // skip no sockets items
                if (ItemModsComponent.ItemRarity == ItemRarity.Unique) return Actions.CantDecide; // unique will be priced by other filter
                return Item.GetComponent<Sockets>().LargestLinkSize == 6
                    ? Actions.Vendor
                    : Actions.CantDecide;
            }
            catch (Exception)
            {
                return Actions.Keep;
            }
        }
    }
}