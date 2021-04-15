namespace EZVendor.Item.Filters
{
    internal interface IEvaluate
    {
        Actions Evaluate();
    }

    internal enum Actions
    {
        Vendor,
        Keep,
        CantDecide
    }
}