using System;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;

namespace EZVendor.Item.Filters
{
    /// <summary>
    /// Run last
    /// </summary>
    internal class VendorForAlts2Filter : AbstractRareItem
    {
        public VendorForAlts2Filter(GameController gameController,
            NormalInventoryItem normalInventoryItem)
            : base(gameController, normalInventoryItem)
        {
        }

        public override Actions Evaluate()
        {
            try
            {
                if (!Item.HasComponent<Sockets>()) return Actions.CantDecide;
                if (Item.HasComponent<Map>()) return Actions.CantDecide;
                if (ItemRarity != ItemRarity.Magic) return Actions.CantDecide;
                switch (Item.GetComponent<Sockets>().NumberOfSockets)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                        return Actions.Vendor;
                    default:
                        return Actions.CantDecide;
                }
            }
            catch (Exception)
            {
                return Actions.CantDecide;
            }
        }
    }
}