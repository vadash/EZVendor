using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;

// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable MemberCanBePrivate.Global
namespace EZVendor.Item.Filters
{
    internal abstract class AbstractRareItem : AbstractBasicItem
    {
        protected readonly float AttackSpeed;
        protected readonly bool CanCraftFlatLife;
        protected readonly bool CanCraftResist;
        protected readonly float CastSpeed;
        protected readonly bool Corrupted;
        protected readonly float CritChance;
        protected readonly float CritDamage;
        protected readonly float Dexterity;
        protected readonly float FlaskReduce;
        protected readonly float FlatAccuracy;
        protected readonly float FlatArmor;
        protected readonly float FlatES;
        protected readonly float FlatLife;
        protected readonly float FlatMana;
        protected readonly float FlatPhysical;
        protected readonly bool HaveCraftedMod;
        protected readonly float Intelligence;
        protected readonly List<ItemMod> ItemMods;
        protected readonly ItemRarity ItemRarity;
        protected readonly float MovementSpeed;
        protected readonly float PercentES;
        protected readonly float PercentLife;
        protected readonly int PrefixCount;
        protected readonly float Sockets;
        protected readonly float Strength;
        protected readonly int SuffixCount;
        protected readonly float TotalResists;
        protected readonly bool Veiled;
        protected readonly float WED;

        /// <summary>
        ///     Used for simulations
        /// </summary>
        protected int CraftedCount = 0;

        protected AbstractRareItem(
            GameController gameController,
            NormalInventoryItem normalInventoryItem)
            : base(gameController, normalInventoryItem)
        {
            if (ItemModsComponent == null) return;
            ItemMods = ItemModsComponent.ItemMods;
            ItemRarity = ItemModsComponent.ItemRarity;
            HaveCraftedMod = CheckCraftedMod(ItemMods);
            var prefixPlusSuffixMods = FilterPrefixPlusSuffixMods(ItemMods);
            SuffixCount = FilterSuffixMods(prefixPlusSuffixMods).Count();
            PrefixCount = prefixPlusSuffixMods.Count() - SuffixCount;
            FlatLife = GetFlatLife();
            PercentLife = GetPercentLife();
            CanCraftFlatLife = GetCanCraftFlatLife();
            FlatMana = GetFlatMana();
            FlatES = GetFlatES();
            PercentES = GetPercentES();
            TotalResists = GetTotalResists();
            CanCraftResist = CheckCanCraftResist();
            MovementSpeed = GetMovementSpeed();
            Dexterity = GetDexterity();
            Intelligence = GetIntelligence();
            Strength = GetStrength();
            Sockets = GetSockets();
            FlatArmor = GetArmor();
            WED = GetWED();
            FlaskReduce = GetFlaskReduction();
            FlatAccuracy = GetFlatAccuracy();
            AttackSpeed = GetAttackSpeed();
            CastSpeed = GetCastSpeed();
            FlatPhysical = GetFlatPhys();
            CritChance = GetGlobalCritChance();
            CritDamage = GetCritMultiplier();
            Corrupted = Item.Path.Contains("Breach") ||
                        Item.Path.Contains("Talisman");
            Veiled = GetModByGroup("Veiled") > 0;
        }

        /// <summary>
        ///     Base value for total weight (counting veiled, crafted count)
        /// </summary>
        protected int InitialWeight => (CraftedCount > 1 ? 1 - CraftedCount : 0) + (Veiled ? 1 : 0);

        public abstract override Actions Evaluate();

        protected static float GetWeight(float value, float threshold1)
        {
            return value >= threshold1 ? 1 : 0;
        }

        protected float GetWeight(float value, float threshold1, float threshold2)
        {
            return GetWeight(value, threshold1) + GetWeight(value, threshold2);
        }

        protected float GetWeight(float value, float threshold1, float threshold2, float threshold3)
        {
            return GetWeight(value, threshold1) + GetWeight(value, threshold2) + GetWeight(value, threshold3);
        }

        protected static float GetWeight(float value, float threshold1, float threshold2, float threshold3,
            float threshold4)
        {
            return GetWeight(value, threshold1) + GetWeight(value, threshold2) + GetWeight(value, threshold3) +
                   GetWeight(value, threshold4);
        }

        private float GetFlatLife()
        {
            var total1 = GetModByGroup("IncreasedLife");
            var total2 = GetHumanMod(@"([0-9]+) to maximum Life");
            return Math.Max(total1, total2);
        }

        private float GetPercentLife()
        {
            var total1 = GetModByGroup("MaximumLifeIncreasePercent");
            var total2 = GetHumanMod(@"([0-9]+)% increased.+Life");
            return Math.Max(total1, total2);
        }

        private bool GetCanCraftFlatLife()
        {
            if (HaveCraftedMod) return false;
            return PrefixCount < 3 &&
                   GetModByNameGroup("IncreasedLife", "IncreasedLife") == 0;
        }

        private float GetFlatMana()
        {
            var total1 = GetModByGroup("IncreasedMana");
            var total2 = GetHumanMod(@"([0-9]+) to maximum Mana");
            return Math.Max(total1, total2);
        }

        private float GetFlatES()
        {
            var total1 = GetModByGroup("IncreasedEnergyShield");
            var total2 = GetHumanMod(@"([0-9]+) to maximum Energy Shield");
            return Math.Max(total1, total2);
        }

