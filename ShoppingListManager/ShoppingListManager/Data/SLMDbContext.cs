using Microsoft.EntityFrameworkCore;
using ShoppingListManager.Models;

namespace ShoppingListManager.Data;

public class SLMDbContext : DbContext
{
    public SLMDbContext(DbContextOptions<SLMDbContext> options) : base(options) { }

    public DbSet<Item> ShoppingLists { get; set; }
}