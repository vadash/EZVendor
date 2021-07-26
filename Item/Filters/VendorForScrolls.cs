using System;
using System.Collections.Generic;
using ExileCore;
using ExileCore.PoEMemory.Components;
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
                Item.HasComponent<Stack>() &&
                Item.GetComponent<Stack>().Size <= 15 && // dont vendor full stack transmutes
                new Random().NextDouble() < 0.75 &&
                Item.Path == @"Metadata/Items/Currency/CurrencyUpgradeToMagic")
                return Actions.Vendor;
                
            if (_vendorScraps &&
                new Random().NextDouble() < 0.9 &&
                Item.Path == @"Metadata/Items/Currency/CurrencyArmourQuality")
                return Actions.Vendor;
            
            if (_vendorScraps &&
                new Random().NextDouble() < 0.9 &&
                Item.Path == @"Metadata/Items/Currency/CurrencyWeaponQuality")
                return Actions.Vendor;
            
            return Actions.CantDecide;
        }
    }
}