using Microsoft.EntityFrameworkCore;
using test2.Models;

namespace test2.Data;

public class DatabaseContext : DbContext
{
    public DbSet<Client> Clients { get; set; } 
    public DbSet<Product> Products { get; set; } 
    public DbSet<Status> Statuses { get; set; } 
    public DbSet<Order> Orders { get; set; } 
    public DbSet<ProductOrder> ProdOrders { get; set; } 

    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>().HasData(new List<Client>
        {
            new Client { Id = 1, FirstName = "John", LastName = "Doe" },
            new Client { Id = 2, FirstName = "Jane", LastName = "Doe" },
            new Client { Id = 3, FirstName = "Julie", LastName = "Doe" }
        });
        
        modelBuilder.Entity<Status>().HasData(new List<Status>
        {
            new Status { Id = 1, Name = "Active" },
            new Status { Id = 2, Name = "Unactive" }
        });
        
        modelBuilder.Entity<Product>().HasData(new List<Product>
        {
            new Product { Id = 1, Name = "Phone", Price = 1000.99 },
            new Product { Id = 2, Name = "Laptop", Price = 3000.0 }
        });

        // ZMIANA: Używamy obiektów anonimowych (new { ... }), aby EF Core nie szukał pełnych obiektów 'Status' i 'Client'
        modelBuilder.Entity<Order>().HasData(new object[]
        {
            new { Id = 1, CreatedAt = new DateTime(2025, 1, 1), FulfilledAt = (DateTime?)null, ClientId = 1, StatusId = 1 },
            new { Id = 2, CreatedAt = new DateTime(2026, 9, 3), FulfilledAt = (DateTime?)new DateTime(2026, 9, 9), ClientId = 2, StatusId = 2 },
            new { Id = 3, CreatedAt = new DateTime(2026, 12, 10), FulfilledAt = (DateTime?)null, ClientId = 3, StatusId = 1 }
        });

        // ZMIANA: Tutaj tak samo obiekt anonimowy, na wypadek gdyby Product i Order też były wymagane w modelu
        modelBuilder.Entity<ProductOrder>().HasData(new object[]
        {
            new { ProductId = 1, OrderId = 1, Amount = 100 },
            new { ProductId = 2, OrderId = 2, Amount = 5 },
            new { ProductId = 2, OrderId = 3, Amount = 44 }
        });
    }
}