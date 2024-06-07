using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingListManager.Data;
using ShoppingListManager.Models;

namespace ShoppingListManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShoppingListController : ControllerBase
{
    private readonly SLMDbContext _context;

    public ShoppingListController(SLMDbContext context)
    {
        _context = context;
    }

    [HttpGet("ToBuy")]
    public async Task<IActionResult> GetToBuyList()
    {
        var list = await _context.ShoppingLists
            .Where(item => item.ListType == Item.ShoppingListType.ToBuy).ToListAsync();
        
        // Sort the list based on the current sort mode
        Item.SortMode sortMode = list.FirstOrDefault()?.CurrentSortMode ?? Item.SortMode.Alphabetical;

        // The list should be sorted with important items first, then either alphabetically or by custom sort order
        if (sortMode == Item.SortMode.Alphabetical)
            list = list.OrderBy(i => !i.IsImportant).ThenBy(i => i.Name).ToList();
        else if (sortMode == Item.SortMode.Custom)
            list = list.OrderBy(i => !i.IsImportant).ThenBy(i => i.SortOrder).ToList();

        
        return Ok(list);
    }

    [HttpGet("PrevBought")]
    public async Task<IActionResult> GetPrevBoughtList()
    {
        var list = await _context.ShoppingLists
            .Where(item => item.ListType == Item.ShoppingListType.PrevBought).ToListAsync();
        
        // The previously bought list should always be sorted by alphabetical order
        list = list.OrderBy(i => i.Name).ToList();
        
        return Ok(list);
    }

    [HttpPost("AddItemToBuy")]
    public async Task<IActionResult> AddItemToBuy([FromBody] Item item)
    {
        // If the item already exists in the to buy list, increase the amount by the amount of the new item
        var existingItem = await _context.ShoppingLists
            .Where(i => i.Name == item.Name && i.ListType == Item.ShoppingListType.ToBuy)
            .FirstOrDefaultAsync();
        if (existingItem != null)
        {
            existingItem.Amount += item.Amount;
            _context.ShoppingLists.Update(existingItem);
            await _context.SaveChangesAsync();
            return Ok(existingItem);
        }

        // Else, add the new item to the to buy list
        // Get the current sort mode of the to buy list
        Item.SortMode sortMode = await _context.ShoppingLists
            .Where(i => i.ListType == Item.ShoppingListType.ToBuy)
            .Select(i => i.CurrentSortMode)
            .FirstOrDefaultAsync();
        
        // Set the values of the new item
        item.Id = Guid.NewGuid();
        // Set the sort order of the new item to be the number of items in the to buy list that are not important (this will add the item to the end of the list)
        item.SortOrder = await _context.ShoppingLists
            .Where(i => i.ListType == Item.ShoppingListType.ToBuy && !i.IsImportant).CountAsync();
        item.ListType = Item.ShoppingListType.ToBuy;
        item.CurrentSortMode = sortMode;

        await _context.ShoppingLists.AddAsync(item);
        await _context.SaveChangesAsync();

        return Ok(item);
    }

    [HttpDelete("DeleteItem/{id:Guid}")]
    public async Task<IActionResult> DeleteItem(Guid id)
    {
        var item = await _context.ShoppingLists.FindAsync(id);
        if (item == null)
            return NotFound();
        
        // When an item is deleted, move every item in the to buy list that is below the item up one position
        MoveItemsUp(item);
        
        _context.ShoppingLists.Remove(item);
        await _context.SaveChangesAsync();

        return Ok(item);
    }

    [HttpPost("ToggleItemImportance/{id:Guid}")]
    public async Task<IActionResult> ToggleItemImportance(Guid id)
    {
        var item = await _context.ShoppingLists.FindAsync(id);
        if (item == null)
            return NotFound();
        
        // When an item's importance is toggled, move every item in the to buy list that is below the item up one position
        MoveItemsUp(item);
        
        item.IsImportant = !item.IsImportant;

        // Set the new sort order of the item based on its importance
        // This will set the sort order so that the toggled item is placed at the end of the list
        item.SortOrder = item.IsImportant
            ? await _context.ShoppingLists
                .Where(i => i.ListType == Item.ShoppingListType.ToBuy && i.IsImportant).CountAsync()
            : await _context.ShoppingLists
                .Where(i => i.ListType == Item.ShoppingListType.ToBuy && !i.IsImportant).CountAsync();

        _context.ShoppingLists.Update(item);
        await _context.SaveChangesAsync();

        return Ok(item);
    }

