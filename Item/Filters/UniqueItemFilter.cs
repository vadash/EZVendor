using System;
using System.Linq;
using System.Text.RegularExpressions;
using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;
using EZVendor.Item.Ninja;

namespace EZVendor.Item.Filters
{
    internal class UniqueItemFilter : AbstractBasicItem
    {
        private readonly INinjaProvider _ninjaProvider;

        public UniqueItemFilter(
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
                if (ItemModsComponent.UniqueName.Length <= 4) return Actions.Vendor;
                if (ItemModsComponent.UniqueName == @"Hotfooted") return Actions.Vendor; // Hotheaded
                var garbage = _ninjaProvider.GetCheapUniques();
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