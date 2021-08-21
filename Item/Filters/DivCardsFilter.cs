using System;
using System.Linq;
using System.Text.RegularExpressions;
using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;
using EZVendor.Item.DivCards;

namespace EZVendor.Item.Filters
{
    internal class DivCardsFilter : AbstractBasicItem
    {
        private readonly IDivCardsProvider _divCardsProvider;

        public DivCardsFilter(
            GameController gameController,
            NormalInventoryItem normalInventoryItem,
            IDivCardsProvider divCardsProvider)
            : base(gameController, normalInventoryItem)
        {
            _divCardsProvider = divCardsProvider;
        }

        public override Actions Evaluate()
        {
            try
            {
                if (BaseItemType.ClassName != "DivinationCard") return Actions.CantDecide;
                var garbage = _divCardsProvider.GetSellDivCardsList();
                return garbage.Any(name => IsSameName(name, ItemModsComponent.UniqueName))
                    ? Actions.Vendor
                    : Actions.Keep;
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