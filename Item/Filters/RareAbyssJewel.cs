using System;
using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;

namespace EZVendor.Item.Filters
{
    internal class RareAbyssJewel : AbstractRareItem
    {
        private readonly bool _lessGarbage;

        public RareAbyssJewel(GameController gameController,
            NormalInventoryItem normalInventoryItem, bool lessGarbage)
            : base(gameController, normalInventoryItem)
        {
            _lessGarbage = lessGarbage;
        }

        public override Actions Evaluate()
        {
            try
            {
                if (BaseItemType.ClassName != "AbyssJewel") return Actions.CantDecide;
                if (ItemRarity != ItemRarity.Rare) return Actions.CantDecide;
                var weight = 0f;

                #region defense [-1..2]

                var lifeWeight = GetWeight(FlatLife, 31);
                var flatESWeight = GetWeight(FlatES, 31);
                var defenseTotal1 = lifeWeight + flatESWeight;
                if (defenseTotal1 == 0f) defenseTotal1 = -1f;
                weight += defenseTotal1;

                #endregion

                return (_lessGarbage ? -1 : 0) + weight >= 1f
                    ? Actions.Keep
                    : Actions.Vendor;
            }
            catch (Exception)
            {
                return Actions.Keep;
            }
        }
    }
}