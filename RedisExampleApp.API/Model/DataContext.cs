using Microsoft.EntityFrameworkCore;
namespace RedisExampleApp.API.Model;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options): base(options)
    {
        
    }
    
    public DbSet<Product> Products { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Kalem", Price = 100 },
            new Product { Id = 2, Name = "Silgi", Price = 200 },
            new Product { Id = 3, Name = "Defter", Price = 300 }
        );
    }
}