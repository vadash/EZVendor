using System;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;

namespace EZVendor.Item.Filters
{
    internal class FlaskFilter : AbstractBasicItem
    {
        public FlaskFilter(
            GameController gameController,
            NormalInventoryItem normalInventoryItem)
            : base(gameController, normalInventoryItem)
        {
        }

        public override Actions Evaluate()
        {
            try
            {
                return Item.HasComponent<Flask>()
                    ? ItemRarity == ItemRarity.Normal || ItemRarity == ItemRarity.Magic
                        ? Actions.Vendor
                        : Actions.CantDecide
                    : Actions.CantDecide;
            }
            catch (Exception)
            {
                return Actions.Keep;
            }
        }
    }
}