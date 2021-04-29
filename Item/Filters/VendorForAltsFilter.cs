using System;
using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;

namespace EZVendor.Item.Filters
{
    internal class VendorForAltsFilter : AbstractRareItem
    {
        public VendorForAltsFilter(
            GameController gameController,
            NormalInventoryItem normalInventoryItem)
            : base(gameController, normalInventoryItem)
        {
        }

        public override Actions Evaluate()
        {
            try
            {
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
                        case "AbyssJewel":
                        case "Jewel":
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