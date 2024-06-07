namespace ShoppingListManager.Models;

public class Item
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public int Amount { get; set; }

    public bool IsImportant { get; set; }

    public int SortOrder { get; set; }

    public enum ShoppingListType
    {
        ToBuy,
        PrevBought
    }

    public ShoppingListType ListType { get; set; }

    public enum SortMode
    {
        Alphabetical,
        Custom
    }

    public SortMode CurrentSortMode { get; set; } = SortMode.Alphabetical;
}