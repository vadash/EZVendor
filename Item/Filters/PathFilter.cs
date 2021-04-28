using System;
using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;

namespace EZVendor.Item.Filters
{
    internal class PathFilter : AbstractBasicItem
    {
        public PathFilter(
            GameController gameController,
            NormalInventoryItem normalInventoryItem)
            : base(gameController, normalInventoryItem)
        {
        }

        public override Actions Evaluate()
        {
            try
            {
                if (Item.Path.Contains(@"/MapFragments/")) return Actions.Keep;
                return Actions.CantDecide;
            }
            catch (Exception)
            {
                return Actions.Keep;
            }
        }
    }
}