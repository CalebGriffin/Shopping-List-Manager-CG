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

        list = list.OrderBy(i => !i.IsImportant).ToList();
        
        return Ok(list);
    }

    [HttpGet("PrevBought")]
    public async Task<IActionResult> GetPrevBoughtList()
    {
        var list = await _context.ShoppingLists
            .Where(item => item.ListType == Item.ShoppingListType.PrevBought).ToListAsync();
        
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
        item.Id = Guid.NewGuid();
        item.ListType = Item.ShoppingListType.ToBuy;

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
        
        item.IsImportant = !item.IsImportant;
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
        
        // Reset the values of the item and move it to the previously bought list
        item.Amount = 0;
        item.ListType = Item.ShoppingListType.PrevBought;
        item.SortOrder = -1;
        item.IsImportant = false;

        _context.ShoppingLists.Update(item);
        await _context.SaveChangesAsync();

        return Ok(item);
    }
}