using System;
using System.Collections.Generic;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;
using EZVendor.Item.Filters;
using EZVendor.Item.Ninja;

namespace EZVendor.Item
{
    internal class ItemFactory : IItemFactory
    {
        private readonly GameController _gameController;
        private readonly INinjaProvider _ninjaProvider;
        private readonly bool _vendorTransmutes;
        private readonly bool _vendorScraps;

        public ItemFactory(
            GameController gameController,
            INinjaProvider ninjaProvider,
            bool vendorTransmutes,
            bool vendorScraps
        )
        {
            _gameController = gameController;
            _ninjaProvider = ninjaProvider;
            _vendorTransmutes = vendorTransmutes;
            _vendorScraps = vendorScraps;
        }

        public Actions Evaluate(NormalInventoryItem normalInventoryItem)
        {
            try
            {
                #region keep invalid items

                var item = normalInventoryItem?.Item;
                if (item == null ||
                    item.Address == 0 ||
                    !item.IsValid ||
                    !item.HasComponent<Base>())
                    return Actions.Keep;
                
                if (item.GetComponent<Mods>()?.ItemMods == null)
                {
                    return item?.GetComponent<Mods>()?.ItemRarity == ItemRarity.Rare ||
                           item?.GetComponent<Mods>()?.ItemRarity == ItemRarity.Unique
                        ? Actions.Vendor
                        : Actions.Keep;
                }

                #endregion

                List<IEvaluate> filters;
                if (item.HasComponent<Mods>())
                    filters = new List<IEvaluate>
                    {
                        new VendorForAltsFilter(_gameController, normalInventoryItem, _vendorTransmutes, _vendorScraps),
                        new SixSocketFilter(_gameController, normalInventoryItem),
                        new SixLinkFilter(_gameController, normalInventoryItem),
                        new InfluencedFilter(_gameController, normalInventoryItem),
                        new MapFilter(_gameController, normalInventoryItem),
                        new EnchantedHelmetFilter(_gameController, normalInventoryItem),
                        new RareRingFilter(_gameController, normalInventoryItem),
                        new RareAmuletFilter(_gameController, normalInventoryItem),
                        new RareBeltFilter(_gameController, normalInventoryItem),
                        new RareGlovesFilter(_gameController, normalInventoryItem),
                        new RareBootsFilter(_gameController, normalInventoryItem),
                        new RareJewel(_gameController, normalInventoryItem),
                        new RareAbyssJewel(_gameController, normalInventoryItem),
                        new RareOneHanded(_gameController, normalInventoryItem),
                        new UniqueItemFilter(_gameController, normalInventoryItem, _ninjaProvider)
                    };
                else
                    filters = new List<IEvaluate>
                    {
                        new VendorForAltsFilter(_gameController, normalInventoryItem, _vendorTransmutes, _vendorScraps),
                    };

                #region decide vendor/Keep

                var nVendor = 0;
                var nKeep = 0;
                foreach (var filter in filters)
                {
                    if (nKeep > 0) return Actions.Keep;
                    switch (filter.Evaluate())
                    {
                        case Actions.Vendor:
                            nVendor++;
                            break;
                        case Actions.Keep:
                            nKeep++;
                            break;
                        case Actions.CantDecide:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                return nVendor > 0 ? Actions.Vendor : Actions.Keep;

                #endregion
            }
            catch (Exception)
            {
                return Actions.Keep;
            }
        }
    }
}