using System;
using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;

namespace EZVendor.Item.Filters
{
    internal class InfluenceFilter : AbstractBasicItem
    {
        public InfluenceFilter(
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
                var n = 0;
                if (ItemBaseComponent.isCrusader) n++;
                if (ItemBaseComponent.isElder) n++;
                if (ItemBaseComponent.isHunter) n++;
                if (ItemBaseComponent.isRedeemer) n++;
                if (ItemBaseComponent.isShaper) n++;
                if (ItemBaseComponent.isWarlord) n++;
                return n == 1 || n == 2
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