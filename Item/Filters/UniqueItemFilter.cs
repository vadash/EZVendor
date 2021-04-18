using System;
using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;
using EZVendor.Item.Ninja;

namespace EZVendor.Item.Filters
{
    internal class UniqueItemFilter : AbstractBasicItem
    {
        private readonly INinjaProvider _ninjaProvider;

        public UniqueItemFilter(
            GameController gameController,
            NormalInventoryItem normalInventoryItem,
            INinjaProvider ninjaProvider)
            : base(gameController, normalInventoryItem)
        {
            _ninjaProvider = ninjaProvider;
        }

        public override Actions Evaluate()
        {
            try
            {
                if (ItemModsComponent.UniqueName.Length <= 4)
                    return Actions.Vendor;
                var garbage = _ninjaProvider.GetCheapUniques();
                return garbage.Contains(ItemModsComponent.UniqueName)
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