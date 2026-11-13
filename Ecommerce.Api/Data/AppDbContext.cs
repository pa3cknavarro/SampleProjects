namespace Ecommerce.Api.Data;

using Microsoft.EntityFrameworkCore;
using Ecommerce.Api.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Province> Provinces { get; set; } = null!;
    public DbSet<City> Cities { get; set; } = null!;
    public DbSet<Barangay> Barangays { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<UnitOfMeasure> UnitOfMeasures { get; set; } = null!;
    public DbSet<UnitPrice> UnitPrices { get; set; } = null!;
    public DbSet<AreaAvailable> AreasAvailable { get; set; } = null!;
    public DbSet<CustomerClass> CustomerClasses { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderDetail> OrderDetails { get; set; } = null!;
    public DbSet<CartItem> CartItems { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<City>().HasKey(c => new { c.ProvinceId, c.CityId });
        modelBuilder.Entity<Barangay>().HasKey(b => new { b.ProvinceId, b.CityId, b.BarangayId });
        modelBuilder.Entity<UnitPrice>().HasKey(u => new { u.ProductId, u.UomId, u.UnitPriceId });

        modelBuilder.Entity<City>().HasOne(c => c.Province).WithMany(p => p.Cities)
            .HasForeignKey(c => c.ProvinceId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Barangay>().HasOne(b => b.City).WithMany(c => c.Barangays)
            .HasForeignKey(b => new { b.ProvinceId, b.CityId }).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Barangay>().HasOne(b => b.Province).WithMany()
            .HasForeignKey(b => b.ProvinceId).OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Product>().HasOne(p => p.Category).WithMany()
            .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<UnitPrice>().HasOne(u => u.Product).WithMany(p => p.UnitPrices)
            .HasForeignKey(u => u.ProductId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UnitPrice>().HasOne(u => u.UnitOfMeasure).WithMany()
            .HasForeignKey(u => u.UomId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AreaAvailable>().HasOne(a => a.Product).WithMany(p => p.AreasAvailable)
            .HasForeignKey(a => a.ProductId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AreaAvailable>().HasOne(a => a.Province).WithMany()
            .HasForeignKey(a => a.ProvinceId).OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Customer>().HasOne(c => c.CustomerClass).WithMany()
            .HasForeignKey(c => c.CustomerClassId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Customer>().HasOne(c => c.Province).WithMany()
            .HasForeignKey(c => c.ProvinceId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Customer>().HasOne(c => c.City).WithMany()
            .HasForeignKey(c => new { c.ProvinceId, c.CityId }).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Customer>().HasOne(c => c.Barangay).WithMany()
            .HasForeignKey(c => new { c.ProvinceId, c.CityId, c.BarangayId }).OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Order>().HasOne(o => o.Customer).WithMany()
            .HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<OrderDetail>().HasOne(d => d.Order).WithMany(o => o.OrderDetail)
            .HasForeignKey(d => d.OrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OrderDetail>().HasOne(d => d.Product).WithMany()
            .HasForeignKey(d => d.ProductId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<OrderDetail>().HasOne(d => d.UnitOfMeasure).WithMany()
            .HasForeignKey(d => d.UomId).OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CartItem>().HasOne(c => c.User).WithMany()
            .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CartItem>().HasOne(c => c.Product).WithMany()
            .HasForeignKey(c => c.ProductId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CartItem>().HasOne(c => c.Uom).WithMany()
            .HasForeignKey(c => c.UomId).OnDelete(DeleteBehavior.NoAction);
    }
}
