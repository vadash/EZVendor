using System;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;

namespace EZVendor.Item.Filters
{
    internal class RareOneHanded : AbstractRareItem
    {
        private readonly bool _lessGarbage;

        private readonly string[] _oneHandedBases =
        {
            "Dagger", "Rune Dagger", "Wand", "Sceptre", "Thrusting One Hand Sword",
            "Claw", "One Hand Sword", "One Hand Axe", "One Hand Mace"
        };

        public RareOneHanded(GameController gameController,
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
                if (!_oneHandedBases.Contains(BaseItemType.ClassName)) return Actions.CantDecide;
                var weight = 0f;

                if (GetModByName("GlobalSpellGemsLevel") > 0) weight++;
                if (GetModByName("MinionDamageOnWeaponEnhancedLevel50ModNew") > 0) weight++;

                return weight >= 1f
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