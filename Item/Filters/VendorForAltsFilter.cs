using System;
using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;

namespace EZVendor.Item.Filters
{
    internal class VendorForAltsFilter : AbstractRareItem
    {
        private readonly bool _vendorTransmutes;
        private readonly bool _vendorScraps;

        public VendorForAltsFilter(
            GameController gameController,
            NormalInventoryItem normalInventoryItem,
            bool vendorTransmutes,
            bool vendorScraps)
            : base(gameController, normalInventoryItem)
        {
            _vendorTransmutes = vendorTransmutes;
            _vendorScraps = vendorScraps;
        }

        public override Actions Evaluate()
        {
            try
            {
                if (BaseItemType.ClassName == "Incubator" ||
                    Item.Path.Contains(@"Incubation"))
                {
                    return Actions.Keep;
                }
                
                if (_vendorTransmutes &&
                    new Random().NextDouble() < 0.75 &&
                    Item.Path == @"Metadata/Items/Currency/CurrencyUpgradeToMagic")
                    return Actions.Vendor;
                
                if (_vendorScraps &&
                    Item.Path == @"Metadata/Items/Currency/CurrencyArmourQuality")
                    return Actions.Vendor;

                if (ItemRarity == ItemRarity.Magic ||
                    ItemRarity == ItemRarity.Rare)
                    switch (BaseItemType.ClassName)
                    {
                        case "Body Armour":
                        case "Quiver":
                        case "Helmet":
                        case "Boots":
                        case "Gloves":
                        case "Shield":
                        case "Belt":
                        case "Ring":
                        case "Amulet":
                        case "Dagger":
                        case "Rune Dagger":
                        case "Wand":
                        case "Sceptre":
                        case "Thrusting One Hand Sword":
                        case "Staff":
                        case "Warstaff":
                        case "Claw":
                        case "One Hand Sword":
                        case "Two Hand Sword":
                        case "One Hand Axe":
                        case "Two Hand Axe":
                        case "One Hand Mace":
                        case "Two Hand Mace":
                        case "Bow":
                            return Actions.Vendor;
                        default:
                            return Actions.CantDecide;
                    }

                return Actions.CantDecide;
            }
            catch (Exception)
            {
                return Actions.Keep;
            }
        }
    }
}