using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;

namespace EZVendor.Item.Filters
{
    internal class EnchantedFilter : AbstractBasicItem
    {
        private readonly IList<string> _goodEnchantsAnyBase = new List<string>
        {
            "Enchantment Temporal Chains Curse Effect 2",
            "Enchantment Temporal Chains Curse Effect 1",
            "Enchantment Lancing Steel Number of Additional Projectiles 1",
            "Enchantment Ice Nova Damage 2",
            "Enchantment Blade Vortex Duration 2",
            "Enchantment Berserk Effect 2",
            "Enchantment Spellslinger Reservation 2",
            "Enchantment Hatred Mana Reservation 2",
            "Enchantment Frost Bomb Cooldown Speed 2",
            "Storm Brand Attached Target Lightning Penetration 2",
            "Enchantment Cyclone Attack Speed 2",
            "Enchantment Barrage Num Of Additional Projectiles 1",
            "Enchantment General's Cry Additional Mirage Warrior 1",
            "Enchantment Herald Of Thunder Mana Reservation 2",
            "Enchantment Ice Spear Additional Projectile 1",
            // 10-15c below
            "Enchantment Toxic Rain Num Of Additional Projectiles 1",
            "Enchantment Elemental Hit Attack Speed 2",
            "Enchantment Frost Fury Additional Max Number Of Stages 1",
            "Enchantment Double Slash Added Phys To Bleeding 2",
            "Enchantment Tornado Shot Num Of Secondary Projectiles 2",
            "Enchantment Arc Num Of Additional Projectiles In Chain 1",
            "Enchantment Blade Blast Area of Effect 2"
        };

        public EnchantedFilter(
            GameController gameController,
            NormalInventoryItem normalInventoryItem)
            : base(gameController, normalInventoryItem)
        {
        }

        public override Actions Evaluate()
        {
            try
            {
                if (BaseItemType.ClassName != "Helmet") return Actions.CantDecide;
                if (!IsEnchanted()) return Actions.CantDecide;
                var enchantedMod = GetEnchantedMod();
                return _goodEnchantsAnyBase.Any(mod => IsEqual(enchantedMod, mod))
                    ? Actions.Keep
                    : Actions.Vendor;
            }
            catch (Exception)
            {
                return Actions.Keep;
            }
        }

        private static bool IsEqual(string s1, string s2)
        {
            s1 = s1.Trim().Replace("'", "").Replace(" ", "").ToLower();
            s2 = s2.Trim().Replace("'", "").Replace(" ", "").ToLower();
            return s1 == s2;
        }

        private bool IsEnchanted()
        {
            return !string.IsNullOrEmpty(GetEnchantedMod());
        }

        private string GetEnchantedMod()
        {
            return ItemModsComponent.ItemMods.Where(mod => mod?.Group == "SkillEnchantment")
                .Select(mod => mod.DisplayName)
                .FirstOrDefault();
        }
    }
}