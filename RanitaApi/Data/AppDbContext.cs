using Microsoft.EntityFrameworkCore;
using RanitaApi.Models;

namespace RanitaApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<CategoryAttribute> CategoryAttributes { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ClientPushSubscription> ClientPushSubscriptions { get; set; }
        public DbSet<PushSubscriptionModel> PushSubscriptions { get; set; }

        public DbSet<Seller> Sellers { get; set; }
        public DbSet<SellerProduct> SellerProducts { get; set; }
        public DbSet<SellerPayout> SellerPayouts { get; set; }
        public DbSet<SellerPushSubscription> SellerPushSubscriptions { get; set; }
        public DbSet<CommissionSetting> CommissionSettings { get; set; }
        public DbSet<SiteSetting> SiteSettings { get; set; }
        public DbSet<SiteEvent> SiteEvents { get; set; }
        public DbSet<FlashSale> FlashSales { get; set; }
        public DbSet<FlashSaleRequest> FlashSaleRequests { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {



            modelBuilder.Entity<FlashSaleRequest>(entity =>
            {
                entity.Property(f => f.FlashPrice).HasPrecision(18, 2);
                entity.Property(f => f.OriginalPrice).HasPrecision(18, 2);

                entity.HasOne(f => f.Seller)
                      .WithMany()
                      .HasForeignKey(f => f.SellerId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Product)
                      .WithMany()
                      .HasForeignKey(f => f.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Variant)
                      .WithMany()
                      .HasForeignKey(f => f.VariantId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);
            });



            modelBuilder.Entity<FlashSale>(entity =>
            {
                entity.Property(f => f.FlashPrice).HasPrecision(18, 2);
                entity.Property(f => f.OriginalPrice).HasPrecision(18, 2);

                entity.HasOne(f => f.Product)
                      .WithMany()
                      .HasForeignKey(f => f.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Variant)
                      .WithMany()
                      .HasForeignKey(f => f.VariantId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);
            });


            modelBuilder.Entity<CommissionSetting>()
    .Property(c => c.Rate)
    .HasPrecision(5, 4);


            modelBuilder.Entity<Seller>(entity =>
            {
                entity.Property(s => s.CommissionRate).HasPrecision(5, 4);

                entity.HasOne(s => s.Client)
                      .WithMany()
                      .HasForeignKey(s => s.ClientId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SellerProduct>(entity =>
            {
                entity.Property(sp => sp.Price).HasPrecision(18, 2);
                entity.Property(sp => sp.OldPrice).HasPrecision(18, 2);

                entity.HasOne(sp => sp.Seller)
                      .WithMany(s => s.SellerProducts)
                      .HasForeignKey(sp => sp.SellerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SellerPayout>(entity =>
            {
                entity.Property(p => p.GrossAmount).HasPrecision(18, 2);
                entity.Property(p => p.CommissionAmount).HasPrecision(18, 2);
                entity.Property(p => p.NetAmount).HasPrecision(18, 2);

                entity.HasOne(p => p.Seller)
                      .WithMany(s => s.Payouts)
                      .HasForeignKey(p => p.SellerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });


            modelBuilder.Entity<Review>()
    .Property(r => r.Note)
    .HasMaxLength(5);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany()
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Client)
                .WithMany()
                .HasForeignKey(r => r.ClientId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
    .Property(o => o.Total)
    .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(i => i.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}