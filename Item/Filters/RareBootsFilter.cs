using System;
using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;

namespace EZVendor.Item.Filters
{
    internal class RareBootsFilter : AbstractRareItem
    {
        private readonly bool _lessGarbage;

        public RareBootsFilter(GameController gameController,
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
                if (BaseItemType.ClassName != "Boots") return Actions.CantDecide;
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

                #region speed [0..4]

                var speed = GetWeight(MovementSpeed, 20, 25, 30, 35);
                weight += speed;

                #endregion

                return (_lessGarbage ? 1 : 0) + InitialWeight + weight >= 5f
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