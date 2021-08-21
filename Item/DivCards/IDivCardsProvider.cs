using System.Collections.Generic;

namespace EZVendor.Item.DivCards
{
    public interface IDivCardsProvider
    {
        List<string> GetSellDivCardsList();
    }
}