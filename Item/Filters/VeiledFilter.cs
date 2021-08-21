using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;

namespace EZVendor.Item.Filters
{
    internal class VeiledFilter : AbstractBasicItem
    {
        public VeiledFilter(
            GameController gameController,
            NormalInventoryItem normalInventoryItem) 
            : base(gameController,normalInventoryItem)
        {
        }

        public override Actions Evaluate()
        {
            if (BaseItemType.ClassName == "Helmet" && Veiled) return Actions.Keep; // save for unlock +1 zombie craft
            return Actions.CantDecide;
        }
    }
}