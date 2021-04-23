using System;
using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;

namespace EZVendor.Item.Filters
{
    internal class VendorForScrolls : AbstractBasicItem
    {
        private readonly bool _vendorTransmutes;
        private readonly bool _vendorScraps;

        public VendorForScrolls(
            GameController gameController,
            NormalInventoryItem normalInventoryItem,
            bool vendorTransmutes,
            bool vendorScraps) : base(
            gameController, normalInventoryItem)
        {
            _vendorTransmutes = vendorTransmutes;
            _vendorScraps = vendorScraps;
        }

        public override Actions Evaluate()
        {
            if (BaseItemType.ClassName == "Incubator" ||
                Item.Path.Contains(@"Incubation"))
            {
                return Actions.Keep;
            }
                
            if (_vendorTransmutes &&
                new Random().NextDouble() < 0.6 &&
                Item.Path == @"Metadata/Items/Currency/CurrencyUpgradeToMagic")
                return Actions.Vendor;
                
            if (_vendorScraps &&
                new Random().NextDouble() < 0.85 &&
                Item.Path == @"Metadata/Items/Currency/CurrencyArmourQuality")
                return Actions.Vendor;
            
            if (_vendorScraps &&
                new Random().NextDouble() < 0.85 &&
                Item.Path == @"Metadata/Items/Currency/CurrencyWeaponQuality")
                return Actions.Vendor;
            
            return Actions.CantDecide;
        }
    }
}