        private int GetPercentES()
        {
            var total1 = GetModByGroup("EnergyShieldPercent");
            var total2 = GetHumanMod(@"([0-9]+)% increased.+Energy Shield");
            return Math.Max(total1, total2);
        }

        private float GetTotalResists()
        {
            var total = 0;
            try
            {
                foreach (var mod in ItemMods.Where(mod => mod.Group.Contains("Resist")))
                    if (mod.Group.Contains("AllResistances")) total += mod.Value1 * 3;
                    else if (mod.Group.Contains("And")) total += mod.Value1 * 2;
                    else total += mod.Value1;
            }
            catch (Exception)
            {
                // ignored
            }

            return total;
        }

        private bool CheckCanCraftResist()
        {
            if (HaveCraftedMod) return false;
            return SuffixCount < 3;
        }

        private float GetFlatPhys()
        {
            return GetModByGroupWithAverage("PhysicalDamage");
        }

        private float GetAttackSpeed()
        {
            return GetModByGroup("IncreasedAttackSpeed");
        }

        private float GetMovementSpeed()
        {
            return GetModByGroup("MovementVelocity");
        }

        private float GetCastSpeed()
        {
            return GetModByGroup("IncreasedCastSpeed");
        }

        private float GetWED()
        {
            return GetModByGroup("ElementalDamagePercent");
        }

        private float GetArmor()
        {
            return GetModByGroup("IncreasedPhysicalDamageReductionRating");
        }

        private float GetFlaskReduction()
        {
            return GetModByGroup("BeltFlaskCharges");
        }

        private float GetFlatAccuracy()
        {
            return GetModByGroup("IncreasedAccuracy");
        }

        private float GetCritMultiplier()
        {
            return GetModByGroup("CriticalStrikeMultiplier");
        }

        private float GetGlobalCritChance()
        {
            return GetModByGroup("CriticalStrikeChanceIncrease");
        }

        private float GetDexterity()
        {
            return GetModByName("Dexterity") +
                   GetModByName("AllAttributes") +
                   GetModByName("HybridDexInt") +
                   GetModByName("HybridStrDex");
        }

        private float GetIntelligence()
        {
            return GetModByName("Intelligence") +
                   GetModByName("AllAttributes") +
                   GetModByName("HybridDexInt") +
                   GetModByName("HybridStrInt");
        }

        private float GetStrength()
        {
            return GetModByName("Strength") +
                   GetModByName("AllAttributes") +
                   GetModByName("HybridStrDex") +
                   GetModByName("HybridStrInt");
        }

        private float GetSockets()
        {
            var total = 0;
            try
            {
                if (Item.HasComponent<Sockets>()) total = Item.GetComponent<Sockets>().NumberOfSockets;
            }
            catch (Exception)
            {
                // ignored
            }

            return total;
        }

        private int GetHumanMod(string regexpStr)
        {
            var total = 0;
            try
            {
                var regexp1 = new Regex(regexpStr, RegexOptions.IgnoreCase);
                foreach (var match in ItemModsComponent.HumanStats.Select(mod => regexp1.Match(mod)))
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var result))
                        total += result;
            }
            catch (Exception)
            {
                // ignored
            }

            return total;
        }

        private int GetModByGroup(string partialModGroup)
        {
            var total = 0;
            try
            {
                total = ItemMods
                    .Where(mod => mod.Group.ToLower().Contains(partialModGroup.ToLower()))
                    .Sum(mod => mod.Value1);
            }
            catch (Exception)
            {
                // ignored
            }

            return Math.Abs(total);
        }

        protected int GetModByName(string partialModName)
        {
            var total = 0;
            try
            {
                total = ItemMods
                    .Where(mod => mod.Name.ToLower().Contains(partialModName.ToLower()))
                    .Sum(mod => mod.Value1);
            }
            catch (Exception)
            {
                // ignored
            }

            return Math.Abs(total);
        }

        private int GetModByNameGroup(string partialModName, string partialModGroup)
        {
            var total = 0;
            try
            {
                total = ItemMods
                    .Where(mod => mod.Name.ToLower().Contains(partialModName.ToLower()))
                    .Where(mod => mod.Group.ToLower().Contains(partialModGroup.ToLower()))
                    .Sum(mod => mod.Value1);
            }
            catch (Exception)
            {
                // ignored
            }

            return Math.Abs(total);
        }

        private float GetModByGroupWithAverage(string partialModGroup)
        {
            var total = 0f;
            try
            {
                total = ItemMods
                    .Where(mod => mod.Group.ToLower().Contains(partialModGroup.ToLower()))
                    .Sum(mod => (mod.Value1 + mod.Value2) / 2f);
            }
            catch (Exception)
            {
                // ignored
            }

            return total;
        }

        private static bool CheckCraftedMod(IEnumerable<ItemMod> input)
        {
            return input.Any(itemMod => itemMod.Name.Contains("Master"));
        }

        private static IEnumerable<ItemMod> FilterPrefixPlusSuffixMods(IEnumerable<ItemMod> input)
        {
            return input.Where(itemMod => !itemMod.Name.Contains("Enchantment") &&
                                          !itemMod.Group.Contains("Enchantment") &
                                          !itemMod.Name.Contains("Implicit") &&
                                          !itemMod.Group.Contains("Implicit"));
        }

        private static IEnumerable<ItemMod> FilterSuffixMods(IEnumerable<ItemMod> input)
        {
            return input.Where(itemMod => itemMod.DisplayName.Contains("of"));
        }
    }
}