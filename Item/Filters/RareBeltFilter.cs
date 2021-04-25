using System;
using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;

// ReSharper disable UnusedVariable
namespace EZVendor.Item.Filters
{
    internal class RareBeltFilter : AbstractRareItem
    {
        private readonly bool _lessGarbage;

        public RareBeltFilter(GameController gameController,
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
                if (BaseItemType.ClassName != "Belt") return Actions.CantDecide;
                var weight = 0f;

                #region socket [0/2]

                weight += 2f * GetWeight(Sockets, 1);

                #endregion

                #region attributes [0..1]

                var attributes = Math.Min(1,
                    GetWeight(Dexterity, 43) + GetWeight(Strength, 43) + GetWeight(Intelligence, 43));
                weight += attributes;

                #endregion

                #region defense [-1..2]

                var lifeValue = Math.Max(FlatLife, CanCraftFlatLife ? 55 : 0);
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

                var armor = GetWeight(FlatArmor, 461);
                var wed = GetWeight(WED, 43);
                var flaskReduce = GetWeight(FlaskReduce, 10);
                weight += armor + wed + flaskReduce;

                #endregion

                return (_lessGarbage ? -1 : 0) + InitialWeight + weight >= 4f
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