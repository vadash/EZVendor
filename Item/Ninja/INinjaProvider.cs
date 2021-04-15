using System.Collections.Generic;

namespace EZVendor.Item.Ninja
{
    internal interface INinjaProvider
    {
        HashSet<string> GetCheapUniques();
    }
}