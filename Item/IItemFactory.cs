using ExileCore.PoEMemory.Elements.InventoryElements;
using EZVendor.Item.Filters;

namespace EZVendor.Item
{
    internal interface IItemFactory
    {
        Actions Evaluate(NormalInventoryItem normalInventoryItem);
    }
}