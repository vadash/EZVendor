using System;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;

namespace EZVendor.Item.Filters
{
    internal class SixSocketFilter : AbstractBasicItem
    {
        public SixSocketFilter(
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
                return Item.GetComponent<Sockets>().NumberOfSockets == 6 &&
                       Item.GetComponent<Sockets>().LargestLinkSize < 6
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