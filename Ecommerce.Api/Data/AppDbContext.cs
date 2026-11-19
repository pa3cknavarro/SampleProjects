// Namespace for the EF Core context.
namespace Ecommerce.Api.Data;

// DbContext, DbSet, ModelBuilder, and DeleteBehavior.
using Microsoft.EntityFrameworkCore;

// Entity types exposed as DbSet properties.
using Ecommerce.Api.Models;

// The unit of work. AddDbContext registers this class, and each scope gets one instance.
public class AppDbContext : DbContext
{
    // Options carry the configured provider and connection. base(options) passes
    // them to DbContext, which uses them for every query on this instance.
    // The constructor body is empty.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // DbSet is the LINQ entry point for provinces. null! suppresses the warning that
    // EF assigns the set after construction, so it is not actually left null.
    public DbSet<Province> Provinces { get; set; } = null!;

    // Query and save cities through this set.
    public DbSet<City> Cities { get; set; } = null!;

    // Query and save barangays through this set.
    public DbSet<Barangay> Barangays { get; set; } = null!;

    // Query and save categories through this set.
    public DbSet<Category> Categories { get; set; } = null!;

    // Query and save products through this set.
    public DbSet<Product> Products { get; set; } = null!;

    // Query and save units of measure through this set.
    public DbSet<UnitOfMeasure> UnitOfMeasures { get; set; } = null!;

    // Query and save unit prices through this set.
    public DbSet<UnitPrice> UnitPrices { get; set; } = null!;

    // Query and save product-availability rows through this set.
    public DbSet<AreaAvailable> AreasAvailable { get; set; } = null!;

    // Query and save customer classes through this set.
    public DbSet<CustomerClass> CustomerClasses { get; set; } = null!;

    // Query and save customers through this set.
    public DbSet<Customer> Customers { get; set; } = null!;

    // Query and save order headers through this set.
    public DbSet<Order> Orders { get; set; } = null!;

    // Query and save order lines through this set.
    public DbSet<OrderDetail> OrderDetails { get; set; } = null!;

    // Query and save cart lines through this set.
    public DbSet<CartItem> CartItems { get; set; } = null!;

    // Query and save users through this set.
    public DbSet<User> Users { get; set; } = null!;

    public DbSet<Notification> Notifications { get; set; } = null!;

    // EF calls this while building the model. override replaces the base implementation,
    // and protected means only this class and subclasses can call it.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Runs EF conventions first so the fluent calls below only add or replace relationships.
        base.OnModelCreating(modelBuilder);

        // City's key is the anonymous object of ProvinceId and CityId, matching the composite attribute.
        modelBuilder.Entity<City>().HasKey(c => new { c.ProvinceId, c.CityId });

        // Barangay's key is the three location ids together.
        modelBuilder.Entity<Barangay>().HasKey(b => new { b.ProvinceId, b.CityId, b.BarangayId });

        // Unit price's key is product, unit, and price id together.
        modelBuilder.Entity<UnitPrice>().HasKey(u => new { u.ProductId, u.UomId, u.UnitPriceId });
        modelBuilder.Entity<UnitPrice>().HasIndex(u => new { u.ProductId, u.UomId }).IsUnique();

        // A city has one province, and a province has many cities through Province.Cities.
        modelBuilder.Entity<City>().HasOne(c => c.Province).WithMany(p => p.Cities)
            // The foreign key is City.ProvinceId, and deleting a province does not delete cities.
            .HasForeignKey(c => c.ProvinceId).OnDelete(DeleteBehavior.NoAction);

        // A barangay has one city, and a city has many barangays through City.Barangays.
        modelBuilder.Entity<Barangay>().HasOne(b => b.City).WithMany(c => c.Barangays)
            // The foreign key must include both parts of City's composite key.
            // Delete does not cascade.
            .HasForeignKey(b => new { b.ProvinceId, b.CityId }).OnDelete(DeleteBehavior.NoAction);

        // A barangay also points directly at a province. WithMany() with no collection
        // means Province has no navigation back to these barangays.
        modelBuilder.Entity<Barangay>().HasOne(b => b.Province).WithMany()
            // Only ProvinceId participates in this second relationship. Delete does not cascade.
            .HasForeignKey(b => b.ProvinceId).OnDelete(DeleteBehavior.NoAction);

