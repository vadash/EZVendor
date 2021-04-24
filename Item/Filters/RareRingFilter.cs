using System;
using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;

// ReSharper disable UnusedVariable
namespace EZVendor.Item.Filters
{
    internal class RareRingFilter : AbstractRareItem
    {
        public RareRingFilter(GameController gameController,
            NormalInventoryItem normalInventoryItem, bool lessGarbage)
            : base(gameController, normalInventoryItem)
        {
        }

        public override Actions Evaluate()
        {
            try
            {
                if (ItemRarity != ItemRarity.Rare) return Actions.CantDecide;
                if (BaseItemType.ClassName != "Ring") return Actions.CantDecide;
                var weight = 0f;


                #region corupted [-1..0]

                if (Corrupted) weight--;

                #endregion

                #region socket [0..1]

                weight += GetWeight(Sockets, 1);

                #endregion

                #region attributes [0..1]

                var attributes = Math.Min(1,
                    GetWeight(Dexterity, 43) + GetWeight(Strength, 43) + GetWeight(Intelligence, 43));
                weight += attributes;

                #endregion

                #region defense [-1..2]

                var lifeValue = Math.Max(FlatLife, CanCraftFlatLife ? 55 : 0);
                var lifeWeight = GetWeight(lifeValue, 60);
                var lifeManaWeight = GetWeight(lifeValue + FlatMana, 95);
                var manaWeight = GetWeight(FlatMana, 65);
                var flatESWeight = GetWeight(FlatES, 44);
                var defenseTotal1 =
                    Math.Min(lifeWeight + lifeManaWeight + flatESWeight, 1) +
                    lifeWeight * manaWeight; // bonus for big life + mana
                if (CanCraftFlatLife && defenseTotal1 >= 1f) CraftedCount++;
                if (defenseTotal1 == 0f) defenseTotal1 = -1f;
                weight += defenseTotal1;

                #endregion

                #region physical [0..3] with penalty 1

                weight += Math.Max(0,
                    GetWeight(FlatPhysical, 10) +
                    GetWeight(WED, 37) +
                    GetWeight(FlatAccuracy, 351) +
                    GetWeight(AttackSpeed, 5) - 1);

                #endregion

                #region resist [0..3]

                var totalRes = GetWeight(TotalResists, 42, 76, 101) + (CanCraftResist ? 1 : 0);
                if (CanCraftResist) CraftedCount++;
                weight += totalRes;

                #endregion

                return InitialWeight + weight >= 3
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