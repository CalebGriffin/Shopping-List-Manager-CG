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
}