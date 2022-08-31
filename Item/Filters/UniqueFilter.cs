using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;
using EZVendor.Item.Ninja;

namespace EZVendor.Item.Filters
{
    internal class UniqueFilter : AbstractBasicItem
    {
        private readonly INinjaProvider _ninjaProvider;

        public UniqueFilter(
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
                if (ItemModsComponent.ItemRarity != ItemRarity.Unique) return Actions.CantDecide;
                if (ItemModsComponent.UniqueName == @"Divide and Conquer") return Actions.Vendor; // 1с jewel
                if (ItemModsComponent.UniqueName == @"Hotfooted") return Actions.Vendor; // Hotheaded
                if (ItemModsComponent.UniqueName == @"Ondar's Flight") return Actions.Vendor; // Victario's Flight
                if (ItemModsComponent.UniqueName == @"Rigvald's Charge") return Actions.Vendor; // big ass sword
                if (ItemModsComponent.UniqueName == @"Leper's Alms") return Actions.Vendor; // 3.19
                var garbage = Item?.GetComponent<Sockets>()?.LargestLinkSize == 6
                    ? _ninjaProvider.GetCheap6LUniques()
                    : _ninjaProvider.GetCheap0LUniques();
                return garbage.Any(name => IsSameName(name, ItemModsComponent.UniqueName))
                    ? Actions.Vendor
                    : Actions.CantDecide;
            }
            catch (Exception)
            {
                return Actions.Keep;
            }
        }

        private static bool IsSameName(string s1, string s2)
        {
            var rgx = new Regex("[^a-z]");
            s1 = rgx.Replace(s1.ToLower(), "");
            s2 = rgx.Replace(s2.ToLower(), "");
            return s1 == s2;
        }
    }
}