    [HttpPost("MoveItemToPrevBought/{id:Guid}")]
    public async Task<IActionResult> MoveItemToPrevBought(Guid id)
    {
        var item = await _context.ShoppingLists.FindAsync(id);
        if (item == null)
            return NotFound();
        
        // When an item is moved to the previously bought list, move every item in the to buy list that is below the item up one position
        MoveItemsUp(item);
        
        // Reset the values of the item and move it to the previously bought list
        item.Amount = 0;
        item.ListType = Item.ShoppingListType.PrevBought;
        item.SortOrder = -1;
        item.IsImportant = false;

        _context.ShoppingLists.Update(item);
        await _context.SaveChangesAsync();

        return Ok(item);
    }

    [HttpPost("AddItemFromPrevBought/{id:Guid}")]
    public async Task<IActionResult> AddItemFromPrevBought(Guid id)
    {
        var item = await _context.ShoppingLists.FindAsync(id);
        if (item == null)
            return NotFound();
        
        // If the item exists in the to buy list, increase the amount by 1
        var existingItem = await _context.ShoppingLists
            .Where(i => i.ListType == Item.ShoppingListType.ToBuy && i.Name == item.Name)
            .FirstOrDefaultAsync();
        if (existingItem != null)
        {
            existingItem.Amount++;
            _context.ShoppingLists.Update(existingItem);
            await _context.SaveChangesAsync();
            return Ok(existingItem);
        }

        // Else, add the item to the to buy list with an amount of 1
        // Get the current sort mode of the to buy list
        Item.SortMode sortMode = await _context.ShoppingLists
            .Where(i => i.ListType == Item.ShoppingListType.ToBuy)
            .Select(i => i.CurrentSortMode)
            .FirstOrDefaultAsync();
        
        // Set the values of the new item
        var newItem = new Item
        {
            Id = Guid.NewGuid(),
            Name = item.Name,
            Amount = 1,
            IsImportant = false,
            SortOrder = await _context.ShoppingLists
                .Where(i => i.ListType == Item.ShoppingListType.ToBuy && !i.IsImportant).CountAsync(),
            ListType = Item.ShoppingListType.ToBuy,
            CurrentSortMode = sortMode
        };
        
        return await AddItemToBuy(newItem);
    }

    [HttpPost("ChangeSortMode/{sortMode:int}")]
    public async Task<IActionResult> ChangeSortMode(int sortMode)
    {
        var items = await _context.ShoppingLists
            .Where(i => i.ListType == Item.ShoppingListType.ToBuy)
            .ToListAsync();
        
        foreach (var item in items)
            item.CurrentSortMode = (Item.SortMode)sortMode;
        
        _context.ShoppingLists.UpdateRange(items);
        await _context.SaveChangesAsync();

        return Ok(items);
    }

    [HttpPost("MoveItem/{id:Guid}/{direction:int}")]
    public async Task<IActionResult> MoveItem(Guid id, int direction)
    {
        var item = await _context.ShoppingLists.FindAsync(id);
        if (item == null)
            return NotFound();
        
        // If the item is already at the top or bottom of the list, return a bad request
        if (item.SortOrder + direction < 0)
            return BadRequest();
        else if (item.SortOrder + direction >= await _context.ShoppingLists
            .Where(i => i.ListType == Item.ShoppingListType.ToBuy && i.IsImportant == item.IsImportant).CountAsync())
            return BadRequest();
        
        // Swap the sort order of the item with the item above or below it
        var otherItem = await _context.ShoppingLists
            .Where(i => i.ListType == Item.ShoppingListType.ToBuy && i.IsImportant == item.IsImportant && i.SortOrder == item.SortOrder + direction)
            .FirstOrDefaultAsync();
        if (otherItem == null)
            return NotFound();
        
        otherItem.SortOrder -= direction;
        item.SortOrder += direction;

        _context.ShoppingLists.UpdateRange(item, otherItem);
        await _context.SaveChangesAsync();

        return Ok(new { item, otherItem });
    }

    // Move every item in the to buy list that is below the provided item up one position (decrease the sort order by 1)
    // This will work for both important and non-important items
    private void MoveItemsUp(Item item)
    {
        var items = _context.ShoppingLists
            .Where(i => i.ListType == Item.ShoppingListType.ToBuy && i.IsImportant == item.IsImportant && i.SortOrder > item.SortOrder).ToList();
        
        foreach (var i in items)
            i.SortOrder--;
    }
}