using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Models;

namespace StockFlowPro.Data
{
    public class FoodieDbContext : IdentityDbContext
    {
        public FoodieDbContext(DbContextOptions<FoodieDbContext> options)
            : base(options)
        {
        }

        public DbSet<Facility> Facilities { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OrderProcessing> OrderProcessings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            var decimalProps = builder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?));

            foreach (var property in decimalProps)
            {
                property.SetColumnType("decimal(18,2)");
            }

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId);

            builder.Entity<Order>()
                .HasOne(o => o.OrderProcessing)
                .WithOne(op => op.Order)
                .HasForeignKey<OrderProcessing>(op => op.OrderId);

            // Employee to User (One-to-One with Unique Constraint)
            builder.Entity<Employee>()
                .HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<Employee>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Cascading delete

            builder.Entity<Employee>()
                .HasIndex(e => e.UserId)
                .IsUnique();
        }
    }
}
