using System;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;

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
                if (!Item.HasComponent<Sockets>()) return Actions.CantDecide;
                if (ItemModsComponent.UniqueName == "Tabula") return Actions.Vendor;
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