        // A product has one category. Category has no collection navigation, so WithMany() is empty.
        modelBuilder.Entity<Product>().HasOne(p => p.Category).WithMany()
            // CategoryId is the foreign key. Deleting a category does not delete products.
            .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.NoAction);

        // A unit price belongs to one product, and a product exposes UnitPrices.
        modelBuilder.Entity<UnitPrice>().HasOne(u => u.Product).WithMany(p => p.UnitPrices)
            // ProductId is the foreign key. Cascade means deleting a product deletes its prices.
            // This overrides the NoAction attribute on the navigation.
            .HasForeignKey(u => u.ProductId).OnDelete(DeleteBehavior.Cascade);

        // A unit price also points at a unit of measure, which has no collection back.
        modelBuilder.Entity<UnitPrice>().HasOne(u => u.UnitOfMeasure).WithMany()
            // UomId is the foreign key. Deleting a unit does not delete prices.
            .HasForeignKey(u => u.UomId).OnDelete(DeleteBehavior.NoAction);

        // An availability row belongs to one product, and the product exposes AreasAvailable.
        modelBuilder.Entity<AreaAvailable>().HasOne(a => a.Product).WithMany(p => p.AreasAvailable)
            // Deleting a product cascade-deletes its availability rows.
            .HasForeignKey(a => a.ProductId).OnDelete(DeleteBehavior.Cascade);

        // Availability also points at a province with no collection on Province.
        modelBuilder.Entity<AreaAvailable>().HasOne(a => a.Province).WithMany()
            // ProvinceId is the foreign key. Deleting a province does not delete availability rows.
            .HasForeignKey(a => a.ProvinceId).OnDelete(DeleteBehavior.NoAction);

        // A customer has one class, and the class has no collection navigation.
        modelBuilder.Entity<Customer>().HasOne(c => c.CustomerClass).WithMany()
            // CustomerClassId is the foreign key. Delete does not cascade.
            .HasForeignKey(c => c.CustomerClassId).OnDelete(DeleteBehavior.NoAction);

        // A customer points at a province with no back-collection.
        modelBuilder.Entity<Customer>().HasOne(c => c.Province).WithMany()
            // ProvinceId is the foreign key. Delete does not cascade.
            .HasForeignKey(c => c.ProvinceId).OnDelete(DeleteBehavior.NoAction);

        // A customer points at a city. The foreign key includes the province because City's key is composite.
        modelBuilder.Entity<Customer>().HasOne(c => c.City).WithMany()
            // Both ids are required to find the city. Delete does not cascade.
            .HasForeignKey(c => new { c.ProvinceId, c.CityId }).OnDelete(DeleteBehavior.NoAction);

        // A customer points at a barangay. The foreign key includes all three parts of that key.
        modelBuilder.Entity<Customer>().HasOne(c => c.Barangay).WithMany()
            // Province, city, and barangay ids together identify the barangay. Delete does not cascade.
            .HasForeignKey(c => new { c.ProvinceId, c.CityId, c.BarangayId }).OnDelete(DeleteBehavior.NoAction);

        // An order has one customer, and Customer has no Orders collection.
        modelBuilder.Entity<Order>().HasOne(o => o.Customer).WithMany()
            // CustomerId is the foreign key. Deleting a customer does not delete orders.
            .HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Order>().HasOne(o => o.ShippingProvince).WithMany()
            .HasForeignKey(o => o.ShippingProvinceId).OnDelete(DeleteBehavior.NoAction);

        // An order line belongs to one order, and the order exposes OrderDetail.
        modelBuilder.Entity<OrderDetail>().HasOne(d => d.Order).WithMany(o => o.OrderDetail)
            // Deleting an order cascade-deletes its lines, overriding the NoAction attribute.
            .HasForeignKey(d => d.OrderId).OnDelete(DeleteBehavior.Cascade);

        // A line points at a product with no back-collection.
        modelBuilder.Entity<OrderDetail>().HasOne(d => d.Product).WithMany()
            // ProductId is the foreign key. Deleting a product does not delete lines.
            .HasForeignKey(d => d.ProductId).OnDelete(DeleteBehavior.NoAction);

        // A line points at a unit of measure with no back-collection.
        modelBuilder.Entity<OrderDetail>().HasOne(d => d.UnitOfMeasure).WithMany()
            // UomId is the foreign key. Deleting a unit does not delete lines.
            .HasForeignKey(d => d.UomId).OnDelete(DeleteBehavior.NoAction);

        // A cart line points at a user with no back-collection.
        modelBuilder.Entity<CartItem>().HasOne(c => c.User).WithMany()
            // UserId is the foreign key. Deleting a user does not delete cart lines.
            .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.NoAction);

        // A cart line points at a product with no back-collection.
        modelBuilder.Entity<CartItem>().HasOne(c => c.Product).WithMany()
            // ProductId is the foreign key. Deleting a product does not delete cart lines.
            .HasForeignKey(c => c.ProductId).OnDelete(DeleteBehavior.NoAction);

        // A cart line points at a unit of measure with no back-collection.
        modelBuilder.Entity<CartItem>().HasOne(c => c.Uom).WithMany()
            // UomId is the foreign key. Deleting a unit does not delete cart lines.
            .HasForeignKey(c => c.UomId).OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Notification>().HasOne(n => n.User).WithMany()
            .HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Notification>().HasIndex(n => new { n.UserId, n.CreatedOn });
        modelBuilder.Entity<AreaAvailable>().HasIndex(a => new { a.ProductId, a.ProvinceId }).IsUnique();
    }
    // Ends OnModelCreating. The model is now frozen for this context type.
}
// Ends the database context.
