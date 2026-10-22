using EcommerceBase.Web.Domain;
using EcommerceBase.Web.Domain.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EcommerceBase.Web.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<StoreSettings> StoreSettings => Set<StoreSettings>();
    public DbSet<IntegrationSetting> IntegrationSettings => Set<IntegrationSetting>();

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponUsage> CouponUsages => Set<CouponUsage>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    public DbSet<EmailOutbox> EmailOutbox => Set<EmailOutbox>();
    public DbSet<FiscalDocument> FiscalDocuments => Set<FiscalDocument>();
    public DbSet<FiscalEvent> FiscalEvents => Set<FiscalEvent>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        b.Properties<decimal>().HavePrecision(18, 2);
        b.Properties<OrderStatus>().HaveConversion<string>().HaveMaxLength(30);
        b.Properties<PaymentStatus>().HaveConversion<string>().HaveMaxLength(30);
        b.Properties<DiscountType>().HaveConversion<string>().HaveMaxLength(30);
        b.Properties<StockMovementType>().HaveConversion<string>().HaveMaxLength(30);
        b.Properties<EmailStatus>().HaveConversion<string>().HaveMaxLength(30);
        b.Properties<FiscalStatus>().HaveConversion<string>().HaveMaxLength(30);
    }

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);

        m.Entity<ApplicationUser>(e =>
        {
            e.HasIndex(u => u.IsActive);
        });

        m.Entity<Address>(e =>
        {
            e.HasOne(a => a.User).WithMany(u => u.Addresses).HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(a => a.UserId);
        });

        m.Entity<StoreSettings>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).ValueGeneratedNever();
            e.ToTable(t => t.HasCheckConstraint("CK_StoreSettings_Singleton", "\"Id\" = 1"));
        });

        m.Entity<IntegrationSetting>(e =>
        {
            e.HasIndex(s => s.Key).IsUnique();
        });

        m.Entity<Category>(e =>
        {
            e.HasIndex(c => c.Slug).IsUnique();
            e.HasOne(c => c.Parent).WithMany(c => c.Children).HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<Brand>(e =>
        {
            e.HasIndex(b => b.Slug).IsUnique();
        });

        m.Entity<Product>(e =>
        {
            e.HasIndex(p => p.Slug).IsUnique();
            e.HasIndex(p => p.Sku).IsUnique();
            e.HasIndex(p => new { p.IsActive, p.IsFeatured });
            e.HasIndex(p => p.CategoryId);
            e.Property(p => p.WeightKg).HasPrecision(10, 3);
            e.Property(p => p.HeightCm).HasPrecision(10, 2);
            e.Property(p => p.WidthCm).HasPrecision(10, 2);
            e.Property(p => p.LengthCm).HasPrecision(10, 2);
            e.HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Brand).WithMany().HasForeignKey(p => p.BrandId).OnDelete(DeleteBehavior.SetNull);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Products_Stock_NonNegative", "\"StockQuantity\" >= 0");
                t.HasCheckConstraint("CK_Products_Price_Positive", "\"Price\" > 0");
            });
        });

        m.Entity<ProductImage>(e =>
        {
            e.HasOne(i => i.Product).WithMany(p => p.Images).HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<StockMovement>(e =>
        {
            e.HasOne(s => s.Product).WithMany().HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(s => new { s.ProductId, s.CreatedAtUtc });
        });

        m.Entity<Cart>(e =>
        {
            e.HasIndex(c => c.UserId).IsUnique().HasFilter("\"UserId\" IS NOT NULL");
            e.HasIndex(c => c.AnonymousToken).IsUnique().HasFilter("\"AnonymousToken\" IS NOT NULL");
        });

        m.Entity<CartItem>(e =>
        {
            e.HasOne(i => i.Cart).WithMany(c => c.Items).HasForeignKey(i => i.CartId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(i => new { i.CartId, i.ProductId }).IsUnique();
        });

        m.Entity<Coupon>(e =>
        {
            e.HasIndex(c => c.Code).IsUnique();
            e.HasMany(c => c.Categories).WithMany().UsingEntity(j => j.ToTable("CouponCategories"));
            e.HasMany(c => c.Products).WithMany().UsingEntity(j => j.ToTable("CouponProducts"));
        });

        m.Entity<CouponUsage>(e =>
        {
            e.HasOne(u => u.Coupon).WithMany().HasForeignKey(u => u.CouponId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(u => new { u.CouponId, u.UserId });
            e.HasIndex(u => u.OrderId);
        });

        m.Entity<Order>(e =>
        {
            e.HasIndex(o => o.Number).IsUnique();
            e.HasIndex(o => o.IdempotencyKey).IsUnique();
            e.HasIndex(o => o.UserId);
            e.HasIndex(o => new { o.Status, o.CreatedAtUtc });
            e.Property(o => o.Version).IsRowVersion();
            e.HasOne(o => o.User).WithMany().HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<OrderItem>(e =>
        {
            e.HasOne(i => i.Order).WithMany(o => o.Items).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<OrderStatusHistory>(e =>
        {
            e.HasOne(h => h.Order).WithMany(o => o.History).HasForeignKey(h => h.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<Payment>(e =>
        {
            e.HasOne(p => p.Order).WithMany(o => o.Payments).HasForeignKey(p => p.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => p.ExternalPaymentId).IsUnique().HasFilter("\"ExternalPaymentId\" IS NOT NULL");
            e.Property(p => p.Version).IsRowVersion();
        });

        m.Entity<PaymentTransaction>(e =>
        {
            e.HasIndex(t => t.EventKey).IsUnique();
            e.HasIndex(t => t.OrderId);
        });

        m.Entity<EmailOutbox>(e =>
        {
            e.HasIndex(x => new { x.Status, x.NextAttemptUtc });
        });

        m.Entity<FiscalDocument>(e =>
        {
            e.HasOne(f => f.Order).WithMany().HasForeignKey(f => f.OrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(f => f.OrderId);
        });

        m.Entity<FiscalEvent>(e =>
        {
            e.HasOne(f => f.FiscalDocument).WithMany(d => d.Events).HasForeignKey(f => f.FiscalDocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<AuditLog>(e =>
        {
            e.HasIndex(a => a.CreatedAtUtc);
        });
    }
}
