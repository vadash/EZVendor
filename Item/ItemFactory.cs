using System;
using System.Collections.Generic;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;
using EZVendor.Item.DivCards;
using EZVendor.Item.Filters;
using EZVendor.Item.Ninja;

// ReSharper disable ConstantConditionalAccessQualifier

namespace EZVendor.Item
{
    internal class ItemFactory : IItemFactory
    {
        private readonly GameController _gameController;
        private readonly INinjaProvider _ninjaProvider;
        private readonly IDivCardsProvider _divCardsProvider;
        private readonly bool _bypassBrokenItemMods;
        private readonly bool _vendorInfluenced;
        private readonly bool _saveVeiledHelmets;
        private readonly bool _settingsSaveEnchantedHelmets;

        public ItemFactory(GameController gameController,
            INinjaProvider ninjaProvider,
            IDivCardsProvider divCardsProvider,
            bool bypassBrokenItemMods,
            bool vendorInfluenced,
            bool saveVeiledHelmets,
            bool settingsSaveEnchantedHelmets
            )
        {
            _gameController = gameController;
            _ninjaProvider = ninjaProvider;
            _divCardsProvider = divCardsProvider;
            _bypassBrokenItemMods = bypassBrokenItemMods;
            _vendorInfluenced = vendorInfluenced;
            _saveVeiledHelmets = saveVeiledHelmets;
            _settingsSaveEnchantedHelmets = settingsSaveEnchantedHelmets;
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
                
                #endregion

                #region div cards

                if (item.Path.StartsWith(@"Metadata/Items/DivinationCards/DivinationCard"))
                    return new DivCardsFilter(_gameController, normalInventoryItem, _divCardsProvider).Evaluate();                

                #endregion

                #region vendor rares with broken ItemMods
              
                if (_bypassBrokenItemMods && item.GetComponent<Mods>()?.ItemMods == null)
                {
                    if (item?.GetComponent<Mods>()?.ItemRarity == ItemRarity.Rare) 
                        return Actions.Vendor;
                    if (item?.GetComponent<Mods>()?.ItemRarity == ItemRarity.Unique)
                        return Actions.Vendor;
                }

                #endregion

                List<IEvaluate> filters;
                if (item.HasComponent<Mods>())
                {
                    filters = new List<IEvaluate>();
                    if (!_vendorInfluenced) filters.Add(new InfluenceFilter(_gameController, normalInventoryItem));
                    filters.Add(new FlaskFilter(_gameController, normalInventoryItem));
                    filters.Add(new PathFilter(_gameController, normalInventoryItem));
                    filters.Add(new SixSocketFilter(_gameController, normalInventoryItem));
                    filters.Add(new SixLinkFilter(_gameController, normalInventoryItem));
                    filters.Add(new MapFilter(_gameController, normalInventoryItem));
                    if (_settingsSaveEnchantedHelmets) filters.Add(new EnchantedFilter(_gameController, normalInventoryItem));
                    if (_saveVeiledHelmets) filters.Add(new VeiledFilter(_gameController, normalInventoryItem));
                    filters.Add(new UniqueFilter(_gameController, normalInventoryItem, _ninjaProvider));
                    filters.Add(new ItemBaseFilter(_gameController, normalInventoryItem));
                }
                else
                    return Actions.Keep;

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