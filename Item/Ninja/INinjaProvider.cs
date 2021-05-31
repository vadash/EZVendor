using System.Collections.Generic;

namespace EZVendor.Item.Ninja
{
    internal interface INinjaProvider
    {
        IEnumerable<string> GetCheap0LUniques();
        IEnumerable<string> GetCheap6LUniques();
    }
}