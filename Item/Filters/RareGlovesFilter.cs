using System;
using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;

namespace EZVendor.Item.Filters
{
    internal class RareGlovesFilter : AbstractRareItem
    {
        private readonly bool _lessGarbage;

        public RareGlovesFilter(GameController gameController,
            NormalInventoryItem normalInventoryItem, bool lessGarbage)
            : base(gameController, normalInventoryItem)
        {
            _lessGarbage = lessGarbage;
        }

        public override Actions Evaluate()
        {
            try
            {
                if (ItemRarity != ItemRarity.Rare) return Actions.CantDecide;
                if (BaseItemType.ClassName != "Gloves") return Actions.CantDecide;
                var weight = 0f;

                #region attributes [0..1]

                var attributes = Math.Min(1,
                    GetWeight(Dexterity, 43) + GetWeight(Strength, 43) + GetWeight(Intelligence, 43));
                weight += attributes;

                #endregion

                #region defense [-1..2]

                var lifeValue = Math.Max(FlatLife, CanCraftFlatLife ? 70 : 0);
                var lifeWeight = GetWeight(lifeValue, 70);
                var lifeManaWeight = GetWeight(lifeValue + FlatMana, 120);
                var manaWeight = GetWeight(FlatMana, 65);
                var flatESWeight = GetWeight(FlatES, 39);
                var percentESWeight = GetWeight(PercentES, 80);
                var defenseTotal1 =
                    Math.Min(lifeWeight + lifeManaWeight + flatESWeight * percentESWeight, 1) +
                    lifeWeight * manaWeight + // bonus for big life + mana
                    flatESWeight * percentESWeight; // bonus for big ES flat + percent
                if (CanCraftFlatLife && defenseTotal1 >= 1f) CraftedCount++;
                if (defenseTotal1 == 0f) defenseTotal1 = -1f;
                weight += defenseTotal1;

                #endregion

                #region resist [0..3]

                var totalRes = GetWeight(TotalResists, 42, 76, 101) + (CanCraftResist ? 1 : 0);
                if (CanCraftResist) CraftedCount++;
                weight += totalRes;

                #endregion

                #region other [0..2]

                var aspd = GetWeight(FlatAccuracy, 351);
                var accuracy = GetWeight(AttackSpeed, 14);
                weight += aspd + accuracy;

                #endregion

                return (_lessGarbage ? 1 : 0) + InitialWeight + weight >= 4
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